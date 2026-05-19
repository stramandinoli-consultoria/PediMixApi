using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PediMix.API.Models;
using PediMix.Application.DTOs;
using PediMix.Application.Interfaces;
using PediMix.Domain.Entities;
using PediMix.Domain.Enums;

namespace PediMix.API.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task LogoutAsync(string refreshToken);
    Task LogoutAllAsync(Guid userId);
    Task<UserDto?> GetMeAsync(Guid userId, CancellationToken cancellationToken = default);
}

public class AuthService : IAuthService
{
    private static readonly ConcurrentDictionary<string, RefreshSession> Sessions = new();

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly JwtOptions _jwtOptions;

    public AuthService(IUnitOfWork unitOfWork, IMapper mapper, IOptions<JwtOptions> jwtOptions)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Password != request.ConfirmPassword)
        {
            throw new InvalidOperationException("Password and confirm password must match.");
        }

        if (!request.AcceptTerms || !request.AcceptPrivacy)
        {
            throw new InvalidOperationException("Terms and privacy policy must be accepted.");
        }

        if (await _unitOfWork.Users.EmailExistsAsync(request.Email))
        {
            throw new InvalidOperationException("Email already in use.");
        }

        var firstName = request.FirstName?.Trim() ?? string.Empty;
        var lastName = request.LastName?.Trim() ?? string.Empty;
        var normalizedName = string.Join(' ', new[] { firstName, lastName }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();
        if (string.IsNullOrWhiteSpace(normalizedName) && !string.IsNullOrWhiteSpace(request.Nome))
        {
            normalizedName = request.Nome.Trim();
            var parts = normalizedName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            firstName = parts.FirstOrDefault() ?? string.Empty;
            lastName = string.Join(' ', parts.Skip(1));
        }

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            var emailPrefix = request.Email.Split('@', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "user";
            normalizedName = emailPrefix;
            firstName = emailPrefix;
            lastName = "-";
        }

        var usernameBase = (request.Username ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(usernameBase))
        {
            usernameBase = request.Email.Trim().ToLowerInvariant();
        }

        if (string.IsNullOrWhiteSpace(usernameBase))
        {
            usernameBase = "user";
        }

        var username = usernameBase;
        var suffix = 1;
        while (await _unitOfWork.Users.UsernameExistsAsync(username))
        {
            username = $"{usernameBase}{suffix++}";
        }

        var roleEnum = ParseUserRole(request.Role);
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email.Trim(),
            Username = username,
            FirstName = firstName,
            LastName = string.IsNullOrWhiteSpace(lastName) ? "-" : lastName,
            PasswordHash = HashPassword(request.Password),
            Role = roleEnum,
            IsActive = true,
            IsEmailVerified = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return GenerateAuthResponse(user, DateTime.UtcNow);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email.Trim());
        if (user == null || !user.IsActive)
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        if (user.PasswordHash != HashPassword(request.Password))
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return GenerateAuthResponse(user, DateTime.UtcNow);
    }

    public async Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var refreshHash = HashToken(refreshToken);
        if (!Sessions.TryGetValue(refreshHash, out var session))
        {
            throw new UnauthorizedAccessException("Refresh token invalid.");
        }

        if (session.RevokedAtUtc.HasValue || DateTime.UtcNow > session.ExpiresAtUtc)
        {
            Sessions.TryRemove(refreshHash, out _);
            throw new UnauthorizedAccessException("Refresh token expired or revoked.");
        }

        if (DateTime.UtcNow > session.SessionExpiresAtUtc)
        {
            Sessions.TryRemove(refreshHash, out _);
            throw new UnauthorizedAccessException("Session max duration reached. Login again.");
        }

        var user = await _unitOfWork.Users.GetByIdAsync(session.UserId);
        if (user == null || !user.IsActive)
        {
            throw new UnauthorizedAccessException("User not found or inactive.");
        }

        session.RevokedAtUtc = DateTime.UtcNow;
        Sessions[refreshHash] = session;

        return GenerateAuthResponse(user, session.SessionStartedAtUtc, session.SessionExpiresAtUtc);
    }

    public Task LogoutAsync(string refreshToken)
    {
        var refreshHash = HashToken(refreshToken);
        if (Sessions.TryGetValue(refreshHash, out var session))
        {
            session.RevokedAtUtc = DateTime.UtcNow;
            Sessions[refreshHash] = session;
        }

        return Task.CompletedTask;
    }

    public Task LogoutAllAsync(Guid userId)
    {
        var keys = Sessions
            .Where(kvp => kvp.Value.UserId == userId)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keys)
        {
            if (Sessions.TryGetValue(key, out var session))
            {
                session.RevokedAtUtc = DateTime.UtcNow;
                Sessions[key] = session;
            }
        }

        return Task.CompletedTask;
    }

    public async Task<UserDto?> GetMeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetWithProfilesAsync(userId);
        return user == null ? null : _mapper.Map<UserDto>(user);
    }

    private AuthResponse GenerateAuthResponse(User user, DateTime sessionStartedAtUtc, DateTime? fixedSessionExpiresAtUtc = null)
    {
        var now = DateTime.UtcNow;
        var accessExpires = now.AddMinutes(_jwtOptions.AccessTokenMinutes);
        var sessionExpires = fixedSessionExpiresAtUtc ?? sessionStartedAtUtc.AddHours(_jwtOptions.SessionHours);
        var refreshExpires = now.AddMinutes(Math.Max(1, (int)(sessionExpires - now).TotalMinutes));

        var sessionId = Guid.NewGuid().ToString("N");
        var accessToken = GenerateAccessToken(user, accessExpires, sessionId);
        var refreshToken = GenerateRefreshToken();

        var refreshHash = HashToken(refreshToken);
        Sessions[refreshHash] = new RefreshSession
        {
            UserId = user.Id,
            SessionId = sessionId,
            SessionStartedAtUtc = sessionStartedAtUtc,
            SessionExpiresAtUtc = sessionExpires,
            ExpiresAtUtc = refreshExpires
        };

        return new AuthResponse
        {
            User = _mapper.Map<UserDto>(user),
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = (int)(accessExpires - now).TotalSeconds,
            RefreshExpiresIn = (int)(refreshExpires - now).TotalSeconds
        };
    }

    private string GenerateAccessToken(User user, DateTime expiresAtUtc, string sessionId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("sessionId", sessionId)
        };

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    private static string HashPassword(string rawPassword)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawPassword));
        return Convert.ToHexString(bytes);
    }

    private static UserRole ParseUserRole(string? roleStr)
    {
        if (string.IsNullOrWhiteSpace(roleStr))
            return UserRole.Audience;

        return roleStr.ToUpperInvariant() switch
        {
            "AUDIENCE" => UserRole.Audience,
            "SINGER" => UserRole.Singer,
            "VENUE" => UserRole.Venue,
            "ADMIN" => UserRole.Admin,
            _ => UserRole.Audience
        };
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    private sealed class RefreshSession
    {
        public Guid UserId { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public DateTime SessionStartedAtUtc { get; set; }
        public DateTime SessionExpiresAtUtc { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
        public DateTime? RevokedAtUtc { get; set; }
    }
}
