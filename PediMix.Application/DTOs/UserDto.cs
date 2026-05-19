using PediMix.Domain.Enums;

namespace PediMix.Application.DTOs;

public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public string? Bio { get; set; }
    public string? Cpf { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Whatsapp { get; set; }
    public string? EmergencyContact { get; set; }
    public string? EmergencyPhone { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public UserRole Role { get; set; }
    public bool IsEmailVerified { get; set; }
    public bool IsPhoneVerified { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public AddressDto? Address { get; set; }
    public UserPreferencesDto? Preferences { get; set; }
    public ArtistProfileDto? ArtistProfile { get; set; }
    public VenueProfileDto? VenueProfile { get; set; }
}

public class CreateUserDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
}

public class UpdateUserDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Bio { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
}

public class CompleteProfileDto
{
    public string? Cpf { get; set; }
    public DateTime? BirthDate { get; set; }
    public Gender? Gender { get; set; }
    public AddressDto? Address { get; set; }
    public ContactDto? Contact { get; set; }
}

public class ContactDto
{
    public string? Phone { get; set; }
    public string? Whatsapp { get; set; }
    public string? EmergencyContact { get; set; }
    public string? EmergencyPhone { get; set; }
}

public class AddressDto
{
    public string Street { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string? Complement { get; set; }
    public string Neighborhood { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Cep { get; set; } = string.Empty;
    public string Country { get; set; } = "Brasil";
}

public class UserPreferencesDto
{
    public string Theme { get; set; } = "light";
    public string Language { get; set; } = "pt-BR";
    public bool EmailNotifications { get; set; } = true;
    public bool PushNotifications { get; set; } = true;
    public bool SmsNotifications { get; set; } = false;
    public bool EventReminders { get; set; } = true;
    public bool NewFollowers { get; set; } = true;
    public bool EventUpdates { get; set; } = true;
    public bool ProfileVisible { get; set; } = true;
    public bool ShowLocation { get; set; } = false;
    public bool AllowDirectMessages { get; set; } = true;
}
