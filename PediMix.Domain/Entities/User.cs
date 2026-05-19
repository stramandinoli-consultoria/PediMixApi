using PediMix.Domain.Common;
using PediMix.Domain.Enums;

namespace PediMix.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public string? Bio { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Whatsapp { get; set; }
    public string? Cpf { get; set; }
    public string? EmergencyContact { get; set; }
    public string? EmergencyPhone { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public UserRole Role { get; set; } = UserRole.Audience;
    public bool IsEmailVerified { get; set; } = false;
    public bool IsPhoneVerified { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }

    // Navigation properties
    public virtual Address? Address { get; set; }
    public virtual UserPreferences? Preferences { get; set; }
    public virtual ArtistProfile? ArtistProfile { get; set; }
    public virtual VenueProfile? VenueProfile { get; set; }
    public virtual ICollection<EventUser> EventUsers { get; set; } = new List<EventUser>();
    public virtual ICollection<SongRequest> SongRequests { get; set; } = new List<SongRequest>();
    public virtual ICollection<Event> CreatedEvents { get; set; } = new List<Event>();
}

public class UserPreferences : BaseEntity
{
    public Guid UserId { get; set; }
    public string Theme { get; set; } = "light"; // light, dark, auto
    public string Language { get; set; } = "pt-BR";
    
    // Notification preferences
    public bool EmailNotifications { get; set; } = true;
    public bool PushNotifications { get; set; } = true;
    public bool SmsNotifications { get; set; } = false;
    public bool EventReminders { get; set; } = true;
    public bool NewFollowers { get; set; } = true;
    public bool EventUpdates { get; set; } = true;
    
    // Privacy preferences
    public bool ProfileVisible { get; set; } = true;
    public bool ShowLocation { get; set; } = false;
    public bool AllowDirectMessages { get; set; } = true;

    // Navigation properties
    public virtual User User { get; set; } = null!;
}

public class Address : BaseEntity
{
    public Guid UserId { get; set; }
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
    public virtual User User { get; set; } = null!;
}
