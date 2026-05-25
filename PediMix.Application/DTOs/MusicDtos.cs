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
