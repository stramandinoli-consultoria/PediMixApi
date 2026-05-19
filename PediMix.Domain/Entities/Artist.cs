using PediMix.Domain.Common;

namespace PediMix.Domain.Entities;

public class ArtistProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public string StageName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsVerified { get; set; } = false;
    public int Followers { get; set; } = 0;
    public int TotalEvents { get; set; } = 0;
    public decimal Rating { get; set; } = 0;

    // Social Links
    public string? InstagramUrl { get; set; }
    public string? YoutubeUrl { get; set; }
    public string? SpotifyUrl { get; set; }
    public string? SoundcloudUrl { get; set; }
    public string? TiktokUrl { get; set; }

    // Complete profile - basic info
    public string? FullName { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Genre { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }

    // Complete profile - documents
    public string? Cpf { get; set; }
    public string? Rg { get; set; }
    public string? Cnh { get; set; }
    public string? ProofAddressUrl { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public string? DocumentFrontUrl { get; set; }
    public string? DocumentBackUrl { get; set; }

    // Complete profile - bank data
    public string? BankName { get; set; }
    public string? BankCode { get; set; }
    public string? Agency { get; set; }
    public string? Account { get; set; }
    public string? AccountType { get; set; }
    public string? HolderName { get; set; }
    public string? HolderDocument { get; set; }
    public string? PixKeyType { get; set; }
    public string? PixKey { get; set; }

    // Complete profile - social links
    public string? FacebookUrl { get; set; }
    public string? WebsiteUrl { get; set; }

    // Complete profile - portfolio
    public string? PortfolioSummary { get; set; }
    public string? PortfolioHighlights { get; set; }
    public string? PortfolioLinksJson { get; set; }
    public string? PressKitUrl { get; set; }
    public string? DemoVideoUrl { get; set; }
    public string? RepertoireDocUrl { get; set; }

    // Complete profile - equipment
    public bool HasOwnSoundSystem { get; set; }
    public bool HasOwnLighting { get; set; }
    public bool BringsBand { get; set; }
    public int MusiciansCount { get; set; }
    public string? InstrumentsJson { get; set; }
    public string? TechnicalRider { get; set; }
    public string? TransportInfo { get; set; }
    public int SetupTimeMinutes { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual ICollection<ArtistGenre> ArtistGenres { get; set; } = new List<ArtistGenre>();
    public virtual ICollection<Repertoire> Repertoires { get; set; } = new List<Repertoire>();
    public virtual ICollection<Event> Events { get; set; } = new List<Event>();
}

public class Genre : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#000000";
    public string? Description { get; set; }

    // Navigation properties
    public virtual ICollection<ArtistGenre> ArtistGenres { get; set; } = new List<ArtistGenre>();
    public virtual ICollection<Song> Songs { get; set; } = new List<Song>();
}

public class ArtistGenre : BaseEntity
{
    public Guid ArtistProfileId { get; set; }
    public Guid GenreId { get; set; }

    // Navigation properties
    public virtual ArtistProfile ArtistProfile { get; set; } = null!;
    public virtual Genre Genre { get; set; } = null!;
}
