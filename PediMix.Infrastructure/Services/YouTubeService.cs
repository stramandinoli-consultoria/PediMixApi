using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PediMix.Application.DTOs;
using PediMix.Application.Interfaces;

namespace PediMix.Infrastructure.Services;

/// <summary>
/// Cliente YouTube Data API v3.
/// Cache agressivo (24h) — cada search custa 100 unidades de quota
/// e o limite gratuito é 10.000/dia.
/// </summary>
public class YouTubeService : IYouTubeService
{
    private readonly HttpClient _http;
    private readonly ILogger<YouTubeService> _logger;
    private readonly IMusicCacheService _cache;
    private readonly string _apiKey;

    private const string SearchUrl = "https://www.googleapis.com/youtube/v3/search";

    public YouTubeService(HttpClient http, ILogger<YouTubeService> logger,
        IMusicCacheService cache, IConfiguration config)
    {
        _http = http;
        _logger = logger;
        _cache = cache;
        _apiKey = config["YouTube:ApiKey"] ?? string.Empty;
    }

    public async Task<List<YouTubeVideoDto>> SearchVideosAsync(
        string query, string type = "clip", int maxResults = 5, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning("[YouTube] ApiKey não configurada — service ficará inativo.");
            return new List<YouTubeVideoDto>();
        }

        if (string.IsNullOrWhiteSpace(query))
            return new List<YouTubeVideoDto>();

        // Cache key normalizado
        var cacheKey = $"youtube:{type}:{query.ToLowerInvariant()}:{maxResults}";
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
        var url = $"{SearchUrl}?part=snippet&type=video&q={encoded}"
                + $"&maxResults={maxResults}&key={_apiKey}";

        try
        {
            using var response = await _http.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[YouTube] Search retornou {Status} para query: {Query}",
                    response.StatusCode, query);
                return new List<YouTubeVideoDto>();
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("items", out var items)
                || items.ValueKind != JsonValueKind.Array)
                return new List<YouTubeVideoDto>();

            var videos = new List<YouTubeVideoDto>();
            foreach (var item in items.EnumerateArray())
            {
                try
                {
                    if (!item.TryGetProperty("id", out var idEl)) continue;
                    if (!idEl.TryGetProperty("videoId", out var videoIdEl)) continue;
                    var id = videoIdEl.GetString();
                    if (string.IsNullOrWhiteSpace(id)) continue;

                    var snippet = item.GetProperty("snippet");
                    var thumbs = snippet.GetProperty("thumbnails");

                    var thumb = thumbs.TryGetProperty("high", out var high)
                        ? high
                        : thumbs.TryGetProperty("medium", out var medium)
                            ? medium
                            : thumbs.GetProperty("default");

                    videos.Add(new YouTubeVideoDto
                    {
                        VideoId = id,
                        Title = snippet.GetProperty("title").GetString() ?? string.Empty,
                        ChannelTitle = snippet.GetProperty("channelTitle").GetString() ?? string.Empty,
                        ThumbnailUrl = thumb.GetProperty("url").GetString() ?? string.Empty,
                        WatchUrl = $"https://www.youtube.com/watch?v={id}",
                        Description = snippet.TryGetProperty("description", out var desc)
                            ? desc.GetString() : null
                    });
                }
                catch
                {
                    /* item malformado, ignorar */
                }
            }

            await _cache.SetAsync(cacheKey, videos, TimeSpan.FromDays(1));
            return videos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[YouTube] Search falhou para query: {Query}", query);
            return new List<YouTubeVideoDto>();
        }
    }
}
