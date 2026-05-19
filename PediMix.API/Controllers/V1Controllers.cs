using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PediMix.API.Models;
using PediMix.API.Services;
using PediMix.Application.Commands;
using PediMix.Application.DTOs;
using PediMix.Application.Interfaces;
using PediMix.Application.Queries;
using PediMix.Domain.Entities;
using PediMix.Domain.Enums;

namespace PediMix.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthV1Controller : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthV1Controller(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.RegisterAsync(request, cancellationToken);
            return Ok(ApiResponse<AuthResponse>.Ok(result, "User registered."));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<AuthResponse>.Fail("Register failed.", ex.Message));
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.LoginAsync(request, cancellationToken);
            return Ok(ApiResponse<AuthResponse>.Ok(result, "Authenticated."));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<AuthResponse>.Fail("Authentication failed.", ex.Message));
        }
    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.RefreshAsync(request.RefreshToken, cancellationToken);
            return Ok(ApiResponse<AuthResponse>.Ok(result, "Token refreshed."));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<AuthResponse>.Fail("Refresh failed.", ex.Message));
        }
    }

    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<object>>> Logout([FromBody] LogoutRequest request)
    {
        await _authService.LogoutAsync(request.RefreshToken);
        return Ok(ApiResponse<object>.Ok(new { }, "Session revoked."));
    }

    [Authorize]
    [HttpPost("logout-all")]
    public async Task<ActionResult<ApiResponse<object>>> LogoutAll()
    {
        var userId = User.GetRequiredUserId();
        await _authService.LogoutAllAsync(userId);
        return Ok(ApiResponse<object>.Ok(new { }, "All sessions revoked."));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Me(CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        var user = await _authService.GetMeAsync(userId, cancellationToken);

        if (user == null)
        {
            return NotFound(ApiResponse<UserDto>.Fail("User not found."));
        }

        return Ok(ApiResponse<UserDto>.Ok(user));
    }

    [HttpPost("forgot-password")]
    public ActionResult<ApiResponse<object>> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        return Ok(ApiResponse<object>.Ok(new { request.Email }, "If the email exists, reset instructions were sent."));
    }

    [HttpPost("reset-password")]
    public ActionResult<ApiResponse<object>> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        return Ok(ApiResponse<object>.Ok(new { request.Email }, "Password reset processed."));
    }

    [HttpPost("verify-email")]
    public ActionResult<ApiResponse<object>> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        return Ok(ApiResponse<object>.Ok(new { request.Email }, "Email verification processed."));
    }

    [HttpPost("resend-verification")]
    public ActionResult<ApiResponse<object>> ResendVerification([FromBody] ResendVerificationRequest request)
    {
        return Ok(ApiResponse<object>.Ok(new { request.Email }, "Verification email resent."));
    }
}

[ApiController]
[Route("api/v1/users")]
public class UsersV1Controller : ControllerBase
{
    private static readonly ConcurrentDictionary<Guid, List<UserFavorite>> Favorites = new();

    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;

    public UsersV1Controller(IMediator mediator, IUnitOfWork unitOfWork)
    {
        _mediator = mediator;
        _unitOfWork = unitOfWork;
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetMe()
    {
        var userId = User.GetRequiredUserId();
        var result = await _mediator.Send(new GetUserByIdQuery(userId));

        if (result == null)
        {
            return NotFound(ApiResponse<UserDto>.Fail("User not found."));
        }

        return Ok(ApiResponse<UserDto>.Ok(result));
    }

    [Authorize]
    [HttpPut("me")]
    [HttpPatch("me")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateMe([FromBody] UpdateUserCommand command)
    {
        var userId = User.GetRequiredUserId();
        var patchedCommand = command with { Id = userId };
        var result = await _mediator.Send(patchedCommand);
        return Ok(ApiResponse<UserDto>.Ok(result, "User updated."));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetUserByIdQuery(id));
        if (result == null)
        {
            return NotFound(ApiResponse<UserDto>.Fail("User not found."));
        }

        return Ok(ApiResponse<UserDto>.Ok(result));
    }

    [HttpGet("{id:guid}/stats")]
    public async Task<ActionResult<ApiResponse<object>>> GetStats(Guid id)
    {
        var exists = await _unitOfWork.Users.ExistsAsync(id);
        if (!exists)
        {
            return NotFound(ApiResponse<object>.Fail("User not found."));
        }

        var data = new
        {
            events = 0,
            followers = 0,
            reviews = 0,
            completion = 0
        };

        return Ok(ApiResponse<object>.Ok(data));
    }

    [Authorize]
    [HttpPut("me/complete-profile")]
    public async Task<ActionResult<ApiResponse<UserDto>>> CompleteProfile([FromBody] CompleteProfileRequest request)
    {
        var userId = User.GetRequiredUserId();
        var user = await _unitOfWork.Users.GetWithProfilesAsync(userId);
        if (user == null)
        {
            return NotFound(ApiResponse<UserDto>.Fail("User not found."));
        }

        // Parse gender string to enum
        if (!string.IsNullOrWhiteSpace(request.Gender))
        {
            user.Gender = EnumParsers.ParseGender(request.Gender);
        }

        user.Cpf = request.Cpf ?? user.Cpf;
        user.DateOfBirth = request.BirthDate ?? user.DateOfBirth;
        user.UpdatedAt = DateTime.UtcNow;

        // Update contact fields
        if (request.Contact != null)
        {
            user.PhoneNumber = request.Contact.Phone ?? user.PhoneNumber;
            user.Whatsapp = request.Contact.Whatsapp ?? user.Whatsapp;
            user.EmergencyContact = request.Contact.EmergencyContact ?? user.EmergencyContact;
            user.EmergencyPhone = request.Contact.EmergencyPhone ?? user.EmergencyPhone;
        }

        // Update or create address
        if (request.Address != null)
        {
            var addressExists = user.Address != null;

            if (!addressExists)
            {
                var newAddress = new Address
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ZipCode = request.Address.Cep ?? string.Empty,
                    Street = request.Address.Street ?? string.Empty,
                    Number = request.Address.Number ?? string.Empty,
                    Complement = request.Address.Complement,
                    Neighborhood = request.Address.Neighborhood ?? string.Empty,
                    City = request.Address.City ?? string.Empty,
                    State = request.Address.State ?? string.Empty,
                    Country = "Brasil",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Addresses.AddAsync(newAddress);
                user.Address = newAddress;
            }
            else
            {
                user.Address!.ZipCode = request.Address.Cep ?? user.Address.ZipCode;
                user.Address.Street = request.Address.Street ?? user.Address.Street;
                user.Address.Number = request.Address.Number ?? user.Address.Number;
                user.Address.Complement = request.Address.Complement ?? user.Address.Complement;
                user.Address.Neighborhood = request.Address.Neighborhood ?? user.Address.Neighborhood;
                user.Address.City = request.Address.City ?? user.Address.City;
                user.Address.State = request.Address.State ?? user.Address.State;
                user.Address.UpdatedAt = DateTime.UtcNow;
            }
        }

        // User já rastreado pelo contexto — não chama Update() para evitar marcar grafo todo como Modified
        await _unitOfWork.SaveChangesAsync();

        var dto = await _mediator.Send(new GetUserByIdQuery(userId));
        return Ok(ApiResponse<UserDto>.Ok(dto!, "Profile completed."));
    }

    [Authorize]
    [HttpGet("me/profile-completion")]
    public async Task<ActionResult<ApiResponse<object>>> GetProfileCompletion()
    {
        var userId = User.GetRequiredUserId();
        var user = await _unitOfWork.Users.GetWithProfilesAsync(userId);
        if (user == null)
        {
            return NotFound(ApiResponse<object>.Fail("User not found."));
        }

        var fields = 6;
        var filled = 0;
        if (!string.IsNullOrWhiteSpace(user.FirstName)) filled++;
        if (!string.IsNullOrWhiteSpace(user.LastName)) filled++;
        if (!string.IsNullOrWhiteSpace(user.Email)) filled++;
        if (!string.IsNullOrWhiteSpace(user.PhoneNumber)) filled++;
        if (user.DateOfBirth.HasValue) filled++;
        if (user.Address != null) filled++;

        var percentage = (int)Math.Round((double)filled / fields * 100.0);

        return Ok(ApiResponse<object>.Ok(new { percentage, filled, fields }));
    }

    [Authorize]
    [HttpPost("me/avatar")]
    public async Task<ActionResult<ApiResponse<object>>> UploadAvatar([FromBody] AvatarRequest request)
    {
        var userId = User.GetRequiredUserId();
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            return NotFound(ApiResponse<object>.Fail("User not found."));
        }

        user.Avatar = request.AvatarUrl;
        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new { avatar = user.Avatar }, "Avatar updated."));
    }

    [Authorize]
    [HttpDelete("me/avatar")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteAvatar()
    {
        var userId = User.GetRequiredUserId();
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            return NotFound(ApiResponse<object>.Fail("User not found."));
        }

        user.Avatar = null;
        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new { }, "Avatar removed."));
    }

    [Authorize]
    [HttpPut("me/address")]
    public ActionResult<ApiResponse<object>> UpdateAddress([FromBody] AddressDto request)
    {
        return Ok(ApiResponse<object>.Ok(request, "Address updated."));
    }

    [Authorize]
    [HttpGet("me/location")]
    public async Task<ActionResult<ApiResponse<object>>> GetLocation()
    {
        var userId = User.GetRequiredUserId();
        var user = await _unitOfWork.Users.GetWithProfilesAsync(userId);
        var location = new
        {
            latitude = user?.Address?.Latitude,
            longitude = user?.Address?.Longitude,
            city = user?.Address?.City,
            state = user?.Address?.State
        };

        return Ok(ApiResponse<object>.Ok(location));
    }

    [Authorize]
    [HttpGet("me/preferences")]
    public async Task<ActionResult<ApiResponse<UserPreferencesDto>>> GetPreferences()
    {
        var userId = User.GetRequiredUserId();
        var user = await _unitOfWork.Users.GetWithProfilesAsync(userId);
        if (user?.Preferences == null)
        {
            return Ok(ApiResponse<UserPreferencesDto>.Ok(new UserPreferencesDto()));
        }

        var dto = new UserPreferencesDto
        {
            Theme = user.Preferences.Theme,
            Language = user.Preferences.Language,
            EmailNotifications = user.Preferences.EmailNotifications,
            PushNotifications = user.Preferences.PushNotifications,
            SmsNotifications = user.Preferences.SmsNotifications,
            EventReminders = user.Preferences.EventReminders,
            NewFollowers = user.Preferences.NewFollowers,
            EventUpdates = user.Preferences.EventUpdates,
            ProfileVisible = user.Preferences.ProfileVisible,
            ShowLocation = user.Preferences.ShowLocation,
            AllowDirectMessages = user.Preferences.AllowDirectMessages
        };

        return Ok(ApiResponse<UserPreferencesDto>.Ok(dto));
    }

    [Authorize]
    [HttpPut("me/preferences")]
    public ActionResult<ApiResponse<UserPreferencesDto>> PutPreferences([FromBody] UserPreferencesDto request)
    {
        return Ok(ApiResponse<UserPreferencesDto>.Ok(request, "Preferences updated."));
    }

    [Authorize]
    [HttpGet("me/favorites")]
    public ActionResult<ApiResponse<IEnumerable<UserFavorite>>> GetFavorites([FromQuery] string? type)
    {
        var userId = User.GetRequiredUserId();
        Favorites.TryGetValue(userId, out var list);
        list ??= new List<UserFavorite>();

        var filtered = string.IsNullOrWhiteSpace(type)
            ? list
            : list.Where(x => x.Type.Equals(type, StringComparison.OrdinalIgnoreCase)).ToList();

        return Ok(ApiResponse<IEnumerable<UserFavorite>>.Ok(filtered));
    }

    [Authorize]
    [HttpPost("me/favorites")]
    public ActionResult<ApiResponse<UserFavorite>> AddFavorite([FromBody] AddFavoriteRequest request)
    {
        var userId = User.GetRequiredUserId();
        var list = Favorites.GetOrAdd(userId, _ => new List<UserFavorite>());

        var item = new UserFavorite
        {
            Id = Guid.NewGuid(),
            RefId = request.RefId,
            Type = request.Type,
            CreatedAt = DateTime.UtcNow
        };

        list.Add(item);
        return Ok(ApiResponse<UserFavorite>.Ok(item, "Favorite added."));
    }

    [Authorize]
    [HttpDelete("me/favorites/{favoriteId:guid}")]
    public ActionResult<ApiResponse<object>> RemoveFavorite(Guid favoriteId)
    {
        var userId = User.GetRequiredUserId();
        if (!Favorites.TryGetValue(userId, out var list))
        {
            return NotFound(ApiResponse<object>.Fail("Favorite not found."));
        }

        var removed = list.RemoveAll(x => x.Id == favoriteId) > 0;
        if (!removed)
        {
            return NotFound(ApiResponse<object>.Fail("Favorite not found."));
        }

        return Ok(ApiResponse<object>.Ok(new { }, "Favorite removed."));
    }
}

[ApiController]
[Route("api/v1/addresses")]
public class AddressesV1Controller : ControllerBase
{
    private readonly IViaCepService _viaCepService;

    public AddressesV1Controller(IViaCepService viaCepService)
    {
        _viaCepService = viaCepService;
    }

    [HttpGet("cep/{cep}")]
    public async Task<ActionResult<ApiResponse<ViaCepResponse?>>> GetByCep(string cep, CancellationToken cancellationToken)
    {
        var cleaned = new string(cep.Where(char.IsDigit).ToArray());
        if (cleaned.Length != 8)
        {
            return BadRequest(ApiResponse<ViaCepResponse?>.Fail("Invalid CEP. Must contain 8 digits."));
        }

        var result = await _viaCepService.GetAddressByCepAsync(cleaned, cancellationToken);

        if (result == null)
        {
            return NotFound(ApiResponse<ViaCepResponse?>.Fail("CEP not found."));
        }

        return Ok(ApiResponse<ViaCepResponse?>.Ok(result, "Address found."));
    }
}

[ApiController]
[Route("api/v1/artists")]
public class ArtistsV1Controller : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public ArtistsV1Controller(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<ArtistProfile>>>> GetAll([FromQuery] string? query)
    {
        var data = string.IsNullOrWhiteSpace(query)
            ? await _unitOfWork.ArtistProfiles.GetAllAsync()
            : await _unitOfWork.ArtistProfiles.SearchAsync(query);

        return Ok(ApiResponse<IEnumerable<ArtistProfile>>.Ok(data));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ArtistProfile>>> GetById(Guid id)
    {
        var item = await _unitOfWork.ArtistProfiles.GetByIdAsync(id);
        if (item == null)
        {
            return NotFound(ApiResponse<ArtistProfile>.Fail("Artist not found."));
        }

        return Ok(ApiResponse<ArtistProfile>.Ok(item));
    }

    [HttpGet("{id:guid}/events")]
    public async Task<ActionResult<ApiResponse<IEnumerable<Event>>>> GetEvents(Guid id)
    {
        var items = await _unitOfWork.Events.GetByArtistAsync(id);
        return Ok(ApiResponse<IEnumerable<Event>>.Ok(items));
    }

    [HttpGet("{id:guid}/repertoire")]
    public async Task<ActionResult<ApiResponse<IEnumerable<Repertoire>>>> GetRepertoire(Guid id)
    {
        var items = await _unitOfWork.Repertoires.GetByArtistAsync(id);
        return Ok(ApiResponse<IEnumerable<Repertoire>>.Ok(items));
    }

    [HttpGet("{id:guid}/ratings")]
    public async Task<ActionResult<ApiResponse<object>>> GetRatings(Guid id)
    {
        var item = await _unitOfWork.ArtistProfiles.GetByIdAsync(id);
        if (item == null)
        {
            return NotFound(ApiResponse<object>.Fail("Artist not found."));
        }

        return Ok(ApiResponse<object>.Ok(new { rating = item.Rating, followers = item.Followers }));
    }

    [Authorize]
    [HttpPost("{id:guid}/follow")]
    public ActionResult<ApiResponse<object>> Follow(Guid id)
    {
        return Ok(ApiResponse<object>.Ok(new { artistId = id }, "Follow registered."));
    }

    [Authorize]
    [HttpDelete("{id:guid}/follow")]
    public ActionResult<ApiResponse<object>> Unfollow(Guid id)
    {
        return Ok(ApiResponse<object>.Ok(new { artistId = id }, "Follow removed."));
    }

    [Authorize]
    [HttpGet("me/dashboard")]
    public ActionResult<ApiResponse<object>> MyDashboard()
    {
        return Ok(ApiResponse<object>.Ok(new { todayRequests = 0, upcomingEvents = 0, followers = 0 }));
    }

    [Authorize]
    [HttpPut("me/profile")]
    public ActionResult<ApiResponse<object>> UpdateMyProfile([FromBody] ArtistBasicProfileRequest payload)
    {
        var userId = User.GetRequiredUserId();
        var artist = _unitOfWork.ArtistProfiles.GetByUserIdAsync(userId).GetAwaiter().GetResult();
        if (artist == null)
        {
            return NotFound(ApiResponse<object>.Fail("Artist profile not found."));
        }

        artist.StageName = payload.StageName ?? artist.StageName;
        artist.Description = payload.Description ?? artist.Description;
        artist.InstagramUrl = payload.InstagramUrl ?? artist.InstagramUrl;
        artist.YoutubeUrl = payload.YoutubeUrl ?? artist.YoutubeUrl;
        artist.SpotifyUrl = payload.SpotifyUrl ?? artist.SpotifyUrl;
        artist.SoundcloudUrl = payload.SoundcloudUrl ?? artist.SoundcloudUrl;
        artist.TiktokUrl = payload.TiktokUrl ?? artist.TiktokUrl;

        _unitOfWork.ArtistProfiles.UpdateAsync(artist).GetAwaiter().GetResult();
        _unitOfWork.SaveChangesAsync().GetAwaiter().GetResult();

        return Ok(ApiResponse<object>.Ok(payload, "Artist profile updated."));
    }

    [Authorize]
    [HttpGet("me/complete-profile")]
    public ActionResult<ApiResponse<object>> GetMyCompleteProfile()
    {
        var userId = User.GetRequiredUserId();
        var artist = _unitOfWork.ArtistProfiles.GetByUserIdAsync(userId).GetAwaiter().GetResult();
        if (artist == null)
        {
            return NotFound(ApiResponse<object>.Fail("Artist profile not found."));
        }

        var portfolioLinks = string.IsNullOrWhiteSpace(artist.PortfolioLinksJson)
            ? new List<string>()
            : JsonSerializer.Deserialize<List<string>>(artist.PortfolioLinksJson) ?? new List<string>();

        var instruments = string.IsNullOrWhiteSpace(artist.InstrumentsJson)
            ? new List<string>()
            : JsonSerializer.Deserialize<List<string>>(artist.InstrumentsJson) ?? new List<string>();

        var data = new ArtistCompleteProfileRequest
        {
            BasicInfo = new ArtistBasicInfoSection
            {
                StageName = artist.StageName,
                FullName = artist.FullName,
                BirthDate = artist.BirthDate,
                Phone = artist.Phone,
                Email = artist.Email,
                Genre = artist.Genre,
                City = artist.City,
                State = artist.State,
                Bio = artist.Description
            },
            Documents = new ArtistDocumentsSection
            {
                Cpf = artist.Cpf,
                Rg = artist.Rg,
                Cnh = artist.Cnh,
                ProofAddressUrl = artist.ProofAddressUrl,
                ProfilePhotoUrl = artist.ProfilePhotoUrl,
                DocumentFrontUrl = artist.DocumentFrontUrl,
                DocumentBackUrl = artist.DocumentBackUrl
            },
            BankData = new ArtistBankDataSection
            {
                BankName = artist.BankName,
                BankCode = artist.BankCode,
                Agency = artist.Agency,
                Account = artist.Account,
                AccountType = artist.AccountType,
                HolderName = artist.HolderName,
                HolderDocument = artist.HolderDocument,
                PixKeyType = artist.PixKeyType,
                PixKey = artist.PixKey
            },
            SocialLinks = new ArtistSocialLinksSection
            {
                Instagram = artist.InstagramUrl,
                Youtube = artist.YoutubeUrl,
                Spotify = artist.SpotifyUrl,
                Tiktok = artist.TiktokUrl,
                Facebook = artist.FacebookUrl,
                Website = artist.WebsiteUrl
            },
            Portfolio = new ArtistPortfolioSection
            {
                PortfolioSummary = artist.PortfolioSummary,
                PortfolioHighlights = artist.PortfolioHighlights,
                PortfolioLinks = portfolioLinks,
                PressKitUrl = artist.PressKitUrl,
                DemoVideoUrl = artist.DemoVideoUrl,
                RepertoireDocUrl = artist.RepertoireDocUrl
            },
            Equipment = new ArtistEquipmentSection
            {
                HasOwnSoundSystem = artist.HasOwnSoundSystem,
                HasOwnLighting = artist.HasOwnLighting,
                BringsBand = artist.BringsBand,
                MusiciansCount = artist.MusiciansCount,
                Instruments = instruments,
                TechnicalRider = artist.TechnicalRider,
                TransportInfo = artist.TransportInfo,
                SetupTimeMinutes = artist.SetupTimeMinutes
            }
        };

        return Ok(ApiResponse<object>.Ok(data));
    }

    [Authorize]
    [HttpPut("me/complete-profile")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateMyCompleteProfile([FromBody] ArtistCompleteProfileRequest payload)
    {
        var normalized = NormalizeArtistCompletePayload(payload);
        var validationErrors = ValidateArtistCompleteProfile(normalized);
        if (validationErrors.Count > 0)
        {
            return BadRequest(ApiResponse<object>.Fail("Campos obrigatórios não preenchidos ou inválidos.", validationErrors.ToArray()));
        }

        var userId = User.GetRequiredUserId();
        var user = await _unitOfWork.Users.GetWithProfilesAsync(userId);
        if (user == null || !user.IsActive)
        {
            return Unauthorized(ApiResponse<object>.Fail("Usuário não autenticado ou inválido no contexto do auth/me."));
        }

        var artist = await _unitOfWork.ArtistProfiles.GetByUserIdAsync(userId);
        var artistEmail = normalized.BasicInfo?.Email?.Trim();
        if (!string.IsNullOrWhiteSpace(artistEmail))
        {
            var existingByEmail = await _unitOfWork.ArtistProfiles.GetByEmailAsync(artistEmail);
            if (existingByEmail != null && existingByEmail.UserId != userId)
            {
                return Conflict(ApiResponse<object>.Fail("E-mail já cadastrado em outro perfil de artista."));
            }
        }

        var isInsert = false;
        if (artist == null)
        {
            isInsert = true;
            artist = new ArtistProfile
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        if (normalized.BasicInfo != null)
        {
            artist.StageName = normalized.BasicInfo.StageName ?? artist.StageName;
            artist.FullName = normalized.BasicInfo.FullName ?? artist.FullName;
            artist.BirthDate = normalized.BasicInfo.BirthDate ?? artist.BirthDate;
            artist.Phone = normalized.BasicInfo.Phone ?? artist.Phone;
            artist.Email = normalized.BasicInfo.Email ?? artist.Email;
            artist.Genre = normalized.BasicInfo.Genre ?? artist.Genre;
            artist.City = normalized.BasicInfo.City ?? artist.City;
            artist.State = normalized.BasicInfo.State ?? artist.State;
            artist.Description = normalized.BasicInfo.Bio ?? artist.Description;
        }

        if (normalized.Documents != null)
        {
            artist.Cpf = normalized.Documents.Cpf ?? artist.Cpf;
            artist.Rg = normalized.Documents.Rg ?? artist.Rg;
            artist.Cnh = normalized.Documents.Cnh ?? artist.Cnh;
            artist.ProofAddressUrl = normalized.Documents.ProofAddressUrl ?? artist.ProofAddressUrl;
            artist.ProfilePhotoUrl = normalized.Documents.ProfilePhotoUrl ?? artist.ProfilePhotoUrl;
            artist.DocumentFrontUrl = normalized.Documents.DocumentFrontUrl ?? artist.DocumentFrontUrl;
            artist.DocumentBackUrl = normalized.Documents.DocumentBackUrl ?? artist.DocumentBackUrl;
        }

        if (normalized.BankData != null)
        {
            artist.BankName = normalized.BankData.BankName ?? artist.BankName;
            artist.BankCode = normalized.BankData.BankCode ?? artist.BankCode;
            artist.Agency = normalized.BankData.Agency ?? artist.Agency;
            artist.Account = normalized.BankData.Account ?? artist.Account;
            artist.AccountType = normalized.BankData.AccountType ?? artist.AccountType;
            artist.HolderName = normalized.BankData.HolderName ?? artist.HolderName;
            artist.HolderDocument = normalized.BankData.HolderDocument ?? artist.HolderDocument;
            artist.PixKeyType = normalized.BankData.PixKeyType ?? artist.PixKeyType;
            artist.PixKey = normalized.BankData.PixKey ?? artist.PixKey;
        }

        if (normalized.SocialLinks != null)
        {
            artist.InstagramUrl = normalized.SocialLinks.Instagram ?? artist.InstagramUrl;
            artist.YoutubeUrl = normalized.SocialLinks.Youtube ?? artist.YoutubeUrl;
            artist.SpotifyUrl = normalized.SocialLinks.Spotify ?? artist.SpotifyUrl;
            artist.TiktokUrl = normalized.SocialLinks.Tiktok ?? artist.TiktokUrl;
            artist.FacebookUrl = normalized.SocialLinks.Facebook ?? artist.FacebookUrl;
            artist.WebsiteUrl = normalized.SocialLinks.Website ?? artist.WebsiteUrl;
        }

        if (normalized.Portfolio != null)
        {
            artist.PortfolioSummary = normalized.Portfolio.PortfolioSummary ?? artist.PortfolioSummary;
            artist.PortfolioHighlights = normalized.Portfolio.PortfolioHighlights ?? artist.PortfolioHighlights;
            artist.PortfolioLinksJson = JsonSerializer.Serialize(normalized.Portfolio.PortfolioLinks ?? new List<string>());
            artist.PressKitUrl = normalized.Portfolio.PressKitUrl ?? artist.PressKitUrl;
            artist.DemoVideoUrl = normalized.Portfolio.DemoVideoUrl ?? artist.DemoVideoUrl;
            artist.RepertoireDocUrl = normalized.Portfolio.RepertoireDocUrl ?? artist.RepertoireDocUrl;
        }

        if (normalized.Equipment != null)
        {
            artist.HasOwnSoundSystem = normalized.Equipment.HasOwnSoundSystem;
            artist.HasOwnLighting = normalized.Equipment.HasOwnLighting;
            artist.BringsBand = normalized.Equipment.BringsBand;
            artist.MusiciansCount = normalized.Equipment.MusiciansCount;
            artist.InstrumentsJson = JsonSerializer.Serialize(normalized.Equipment.Instruments ?? new List<string>());
            artist.TechnicalRider = normalized.Equipment.TechnicalRider ?? artist.TechnicalRider;
            artist.TransportInfo = normalized.Equipment.TransportInfo ?? artist.TransportInfo;
            artist.SetupTimeMinutes = normalized.Equipment.SetupTimeMinutes;
        }

        if (isInsert)
        {
            await _unitOfWork.ArtistProfiles.AddAsync(artist);
        }
        else
        {
            await _unitOfWork.ArtistProfiles.UpdateAsync(artist);
        }

        await _unitOfWork.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(normalized, isInsert ? "Artist complete profile created." : "Artist complete profile updated."));
    }

    private static ArtistCompleteProfileRequest NormalizeArtistCompletePayload(ArtistCompleteProfileRequest payload)
    {
        var hasSectionPayload = payload.BasicInfo != null || payload.Documents != null || payload.BankData != null ||
                                payload.SocialLinks != null || payload.Portfolio != null || payload.Equipment != null;

        if (hasSectionPayload)
        {
            return payload;
        }

        var musiciansCount = ParseIntOrDefault(payload.MusiciansCount, 0);
        var setupTimeMinutes = ParseIntOrDefault(payload.SetupTimeMinutes, 0);

        return new ArtistCompleteProfileRequest
        {
            BasicInfo = new ArtistBasicInfoSection
            {
                StageName = payload.StageName,
                FullName = payload.FullName,
                BirthDate = payload.BirthDate,
                Phone = payload.Phone,
                Email = payload.Email,
                Genre = payload.Genre,
                City = payload.City,
                State = payload.State,
                Bio = payload.Bio
            },
            Documents = new ArtistDocumentsSection
            {
                Cpf = payload.Cpf,
                Rg = payload.Rg,
                Cnh = payload.Cnh,
                ProofAddressUrl = payload.ProofAddressUrl,
                ProfilePhotoUrl = payload.ProfilePhotoUrl,
                DocumentFrontUrl = payload.DocumentFrontUrl,
                DocumentBackUrl = payload.DocumentBackUrl
            },
            BankData = new ArtistBankDataSection
            {
                BankName = payload.BankName,
                BankCode = payload.BankCode,
                Agency = payload.Agency,
                Account = payload.Account,
                AccountType = payload.AccountType,
                HolderName = payload.HolderName,
                HolderDocument = payload.HolderDocument,
                PixKeyType = payload.PixKeyType,
                PixKey = payload.PixKey
            },
            SocialLinks = new ArtistSocialLinksSection
            {
                Instagram = payload.Instagram,
                Youtube = payload.Youtube,
                Spotify = payload.Spotify,
                Tiktok = payload.Tiktok,
                Facebook = payload.Facebook,
                Website = payload.Website
            },
            Portfolio = new ArtistPortfolioSection
            {
                PortfolioSummary = payload.PortfolioSummary,
                PortfolioHighlights = payload.PortfolioHighlights,
                PortfolioLinks = ParseStringList(payload.PortfolioLinks),
                PressKitUrl = payload.PressKitUrl,
                DemoVideoUrl = payload.DemoVideoUrl,
                RepertoireDocUrl = payload.RepertoireDocUrl
            },
            Equipment = new ArtistEquipmentSection
            {
                HasOwnSoundSystem = payload.HasOwnSoundSystem ?? false,
                HasOwnLighting = payload.HasOwnLighting ?? false,
                BringsBand = payload.BringsBand ?? false,
                MusiciansCount = musiciansCount,
                Instruments = ParseStringList(payload.Instruments),
                TechnicalRider = payload.TechnicalRider,
                TransportInfo = payload.TransportInfo,
                SetupTimeMinutes = setupTimeMinutes
            }
        };
    }

    private static List<string> ValidateArtistCompleteProfile(ArtistCompleteProfileRequest payload)
    {
        var errors = new List<string>();
        var basic = payload.BasicInfo;

        if (basic == null)
        {
            errors.Add("Campo obrigatório ausente: basicInfo");
            return errors;
        }

        if (string.IsNullOrWhiteSpace(basic.StageName)) errors.Add("Campo obrigatório: stageName");
        if (string.IsNullOrWhiteSpace(basic.FullName)) errors.Add("Campo obrigatório: fullName");
        if (!basic.BirthDate.HasValue) errors.Add("Campo obrigatório: birthDate");
        if (string.IsNullOrWhiteSpace(basic.Phone)) errors.Add("Campo obrigatório: phone");
        if (string.IsNullOrWhiteSpace(basic.Email)) errors.Add("Campo obrigatório: email");
        if (!string.IsNullOrWhiteSpace(basic.Email) && !basic.Email.Contains('@')) errors.Add("Campo inválido: email");
        if (string.IsNullOrWhiteSpace(basic.Genre)) errors.Add("Campo obrigatório: genre");
        if (string.IsNullOrWhiteSpace(basic.City)) errors.Add("Campo obrigatório: city");
        if (string.IsNullOrWhiteSpace(basic.State)) errors.Add("Campo obrigatório: state");
        if (string.IsNullOrWhiteSpace(basic.Bio)) errors.Add("Campo obrigatório: bio");

        return errors;
    }

    private static int ParseIntOrDefault(string? value, int defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value)) return defaultValue;
        return int.TryParse(value, out var parsed) ? parsed : defaultValue;
    }

    private static List<string> ParseStringList(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return new List<string>();
        return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }

    [Authorize]
    [HttpGet("me/weekly-stats")]
    public ActionResult<ApiResponse<object>> WeeklyStats()
    {
        return Ok(ApiResponse<object>.Ok(new { week = DateTime.UtcNow.Date, requests = 0, plays = 0 }));
    }

    [Authorize]
    [HttpGet("me/feedbacks")]
    public ActionResult<ApiResponse<object>> Feedbacks()
    {
        return Ok(ApiResponse<object>.Ok(new { items = Array.Empty<object>() }));
    }

    [HttpGet("{id:guid}/reviews")]
    public ActionResult<ApiResponse<object>> GetReviews(Guid id)
    {
        return Ok(ApiResponse<object>.Ok(new { artistId = id, items = Array.Empty<object>() }));
    }

    [Authorize]
    [HttpPost("{id:guid}/reviews")]
    public ActionResult<ApiResponse<object>> CreateReviews(Guid id, [FromBody] object payload)
    {
        return Ok(ApiResponse<object>.Ok(new { artistId = id, payload }, "Review created."));
    }
}

[ApiController]
[Route("api/v1/venues")]
public class VenuesV1Controller : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public VenuesV1Controller(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<VenueProfile>>>> GetAll([FromQuery] string? query)
    {
        var data = string.IsNullOrWhiteSpace(query)
            ? await _unitOfWork.VenueProfiles.GetAllAsync()
            : await _unitOfWork.VenueProfiles.SearchAsync(query);

        return Ok(ApiResponse<IEnumerable<VenueProfile>>.Ok(data));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<VenueProfile>>> GetById(Guid id)
    {
        var item = await _unitOfWork.VenueProfiles.GetWithAddressAsync(id);
        if (item == null)
        {
            return NotFound(ApiResponse<VenueProfile>.Fail("Venue not found."));
        }

        return Ok(ApiResponse<VenueProfile>.Ok(item));
    }

    [HttpGet("{id:guid}/events")]
    public async Task<ActionResult<ApiResponse<IEnumerable<Event>>>> GetEvents(Guid id)
    {
        var items = await _unitOfWork.Events.GetByVenueAsync(id);
        return Ok(ApiResponse<IEnumerable<Event>>.Ok(items));
    }

    [HttpGet("{id:guid}/artists")]
    public ActionResult<ApiResponse<object>> GetArtists(Guid id)
    {
        return Ok(ApiResponse<object>.Ok(new { venueId = id, items = Array.Empty<object>() }));
    }

    [HttpGet("{id:guid}/menu")]
    public ActionResult<ApiResponse<object>> GetMenu(Guid id)
    {
        return Ok(ApiResponse<object>.Ok(new { venueId = id, items = Array.Empty<object>() }));
    }

    [Authorize]
    [HttpPut("{id:guid}/menu")]
    public ActionResult<ApiResponse<object>> UpdateMenu(Guid id, [FromBody] VenueMenuRequest payload)
    {
        var venue = _unitOfWork.VenueProfiles.GetWithAddressAsync(id).GetAwaiter().GetResult();
        if (venue == null)
        {
            return NotFound(ApiResponse<object>.Fail("Venue not found."));
        }

        venue.MenuItemsJson = JsonSerializer.Serialize(payload.MenuItems ?? new List<VenueMenuItemRequest>());
        venue.MenuDocumentUrl = payload.MenuDocumentUrl;
        venue.MenuDocumentName = payload.MenuDocumentName;
        venue.GalleryJson = JsonSerializer.Serialize(payload.Gallery ?? new List<string>());

        _unitOfWork.VenueProfiles.UpdateAsync(venue).GetAwaiter().GetResult();
        _unitOfWork.SaveChangesAsync().GetAwaiter().GetResult();

        return Ok(ApiResponse<object>.Ok(new { venueId = id, payload }, "Venue menu updated."));
    }

    [HttpGet("nearby")]
    public ActionResult<ApiResponse<object>> Nearby([FromQuery] decimal lat, [FromQuery] decimal lng, [FromQuery] int radius = 10)
    {
        return Ok(ApiResponse<object>.Ok(new { lat, lng, radius, items = Array.Empty<object>() }));
    }

    [Authorize]
    [HttpGet("me/profile")]
    public ActionResult<ApiResponse<object>> GetMyProfile()
    {
        var userId = User.GetRequiredUserId();
        var venue = _unitOfWork.VenueProfiles.GetByUserIdAsync(userId).GetAwaiter().GetResult();
        if (venue == null)
        {
            return NotFound(ApiResponse<object>.Fail("Venue profile not found."));
        }

        var data = new
        {
            name = venue.Name,
            description = venue.Description,
            capacity = venue.Capacity,
            type = (int)venue.Type,
            phone = venue.Phone,
            email = venue.Email,
            website = venue.Website,
            street = venue.VenueAddress?.Street,
            number = venue.VenueAddress?.Number,
            neighborhood = venue.VenueAddress?.Neighborhood,
            city = venue.VenueAddress?.City,
            state = venue.VenueAddress?.State,
            zipCode = venue.VenueAddress?.ZipCode,
            latitude = venue.VenueAddress?.Latitude,
            longitude = venue.VenueAddress?.Longitude
        };

        return Ok(ApiResponse<object>.Ok(data));
    }

    [Authorize]
    [HttpPut("me/profile")]
    public ActionResult<ApiResponse<object>> UpdateMyProfile([FromBody] VenueCommercialProfileRequest payload)
    {
        var userId = User.GetRequiredUserId();
        var venue = _unitOfWork.VenueProfiles.GetByUserIdAsync(userId).GetAwaiter().GetResult();
        if (venue == null)
        {
            return NotFound(ApiResponse<object>.Fail("Venue profile not found."));
        }

        venue.Name = payload.Name ?? venue.Name;
        venue.Description = payload.Description ?? venue.Description;
        venue.Capacity = payload.Capacity ?? venue.Capacity;
        if (payload.Type.HasValue)
        {
            venue.Type = (VenueType)payload.Type.Value;
        }
        venue.Phone = payload.Phone ?? venue.Phone;
        venue.Email = payload.Email ?? venue.Email;
        venue.Website = payload.Website ?? venue.Website;

        if (venue.VenueAddress == null)
        {
            venue.VenueAddress = new VenueAddress
            {
                VenueProfileId = venue.Id,
                Country = "Brasil"
            };
        }

        venue.VenueAddress.Street = payload.Street ?? venue.VenueAddress.Street;
        venue.VenueAddress.Number = payload.Number ?? venue.VenueAddress.Number;
        venue.VenueAddress.Neighborhood = payload.Neighborhood ?? venue.VenueAddress.Neighborhood;
        venue.VenueAddress.City = payload.City ?? venue.VenueAddress.City;
        venue.VenueAddress.State = payload.State ?? venue.VenueAddress.State;
        venue.VenueAddress.ZipCode = payload.ZipCode ?? venue.VenueAddress.ZipCode;
        venue.VenueAddress.Latitude = payload.Latitude ?? venue.VenueAddress.Latitude;
        venue.VenueAddress.Longitude = payload.Longitude ?? venue.VenueAddress.Longitude;

        _unitOfWork.VenueProfiles.UpdateAsync(venue).GetAwaiter().GetResult();
        _unitOfWork.SaveChangesAsync().GetAwaiter().GetResult();

        return Ok(ApiResponse<object>.Ok(payload, "Venue profile updated."));
    }
}

[ApiController]
[Route("api/v1/events")]
public class EventsV1Controller : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;

    public EventsV1Controller(IMediator mediator, IUnitOfWork unitOfWork)
    {
        _mediator = mediator;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<EventDto>>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var today = DateTime.UtcNow.Date;
        var data = await _mediator.Send(new GetEventsByDateRangeQuery(today.AddYears(-1), today.AddYears(2)));
        var paged = data.Skip((page - 1) * pageSize).Take(pageSize);
        return Ok(ApiResponse<IEnumerable<EventDto>>.Ok(paged));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<EventDto>>> GetById(Guid id)
    {
        var item = await _mediator.Send(new GetEventByIdQuery(id));
        if (item == null)
        {
            return NotFound(ApiResponse<EventDto>.Fail("Event not found."));
        }

        return Ok(ApiResponse<EventDto>.Ok(item));
    }

    [Authorize]
    [HttpGet("week")]
    public async Task<ActionResult<ApiResponse<IEnumerable<EventDto>>>> Week()
    {
        var start = DateTime.UtcNow.Date;
        var end = start.AddDays(7);
        var data = await _mediator.Send(new GetEventsByDateRangeQuery(start, end));
        return Ok(ApiResponse<IEnumerable<EventDto>>.Ok(data));
    }

    [HttpGet("today/live")]
    public async Task<ActionResult<ApiResponse<IEnumerable<EventDto>>>> TodayLive()
    {
        var today = DateTime.UtcNow.Date;
        var data = await _mediator.Send(new GetEventsByDateRangeQuery(today, today));
        return Ok(ApiResponse<IEnumerable<EventDto>>.Ok(data));
    }

    [Authorize]
    [HttpPost("{id:guid}/like")]
    public async Task<ActionResult<ApiResponse<object>>> Like(Guid id)
    {
        var item = await _unitOfWork.Events.GetByIdAsync(id);
        if (item == null)
        {
            return NotFound(ApiResponse<object>.Fail("Event not found."));
        }

        item.Likes += 1;
        await _unitOfWork.Events.UpdateAsync(item);
        await _unitOfWork.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { likes = item.Likes }));
    }

    [Authorize]
    [HttpDelete("{id:guid}/like")]
    public async Task<ActionResult<ApiResponse<object>>> Unlike(Guid id)
    {
        var item = await _unitOfWork.Events.GetByIdAsync(id);
        if (item == null)
        {
            return NotFound(ApiResponse<object>.Fail("Event not found."));
        }

        if (item.Likes > 0)
        {
            item.Likes -= 1;
            await _unitOfWork.Events.UpdateAsync(item);
            await _unitOfWork.SaveChangesAsync();
        }

        return Ok(ApiResponse<object>.Ok(new { likes = item.Likes }));
    }

    [Authorize]
    [HttpPost("{id:guid}/share")]
    public async Task<ActionResult<ApiResponse<object>>> Share(Guid id)
    {
        var item = await _unitOfWork.Events.GetByIdAsync(id);
        if (item == null)
        {
            return NotFound(ApiResponse<object>.Fail("Event not found."));
        }

        item.Shares += 1;
        await _unitOfWork.Events.UpdateAsync(item);
        await _unitOfWork.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { shares = item.Shares }));
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<EventDto>>> Create([FromBody] CreateEventCommand command)
    {
        var dto = await _mediator.Send(command);
        return Ok(ApiResponse<EventDto>.Ok(dto, "Event created."));
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Update(Guid id, [FromBody] UpdateEventRequest request)
    {
        var item = await _unitOfWork.Events.GetByIdAsync(id);
        if (item == null)
        {
            return NotFound(ApiResponse<object>.Fail("Event not found."));
        }

        item.Title = request.Title ?? item.Title;
        item.Description = request.Description ?? item.Description;
        item.Date = request.Date ?? item.Date;
        item.StartTime = request.StartTime ?? item.StartTime;
        item.EndTime = request.EndTime ?? item.EndTime;

        await _unitOfWork.Events.UpdateAsync(item);
        await _unitOfWork.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { id = item.Id }, "Event updated."));
    }

    [Authorize]
    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<object>>> ChangeStatus(Guid id, [FromBody] ChangeEventStatusRequest request)
    {
        var item = await _unitOfWork.Events.GetByIdAsync(id);
        if (item == null)
        {
            return NotFound(ApiResponse<object>.Fail("Event not found."));
        }

        item.Status = request.Status;
        await _unitOfWork.Events.UpdateAsync(item);
        await _unitOfWork.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { id = item.Id, status = item.Status }));
    }

    [HttpGet("{id:guid}/reviews")]
    public ActionResult<ApiResponse<object>> GetReviews(Guid id)
    {
        return Ok(ApiResponse<object>.Ok(new { eventId = id, items = Array.Empty<object>() }));
    }

    [Authorize]
    [HttpPost("{id:guid}/reviews")]
    public ActionResult<ApiResponse<object>> CreateReview(Guid id, [FromBody] object payload)
    {
        return Ok(ApiResponse<object>.Ok(new { eventId = id, payload }, "Review created."));
    }
}

[ApiController]
[Route("api/v1/repertoires")]
public class RepertoiresV1Controller : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public RepertoiresV1Controller(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<Repertoire>>>> GetMine([FromQuery] Guid? artistProfileId)
    {
        if (artistProfileId.HasValue)
        {
            var data = await _unitOfWork.Repertoires.GetByArtistAsync(artistProfileId.Value);
            return Ok(ApiResponse<IEnumerable<Repertoire>>.Ok(data));
        }

        return Ok(ApiResponse<IEnumerable<Repertoire>>.Ok(Array.Empty<Repertoire>()));
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<Repertoire>>> Create([FromBody] CreateRepertoireRequest request)
    {
        var item = new Repertoire
        {
            Id = Guid.NewGuid(),
            ArtistProfileId = request.ArtistProfileId,
            Name = request.Name,
            Description = request.Description,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repertoires.AddAsync(item);
        await _unitOfWork.SaveChangesAsync();
        return Ok(ApiResponse<Repertoire>.Ok(item, "Repertoire created."));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<Repertoire>>> GetById(Guid id)
    {
        var item = await _unitOfWork.Repertoires.GetWithSongsAsync(id);
        if (item == null)
        {
            return NotFound(ApiResponse<Repertoire>.Fail("Repertoire not found."));
        }

        return Ok(ApiResponse<Repertoire>.Ok(item));
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<Repertoire>>> Update(Guid id, [FromBody] UpdateRepertoireRequest request)
    {
        var item = await _unitOfWork.Repertoires.GetByIdAsync(id);
        if (item == null)
        {
            return NotFound(ApiResponse<Repertoire>.Fail("Repertoire not found."));
        }

        item.Name = request.Name ?? item.Name;
        item.Description = request.Description ?? item.Description;
        item.IsActive = request.IsActive ?? item.IsActive;

        await _unitOfWork.Repertoires.UpdateAsync(item);
        await _unitOfWork.SaveChangesAsync();
        return Ok(ApiResponse<Repertoire>.Ok(item, "Repertoire updated."));
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id)
    {
        var exists = await _unitOfWork.Repertoires.ExistsAsync(id);
        if (!exists)
        {
            return NotFound(ApiResponse<object>.Fail("Repertoire not found."));
        }

        await _unitOfWork.Repertoires.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { }, "Repertoire removed."));
    }

    [Authorize]
    [HttpPatch("{id:guid}/activate")]
    public async Task<ActionResult<ApiResponse<object>>> Activate(Guid id)
    {
        var item = await _unitOfWork.Repertoires.GetByIdAsync(id);
        if (item == null)
        {
            return NotFound(ApiResponse<object>.Fail("Repertoire not found."));
        }

        item.IsActive = true;
        await _unitOfWork.Repertoires.UpdateAsync(item);
        await _unitOfWork.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { id }, "Repertoire activated."));
    }

    [Authorize]
    [HttpPatch("{id:guid}/deactivate")]
    public async Task<ActionResult<ApiResponse<object>>> Deactivate(Guid id)
    {
        var item = await _unitOfWork.Repertoires.GetByIdAsync(id);
        if (item == null)
        {
            return NotFound(ApiResponse<object>.Fail("Repertoire not found."));
        }

        item.IsActive = false;
        await _unitOfWork.Repertoires.UpdateAsync(item);
        await _unitOfWork.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { id }, "Repertoire deactivated."));
    }

    [Authorize]
    [HttpPost("{id:guid}/songs")]
    public ActionResult<ApiResponse<object>> AddSongs(Guid id, [FromBody] AddSongsRequest request)
    {
        return Ok(ApiResponse<object>.Ok(new { repertoireId = id, songIds = request.SongIds }, "Songs linked."));
    }

    [Authorize]
    [HttpDelete("{id:guid}/songs/{songId:guid}")]
    public ActionResult<ApiResponse<object>> RemoveSong(Guid id, Guid songId)
    {
        return Ok(ApiResponse<object>.Ok(new { repertoireId = id, songId }, "Song removed."));
    }

    [Authorize]
    [HttpPatch("{id:guid}/songs/reorder")]
    public ActionResult<ApiResponse<object>> Reorder(Guid id, [FromBody] ReorderSongsRequest request)
    {
        return Ok(ApiResponse<object>.Ok(new { repertoireId = id, request.Orders }, "Songs reordered."));
    }
}

[ApiController]
[Route("api/v1/songs")]
public class SongsV1Controller : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;

    public SongsV1Controller(IMediator mediator, IUnitOfWork unitOfWork)
    {
        _mediator = mediator;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<SongDto>>>> Search([FromQuery] string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            var all = await _unitOfWork.Songs.GetAllAsync();
            var list = all.Select(x => new SongDto
            {
                Id = x.Id,
                Title = x.Title,
                Artist = x.Artist,
                Duration = x.Duration,
                Year = x.Year,
                IsPopular = x.IsPopular,
                Genre = new GenreDto { Id = x.GenreId }
            });

            return Ok(ApiResponse<IEnumerable<SongDto>>.Ok(list));
        }

        var result = await _mediator.Send(new SearchSongsQuery(query));
        return Ok(ApiResponse<IEnumerable<SongDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<Song>>> GetById(Guid id)
    {
        var item = await _unitOfWork.Songs.GetByIdAsync(id);
        if (item == null)
        {
            return NotFound(ApiResponse<Song>.Fail("Song not found."));
        }

        return Ok(ApiResponse<Song>.Ok(item));
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<SongDto>>> Create([FromBody] CreateSongCommand command)
    {
        var dto = await _mediator.Send(command);
        return Ok(ApiResponse<SongDto>.Ok(dto, "Song created."));
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<Song>>> Update(Guid id, [FromBody] UpdateSongRequest request)
    {
        var item = await _unitOfWork.Songs.GetByIdAsync(id);
        if (item == null)
        {
            return NotFound(ApiResponse<Song>.Fail("Song not found."));
        }

        item.Title = request.Title ?? item.Title;
        item.Artist = request.Artist ?? item.Artist;
        item.Duration = request.Duration ?? item.Duration;
        item.Year = request.Year ?? item.Year;

        await _unitOfWork.Songs.UpdateAsync(item);
        await _unitOfWork.SaveChangesAsync();
        return Ok(ApiResponse<Song>.Ok(item, "Song updated."));
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id)
    {
        var exists = await _unitOfWork.Songs.ExistsAsync(id);
        if (!exists)
        {
            return NotFound(ApiResponse<object>.Fail("Song not found."));
        }

        await _unitOfWork.Songs.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { }, "Song removed."));
    }
}

[ApiController]
[Route("api/v1/song-requests")]
public class SongRequestsV1Controller : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;

    public SongRequestsV1Controller(IMediator mediator, IUnitOfWork unitOfWork)
    {
        _mediator = mediator;
        _unitOfWork = unitOfWork;
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<SongRequestDto>>> Create([FromBody] CreateSongRequestCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);
            return Ok(ApiResponse<SongRequestDto>.Ok(result, "Song request created."));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<SongRequestDto>.Fail("Song request failed.", ex.Message));
        }
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<IEnumerable<SongRequest>>>> MyRequests()
    {
        var userId = User.GetRequiredUserId();
        var data = await _unitOfWork.SongRequests.GetByUserAsync(userId);
        return Ok(ApiResponse<IEnumerable<SongRequest>>.Ok(data));
    }

    [HttpGet("event/{eventId:guid}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<SongRequestDto>>>> ByEvent(Guid eventId, [FromQuery] bool pendingOnly = false)
    {
        var result = await _mediator.Send(new GetSongRequestsByEventQuery(eventId, pendingOnly));
        return Ok(ApiResponse<IEnumerable<SongRequestDto>>.Ok(result));
    }

    [HttpGet("live/{artistId:guid}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<SongRequest>>>> LiveByArtist(Guid artistId)
    {
        var events = await _unitOfWork.Events.GetByArtistAsync(artistId);
        var eventIds = events.Select(x => x.Id).ToHashSet();

        var all = new List<SongRequest>();
        foreach (var eventId in eventIds)
        {
            var reqs = await _unitOfWork.SongRequests.GetPendingByEventAsync(eventId);
            all.AddRange(reqs);
        }

        return Ok(ApiResponse<IEnumerable<SongRequest>>.Ok(all));
    }

    [Authorize]
    [HttpPatch("{id:guid}/accept")]
    public Task<ActionResult<ApiResponse<object>>> Accept(Guid id)
    {
        return ChangeStatus(id, SongRequestStatus.Accepted);
    }

    [Authorize]
    [HttpPatch("{id:guid}/decline")]
    public Task<ActionResult<ApiResponse<object>>> Decline(Guid id)
    {
        return ChangeStatus(id, SongRequestStatus.Declined);
    }

    [Authorize]
    [HttpPatch("{id:guid}/play")]
    public Task<ActionResult<ApiResponse<object>>> Play(Guid id)
    {
        return ChangeStatus(id, SongRequestStatus.Played);
    }

    [Authorize]
    [HttpPatch("{id:guid}/finish")]
    public Task<ActionResult<ApiResponse<object>>> Finish(Guid id)
    {
        return ChangeStatus(id, SongRequestStatus.Played);
    }

    [Authorize]
    [HttpPost("{id:guid}/vote")]
    public async Task<ActionResult<ApiResponse<object>>> Vote(Guid id)
    {
        var item = await _unitOfWork.SongRequests.GetByIdAsync(id);
        if (item == null)
        {
            return NotFound(ApiResponse<object>.Fail("Song request not found."));
        }

        item.Votes += 1;
        await _unitOfWork.SongRequests.UpdateAsync(item);
        await _unitOfWork.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { votes = item.Votes }));
    }

    [Authorize]
    [HttpDelete("{id:guid}/vote")]
    public async Task<ActionResult<ApiResponse<object>>> RemoveVote(Guid id)
    {
        var item = await _unitOfWork.SongRequests.GetByIdAsync(id);
        if (item == null)
        {
            return NotFound(ApiResponse<object>.Fail("Song request not found."));
        }

        if (item.Votes > 0)
        {
            item.Votes -= 1;
            await _unitOfWork.SongRequests.UpdateAsync(item);
            await _unitOfWork.SaveChangesAsync();
        }

        return Ok(ApiResponse<object>.Ok(new { votes = item.Votes }));
    }

    private async Task<ActionResult<ApiResponse<object>>> ChangeStatus(Guid id, SongRequestStatus status)
    {
        var item = await _unitOfWork.SongRequests.GetByIdAsync(id);
        if (item == null)
        {
            return NotFound(ApiResponse<object>.Fail("Song request not found."));
        }

        item.Status = status;
        if (status == SongRequestStatus.Played)
        {
            item.PlayedAt = DateTime.UtcNow;
        }

        await _unitOfWork.SongRequests.UpdateAsync(item);
        await _unitOfWork.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { id = item.Id, status = item.Status }));
    }
}

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin")]
public class AdminV1Controller : ControllerBase
{
    [HttpGet("users")]
    public ActionResult<ApiResponse<object>> Users()
    {
        return Ok(ApiResponse<object>.Ok(new { items = Array.Empty<object>() }));
    }

    [HttpPatch("users/{id:guid}/status")]
    public ActionResult<ApiResponse<object>> PatchUserStatus(Guid id, [FromBody] object payload)
    {
        return Ok(ApiResponse<object>.Ok(new { id, payload }, "Status updated."));
    }

    [HttpGet("moderation/comments")]
    public ActionResult<ApiResponse<object>> ModerationComments()
    {
        return Ok(ApiResponse<object>.Ok(new { items = Array.Empty<object>() }));
    }

    [HttpPatch("moderation/comments/{id:guid}/approve")]
    public ActionResult<ApiResponse<object>> ApproveComment(Guid id)
    {
        return Ok(ApiResponse<object>.Ok(new { id }, "Comment approved."));
    }

    [HttpPatch("moderation/comments/{id:guid}/reject")]
    public ActionResult<ApiResponse<object>> RejectComment(Guid id)
    {
        return Ok(ApiResponse<object>.Ok(new { id }, "Comment rejected."));
    }

    [HttpGet("reports")]
    public ActionResult<ApiResponse<object>> Reports()
    {
        return Ok(ApiResponse<object>.Ok(new { items = Array.Empty<object>() }));
    }
}

[ApiController]
[Authorize]
[Route("api/v1/uploads")]
public class UploadsV1Controller : ControllerBase
{
    [HttpPost("image")]
    public ActionResult<ApiResponse<object>> UploadImage([FromBody] object payload)
    {
        return Ok(ApiResponse<object>.Ok(new { payload }, "Image uploaded."));
    }

    [HttpPost("audio")]
    public ActionResult<ApiResponse<object>> UploadAudio([FromBody] object payload)
    {
        return Ok(ApiResponse<object>.Ok(new { payload }, "Audio uploaded."));
    }

    [HttpDelete("{fileId}")]
    public ActionResult<ApiResponse<object>> DeleteFile(string fileId)
    {
        return Ok(ApiResponse<object>.Ok(new { fileId }, "File deleted."));
    }
}

[ApiController]
[Authorize]
[Route("api/v1/notifications")]
public class NotificationsV1Controller : ControllerBase
{
    [HttpGet("me")]
    public ActionResult<ApiResponse<object>> MyNotifications()
    {
        return Ok(ApiResponse<object>.Ok(new { items = Array.Empty<object>() }));
    }

    [HttpPatch("{id:guid}/read")]
    public ActionResult<ApiResponse<object>> MarkRead(Guid id)
    {
        return Ok(ApiResponse<object>.Ok(new { id }, "Notification updated."));
    }

    [HttpPatch("read-all")]
    public ActionResult<ApiResponse<object>> MarkAllRead()
    {
        return Ok(ApiResponse<object>.Ok(new { }, "All notifications updated."));
    }
}

static class ClaimsPrincipalExtensions
{
    public static Guid GetRequiredUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? principal.FindFirstValue("sub");

        if (Guid.TryParse(value, out var id))
        {
            return id;
        }

        throw new UnauthorizedAccessException("Authenticated user id not found.");
    }
}

public class AvatarRequest
{
    public string AvatarUrl { get; set; } = string.Empty;
}

public class AddFavoriteRequest
{
    public string Type { get; set; } = string.Empty;
    public Guid RefId { get; set; }
}

public class UserFavorite
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public Guid RefId { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Helper methods for parsing enum strings
public static class EnumParsers
{
    public static Gender? ParseGender(string? genderStr)
    {
        if (string.IsNullOrWhiteSpace(genderStr))
            return null;

        return genderStr.ToLowerInvariant() switch
        {
            "masculino" or "male" or "m" => Gender.Male,
            "feminino" or "female" or "f" => Gender.Female,
            "outro" or "other" or "o" => Gender.Other,
            "prefiro não informar" or "prefiro nao informar" or "prefer not to say" or "pns" => Gender.PreferNotToSay,
            _ => null
        };
    }
}

public static class ControllerExtensions
{
    public static Gender? ParseGender(this ControllerBase _, string? genderStr)
    {
        return EnumParsers.ParseGender(genderStr);
    }
}

public class CompleteProfileRequest
{
    public string? Cpf { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? Gender { get; set; } // "masculino", "feminino", "outro", "prefiro-nao-informar"
    public AddressRequestDto? Address { get; set; }
    public ContactRequestDto? Contact { get; set; }
}

public class AddressRequestDto
{
    public string? Cep { get; set; }
    public string? Street { get; set; }
    public string? Number { get; set; }
    public string? Complement { get; set; }
    public string? Neighborhood { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
}

public class ContactRequestDto
{
    public string? Phone { get; set; }
    public string? Whatsapp { get; set; }
    public string? EmergencyContact { get; set; }
    public string? EmergencyPhone { get; set; }
}

public class UpdateEventRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateTime? Date { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
}

public class ChangeEventStatusRequest
{
    public EventStatus Status { get; set; }
}

public class CreateRepertoireRequest
{
    public Guid ArtistProfileId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class UpdateRepertoireRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}

public class AddSongsRequest
{
    public List<Guid> SongIds { get; set; } = new();
}

public class ReorderSongsRequest
{
    public List<ReorderItem> Orders { get; set; } = new();
}

public class ReorderItem
{
    public Guid SongId { get; set; }
    public int Order { get; set; }
}

public class UpdateSongRequest
{
    public string? Title { get; set; }
    public string? Artist { get; set; }
    public string? Duration { get; set; }
    public int? Year { get; set; }
}

public class ArtistCompleteProfileRequest
{
    public ArtistBasicInfoSection? BasicInfo { get; set; }
    public ArtistDocumentsSection? Documents { get; set; }
    public ArtistBankDataSection? BankData { get; set; }
    public ArtistSocialLinksSection? SocialLinks { get; set; }
    public ArtistPortfolioSection? Portfolio { get; set; }
    public ArtistEquipmentSection? Equipment { get; set; }

    // Compatibilidade com payload flat
    public string? StageName { get; set; }
    public string? FullName { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Genre { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Bio { get; set; }

    public string? Cpf { get; set; }
    public string? Rg { get; set; }
    public string? Cnh { get; set; }
    public string? ProofAddressUrl { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public string? DocumentFrontUrl { get; set; }
    public string? DocumentBackUrl { get; set; }

    public string? BankName { get; set; }
    public string? BankCode { get; set; }
    public string? Agency { get; set; }
    public string? Account { get; set; }
    public string? AccountType { get; set; }
    public string? HolderName { get; set; }
    public string? HolderDocument { get; set; }
    public string? PixKeyType { get; set; }
    public string? PixKey { get; set; }

    public string? Instagram { get; set; }
    public string? Youtube { get; set; }
    public string? Spotify { get; set; }
    public string? Tiktok { get; set; }
    public string? Facebook { get; set; }
    public string? Website { get; set; }

    public string? PortfolioSummary { get; set; }
    public string? PortfolioHighlights { get; set; }
    public string? PortfolioLinks { get; set; }
    public string? PressKitUrl { get; set; }
    public string? DemoVideoUrl { get; set; }
    public string? RepertoireDocUrl { get; set; }

    public bool? HasOwnSoundSystem { get; set; }
    public bool? HasOwnLighting { get; set; }
    public bool? BringsBand { get; set; }
    public string? MusiciansCount { get; set; }
    public string? Instruments { get; set; }
    public string? TechnicalRider { get; set; }
    public string? TransportInfo { get; set; }
    public string? SetupTimeMinutes { get; set; }
}

public class ArtistBasicProfileRequest
{
    public string? StageName { get; set; }
    public string? Description { get; set; }
    public string? InstagramUrl { get; set; }
    public string? YoutubeUrl { get; set; }
    public string? SpotifyUrl { get; set; }
    public string? SoundcloudUrl { get; set; }
    public string? TiktokUrl { get; set; }
}

public class ArtistBasicInfoSection
{
    public string? StageName { get; set; }
    public string? FullName { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Genre { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Bio { get; set; }
}

public class ArtistDocumentsSection
{
    public string? Cpf { get; set; }
    public string? Rg { get; set; }
    public string? Cnh { get; set; }
    public string? ProofAddressUrl { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public string? DocumentFrontUrl { get; set; }
    public string? DocumentBackUrl { get; set; }
}

public class ArtistBankDataSection
{
    public string? BankName { get; set; }
    public string? BankCode { get; set; }
    public string? Agency { get; set; }
    public string? Account { get; set; }
    public string? AccountType { get; set; }
    public string? HolderName { get; set; }
    public string? HolderDocument { get; set; }
    public string? PixKeyType { get; set; }
    public string? PixKey { get; set; }
}

public class ArtistSocialLinksSection
{
    public string? Instagram { get; set; }
    public string? Youtube { get; set; }
    public string? Spotify { get; set; }
    public string? Tiktok { get; set; }
    public string? Facebook { get; set; }
    public string? Website { get; set; }
}

public class ArtistPortfolioSection
{
    public string? PortfolioSummary { get; set; }
    public string? PortfolioHighlights { get; set; }
    public List<string> PortfolioLinks { get; set; } = new();
    public string? PressKitUrl { get; set; }
    public string? DemoVideoUrl { get; set; }
    public string? RepertoireDocUrl { get; set; }
}

public class ArtistEquipmentSection
{
    public bool HasOwnSoundSystem { get; set; }
    public bool HasOwnLighting { get; set; }
    public bool BringsBand { get; set; }
    public int MusiciansCount { get; set; }
    public List<string> Instruments { get; set; } = new();
    public string? TechnicalRider { get; set; }
    public string? TransportInfo { get; set; }
    public int SetupTimeMinutes { get; set; }
}

public class VenueCommercialProfileRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int? Capacity { get; set; }
    public int? Type { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? Street { get; set; }
    public string? Number { get; set; }
    public string? Neighborhood { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}

public class VenueMenuRequest
{
    public List<VenueMenuItemRequest> MenuItems { get; set; } = new();
    public string? MenuDocumentUrl { get; set; }
    public string? MenuDocumentName { get; set; }
    public List<string> Gallery { get; set; } = new();
}

public class VenueMenuItemRequest
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsPortion { get; set; }
}
