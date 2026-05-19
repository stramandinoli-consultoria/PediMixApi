using PediMix.Domain.Enums;

namespace PediMix.Application.DTOs;

public class ArtistProfileDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string StageName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Genre { get; set; }
    public string? FullName { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
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
    public string? FacebookUrl { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? PortfolioSummary { get; set; }
    public string? PortfolioHighlights { get; set; }
    public string? PortfolioLinksJson { get; set; }
    public string? PressKitUrl { get; set; }
    public string? DemoVideoUrl { get; set; }
    public string? RepertoireDocUrl { get; set; }
    public bool HasOwnSoundSystem { get; set; }
    public bool HasOwnLighting { get; set; }
    public bool BringsBand { get; set; }
    public int MusiciansCount { get; set; }
    public string? InstrumentsJson { get; set; }
    public string? TechnicalRider { get; set; }
    public string? TransportInfo { get; set; }
    public int SetupTimeMinutes { get; set; }
    public bool IsVerified { get; set; }
    public int Followers { get; set; }
    public int TotalEvents { get; set; }
    public decimal Rating { get; set; }
    public string? InstagramUrl { get; set; }
    public string? YoutubeUrl { get; set; }
    public string? SpotifyUrl { get; set; }
    public string? SoundcloudUrl { get; set; }
    public string? TiktokUrl { get; set; }
    public string? Instagram
    {
        get => InstagramUrl;
        set => InstagramUrl = value;
    }
    public string? Youtube
    {
        get => YoutubeUrl;
        set => YoutubeUrl = value;
    }
    public string? Spotify
    {
        get => SpotifyUrl;
        set => SpotifyUrl = value;
    }
    public string? Tiktok
    {
        get => TiktokUrl;
        set => TiktokUrl = value;
    }
    public string? Facebook
    {
        get => FacebookUrl;
        set => FacebookUrl = value;
    }
    public string? Website
    {
        get => WebsiteUrl;
        set => WebsiteUrl = value;
    }
    public List<string> PortfolioLinks { get; set; } = new();
    public List<string> Instruments { get; set; } = new();
    public List<GenreDto> Genres { get; set; } = new();
    public List<RepertoireDto> Repertoires { get; set; } = new();
}

public class CreateArtistProfileDto
{
    public string StageName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? InstagramUrl { get; set; }
    public string? YoutubeUrl { get; set; }
    public string? SpotifyUrl { get; set; }
    public string? SoundcloudUrl { get; set; }
    public string? TiktokUrl { get; set; }
    public List<Guid> GenreIds { get; set; } = new();
}

public class UpdateArtistProfileDto
{
    public string? StageName { get; set; }
    public string? Description { get; set; }
    public string? InstagramUrl { get; set; }
    public string? YoutubeUrl { get; set; }
    public string? SpotifyUrl { get; set; }
    public string? SoundcloudUrl { get; set; }
    public string? TiktokUrl { get; set; }
    public List<Guid>? GenreIds { get; set; }
}

public class VenueProfileDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public VenueType Type { get; set; }
    public bool IsVerified { get; set; }
    public decimal Rating { get; set; }
    public int TotalEvents { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public VenueAddressDto? VenueAddress { get; set; }
    public List<AmenityDto> Amenities { get; set; } = new();
}

public class CreateVenueProfileDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public VenueType Type { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public VenueAddressDto? VenueAddress { get; set; }
    public List<Guid> AmenityIds { get; set; } = new();
}

public class VenueAddressDto
{
    public string Street { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string? Complement { get; set; }
    public string Neighborhood { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string Country { get; set; } = "Brasil";
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}

public class AmenityDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string? Description { get; set; }
}
