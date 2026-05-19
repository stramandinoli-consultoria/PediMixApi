using Microsoft.EntityFrameworkCore;
using PediMix.Application.Interfaces;
using PediMix.Domain.Entities;
using PediMix.Infrastructure.Data;

namespace PediMix.Infrastructure.Repositories;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(PediMixDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
    }

    public async Task<User?> GetWithProfilesAsync(Guid id)
    {
        return await _dbSet
            .Include(u => u.Address)
            .Include(u => u.Preferences)
            .Include(u => u.ArtistProfile)
                .ThenInclude(ap => ap.ArtistGenres)
                .ThenInclude(ag => ag.Genre)
            .Include(u => u.ArtistProfile)
                .ThenInclude(ap => ap.Repertoires)
                .ThenInclude(r => r.RepertoireSongs)
                .ThenInclude(rs => rs.Song)
            .Include(u => u.VenueProfile)
                .ThenInclude(vp => vp.VenueAddress)
            .Include(u => u.VenueProfile)
                .ThenInclude(vp => vp.VenueAmenities)
                .ThenInclude(va => va.Amenity)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _dbSet
            .AnyAsync(u => u.Email.ToLower() == email.ToLower());
    }

    public async Task<bool> UsernameExistsAsync(string username)
    {
        return await _dbSet
            .AnyAsync(u => u.Username.ToLower() == username.ToLower());
    }
}

public class SongRepository : GenericRepository<Song>, ISongRepository
{
    public SongRepository(PediMixDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Song>> GetByGenreAsync(Guid genreId)
    {
        return await _dbSet
            .Include(s => s.Genre)
            .Where(s => s.GenreId == genreId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Song>> SearchAsync(string query)
    {
        var loweredQuery = query.ToLower();
        return await _dbSet
            .Include(s => s.Genre)
            .Where(s => s.Title.ToLower().Contains(loweredQuery) || 
                       s.Artist.ToLower().Contains(loweredQuery) ||
                       s.Genre.Name.ToLower().Contains(loweredQuery))
            .OrderBy(s => s.Title)
            .ToListAsync();
    }

    public async Task<IEnumerable<Song>> GetPopularSongsAsync(int count = 25)
    {
        return await _dbSet
            .Include(s => s.Genre)
            .Where(s => s.IsPopular)
            .OrderBy(s => s.Title)
            .Take(count)
            .ToListAsync();
    }

    public async Task<IEnumerable<Song>> GetByArtistAsync(string artist)
    {
        return await _dbSet
            .Include(s => s.Genre)
            .Where(s => s.Artist.ToLower().Contains(artist.ToLower()))
            .OrderBy(s => s.Title)
            .ToListAsync();
    }
}

public class RepertoireRepository : GenericRepository<Repertoire>, IRepertoireRepository
{
    public RepertoireRepository(PediMixDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Repertoire>> GetByArtistAsync(Guid artistProfileId)
    {
        return await _dbSet
            .Where(r => r.ArtistProfileId == artistProfileId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<Repertoire?> GetWithSongsAsync(Guid id)
    {
        return await _dbSet
            .Include(r => r.RepertoireSongs)
                .ThenInclude(rs => rs.Song)
                .ThenInclude(s => s.Genre)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<IEnumerable<Repertoire>> GetActiveByArtistAsync(Guid artistProfileId)
    {
        return await _dbSet
            .Include(r => r.RepertoireSongs)
                .ThenInclude(rs => rs.Song)
                .ThenInclude(s => s.Genre)
            .Where(r => r.ArtistProfileId == artistProfileId && r.IsActive)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }
}

public class EventRepository : GenericRepository<Event>, IEventRepository
{
    public EventRepository(PediMixDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Event>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Include(e => e.ArtistProfile)
            .Include(e => e.VenueProfile)
                .ThenInclude(vp => vp.VenueAddress)
            .Where(e => e.Date >= startDate && e.Date <= endDate)
            .OrderBy(e => e.Date)
            .ThenBy(e => e.StartTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Event>> GetByArtistAsync(Guid artistProfileId)
    {
        return await _dbSet
            .Include(e => e.VenueProfile)
                .ThenInclude(vp => vp.VenueAddress)
            .Where(e => e.ArtistProfileId == artistProfileId)
            .OrderByDescending(e => e.Date)
            .ToListAsync();
    }

    public async Task<IEnumerable<Event>> GetByVenueAsync(Guid venueProfileId)
    {
        return await _dbSet
            .Include(e => e.ArtistProfile)
            .Where(e => e.VenueProfileId == venueProfileId)
            .OrderByDescending(e => e.Date)
            .ToListAsync();
    }

    public async Task<IEnumerable<Event>> GetByCityAsync(string city)
    {
        return await _dbSet
            .Include(e => e.ArtistProfile)
            .Include(e => e.VenueProfile)
                .ThenInclude(vp => vp.VenueAddress)
            .Where(e => e.VenueProfile != null && 
                       e.VenueProfile.VenueAddress != null && 
                       e.VenueProfile.VenueAddress.City.ToLower() == city.ToLower())
            .OrderBy(e => e.Date)
            .ToListAsync();
    }

    public async Task<IEnumerable<Event>> SearchAsync(string query, int page = 1, int pageSize = 10)
    {
        var loweredQuery = query.ToLower();
        return await _dbSet
            .Include(e => e.ArtistProfile)
            .Include(e => e.VenueProfile)
                .ThenInclude(vp => vp.VenueAddress)
            .Include(e => e.EventGenres)
                .ThenInclude(eg => eg.Genre)
            .Where(e => e.Title.ToLower().Contains(loweredQuery) ||
                       e.Description.ToLower().Contains(loweredQuery) ||
                       (e.ArtistProfile != null && e.ArtistProfile.StageName.ToLower().Contains(loweredQuery)) ||
                       (e.VenueProfile != null && e.VenueProfile.Name.ToLower().Contains(loweredQuery)))
            .OrderBy(e => e.Date)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<Event?> GetWithDetailsAsync(Guid id)
    {
        return await _dbSet
            .Include(e => e.CreatedBy)
            .Include(e => e.ArtistProfile)
                .ThenInclude(ap => ap.User)
            .Include(e => e.VenueProfile)
                .ThenInclude(vp => vp.VenueAddress)
            .Include(e => e.EventGenres)
                .ThenInclude(eg => eg.Genre)
            .Include(e => e.EventTags)
                .ThenInclude(et => et.Tag)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<IEnumerable<Event>> GetUpcomingEventsAsync(int count = 10)
    {
        return await _dbSet
            .Include(e => e.ArtistProfile)
            .Include(e => e.VenueProfile)
                .ThenInclude(vp => vp.VenueAddress)
            .Where(e => e.Date >= DateTime.Today && e.Status == Domain.Enums.EventStatus.Published)
            .OrderBy(e => e.Date)
            .ThenBy(e => e.StartTime)
            .Take(count)
            .ToListAsync();
    }
}

public class SongRequestRepository : GenericRepository<SongRequest>, ISongRequestRepository
{
    public SongRequestRepository(PediMixDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<SongRequest>> GetByEventAsync(Guid eventId)
    {
        return await _dbSet
            .Include(sr => sr.Song)
                .ThenInclude(s => s.Genre)
            .Include(sr => sr.RequestedBy)
            .Where(sr => sr.EventId == eventId)
            .OrderByDescending(sr => sr.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<SongRequest>> GetPendingByEventAsync(Guid eventId)
    {
        return await _dbSet
            .Include(sr => sr.Song)
                .ThenInclude(s => s.Genre)
            .Include(sr => sr.RequestedBy)
            .Where(sr => sr.EventId == eventId && sr.Status == Domain.Enums.SongRequestStatus.Pending)
            .OrderByDescending(sr => sr.Votes)
            .ThenByDescending(sr => sr.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<SongRequest>> GetByUserAsync(Guid userId)
    {
        return await _dbSet
            .Include(sr => sr.Song)
                .ThenInclude(s => s.Genre)
            .Include(sr => sr.Event)
            .Where(sr => sr.RequestedById == userId)
            .OrderByDescending(sr => sr.CreatedAt)
            .ToListAsync();
    }

    public async Task<SongRequest?> GetByEventAndSongAsync(Guid eventId, Guid songId, Guid userId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(sr => sr.EventId == eventId && 
                                      sr.SongId == songId && 
                                      sr.RequestedById == userId);
    }
}
