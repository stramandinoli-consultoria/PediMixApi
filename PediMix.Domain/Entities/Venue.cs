using PediMix.Domain.Common;
using PediMix.Domain.Enums;

namespace PediMix.Domain.Entities;

public class VenueProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public VenueType Type { get; set; }
    public bool IsVerified { get; set; } = false;
    public decimal Rating { get; set; } = 0;
    public int TotalEvents { get; set; } = 0;

    // Contact Info
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }

    // Menu and gallery
    public string? MenuItemsJson { get; set; }
    public string? MenuDocumentUrl { get; set; }
    public string? MenuDocumentName { get; set; }
    public string? GalleryJson { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual VenueAddress? VenueAddress { get; set; }
    public virtual ICollection<VenueAmenity> VenueAmenities { get; set; } = new List<VenueAmenity>();
    public virtual ICollection<Event> Events { get; set; } = new List<Event>();
}

public class VenueAddress : BaseEntity
{
    public Guid VenueProfileId { get; set; }
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

    // Navigation properties
    public virtual VenueProfile VenueProfile { get; set; } = null!;
}

public class Amenity : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Navigation properties
    public virtual ICollection<VenueAmenity> VenueAmenities { get; set; } = new List<VenueAmenity>();
}

public class VenueAmenity : BaseEntity
{
    public Guid VenueProfileId { get; set; }
    public Guid AmenityId { get; set; }

    // Navigation properties
    public virtual VenueProfile VenueProfile { get; set; } = null!;
    public virtual Amenity Amenity { get; set; } = null!;
}
