using System.Text.Json;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PediMix.Application.DTOs;
using PediMix.Application.Interfaces;

namespace PediMix.Infrastructure.Services;

/// <summary>
/// Busca letras com fallback automático:
///   1. Cache (Redis/Memory)
///   2. LRCLib (lrclib.net) — gratuito, sem key, com suporte a LRC
///   3. Lyrically (api.lyrics.ovh) — gratuito, sem key
///   4. Vagalume — fallback BR, requer key
///
/// Falha silenciosa: se nenhuma fonte retornar, devolve null.
/// </summary>
public class LyricsService : ILyricsService
{
    private readonly HttpClient _http;
    private readonly IMusicCacheService _cache;
    private readonly ILogger<LyricsService> _logger;

    private readonly string _vagalumeKey;

    public LyricsService(HttpClient http, IMusicCacheService cache,
        ILogger<LyricsService> logger, IConfiguration config)
    {
        _http = http;
        _cache = cache;
        _logger = logger;
        _vagalumeKey = config["Vagalume:ApiKey"] ?? string.Empty;
    }

    public async Task<LyricsDto?> GetLyricsAsync(string artist, string title, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(artist) || string.IsNullOrWhiteSpace(title))
            return null;

        // 1. Cache
        var cacheKey = $"lyrics:{NormalizeKey(artist)}:{NormalizeKey(title)}";
        var cached = await _cache.GetAsync<LyricsDto>(cacheKey);
        if (cached != null)
        {
            cached.FromCache = true;
            return cached;
        }

        // 2. LRCLib (melhor para letras sincronizadas)
        var lrcLib = await TryLrcLibAsync(artist, title, ct);
        if (lrcLib != null)
        {
            await _cache.SetAsync(cacheKey, lrcLib, TimeSpan.FromDays(30));
            return lrcLib;
        }

        // 3. Lyrically (api.lyrics.ovh)
        var lyrically = await TryLyricallyAsync(artist, title, ct);
        if (lyrically != null)
        {
            await _cache.SetAsync(cacheKey, lyrically, TimeSpan.FromDays(30));
            return lyrically;
        }

        // 4. Vagalume (fallback BR)
        var vagalume = await TryVagalumeAsync(artist, title, ct);
        if (vagalume != null)
        {
            await _cache.SetAsync(cacheKey, vagalume, TimeSpan.FromDays(30));
            return vagalume;
        }

        _logger.LogInformation("[Lyrics] Letra não encontrada para {Artist} - {Title}", artist, title);
        return null;
    }

    // ------------------------------------------------------------
    // LRCLib (gratuito, sem key)
    // Endpoint: https://lrclib.net/api/get?artist_name=...&track_name=...
    // ------------------------------------------------------------
    private async Task<LyricsDto?> TryLrcLibAsync(string artist, string title, CancellationToken ct)
    {
        try
        {
            var encodedArtist = Uri.EscapeDataString(artist);
            var encodedTitle = Uri.EscapeDataString(title);
            var url = $"https://lrclib.net/api/get?artist_name={encodedArtist}&track_name={encodedTitle}";

            using var response = await _http.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            var plain = doc.RootElement.TryGetProperty("plainLyrics", out var plainEl)
                ? plainEl.GetString()
                : null;

            var syncedRaw = doc.RootElement.TryGetProperty("syncedLyrics", out var syncedEl)
                ? syncedEl.GetString()
                : null;

            var syncedLines = ParseSyncedLyrics(syncedRaw);
            var hasSynced = syncedLines.Count > 0;

            var content = !string.IsNullOrWhiteSpace(plain)
                ? plain!.Trim()
                : string.Join("\n", syncedLines.Select(s => s.Text).Where(t => !string.IsNullOrWhiteSpace(t)));

            if (string.IsNullOrWhiteSpace(content)) return null;

            return new LyricsDto
            {
                Artist = artist,
                Title = title,
                Content = content,
                IsSynced = hasSynced,
                SyncedLines = hasSynced ? syncedLines : null,
                Source = "lrclib",
                FromCache = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Lyrics][LRCLib] falhou para {Artist} - {Title}", artist, title);
            return null;
        }
    }

    // ------------------------------------------------------------
    // Lyrically (lyrics.ovh — gratuito, sem key)
    // Endpoint: https://api.lyrics.ovh/v1/{artist}/{title}
    // ------------------------------------------------------------
    private async Task<LyricsDto?> TryLyricallyAsync(string artist, string title, CancellationToken ct)
    {
        try
        {
            var encodedArtist = Uri.EscapeDataString(artist);
            var encodedTitle = Uri.EscapeDataString(title);
            var url = $"https://api.lyrics.ovh/v1/{encodedArtist}/{encodedTitle}";

            using var response = await _http.GetAsync(url, ct);
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
            _logger.LogWarning(ex, "[Lyrics][Lyrically] falhou para {Artist} - {Title}", artist, title);
            return null;
        }
    }

    // ------------------------------------------------------------
    // Vagalume (fallback, foco em músicas BR — requer apiKey)
    // Endpoint: https://api.vagalume.com.br/search.php
    // ------------------------------------------------------------
    private async Task<LyricsDto?> TryVagalumeAsync(string artist, string title, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_vagalumeKey)) return null;

        try
        {
            var encodedArtist = Uri.EscapeDataString(artist);
            var encodedTitle = Uri.EscapeDataString(title);
            var url = $"https://api.vagalume.com.br/search.php?art={encodedArtist}&mus={encodedTitle}&apikey={_vagalumeKey}";

            using var response = await _http.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("mus", out var musArray)
                || musArray.ValueKind != JsonValueKind.Array)
                return null;

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
            _logger.LogWarning(ex, "[Lyrics][Vagalume] falhou para {Artist} - {Title}", artist, title);
            return null;
        }
    }

    private static string NormalizeKey(string value)
        => value.ToLowerInvariant().Replace(" ", "_").Replace("/", "_");

    private static List<SyncedLineDto> ParseSyncedLyrics(string? syncedLyrics)
    {
        var result = new List<SyncedLineDto>();
        if (string.IsNullOrWhiteSpace(syncedLyrics)) return result;

        var lines = syncedLyrics.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var line in lines)
        {
            if (!line.StartsWith('[')) continue;

            var lastTagEnd = line.LastIndexOf(']');
            if (lastTagEnd <= 0 || lastTagEnd >= line.Length - 1) continue;

            var text = line[(lastTagEnd + 1)..].Trim();
            if (string.IsNullOrWhiteSpace(text)) continue;

            var cursor = 0;
            while (cursor < line.Length && line[cursor] == '[')
            {
                var end = line.IndexOf(']', cursor + 1);
                if (end < 0) break;

                var tag = line[(cursor + 1)..end];
                if (TryParseLrcTimestamp(tag, out var seconds))
                {
                    result.Add(new SyncedLineDto
                    {
                        TimeSeconds = seconds,
                        Text = text
                    });
                }

                cursor = end + 1;
                if (cursor >= line.Length || line[cursor] != '[') break;
            }
        }

        return result.OrderBy(x => x.TimeSeconds).ToList();
    }

    private static bool TryParseLrcTimestamp(string value, out double seconds)
    {
        seconds = 0;
        var parts = value.Split(':', 2);
        if (parts.Length != 2) return false;

        if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var minutes))
            return false;

        if (!double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var secPart))
            return false;

        seconds = (minutes * 60d) + secPart;
        return seconds >= 0;
    }
}
