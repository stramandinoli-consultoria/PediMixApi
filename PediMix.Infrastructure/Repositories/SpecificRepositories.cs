using Microsoft.EntityFrameworkCore;
using PediMix.Application.Interfaces;
using PediMix.Domain.Entities;
using PediMix.Infrastructure.Data;

namespace PediMix.Infrastructure.Repositories;

public class ArtistProfileRepository : GenericRepository<ArtistProfile>, IArtistProfileRepository
{
    public ArtistProfileRepository(PediMixDbContext context) : base(context)
    {
    }

    public async Task<ArtistProfile?> GetByUserIdAsync(Guid userId)
    {
        return await _dbSet
            .Include(ap => ap.ArtistGenres)
                .ThenInclude(ag => ag.Genre)
            .FirstOrDefaultAsync(ap => ap.UserId == userId);
    }

    public async Task<ArtistProfile?> GetByEmailAsync(string email)
    {
        var normalized = email.Trim().ToLower();
        return await _dbSet.FirstOrDefaultAsync(ap => ap.Email != null && ap.Email.ToLower() == normalized);
    }

    public async Task<ArtistProfile?> GetWithRepertoiresAsync(Guid id)
    {
        return await _dbSet
            .Include(ap => ap.Repertoires)
                .ThenInclude(r => r.RepertoireSongs)
                .ThenInclude(rs => rs.Song)
            .Include(ap => ap.ArtistGenres)
                .ThenInclude(ag => ag.Genre)
            .FirstOrDefaultAsync(ap => ap.Id == id);
    }

    public async Task<IEnumerable<ArtistProfile>> GetByGenreAsync(Guid genreId)
    {
        return await _dbSet
            .Include(ap => ap.User)
            .Include(ap => ap.ArtistGenres)
                .ThenInclude(ag => ag.Genre)
            .Where(ap => ap.ArtistGenres.Any(ag => ag.GenreId == genreId))
            .OrderByDescending(ap => ap.Rating)
            .ThenByDescending(ap => ap.Followers)
            .ToListAsync();
    }

    public async Task<IEnumerable<ArtistProfile>> SearchAsync(string query)
    {
        var loweredQuery = query.ToLower();
        return await _dbSet
            .Include(ap => ap.User)
            .Include(ap => ap.ArtistGenres)
                .ThenInclude(ag => ag.Genre)
            .Where(ap => ap.StageName.ToLower().Contains(loweredQuery) ||
                        ap.Description.ToLower().Contains(loweredQuery) ||
                        ap.User.FirstName.ToLower().Contains(loweredQuery) ||
                        ap.User.LastName.ToLower().Contains(loweredQuery))
            .OrderByDescending(ap => ap.IsVerified)
            .ThenByDescending(ap => ap.Rating)
            .ToListAsync();
    }
}

public class VenueProfileRepository : GenericRepository<VenueProfile>, IVenueProfileRepository
{
    public VenueProfileRepository(PediMixDbContext context) : base(context)
    {
    }

    public async Task<VenueProfile?> GetByUserIdAsync(Guid userId)
    {
        return await _dbSet
            .Include(vp => vp.VenueAddress)
            .Include(vp => vp.VenueAmenities)
                .ThenInclude(va => va.Amenity)
            .FirstOrDefaultAsync(vp => vp.UserId == userId);
    }

    public async Task<VenueProfile?> GetWithAddressAsync(Guid id)
    {
        return await _dbSet
            .Include(vp => vp.VenueAddress)
            .Include(vp => vp.VenueAmenities)
                .ThenInclude(va => va.Amenity)
            .FirstOrDefaultAsync(vp => vp.Id == id);
    }

    public async Task<IEnumerable<VenueProfile>> GetByCityAsync(string city)
    {
        return await _dbSet
            .Include(vp => vp.VenueAddress)
            .Include(vp => vp.VenueAmenities)
                .ThenInclude(va => va.Amenity)
            .Where(vp => vp.VenueAddress != null && 
                        vp.VenueAddress.City.ToLower() == city.ToLower())
            .OrderByDescending(vp => vp.Rating)
            .ToListAsync();
    }

    public async Task<IEnumerable<VenueProfile>> SearchAsync(string query)
    {
        var loweredQuery = query.ToLower();
        return await _dbSet
            .Include(vp => vp.VenueAddress)
            .Include(vp => vp.VenueAmenities)
                .ThenInclude(va => va.Amenity)
            .Where(vp => vp.Name.ToLower().Contains(loweredQuery) ||
                        vp.Description.ToLower().Contains(loweredQuery) ||
                        (vp.VenueAddress != null && 
                         (vp.VenueAddress.City.ToLower().Contains(loweredQuery) ||
                          vp.VenueAddress.Neighborhood.ToLower().Contains(loweredQuery))))
            .OrderByDescending(vp => vp.IsVerified)
            .ThenByDescending(vp => vp.Rating)
            .ToListAsync();
    }
}

public class GenreRepository : GenericRepository<Genre>, IGenreRepository
{
    public GenreRepository(PediMixDbContext context) : base(context)
    {
    }

    public async Task<Genre?> GetByNameAsync(string name)
    {
        return await _dbSet
            .FirstOrDefaultAsync(g => g.Name.ToLower() == name.ToLower());
    }

    public async Task<IEnumerable<Genre>> GetMostUsedAsync(int count = 10)
    {
        return await _dbSet
            .Include(g => g.Songs)
            .OrderByDescending(g => g.Songs.Count)
            .Take(count)
            .ToListAsync();
    }
}

public class AddressRepository : GenericRepository<Address>, IAddressRepository
{
    public AddressRepository(PediMixDbContext context) : base(context) { }
}

public class UnitOfWork : IUnitOfWork
{
    private readonly PediMixDbContext _context;
    private Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? _transaction;

    public UnitOfWork(PediMixDbContext context)
    {
        _context = context;
        Users = new UserRepository(_context);
        Addresses = new AddressRepository(_context);
        ArtistProfiles = new ArtistProfileRepository(_context);
        VenueProfiles = new VenueProfileRepository(_context);
        Songs = new SongRepository(_context);
        Repertoires = new RepertoireRepository(_context);
        Events = new EventRepository(_context);
        SongRequests = new SongRequestRepository(_context);
        Genres = new GenreRepository(_context);

        // Music integrations
        SongExternalData = new SongExternalDataRepository(_context);
        SongLyrics = new SongLyricsRepository(_context);
        ExternalArtists = new ExternalArtistRepository(_context);
    }

    public IUserRepository Users { get; }
    public IAddressRepository Addresses { get; }
    public IArtistProfileRepository ArtistProfiles { get; }
    public IVenueProfileRepository VenueProfiles { get; }
    public ISongRepository Songs { get; }
    public IRepertoireRepository Repertoires { get; }
    public IEventRepository Events { get; }
    public ISongRequestRepository SongRequests { get; }
    public IGenreRepository Genres { get; }

    // Music integrations
    public ISongExternalDataRepository SongExternalData { get; }
    public ISongLyricsRepository SongLyrics { get; }
    public IExternalArtistRepository ExternalArtists { get; }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
