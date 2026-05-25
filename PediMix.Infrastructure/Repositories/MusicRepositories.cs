using Microsoft.EntityFrameworkCore;
using PediMix.Application.Interfaces;
using PediMix.Domain.Entities;
using PediMix.Infrastructure.Data;

namespace PediMix.Infrastructure.Repositories;

public class SongExternalDataRepository
    : GenericRepository<SongExternalData>, ISongExternalDataRepository
{
    public SongExternalDataRepository(PediMixDbContext context) : base(context) { }

    public async Task<SongExternalData?> GetBySongIdAsync(Guid songId)
        => await _dbSet.FirstOrDefaultAsync(x => x.SongId == songId);

    public async Task<SongExternalData?> GetBySpotifyIdAsync(string spotifyId)
        => await _dbSet.FirstOrDefaultAsync(x => x.SpotifyId == spotifyId);
}

public class SongLyricsRepository : GenericRepository<SongLyrics>, ISongLyricsRepository
{
    public SongLyricsRepository(PediMixDbContext context) : base(context) { }

    public async Task<SongLyrics?> GetBySongIdAsync(Guid songId)
        => await _dbSet
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(x => x.SongId == songId);

    public async Task<IEnumerable<SongLyrics>> GetAllBySongIdAsync(Guid songId)
        => await _dbSet
            .Where(x => x.SongId == songId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
}

public class ExternalArtistRepository
    : GenericRepository<ExternalArtist>, IExternalArtistRepository
{
    public ExternalArtistRepository(PediMixDbContext context) : base(context) { }

    public async Task<ExternalArtist?> GetBySpotifyIdAsync(string spotifyId)
        => await _dbSet.FirstOrDefaultAsync(x => x.SpotifyId == spotifyId);

    public async Task<IEnumerable<ExternalArtist>> SearchByNameAsync(string query, int limit = 10)
    {
        var q = (query ?? string.Empty).ToLowerInvariant();
        return await _dbSet
            .Where(x => x.Name.ToLower().Contains(q))
            .OrderByDescending(x => x.Popularity)
            .Take(limit)
            .ToListAsync();
    }
}
