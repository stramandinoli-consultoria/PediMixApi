using AutoMapper;
using MediatR;
using PediMix.Application.Interfaces;
using PediMix.Application.Commands;
using PediMix.Application.DTOs;
using PediMix.Domain.Entities;
using PediMix.Domain.Enums;

namespace PediMix.Application.Handlers.CommandHandlers;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateUserCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // Verificar se email já existe
        if (await _unitOfWork.Users.EmailExistsAsync(request.Email))
        {
            throw new InvalidOperationException("Email já está em uso.");
        }

        // Verificar se username já existe
        if (await _unitOfWork.Users.UsernameExistsAsync(request.Username))
        {
            throw new InvalidOperationException("Username já está em uso.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PasswordHash = request.PasswordHash, // Assumindo que já vem hasheado
            PhoneNumber = request.PhoneNumber,
            Role = request.Role,
            IsActive = true,
            IsEmailVerified = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<UserDto>(user);
    }
}

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, UserDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateUserCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<UserDto> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.Id);
        if (user == null)
        {
            throw new KeyNotFoundException("Usuário não encontrado.");
        }

        // Verificar se email mudou e já existe
        if (user.Email != request.Email && await _unitOfWork.Users.EmailExistsAsync(request.Email))
        {
            throw new InvalidOperationException("Email já está em uso.");
        }

        // Verificar se username mudou e já existe
        if (user.Username != request.Username && await _unitOfWork.Users.UsernameExistsAsync(request.Username))
        {
            throw new InvalidOperationException("Username já está em uso.");
        }

        user.Username = request.Username;
        user.Email = request.Email;
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.PhoneNumber = request.PhoneNumber;
        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<UserDto>(user);
    }
}

public class CreateSongCommandHandler : IRequestHandler<CreateSongCommand, SongDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateSongCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<SongDto> Handle(CreateSongCommand request, CancellationToken cancellationToken)
    {
        var genreId = await ResolveGenreIdAsync(request.GenreId);

        var song = new Song
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Artist = request.Artist,
            Duration = request.Duration?.ToString() ?? "00:00",
            GenreId = genreId,
            Key = request.Key,
            Year = request.Year ?? DateTime.UtcNow.Year,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Songs.AddAsync(song);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<SongDto>(song);
    }

    private async Task<Guid> ResolveGenreIdAsync(Guid requestedGenreId)
    {
        if (requestedGenreId != Guid.Empty && await _unitOfWork.Genres.ExistsAsync(requestedGenreId))
        {
            return requestedGenreId;
        }

        var fallbackGenre = await _unitOfWork.Genres.GetByNameAsync("Outros")
            ?? await _unitOfWork.Genres.GetByNameAsync("Pop")
            ?? await _unitOfWork.Genres.GetByNameAsync("Sertanejo");

        if (fallbackGenre != null)
        {
            return fallbackGenre.Id;
        }

        var createdFallbackGenre = new Genre
        {
            Id = Guid.NewGuid(),
            Name = "Outros",
            Color = "#6B7280",
            Description = "Gênero padrão para músicas sem classificação.",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Genres.AddAsync(createdFallbackGenre);
        await _unitOfWork.SaveChangesAsync();

        return createdFallbackGenre.Id;
    }
}

public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, EventDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateEventCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<EventDto> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Title = request.Name,
            Description = request.Description ?? string.Empty,
            Date = request.StartDateTime.Date,
            StartTime = request.StartDateTime.TimeOfDay,
            EndTime = request.EndDateTime.TimeOfDay,
            ArtistProfileId = request.ArtistProfileId,
            VenueProfileId = request.VenueProfileId,
            MinPrice = request.Price,
            MaxPrice = request.Price,
            TotalCapacity = request.MaxCapacity ?? 0,
            AvailableTickets = request.MaxCapacity ?? 0,
            Status = EventStatus.Published,
            Visibility = request.IsPublic ? EventVisibility.Public : EventVisibility.Private,
            IsPaid = request.Price.HasValue,
            CreatedById = request.ArtistProfileId, // Assumindo que o artista cria o evento
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Events.AddAsync(eventEntity);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<EventDto>(eventEntity);
    }
}

public class CreateSongRequestCommandHandler : IRequestHandler<CreateSongRequestCommand, SongRequestDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateSongRequestCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<SongRequestDto> Handle(CreateSongRequestCommand request, CancellationToken cancellationToken)
    {
        // Verificar se já existe um pedido para esta música neste evento pelo mesmo usuário
        var existingRequest = await _unitOfWork.SongRequests.GetByEventAndSongAsync(
            request.EventId, request.SongId, request.UserId);
        
        if (existingRequest != null)
        {
            throw new InvalidOperationException("Você já fez um pedido para esta música neste evento.");
        }

        var songRequest = new SongRequest
        {
            Id = Guid.NewGuid(),
            EventId = request.EventId,
            SongId = request.SongId,
            RequestedById = request.UserId,
            Message = request.Message,
            Status = SongRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.SongRequests.AddAsync(songRequest);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<SongRequestDto>(songRequest);
    }
}
