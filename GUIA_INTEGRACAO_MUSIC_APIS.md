# PEDIMIX - Guia de Integração com APIs Musicais

Este guia detalha como evoluir o projeto PediMix para integrar APIs musicais externas
(Spotify, Lyrically, Vagalume, YouTube), respeitando exatamente a arquitetura atual:
Clean Architecture + DDD + MediatR + AutoMapper + Repository Pattern + UnitOfWork + MySQL.

---

## 1) Contexto da Arquitetura Atual

### O que já existe:
- `.NET 6` + `ASP.NET Core Web API`
- `Clean Architecture` com camadas: `Domain`, `Application`, `Infrastructure`, `API`
- `MediatR` para CQRS (Commands + Queries)
- `AutoMapper` para DTOs
- `EF Core` + `MySQL` (Railway)
- `JWT Bearer` com refresh token
- `Repository Pattern` + `Unit of Work` via `IUnitOfWork`
- `HttpClient` via DI (já usado no `ViaCepService`)

### O que precisamos adicionar:
- Spotify Web API (busca, metadados, preview)
- Lyrically API (letras sincronizadas)
- Vagalume API (fallback letras em PT-BR)
- YouTube Data API v3 (clipes, lyric videos)
- Redis (cache distribuído)
- Polly (retry + circuit breaker)
- Background Service (warm-up de cache)

---

## 2) Packages NuGet a instalar

### PediMix.Infrastructure.csproj
```xml
<!-- Redis -->
<PackageReference Include="StackExchange.Redis" Version="2.7.33" />
<PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="6.0.36" />

<!-- Resilência HTTP -->
<PackageReference Include="Polly" Version="7.2.4" />
<PackageReference Include="Polly.Extensions.Http" Version="3.0.0" />
<PackageReference Include="Microsoft.Extensions.Http.Polly" Version="6.0.36" />
```

### PediMix.API.csproj
```xml
<!-- Serilog -->
<PackageReference Include="Serilog.AspNetCore" Version="6.1.0" />
<PackageReference Include="Serilog.Sinks.Console" Version="4.1.0" />
<PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
```

---

## 3) Novas Entidades de Domínio

Adicionar em `PediMix.Domain/Entities/MusicEntities.cs`:

```csharp
using PediMix.Domain.Common;

namespace PediMix.Domain.Entities;

/// <summary>
/// Enriquece a Song existente com dados externos do Spotify.
/// Relacionamento: 1 Song -> 0..1 SongExternalData
/// </summary>
public class SongExternalData : BaseEntity
{
    public Guid SongId { get; set; }
    public string? SpotifyId { get; set; }
    public string? SpotifyUri { get; set; }
    public string? AlbumName { get; set; }
    public string? AlbumImageUrl { get; set; }
    public string? PreviewUrl { get; set; }
    public int? DurationMs { get; set; }
    public int? Popularity { get; set; }
    public bool HasLyricsAvailable { get; set; } = false;
    public DateTime? LastSyncedAt { get; set; }

    public virtual Song Song { get; set; } = null!;
}

/// <summary>
/// Letras de músicas, podendo vir de Lyrically ou Vagalume.
/// Fonte rastreável para fallback.
/// </summary>
public class SongLyrics : BaseEntity
{
    public Guid SongId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty; // "lyrically" | "vagalume" | "manual"
    public bool IsSynced { get; set; } = false;          // letra com timestamps sincronizados
    public string? SyncedLyricsJson { get; set; }        // JSON com array de { time, text }
    public DateTime? CachedUntil { get; set; }

    public virtual Song Song { get; set; } = null!;
}

/// <summary>
/// Artista externo do Spotify — distinto do ArtistProfile interno.
/// Usado para busca global, sem vínculo obrigatório com usuário.
/// </summary>
public class ExternalArtist : BaseEntity
{
    public string SpotifyId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? GenresJson { get; set; }    // JSON array de strings
    public int Followers { get; set; } = 0;
    public int Popularity { get; set; } = 0;
    public string? ImageUrl { get; set; }
    public DateTime? LastSyncedAt { get; set; }
}
```

---

## 4) Novos DTOs

Adicionar em `PediMix.Application/DTOs/MusicDtos.cs`:

```csharp
namespace PediMix.Application.DTOs;

// --- Spotify ---

public class SpotifyTrackDto
{
    public string SpotifyId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string Album { get; set; } = string.Empty;
    public string? AlbumImageUrl { get; set; }
    public string? PreviewUrl { get; set; }
    public int DurationMs { get; set; }
    public int Popularity { get; set; }
    public bool HasLyrics { get; set; }
    public string SpotifyUri { get; set; } = string.Empty;
}

public class SpotifyArtistDto
{
    public string SpotifyId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<string> Genres { get; set; } = new();
    public int Followers { get; set; }
    public int Popularity { get; set; }
    public string? ImageUrl { get; set; }
    public List<SpotifyTrackDto> TopTracks { get; set; } = new();
}

// --- Lyrics ---

public class LyricsDto
{
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsSynced { get; set; }
    public List<SyncedLineDto>? SyncedLines { get; set; }
    public string Source { get; set; } = string.Empty;
    public bool FromCache { get; set; }
}

public class SyncedLineDto
{
    public double TimeSeconds { get; set; }
    public string Text { get; set; } = string.Empty;
}

// --- YouTube ---

public class YouTubeVideoDto
{
    public string VideoId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ChannelTitle { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string WatchUrl { get; set; } = string.Empty;
    public string? Description { get; set; }
}

// --- Music Search ---

public class MusicSearchResultDto
{
    public List<SpotifyTrackDto> Tracks { get; set; } = new();
    public List<SpotifyArtistDto> Artists { get; set; } = new();
    public int Total { get; set; }
    public string Query { get; set; } = string.Empty;
    public bool FromCache { get; set; }
}
```

---

## 5) Interfaces dos Serviços Externos

Adicionar em `PediMix.Application/Interfaces/IMusicServices.cs`:

```csharp
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
    /// Fluxo: Redis -> Lyrically -> Vagalume -> null
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
```

---

## 6) Implementação dos Serviços

### 6.1 SpotifyService

Adicionar em `PediMix.Infrastructure/Services/SpotifyService.cs`:

```csharp
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PediMix.Application.DTOs;
using PediMix.Application.Interfaces;

namespace PediMix.Infrastructure.Services;

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

    public SpotifyService(HttpClient http, IConfiguration config,
        ILogger<SpotifyService> logger, IMusicCacheService cache)
    {
        _http = http;
        _config = config;
        _logger = logger;
        _cache = cache;
    }

    // -------------------------------------------------------
    // Token management (Client Credentials Flow — sem usuário)
    // -------------------------------------------------------
    private async Task<string> GetTokenAsync()
    {
        if (_accessToken != null && DateTime.UtcNow < _tokenExpiry)
            return _accessToken;

        await _tokenLock.WaitAsync();
        try
        {
            if (_accessToken != null && DateTime.UtcNow < _tokenExpiry)
                return _accessToken;

            var clientId = _config["Spotify:ClientId"]
                ?? throw new InvalidOperationException("Spotify:ClientId not configured.");
            var clientSecret = _config["Spotify:ClientSecret"]
                ?? throw new InvalidOperationException("Spotify:ClientSecret not configured.");

            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

            var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            request.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            });

            var response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
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

    private async Task<HttpResponseMessage> GetSpotifyAsync(string url)
    {
        var token = await GetTokenAsync();
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _http.SendAsync(request);
    }

    // -------------------------------------------------------
    // Busca
    // -------------------------------------------------------
    public async Task<List<SpotifyTrackDto>> SearchTracksAsync(string query, int limit = 20, CancellationToken ct = default)
    {
        var cacheKey = $"spotify:tracks:{query.ToLower()}:{limit}";
        var cached = await _cache.GetAsync<List<SpotifyTrackDto>>(cacheKey);
        if (cached != null) return cached;

        var encoded = Uri.EscapeDataString(query);
        var url = $"https://api.spotify.com/v1/search?q={encoded}&type=track&limit={limit}&market=BR";

        try
        {
            var response = await GetSpotifyAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var tracks = ParseTracks(json);

            await _cache.SetAsync(cacheKey, tracks, TimeSpan.FromDays(1));
            return tracks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Spotify SearchTracks failed for query: {Query}", query);
            return new List<SpotifyTrackDto>();
        }
    }

    public async Task<List<SpotifyArtistDto>> SearchArtistsAsync(string query, int limit = 10, CancellationToken ct = default)
    {
        var cacheKey = $"spotify:artists:{query.ToLower()}:{limit}";
        var cached = await _cache.GetAsync<List<SpotifyArtistDto>>(cacheKey);
        if (cached != null) return cached;

        var encoded = Uri.EscapeDataString(query);
        var url = $"https://api.spotify.com/v1/search?q={encoded}&type=artist&limit={limit}&market=BR";

        try
        {
            var response = await GetSpotifyAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var artists = ParseArtists(json);

            await _cache.SetAsync(cacheKey, artists, TimeSpan.FromDays(7));
            return artists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Spotify SearchArtists failed for query: {Query}", query);
            return new List<SpotifyArtistDto>();
        }
    }

    public async Task<List<SpotifyTrackDto>> GetArtistTopTracksAsync(string spotifyArtistId, CancellationToken ct = default)
    {
        var cacheKey = $"spotify:artist:toptracks:{spotifyArtistId}";
        var cached = await _cache.GetAsync<List<SpotifyTrackDto>>(cacheKey);
        if (cached != null) return cached;

        var url = $"https://api.spotify.com/v1/artists/{spotifyArtistId}/top-tracks?market=BR";

        try
        {
            var response = await GetSpotifyAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var tracksArray = doc.RootElement.GetProperty("tracks");
            var tracks = ParseTracksArray(tracksArray);

            await _cache.SetAsync(cacheKey, tracks, TimeSpan.FromDays(7));
            return tracks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Spotify GetArtistTopTracks failed for artistId: {ArtistId}", spotifyArtistId);
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

    // -------------------------------------------------------
    // Parsers
    // -------------------------------------------------------
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
                var artistName = artists.EnumerateArray().First().GetProperty("name").GetString() ?? "";
                var album = item.GetProperty("album");
                var images = album.GetProperty("images");
                var imageUrl = images.EnumerateArray().FirstOrDefault().GetProperty("url").GetString();

                result.Add(new SpotifyTrackDto
                {
                    SpotifyId = item.GetProperty("id").GetString() ?? "",
                    SpotifyUri = item.GetProperty("uri").GetString() ?? "",
                    Title = item.GetProperty("name").GetString() ?? "",
                    Artist = artistName,
                    Album = album.GetProperty("name").GetString() ?? "",
                    AlbumImageUrl = imageUrl,
                    PreviewUrl = item.TryGetProperty("preview_url", out var prev) && prev.ValueKind != JsonValueKind.Null
                        ? prev.GetString() : null,
                    DurationMs = item.GetProperty("duration_ms").GetInt32(),
                    Popularity = item.GetProperty("popularity").GetInt32()
                });
            }
            catch { /* item malformado, ignorar */ }
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
                var imageUrl = images.EnumerateArray().FirstOrDefault().GetProperty("url").GetString();

                var genres = item.GetProperty("genres")
                    .EnumerateArray()
                    .Select(g => g.GetString() ?? "")
                    .ToList();

                result.Add(new SpotifyArtistDto
                {
                    SpotifyId = item.GetProperty("id").GetString() ?? "",
                    Name = item.GetProperty("name").GetString() ?? "",
                    Genres = genres,
                    Followers = item.GetProperty("followers").GetProperty("total").GetInt32(),
                    Popularity = item.GetProperty("popularity").GetInt32(),
                    ImageUrl = imageUrl
                });
            }
            catch { /* item malformado, ignorar */ }
        }
        return result;
    }
}
```

### 6.2 LyricsService (com fallback Vagalume)

Adicionar em `PediMix.Infrastructure/Services/LyricsService.cs`:

```csharp
using Microsoft.Extensions.Logging;
using PediMix.Application.DTOs;
using PediMix.Application.Interfaces;
using System.Net.Http.Json;
using System.Text.Json;

namespace PediMix.Infrastructure.Services;

public class LyricsService : ILyricsService
{
    private readonly HttpClient _http;
    private readonly IMusicCacheService _cache;
    private readonly ILogger<LyricsService> _logger;

    // Vagalume requer API key (free tier disponível em vagalume.com.br/api)
    private readonly string _vagalumeKey;

    public LyricsService(HttpClient http, IMusicCacheService cache,
        ILogger<LyricsService> logger,
        Microsoft.Extensions.Configuration.IConfiguration config)
    {
        _http = http;
        _cache = cache;
        _logger = logger;
        _vagalumeKey = config["Vagalume:ApiKey"] ?? "";
    }

    public async Task<LyricsDto?> GetLyricsAsync(string artist, string title, CancellationToken ct = default)
    {
        // 1. Cache
        var cacheKey = $"lyrics:{NormalizeKey(artist)}:{NormalizeKey(title)}";
        var cached = await _cache.GetAsync<LyricsDto>(cacheKey);
        if (cached != null)
        {
            cached.FromCache = true;
            return cached;
        }

        // 2. Lyrically
        var lyrically = await TryLyricallyAsync(artist, title, ct);
        if (lyrically != null)
        {
            await _cache.SetAsync(cacheKey, lyrically, TimeSpan.FromDays(30));
            return lyrically;
        }

        // 3. Vagalume (fallback)
        var vagalume = await TryVagalumeAsync(artist, title, ct);
        if (vagalume != null)
        {
            await _cache.SetAsync(cacheKey, vagalume, TimeSpan.FromDays(30));
            return vagalume;
        }

        _logger.LogWarning("Lyrics not found for {Artist} - {Title}", artist, title);
        return null;
    }

    // -------------------------------------------------------
    // Lyrically (lyricsovh é gratuito e sem key)
    // Endpoint: https://api.lyrics.ovh/v1/{artist}/{title}
    // -------------------------------------------------------
    private async Task<LyricsDto?> TryLyricallyAsync(string artist, string title, CancellationToken ct)
    {
        try
        {
            var encodedArtist = Uri.EscapeDataString(artist);
            var encodedTitle = Uri.EscapeDataString(title);
            var url = $"https://api.lyrics.ovh/v1/{encodedArtist}/{encodedTitle}";

            var response = await _http.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("lyrics", out var lyricsEl)) return null;
            var content = lyricsEl.GetString();
            if (string.IsNullOrWhiteSpace(content)) return null;

            return new LyricsDto
            {
                Artist = artist,
                Title = title,
                Content = content.Trim(),
                IsSynced = false,
                Source = "lyrically",
                FromCache = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lyrically failed for {Artist} - {Title}", artist, title);
            return null;
        }
    }

    // -------------------------------------------------------
    // Vagalume (fallback, foco em músicas BR)
    // Endpoint: https://api.vagalume.com.br/search.php
    // -------------------------------------------------------
    private async Task<LyricsDto?> TryVagalumeAsync(string artist, string title, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_vagalumeKey)) return null;

        try
        {
            var encodedArtist = Uri.EscapeDataString(artist);
            var encodedTitle = Uri.EscapeDataString(title);
            var url = $"https://api.vagalume.com.br/search.php?art={encodedArtist}&mus={encodedTitle}&apikey={_vagalumeKey}";

            var response = await _http.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("mus", out var musArray)) return null;
            var mus = musArray.EnumerateArray().FirstOrDefault();
            if (mus.ValueKind == JsonValueKind.Undefined) return null;

            if (!mus.TryGetProperty("text", out var textEl)) return null;
            var content = textEl.GetString();
            if (string.IsNullOrWhiteSpace(content)) return null;

            return new LyricsDto
            {
                Artist = artist,
                Title = title,
                Content = content.Trim(),
                IsSynced = false,
                Source = "vagalume",
                FromCache = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Vagalume failed for {Artist} - {Title}", artist, title);
            return null;
        }
    }

    private static string NormalizeKey(string value)
        => value.ToLowerInvariant().Replace(" ", "_");
}
```

### 6.3 YouTubeService

Adicionar em `PediMix.Infrastructure/Services/YouTubeService.cs`:

```csharp
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PediMix.Application.DTOs;
using PediMix.Application.Interfaces;

namespace PediMix.Infrastructure.Services;

public class YouTubeService : IYouTubeService
{
    private readonly HttpClient _http;
    private readonly ILogger<YouTubeService> _logger;
    private readonly IMusicCacheService _cache;
    private readonly string _apiKey;

    public YouTubeService(HttpClient http, ILogger<YouTubeService> logger,
        IMusicCacheService cache, IConfiguration config)
    {
        _http = http;
        _logger = logger;
        _cache = cache;
        _apiKey = config["YouTube:ApiKey"] ?? "";
    }

    public async Task<List<YouTubeVideoDto>> SearchVideosAsync(string query, string type = "clip", int maxResults = 5, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning("YouTube:ApiKey not configured.");
            return new List<YouTubeVideoDto>();
        }

        var cacheKey = $"youtube:{type}:{Uri.EscapeDataString(query)}:{maxResults}";
        var cached = await _cache.GetAsync<List<YouTubeVideoDto>>(cacheKey);
        if (cached != null) return cached;

        // Enriquece a query conforme o tipo de vídeo pedido
        var enrichedQuery = type switch
        {
            "lyric" => $"{query} lyric video",
            "live"  => $"{query} live",
            _       => $"{query} clipe oficial",
        };

        var encoded = Uri.EscapeDataString(enrichedQuery);
        var url = $"https://www.googleapis.com/youtube/v3/search"
                + $"?part=snippet&type=video&q={encoded}"
                + $"&maxResults={maxResults}&key={_apiKey}";

        try
        {
            var response = await _http.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            var items = doc.RootElement.GetProperty("items");
            var videos = new List<YouTubeVideoDto>();

            foreach (var item in items.EnumerateArray())
            {
                var id = item.GetProperty("id").GetProperty("videoId").GetString() ?? "";
                var snippet = item.GetProperty("snippet");
                var thumbs = snippet.GetProperty("thumbnails");
                var thumb = thumbs.TryGetProperty("high", out var high) ? high : thumbs.GetProperty("default");

                videos.Add(new YouTubeVideoDto
                {
                    VideoId = id,
                    Title = snippet.GetProperty("title").GetString() ?? "",
                    ChannelTitle = snippet.GetProperty("channelTitle").GetString() ?? "",
                    ThumbnailUrl = thumb.GetProperty("url").GetString() ?? "",
                    WatchUrl = $"https://www.youtube.com/watch?v={id}",
                    Description = snippet.TryGetProperty("description", out var desc)
                        ? desc.GetString() : null
                });
            }

            await _cache.SetAsync(cacheKey, videos, TimeSpan.FromDays(1));
            return videos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "YouTube search failed for query: {Query}", query);
            return new List<YouTubeVideoDto>();
        }
    }
}
```

### 6.4 MusicCacheService (Redis)

Adicionar em `PediMix.Infrastructure/Services/MusicCacheService.cs`:

```csharp
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using PediMix.Application.Interfaces;

namespace PediMix.Infrastructure.Services;

public class MusicCacheService : IMusicCacheService
{
    private readonly IDistributedCache _redis;
    private readonly ILogger<MusicCacheService> _logger;

    public MusicCacheService(IDistributedCache redis, ILogger<MusicCacheService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var data = await _redis.GetStringAsync(key);
            if (data == null) return default;
            return JsonSerializer.Deserialize<T>(data);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis GET failed for key: {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiration)
    {
        try
        {
            var data = JsonSerializer.Serialize(value);
            await _redis.SetStringAsync(key, data, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis SET failed for key: {Key}", key);
            // falha silenciosa no cache — a API continua funcionando
        }
    }

    public async Task RemoveAsync(string key)
    {
        try { await _redis.RemoveAsync(key); }
        catch (Exception ex) { _logger.LogWarning(ex, "Redis REMOVE failed: {Key}", key); }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            var data = await _redis.GetStringAsync(key);
            return data != null;
        }
        catch { return false; }
    }
}
```

---

## 7) Políticas de Resilência com Polly

Adicionar em `PediMix.Infrastructure/Policies/HttpPolicies.cs`:

```csharp
using Polly;
using Polly.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace PediMix.Infrastructure.Policies;

public static class HttpPolicies
{
    /// <summary>
    /// Retry com exponential backoff: 1s, 2s, 4s.
    /// Usado em todas as chamadas a APIs externas.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> RetryPolicy()
        => HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (outcome, timeSpan, attempt, _) =>
                {
                    // Log opcional — pode injetar ILogger via factory no DI
                    Console.WriteLine($"[Polly] Retry {attempt} after {timeSpan.TotalSeconds}s due to: {outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString()}");
                });

    /// <summary>
    /// Circuit Breaker: abre após 5 falhas consecutivas por 30s.
    /// Evita banimento por flood em caso de indisponibilidade da API.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> CircuitBreakerPolicy()
        => HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30));

    /// <summary>Combina retry + circuit breaker (use WrapAsync).</summary>
    public static IAsyncPolicy<HttpResponseMessage> CombinedPolicy()
        => Policy.WrapAsync(RetryPolicy(), CircuitBreakerPolicy());
}
```

---

## 8) Novos Controllers

### 8.1 MusicV1Controller

Adicionar em `PediMix.API/Controllers/V1Controllers.cs` ou em arquivo separado
`PediMix.API/Controllers/MusicV1Controller.cs`:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PediMix.API.Models;
using PediMix.Application.DTOs;
using PediMix.Application.Interfaces;

namespace PediMix.API.Controllers;

[ApiController]
[Route("api/v1/music")]
public class MusicV1Controller : ControllerBase
{
    private readonly ISpotifyService _spotify;
    private readonly ILyricsService _lyrics;

    public MusicV1Controller(ISpotifyService spotify, ILyricsService lyrics)
    {
        _spotify = spotify;
        _lyrics = lyrics;
    }

    /// <summary>
    /// Busca músicas e artistas no Spotify.
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<MusicSearchResultDto>>> Search(
        [FromQuery] string query,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest(ApiResponse<MusicSearchResultDto>.Fail("query é obrigatório."));

        var result = await _spotify.SearchAsync(query, ct);
        return Ok(ApiResponse<MusicSearchResultDto>.Ok(result));
    }

    /// <summary>
    /// Busca somente faixas no Spotify.
    /// </summary>
    [HttpGet("tracks")]
    public async Task<ActionResult<ApiResponse<List<SpotifyTrackDto>>>> SearchTracks(
        [FromQuery] string query,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest(ApiResponse<List<SpotifyTrackDto>>.Fail("query é obrigatório."));

        var result = await _spotify.SearchTracksAsync(query, limit, ct);
        return Ok(ApiResponse<List<SpotifyTrackDto>>.Ok(result));
    }

    /// <summary>
    /// Busca letra de uma música (Lyrically → Vagalume → null).
    /// </summary>
    [HttpGet("lyrics")]
    public async Task<ActionResult<ApiResponse<LyricsDto>>> GetLyrics(
        [FromQuery] string artist,
        [FromQuery] string title,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(artist) || string.IsNullOrWhiteSpace(title))
            return BadRequest(ApiResponse<LyricsDto>.Fail("artist e title são obrigatórios."));

        var result = await _lyrics.GetLyricsAsync(artist, title, ct);
        if (result == null)
            return NotFound(ApiResponse<LyricsDto>.Fail("Letra não encontrada."));

        return Ok(ApiResponse<LyricsDto>.Ok(result));
    }
}

[ApiController]
[Route("api/v1/artist-search")]
public class ArtistSearchV1Controller : ControllerBase
{
    private readonly ISpotifyService _spotify;

    public ArtistSearchV1Controller(ISpotifyService spotify)
    {
        _spotify = spotify;
    }

    /// <summary>
    /// Busca artistas no Spotify com top tracks.
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<List<SpotifyArtistDto>>>> Search(
        [FromQuery] string query,
        [FromQuery] int limit = 10,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest(ApiResponse<List<SpotifyArtistDto>>.Fail("query é obrigatório."));

        var result = await _spotify.SearchArtistsAsync(query, limit, ct);
        return Ok(ApiResponse<List<SpotifyArtistDto>>.Ok(result));
    }

    /// <summary>
    /// Retorna as top tracks de um artista pelo SpotifyId.
    /// </summary>
    [HttpGet("{spotifyId}/top-tracks")]
    public async Task<ActionResult<ApiResponse<List<SpotifyTrackDto>>>> TopTracks(
        string spotifyId,
        CancellationToken ct)
    {
        var result = await _spotify.GetArtistTopTracksAsync(spotifyId, ct);
        return Ok(ApiResponse<List<SpotifyTrackDto>>.Ok(result));
    }
}

[ApiController]
[Route("api/v1/youtube")]
public class YouTubeV1Controller : ControllerBase
{
    private readonly IYouTubeService _youtube;

    public YouTubeV1Controller(IYouTubeService youtube)
    {
        _youtube = youtube;
    }

    /// <summary>
    /// Busca vídeos no YouTube.
    /// type: clip | lyric | live
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<List<YouTubeVideoDto>>>> Search(
        [FromQuery] string query,
        [FromQuery] string type = "clip",
        [FromQuery] int maxResults = 5,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest(ApiResponse<List<YouTubeVideoDto>>.Fail("query é obrigatório."));

        var result = await _youtube.SearchVideosAsync(query, type, maxResults, ct);
        return Ok(ApiResponse<List<YouTubeVideoDto>>.Ok(result));
    }
}
```

---

## 9) Registro no Program.cs

Adicionar no `PediMix.API/Program.cs` após os serviços existentes:

```csharp
using PediMix.Infrastructure.Policies;
using PediMix.Infrastructure.Services;
using PediMix.Application.Interfaces;

// Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:ConnectionString"]
        ?? Environment.GetEnvironmentVariable("REDIS_URL")
        ?? "localhost:6379";
});
builder.Services.AddSingleton<IMusicCacheService, MusicCacheService>();

// Spotify — HttpClient com Polly
builder.Services.AddHttpClient<ISpotifyService, SpotifyService>()
    .AddPolicyHandler(HttpPolicies.RetryPolicy())
    .AddPolicyHandler(HttpPolicies.CircuitBreakerPolicy());

// Lyrics — HttpClient com Polly
builder.Services.AddHttpClient<ILyricsService, LyricsService>()
    .AddPolicyHandler(HttpPolicies.RetryPolicy())
    .AddPolicyHandler(HttpPolicies.CircuitBreakerPolicy());

// YouTube — HttpClient com Polly
builder.Services.AddHttpClient<IYouTubeService, YouTubeService>()
    .AddPolicyHandler(HttpPolicies.RetryPolicy())
    .AddPolicyHandler(HttpPolicies.CircuitBreakerPolicy());
```

---

## 10) Variáveis de Ambiente e appsettings

### appsettings.json (adicionar):

```json
{
  "Spotify": {
    "ClientId": "",
    "ClientSecret": ""
  },
  "YouTube": {
    "ApiKey": ""
  },
  "Vagalume": {
    "ApiKey": ""
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  }
}
```

### Railway — variáveis de ambiente:

```env
Spotify__ClientId=seu_client_id
Spotify__ClientSecret=seu_client_secret
YouTube__ApiKey=sua_api_key
Vagalume__ApiKey=sua_api_key
Redis__ConnectionString=redis://default:senha@host:porta
```

> O Railway fornece Redis como addon gratuito. A URL vem em `REDIS_URL`.
> O `MusicCacheService` já está preparado para ler `Redis__ConnectionString` ou `REDIS_URL`.

---

## 11) Como obter as chaves de cada API

### Spotify Web API (gratuito, sem limites agressivos)
1. Acesse: https://developer.spotify.com/dashboard
2. Crie um app → pegue `Client ID` e `Client Secret`
3. Configure `Redirect URIs` (para Client Credentials Flow não precisa)
4. Rate limit: ~100 req/s por app (generoso para uso interno)

### lyrics.ovh (Lyrically — completamente gratuito, sem key)
- Endpoint: `https://api.lyrics.ovh/v1/{artist}/{title}`
- Sem necessidade de cadastro ou chave
- Rate limit informal: não documentado, mas estável para uso moderado

### Vagalume (gratuito com cadastro)
1. Acesse: https://api.vagalume.com.br/
2. Crie conta → gere API Key
3. Rate limit: 50 req/dia no free tier (use apenas como fallback)

### YouTube Data API v3 (gratuito com quota)
1. Acesse: https://console.cloud.google.com
2. Crie projeto → ative "YouTube Data API v3"
3. Crie credencial → `API Key`
4. Quota gratuita: 10.000 unidades/dia
5. Cada search custa 100 unidades → 100 buscas/dia grátis
6. Para economizar: cache agressivo de 24h por query

---

## 12) Endpoints novos no endpoints.ts do React

Adicionar no arquivo `src/api/endpoints.ts` existente, dentro do objeto `API`:

```ts
// Adicionar junto aos outros domains:
music: {
  search: '/api/v1/music/search',
  tracks: '/api/v1/music/tracks',
  lyrics: '/api/v1/music/lyrics',
},
artistSearch: {
  search: '/api/v1/artist-search/search',
  topTracks: (spotifyId: string) => `/api/v1/artist-search/${spotifyId}/top-tracks`,
},
youtube: {
  search: '/api/v1/youtube/search',
},
```

---

## 13) Uso no React

### Busca de músicas

```ts
// Busca geral (tracks + artistas)
const resp = await http.get(API.music.search, { params: { query: 'Jorge Henrique' } });

// Só tracks
const tracks = await http.get(API.music.tracks, { params: { query: 'saudade', limit: 20 } });
```

### Buscar letra

```ts
const lyrics = await http.get(API.music.lyrics, {
  params: { artist: 'Jorge Henrique', title: 'Amor de Verão' }
});
// resp.data.data.content — texto da letra
// resp.data.data.source  — "lyrically" | "vagalume"
// resp.data.data.fromCache — boolean
```

### Buscar clipe no YouTube

```ts
const videos = await http.get(API.youtube.search, {
  params: { query: 'Jorge Henrique Amor de Verão', type: 'clip', maxResults: 3 }
});
// resp.data.data[0].watchUrl — link direto para o YouTube
```

### Top tracks de artista

```ts
const topTracks = await http.get(API.artistSearch.topTracks('spotify_artist_id'));
```

---

## 14) Mapa de telas → chamadas musicais

### Tela: Detalhe de música (no repertório)
- `GET /api/v1/music/lyrics?artist=...&title=...`
- `GET /api/v1/youtube/search?query=...&type=clip`

### Tela: Adicionar música ao repertório (busca)
- `GET /api/v1/music/search?query=...` (retorna Spotify)
- Ao selecionar: salvar `SpotifyId`, `Title`, `Artist`, `AlbumImageUrl`, `PreviewUrl` localmente

### Tela: Perfil de artista externo
- `GET /api/v1/artist-search/search?query=nomeArtista`
- `GET /api/v1/artist-search/{spotifyId}/top-tracks`

### Tela: Karaokê / letra ao vivo
- `GET /api/v1/music/lyrics?artist=...&title=...`
- Usar `syncedLines` se `isSynced = true` para sincronizar com o tempo

---

## 15) Estratégia de cache por TTL

| Tipo            | Chave Redis                            | TTL      |
|-----------------|----------------------------------------|----------|
| Busca Spotify   | `spotify:tracks:{query}:{limit}`       | 1 dia    |
| Artistas        | `spotify:artists:{query}:{limit}`      | 7 dias   |
| Top tracks      | `spotify:artist:toptracks:{spotifyId}` | 7 dias   |
| Letra           | `lyrics:{artist}:{title}`              | 30 dias  |
| YouTube search  | `youtube:{type}:{query}:{max}`         | 1 dia    |

> Estratégia: cache-aside (try cache → se miss, busca API → salva cache).
> Falha silenciosa no Redis — a API continua funcionando sem cache.

---

## 16) Preparação futura: IA para cifras

As interfaces e estrutura de dados já estão prontas para expansão.
Quando o módulo IA for desenvolvido, basta implementar:

```csharp
// PediMix.Application/Interfaces/IMusicServices.cs (adicionar futuramente)
public interface IMusicAiService
{
    /// <summary>
    /// Detecta acordes e gera cifra a partir de áudio ou SpotifyId.
    /// Integrar com: ACRCloud, AudD, ou modelo local com ML.NET
    /// </summary>
    Task<ChordChartDto?> GenerateChordChartAsync(string spotifyId, CancellationToken ct = default);
    Task<int> DetectBpmAsync(string spotifyId, CancellationToken ct = default);
    Task<string?> DetectMusicalKeyAsync(string spotifyId, CancellationToken ct = default);
}

// Adapters preparados para partituras (implementação futura)
public interface ISheetMusicService
{
    Task<string?> GetTabUrlAsync(string artist, string title);  // Songsterr
    Task<string?> GetScoreUrlAsync(string artist, string title); // MuseScore
}
```

---

## 17) Resumo dos arquivos a criar

```
PediMix.Domain/Entities/
  └── MusicEntities.cs          ← SongExternalData, SongLyrics, ExternalArtist

PediMix.Application/DTOs/
  └── MusicDtos.cs               ← SpotifyTrackDto, LyricsDto, YouTubeVideoDto, etc.

PediMix.Application/Interfaces/
  └── IMusicServices.cs          ← ISpotifyService, ILyricsService, IYouTubeService, IMusicCacheService

PediMix.Infrastructure/Services/
  ├── SpotifyService.cs
  ├── LyricsService.cs
  ├── YouTubeService.cs
  └── MusicCacheService.cs

PediMix.Infrastructure/Policies/
  └── HttpPolicies.cs            ← Retry + CircuitBreaker (Polly)

PediMix.API/Controllers/
  └── MusicV1Controller.cs       ← /api/v1/music, /api/v1/artist-search, /api/v1/youtube
```

---

## 18) Próximos passos recomendados

1. Instalar os NuGet packages (seção 2).
2. Criar os arquivos listados na seção 17.
3. Registrar os serviços no `Program.cs` (seção 9).
4. Adicionar variáveis de ambiente Railway (seção 10).
5. Criar a migration para `SongExternalData` e `SongLyrics` no banco MySQL.
6. Testar localmente via Swagger em `/swagger`.
7. Atualizar o `endpoints.ts` do React (seção 12).
8. Implementar as telas de busca e letra usando os novos services.
