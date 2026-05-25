using PediMix.Application.DTOs;

namespace PediMix.Application.Interfaces;

public interface ISpotifyService
{
    /// <summary>Busca faixas no Spotify.</summary>
    Task<List<SpotifyTrackDto>> SearchTracksAsync(string query, int limit = 20, CancellationToken ct = default);

    /// <summary>Busca artistas no Spotify.</summary>
    Task<List<SpotifyArtistDto>> SearchArtistsAsync(string query, int limit = 10, CancellationToken ct = default);

    /// <summary>Retorna as top tracks de um artista pelo SpotifyId.</summary>
    Task<List<SpotifyTrackDto>> GetArtistTopTracksAsync(string spotifyArtistId, CancellationToken ct = default);

    /// <summary>Busca resultado unificado (tracks + artistas).</summary>
    Task<MusicSearchResultDto> SearchAsync(string query, CancellationToken ct = default);
}

public interface ILyricsService
{
    /// <summary>
    /// Busca a letra de uma música.
    /// Fluxo: Cache -> Lyrically -> Vagalume -> null
    /// </summary>
    Task<LyricsDto?> GetLyricsAsync(string artist, string title, CancellationToken ct = default);
}

public interface IYouTubeService
{
    /// <summary>Busca vídeos no YouTube (clipe, lyric video, live).</summary>
    Task<List<YouTubeVideoDto>> SearchVideosAsync(string query, string type = "clip", int maxResults = 5, CancellationToken ct = default);
}

public interface IMusicCacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan expiration);
    Task RemoveAsync(string key);
    Task<bool> ExistsAsync(string key);
}
