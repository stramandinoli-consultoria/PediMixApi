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
