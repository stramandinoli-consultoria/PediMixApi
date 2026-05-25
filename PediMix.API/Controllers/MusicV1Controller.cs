using Microsoft.AspNetCore.Mvc;
using PediMix.API.Models;
using PediMix.Application.DTOs;
using PediMix.Application.Interfaces;

namespace PediMix.API.Controllers;

// ============================================================
// /api/v1/music — Busca de músicas e letras
// ============================================================
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
    /// Busca unificada (tracks + artistas) no Spotify.
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

        if (limit <= 0 || limit > 50) limit = 20;

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

// ============================================================
// /api/v1/artist-search — Busca de artistas externos
// ============================================================
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
    /// Busca artistas no Spotify.
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<List<SpotifyArtistDto>>>> Search(
        [FromQuery] string query,
        [FromQuery] int limit = 10,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest(ApiResponse<List<SpotifyArtistDto>>.Fail("query é obrigatório."));

        if (limit <= 0 || limit > 50) limit = 10;

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
        if (string.IsNullOrWhiteSpace(spotifyId))
            return BadRequest(ApiResponse<List<SpotifyTrackDto>>.Fail("spotifyId é obrigatório."));

        var result = await _spotify.GetArtistTopTracksAsync(spotifyId, ct);
        return Ok(ApiResponse<List<SpotifyTrackDto>>.Ok(result));
    }
}

// ============================================================
// /api/v1/youtube — Vídeos do YouTube (clip, lyric, live)
// ============================================================
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

        if (maxResults <= 0 || maxResults > 25) maxResults = 5;

        var normalizedType = type?.ToLowerInvariant() switch
        {
            "lyric" => "lyric",
            "live" => "live",
            _ => "clip",
        };

        var result = await _youtube.SearchVideosAsync(query, normalizedType, maxResults, ct);
        return Ok(ApiResponse<List<YouTubeVideoDto>>.Ok(result));
    }
}
