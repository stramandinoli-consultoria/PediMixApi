using PediMix.Domain.Entities;

namespace PediMix.Application.Interfaces;

public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
}

public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetWithProfilesAsync(Guid id);
    Task<bool> EmailExistsAsync(string email);
    Task<bool> UsernameExistsAsync(string username);
}

public interface IArtistProfileRepository : IGenericRepository<ArtistProfile>
{
    Task<ArtistProfile?> GetByUserIdAsync(Guid userId);
    Task<ArtistProfile?> GetByEmailAsync(string email);
    Task<ArtistProfile?> GetWithRepertoiresAsync(Guid id);
    Task<IEnumerable<ArtistProfile>> GetByGenreAsync(Guid genreId);
    Task<IEnumerable<ArtistProfile>> SearchAsync(string query);
}

public interface IVenueProfileRepository : IGenericRepository<VenueProfile>
{
    Task<VenueProfile?> GetByUserIdAsync(Guid userId);
    Task<VenueProfile?> GetWithAddressAsync(Guid id);
    Task<IEnumerable<VenueProfile>> GetByCityAsync(string city);
    Task<IEnumerable<VenueProfile>> SearchAsync(string query);
}

public interface ISongRepository : IGenericRepository<Song>
{
    Task<IEnumerable<Song>> GetByGenreAsync(Guid genreId);
    Task<IEnumerable<Song>> SearchAsync(string query);
    Task<IEnumerable<Song>> GetPopularSongsAsync(int count = 25);
    Task<IEnumerable<Song>> GetByArtistAsync(string artist);
}

public interface IRepertoireRepository : IGenericRepository<Repertoire>
{
    Task<IEnumerable<Repertoire>> GetByArtistAsync(Guid artistProfileId);
    Task<Repertoire?> GetWithSongsAsync(Guid id);
    Task<IEnumerable<Repertoire>> GetActiveByArtistAsync(Guid artistProfileId);
}

public interface IEventRepository : IGenericRepository<Event>
{
    Task<IEnumerable<Event>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<Event>> GetByArtistAsync(Guid artistProfileId);
    Task<IEnumerable<Event>> GetByVenueAsync(Guid venueProfileId);
    Task<IEnumerable<Event>> GetByCityAsync(string city);
    Task<IEnumerable<Event>> SearchAsync(string query, int page = 1, int pageSize = 10);
    Task<Event?> GetWithDetailsAsync(Guid id);
    Task<IEnumerable<Event>> GetUpcomingEventsAsync(int count = 10);
}

public interface ISongRequestRepository : IGenericRepository<SongRequest>
{
    Task<IEnumerable<SongRequest>> GetByEventAsync(Guid eventId);
    Task<IEnumerable<SongRequest>> GetPendingByEventAsync(Guid eventId);
    Task<IEnumerable<SongRequest>> GetByUserAsync(Guid userId);
    Task<SongRequest?> GetByEventAndSongAsync(Guid eventId, Guid songId, Guid userId);
}

public interface IGenreRepository : IGenericRepository<Genre>
{
    Task<Genre?> GetByNameAsync(string name);
    Task<IEnumerable<Genre>> GetMostUsedAsync(int count = 10);
}

public interface IAddressRepository : IGenericRepository<PediMix.Domain.Entities.Address>
{
}

// ============================================================
// Music integrations (Spotify, Lyrically, Vagalume)
// ============================================================
public interface ISongExternalDataRepository : IGenericRepository<SongExternalData>
{
    Task<SongExternalData?> GetBySongIdAsync(Guid songId);
    Task<SongExternalData?> GetBySpotifyIdAsync(string spotifyId);
}

public interface ISongLyricsRepository : IGenericRepository<SongLyrics>
{
    Task<SongLyrics?> GetBySongIdAsync(Guid songId);
    Task<IEnumerable<SongLyrics>> GetAllBySongIdAsync(Guid songId);
}

public interface IExternalArtistRepository : IGenericRepository<ExternalArtist>
{
    Task<ExternalArtist?> GetBySpotifyIdAsync(string spotifyId);
    Task<IEnumerable<ExternalArtist>> SearchByNameAsync(string query, int limit = 10);
}

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IAddressRepository Addresses { get; }
    IArtistProfileRepository ArtistProfiles { get; }
    IVenueProfileRepository VenueProfiles { get; }
    ISongRepository Songs { get; }
    IRepertoireRepository Repertoires { get; }
    IEventRepository Events { get; }
    ISongRequestRepository SongRequests { get; }
    IGenreRepository Genres { get; }

    // Music integrations
    ISongExternalDataRepository SongExternalData { get; }
    ISongLyricsRepository SongLyrics { get; }
    IExternalArtistRepository ExternalArtists { get; }

    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
