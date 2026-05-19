using PediMix.Application.DTOs;
using PediMix.Domain.Enums;

namespace PediMix.API.Models;

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = "AUDIENCE"; // "AUDIENCE", "SINGER", "VENUE"
    public bool AcceptTerms { get; set; } = false;
    public bool AcceptPrivacy { get; set; } = false;
    
    // Para compatibilidade com frontend legado
    public string? Nome 
    { 
        get => string.IsNullOrEmpty(FirstName) ? null : $"{FirstName} {LastName}".Trim();
        set
        {
            if (!string.IsNullOrEmpty(value))
            {
                var parts = value.Split(' ', 2);
                FirstName = parts[0];
                LastName = parts.Length > 1 ? parts[1] : string.Empty;
            }
        }
    }
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class LogoutRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class AuthResponse
{
    public UserDto User { get; set; } = new();
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public int RefreshExpiresIn { get; set; }
}

public class ForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class VerifyEmailRequest
{
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}

public class ResendVerificationRequest
{
    public string Email { get; set; } = string.Empty;
}

public class JwtOptions
{
    public string Issuer { get; set; } = "PediMix";
    public string Audience { get; set; } = "PediMix.Client";
    public string Key { get; set; } = "CHANGE_ME_WITH_A_SECURE_KEY_AT_LEAST_32_CHARS";
    public int AccessTokenMinutes { get; set; } = 15;
    public int SessionHours { get; set; } = 2;
}
