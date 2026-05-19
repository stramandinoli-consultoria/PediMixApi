using AutoMapper;
using System.Text.Json;
using PediMix.Application.DTOs;
using PediMix.Domain.Entities;

namespace PediMix.Application.Mappings;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<User, UserDto>()
            .ReverseMap()
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        CreateMap<CreateUserDto, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        CreateMap<UpdateUserDto, User>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        CreateMap<Address, AddressDto>()
            .ForMember(dest => dest.Cep, opt => opt.MapFrom(src => src.ZipCode))
            .ReverseMap()
            .ForMember(dest => dest.ZipCode, opt => opt.MapFrom(src => src.Cep));
        CreateMap<UserPreferences, UserPreferencesDto>().ReverseMap();
    }
}

public class ProfileMappingProfile : Profile
{
    public ProfileMappingProfile()
    {
        CreateMap<ArtistProfile, ArtistProfileDto>()
            .ForMember(dest => dest.Genres, opt => opt.MapFrom(src => src.ArtistGenres.Select(ag => ag.Genre)))
            .ForMember(dest => dest.PortfolioLinks, opt => opt.MapFrom(src => ParseStringList(src.PortfolioLinksJson)))
            .ForMember(dest => dest.Instruments, opt => opt.MapFrom(src => ParseStringList(src.InstrumentsJson)))
            .ReverseMap()
            .ForMember(dest => dest.ArtistGenres, opt => opt.Ignore())
            .ForMember(dest => dest.PortfolioLinksJson, opt => opt.Ignore())
            .ForMember(dest => dest.InstrumentsJson, opt => opt.Ignore());

        CreateMap<CreateArtistProfileDto, ArtistProfile>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ArtistGenres, opt => opt.Ignore());

        CreateMap<VenueProfile, VenueProfileDto>()
            .ForMember(dest => dest.Amenities, opt => opt.MapFrom(src => src.VenueAmenities.Select(va => va.Amenity)))
            .ReverseMap()
            .ForMember(dest => dest.VenueAmenities, opt => opt.Ignore());

        CreateMap<CreateVenueProfileDto, VenueProfile>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.VenueAmenities, opt => opt.Ignore());

        CreateMap<VenueAddress, VenueAddressDto>().ReverseMap();
        CreateMap<Amenity, AmenityDto>().ReverseMap();
    }

    private static List<string> ParseStringList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return json
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .ToList();
        }
    }
}

public class SongMappingProfile : Profile
{
    public SongMappingProfile()
    {
        CreateMap<Song, SongDto>().ReverseMap();
        CreateMap<CreateSongDto, Song>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

        CreateMap<UpdateSongDto, Song>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        CreateMap<Repertoire, RepertoireDto>()
            .ForMember(dest => dest.Songs, opt => opt.MapFrom(src => src.RepertoireSongs.Select(rs => rs.Song)))
            .ReverseMap()
            .ForMember(dest => dest.RepertoireSongs, opt => opt.Ignore());

        CreateMap<CreateRepertoireDto, Repertoire>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.RepertoireSongs, opt => opt.Ignore());

        CreateMap<SongRequest, SongRequestDto>().ReverseMap();
        CreateMap<CreateSongRequestDto, SongRequest>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => Domain.Enums.SongRequestStatus.Pending))
            .ForMember(dest => dest.Votes, opt => opt.MapFrom(src => 0));

        CreateMap<Genre, GenreDto>().ReverseMap();
    }
}

public class EventMappingProfile : Profile
{
    public EventMappingProfile()
    {
        CreateMap<Event, EventDto>()
            .ForMember(dest => dest.Genres, opt => opt.MapFrom(src => src.EventGenres.Select(eg => eg.Genre)))
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.EventTags.Select(et => et.Tag)))
            .ReverseMap()
            .ForMember(dest => dest.EventGenres, opt => opt.Ignore())
            .ForMember(dest => dest.EventTags, opt => opt.Ignore());

        CreateMap<Event, EventListDto>()
            .ForMember(dest => dest.ArtistName, opt => opt.MapFrom(src => src.ArtistProfile != null ? src.ArtistProfile.StageName : ""))
            .ForMember(dest => dest.ArtistAvatar, opt => opt.MapFrom(src => src.ArtistProfile != null ? src.ArtistProfile.User.Avatar : null))
            .ForMember(dest => dest.IsArtistVerified, opt => opt.MapFrom(src => src.ArtistProfile != null && src.ArtistProfile.IsVerified))
            .ForMember(dest => dest.VenueName, opt => opt.MapFrom(src => src.VenueProfile != null ? src.VenueProfile.Name : ""))
            .ForMember(dest => dest.VenueCity, opt => opt.MapFrom(src => src.VenueProfile != null && src.VenueProfile.VenueAddress != null ? src.VenueProfile.VenueAddress.City : ""))
            .ForMember(dest => dest.VenueState, opt => opt.MapFrom(src => src.VenueProfile != null && src.VenueProfile.VenueAddress != null ? src.VenueProfile.VenueAddress.State : ""));

        CreateMap<CreateEventDto, Event>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.EventGenres, opt => opt.Ignore())
            .ForMember(dest => dest.EventTags, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => Domain.Enums.EventStatus.Draft))
            .ForMember(dest => dest.AvailableTickets, opt => opt.MapFrom(src => src.TotalCapacity));

        CreateMap<UpdateEventDto, Event>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        CreateMap<Tag, TagDto>().ReverseMap();
    }
}
