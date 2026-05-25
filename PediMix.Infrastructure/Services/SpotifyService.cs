using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PediMix.Application.DTOs;
using PediMix.Application.Interfaces;

namespace PediMix.Infrastructure.Services;

/// <summary>
/// Cliente Spotify Web API usando Client Credentials Flow (server-to-server).
/// Token mantido em memória, renovado automaticamente 60s antes de expirar.
/// Todas as buscas passam por cache (Redis ou IMemoryCache).
/// </summary>
public class SpotifyService : ISpotifyService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<SpotifyService> _logger;
    private readonly IMusicCacheService _cache;

    // Token em memória (process-level), renovado automaticamente
    private string? _accessToken;
    private DateTime _tokenExpiry = DateTime.MinValue;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    private const string TokenUrl = "https://accounts.spotify.com/api/token";
    private const string ApiBase = "https://api.spotify.com/v1";
    private const string Market = "BR";
    private const int MinSearchLimit = 1;
    private const int MaxSearchLimit = 10;

    public SpotifyService(HttpClient http, IConfiguration config,
        ILogger<SpotifyService> logger, IMusicCacheService cache)
    {
        _http = http;
        _config = config;
        _logger = logger;
        _cache = cache;
    }

    // ============================================================
    // Token management (Client Credentials Flow — sem usuário)
    // ============================================================
    private async Task<string?> GetTokenAsync(CancellationToken ct)
    {
        if (_accessToken != null && DateTime.UtcNow < _tokenExpiry)
            return _accessToken;

        await _tokenLock.WaitAsync(ct);
        try
        {
            if (_accessToken != null && DateTime.UtcNow < _tokenExpiry)
                return _accessToken;

            var clientId = _config["Spotify:ClientId"];
            var clientSecret = _config["Spotify:ClientSecret"];

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            {
                _logger.LogWarning("[Spotify] ClientId/ClientSecret não configurados — service ficará inativo.");
                return null;
            }

            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

            var request = new HttpRequestMessage(HttpMethod.Post, TokenUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            request.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            });

            var response = await _http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[Spotify] Falha ao obter token: {Status}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            _accessToken = doc.RootElement.GetProperty("access_token").GetString()!;
            var expiresIn = doc.RootElement.GetProperty("expires_in").GetInt32();
            _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 60); // renova 60s antes

            return _accessToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private async Task<HttpResponseMessage?> GetSpotifyAsync(string url, CancellationToken ct)
    {
        var token = await GetTokenAsync(ct);
        if (token is null) return null;

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _http.SendAsync(request, ct);
    }

    // ============================================================
    // Busca
    // ============================================================
    public async Task<List<SpotifyTrackDto>> SearchTracksAsync(string query, int limit = 20, CancellationToken ct = default)
    {
        var normalizedLimit = NormalizeLimit(limit);
        var cacheKey = $"spotify:tracks:{query.ToLowerInvariant()}:{normalizedLimit}";
        var cached = await _cache.GetAsync<List<SpotifyTrackDto>>(cacheKey);
        if (cached != null) return cached;

        var encoded = Uri.EscapeDataString(query);
        var url = $"{ApiBase}/search?q={encoded}&type=track&limit={normalizedLimit}&market={Market}";

        try
        {
            var response = await GetSpotifyAsync(url, ct);
            if (response is null || !response.IsSuccessStatusCode)
                return new List<SpotifyTrackDto>();

            var json = await response.Content.ReadAsStringAsync(ct);
            var tracks = ParseTracks(json);

            await _cache.SetAsync(cacheKey, tracks, TimeSpan.FromDays(1));
            return tracks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Spotify] SearchTracks falhou para query: {Query}", query);
            return new List<SpotifyTrackDto>();
        }
    }

    public async Task<List<SpotifyArtistDto>> SearchArtistsAsync(string query, int limit = 10, CancellationToken ct = default)
    {
        var normalizedLimit = NormalizeLimit(limit);
        var cacheKey = $"spotify:artists:{query.ToLowerInvariant()}:{normalizedLimit}";
        var cached = await _cache.GetAsync<List<SpotifyArtistDto>>(cacheKey);
        if (cached != null) return cached;

        var encoded = Uri.EscapeDataString(query);
        var url = $"{ApiBase}/search?q={encoded}&type=artist&limit={normalizedLimit}&market={Market}";

        try
        {
            var response = await GetSpotifyAsync(url, ct);
            if (response is null || !response.IsSuccessStatusCode)
                return new List<SpotifyArtistDto>();

            var json = await response.Content.ReadAsStringAsync(ct);
            var artists = ParseArtists(json);

            await _cache.SetAsync(cacheKey, artists, TimeSpan.FromDays(7));
            return artists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Spotify] SearchArtists falhou para query: {Query}", query);
            return new List<SpotifyArtistDto>();
        }
    }

    public async Task<List<SpotifyTrackDto>> GetArtistTopTracksAsync(string spotifyArtistId, CancellationToken ct = default)
    {
        var cacheKey = $"spotify:artist:toptracks:{spotifyArtistId}";
        var cached = await _cache.GetAsync<List<SpotifyTrackDto>>(cacheKey);
        if (cached != null) return cached;

        var url = $"{ApiBase}/artists/{spotifyArtistId}/top-tracks?market={Market}";

        try
        {
            var response = await GetSpotifyAsync(url, ct);
            if (response is null || !response.IsSuccessStatusCode)
                return new List<SpotifyTrackDto>();

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var tracksArray = doc.RootElement.GetProperty("tracks");
            var tracks = ParseTracksArray(tracksArray);

            await _cache.SetAsync(cacheKey, tracks, TimeSpan.FromDays(7));
            return tracks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Spotify] GetArtistTopTracks falhou para artistId: {ArtistId}", spotifyArtistId);
            return new List<SpotifyTrackDto>();
        }
    }

    public async Task<MusicSearchResultDto> SearchAsync(string query, CancellationToken ct = default)
    {
        var tracksTask = SearchTracksAsync(query, 20, ct);
        var artistsTask = SearchArtistsAsync(query, 5, ct);
        await Task.WhenAll(tracksTask, artistsTask);

        return new MusicSearchResultDto
        {
            Query = query,
            Tracks = tracksTask.Result,
            Artists = artistsTask.Result,
            Total = tracksTask.Result.Count + artistsTask.Result.Count
        };
    }

    private int NormalizeLimit(int requestedLimit)
    {
        var normalized = Math.Clamp(requestedLimit, MinSearchLimit, MaxSearchLimit);
        if (normalized != requestedLimit)
        {
            _logger.LogInformation(
                "[Spotify] limit {RequestedLimit} ajustado para {NormalizedLimit}.",
                requestedLimit,
                normalized);
        }

        return normalized;
    }

    // ============================================================
    // Parsers
    // ============================================================
    private static List<SpotifyTrackDto> ParseTracks(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var items = doc.RootElement
            .GetProperty("tracks")
            .GetProperty("items");

        return ParseTracksArray(items);
    }

    private static List<SpotifyTrackDto> ParseTracksArray(JsonElement items)
    {
        var result = new List<SpotifyTrackDto>();
        foreach (var item in items.EnumerateArray())
        {
            try
            {
                var artists = item.GetProperty("artists");
                var firstArtist = artists.EnumerateArray().FirstOrDefault();
                var artistName = firstArtist.ValueKind == JsonValueKind.Undefined
                    ? string.Empty
                    : firstArtist.GetProperty("name").GetString() ?? string.Empty;

                var album = item.GetProperty("album");
                var images = album.GetProperty("images");
                var firstImage = images.EnumerateArray().FirstOrDefault();
                var imageUrl = firstImage.ValueKind == JsonValueKind.Undefined
                    ? null
                    : firstImage.GetProperty("url").GetString();

                result.Add(new SpotifyTrackDto
                {
                    SpotifyId = item.GetProperty("id").GetString() ?? string.Empty,
                    SpotifyUri = item.GetProperty("uri").GetString() ?? string.Empty,
                    Title = item.GetProperty("name").GetString() ?? string.Empty,
                    Artist = artistName,
                    Album = album.GetProperty("name").GetString() ?? string.Empty,
                    AlbumImageUrl = imageUrl,
                    PreviewUrl = item.TryGetProperty("preview_url", out var prev) && prev.ValueKind != JsonValueKind.Null
                        ? prev.GetString() : null,
                    DurationMs = item.TryGetProperty("duration_ms", out var dur) ? dur.GetInt32() : 0,
                    Popularity = item.TryGetProperty("popularity", out var pop) ? pop.GetInt32() : 0
                });
            }
            catch
            {
                /* item malformado, ignorar */
            }
        }
        return result;
    }

    private static List<SpotifyArtistDto> ParseArtists(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var items = doc.RootElement
            .GetProperty("artists")
            .GetProperty("items");

        var result = new List<SpotifyArtistDto>();
        foreach (var item in items.EnumerateArray())
        {
            try
            {
                var images = item.GetProperty("images");
                var firstImage = images.EnumerateArray().FirstOrDefault();
                var imageUrl = firstImage.ValueKind == JsonValueKind.Undefined
                    ? null
                    : firstImage.GetProperty("url").GetString();

                var genres = item.GetProperty("genres")
                    .EnumerateArray()
                    .Select(g => g.GetString() ?? string.Empty)
                    .Where(g => !string.IsNullOrEmpty(g))
                    .ToList();

                result.Add(new SpotifyArtistDto
                {
                    SpotifyId = item.GetProperty("id").GetString() ?? string.Empty,
                    Name = item.GetProperty("name").GetString() ?? string.Empty,
                    Genres = genres,
                    Followers = item.GetProperty("followers").GetProperty("total").GetInt32(),
                    Popularity = item.GetProperty("popularity").GetInt32(),
                    ImageUrl = imageUrl
                });
            }
            catch
            {
                /* item malformado, ignorar */
            }
        }
        return result;
    }
}
