using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PediMix.Domain.Entities;

namespace PediMix.Infrastructure.Configurations;

public class SongExternalDataConfiguration : IEntityTypeConfiguration<SongExternalData>
{
    public void Configure(EntityTypeBuilder<SongExternalData> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SpotifyId)
            .HasMaxLength(64);

        builder.Property(x => x.SpotifyUri)
            .HasMaxLength(128);

        builder.Property(x => x.AlbumName)
            .HasMaxLength(300);

        builder.Property(x => x.AlbumImageUrl)
            .HasMaxLength(500);

        builder.Property(x => x.PreviewUrl)
            .HasMaxLength(500);

        builder.HasIndex(x => x.SpotifyId);

        builder.HasOne(x => x.Song)
            .WithOne()
            .HasForeignKey<SongExternalData>(x => x.SongId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.SongId).IsUnique();
    }
}

public class SongLyricsConfiguration : IEntityTypeConfiguration<SongLyrics>
{
    public void Configure(EntityTypeBuilder<SongLyrics> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Content)
            .IsRequired()
            .HasColumnType("longtext");

        builder.Property(x => x.Source)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(x => x.SyncedLyricsJson)
            .HasColumnType("longtext");

        builder.HasIndex(x => x.SongId);

        builder.HasOne(x => x.Song)
            .WithMany()
            .HasForeignKey(x => x.SongId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ExternalArtistConfiguration : IEntityTypeConfiguration<ExternalArtist>
{
    public void Configure(EntityTypeBuilder<ExternalArtist> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SpotifyId)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.GenresJson)
            .HasColumnType("longtext");

        builder.Property(x => x.ImageUrl)
            .HasMaxLength(500);

        builder.HasIndex(x => x.SpotifyId).IsUnique();
        builder.HasIndex(x => x.Name);
    }
}
