using Microsoft.EntityFrameworkCore;
using PediMix.Domain.Entities;
using PediMix.Infrastructure.Configurations;

namespace PediMix.Infrastructure.Data;

public class PediMixDbContext : DbContext
{
    public PediMixDbContext(DbContextOptions<PediMixDbContext> options) : base(options)
    {
    }

    // Users
    public DbSet<User> Users { get; set; }
    public DbSet<UserPreferences> UserPreferences { get; set; }
    public DbSet<Address> Addresses { get; set; }

    // Profiles
    public DbSet<ArtistProfile> ArtistProfiles { get; set; }
    public DbSet<VenueProfile> VenueProfiles { get; set; }
    public DbSet<VenueAddress> VenueAddresses { get; set; }

    // Content
    public DbSet<Genre> Genres { get; set; }
    public DbSet<ArtistGenre> ArtistGenres { get; set; }
    public DbSet<Song> Songs { get; set; }
    public DbSet<Repertoire> Repertoires { get; set; }
    public DbSet<RepertoireSong> RepertoireSongs { get; set; }

    // Events
    public DbSet<Event> Events { get; set; }
    public DbSet<EventGenre> EventGenres { get; set; }
    public DbSet<EventUser> EventUsers { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<EventTag> EventTags { get; set; }

    // Requests
    public DbSet<SongRequest> SongRequests { get; set; }

    // Venues
    public DbSet<Amenity> Amenities { get; set; }
    public DbSet<VenueAmenity> VenueAmenities { get; set; }

    // Music integrations (Spotify, Lyrically, Vagalume, YouTube)
    public DbSet<SongExternalData> SongExternalData { get; set; }
    public DbSet<SongLyrics> SongLyrics { get; set; }
    public DbSet<ExternalArtist> ExternalArtists { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new ArtistProfileConfiguration());
        modelBuilder.ApplyConfiguration(new VenueProfileConfiguration());
        modelBuilder.ApplyConfiguration(new SongConfiguration());
        modelBuilder.ApplyConfiguration(new RepertoireConfiguration());
        modelBuilder.ApplyConfiguration(new EventConfiguration());
        modelBuilder.ApplyConfiguration(new SongRequestConfiguration());
        modelBuilder.ApplyConfiguration(new GenreConfiguration());
        modelBuilder.ApplyConfiguration(new SongExternalDataConfiguration());
        modelBuilder.ApplyConfiguration(new SongLyricsConfiguration());
        modelBuilder.ApplyConfiguration(new ExternalArtistConfiguration());

        // Configure many-to-many relationships
        ConfigureManyToManyRelationships(modelBuilder);

        // Configure indexes
        ConfigureIndexes(modelBuilder);
    }

    private static void ConfigureManyToManyRelationships(ModelBuilder modelBuilder)
    {
        // ArtistProfile - Genre (many-to-many)
        modelBuilder.Entity<ArtistGenre>()
            .HasKey(ag => new { ag.ArtistProfileId, ag.GenreId });

        modelBuilder.Entity<ArtistGenre>()
            .HasOne(ag => ag.ArtistProfile)
            .WithMany(a => a.ArtistGenres)
            .HasForeignKey(ag => ag.ArtistProfileId);

        modelBuilder.Entity<ArtistGenre>()
            .HasOne(ag => ag.Genre)
            .WithMany(g => g.ArtistGenres)
            .HasForeignKey(ag => ag.GenreId);

        // Repertoire - Song (many-to-many)
        modelBuilder.Entity<RepertoireSong>()
            .HasKey(rs => new { rs.RepertoireId, rs.SongId });

        modelBuilder.Entity<RepertoireSong>()
            .HasOne(rs => rs.Repertoire)
            .WithMany(r => r.RepertoireSongs)
            .HasForeignKey(rs => rs.RepertoireId);

        modelBuilder.Entity<RepertoireSong>()
            .HasOne(rs => rs.Song)
            .WithMany(s => s.RepertoireSongs)
            .HasForeignKey(rs => rs.SongId);

        // Event - Genre (many-to-many)
        modelBuilder.Entity<EventGenre>()
            .HasKey(eg => new { eg.EventId, eg.GenreId });

        modelBuilder.Entity<EventGenre>()
            .HasOne(eg => eg.Event)
            .WithMany(e => e.EventGenres)
            .HasForeignKey(eg => eg.EventId);

        modelBuilder.Entity<EventGenre>()
            .HasOne(eg => eg.Genre)
            .WithMany()
            .HasForeignKey(eg => eg.GenreId);

        // Event - Tag (many-to-many)
        modelBuilder.Entity<EventTag>()
            .HasKey(et => new { et.EventId, et.TagId });

        modelBuilder.Entity<EventTag>()
            .HasOne(et => et.Event)
            .WithMany(e => e.EventTags)
            .HasForeignKey(et => et.EventId);

        modelBuilder.Entity<EventTag>()
            .HasOne(et => et.Tag)
            .WithMany(t => t.EventTags)
            .HasForeignKey(et => et.TagId);

        // Event - User interactions (many-to-many)
        modelBuilder.Entity<EventUser>()
            .HasKey(eu => new { eu.EventId, eu.UserId, eu.InteractionType });

        modelBuilder.Entity<EventUser>()
            .HasOne(eu => eu.Event)
            .WithMany(e => e.EventUsers)
            .HasForeignKey(eu => eu.EventId);

        modelBuilder.Entity<EventUser>()
            .HasOne(eu => eu.User)
            .WithMany(u => u.EventUsers)
            .HasForeignKey(eu => eu.UserId);

        // Venue - Amenity (many-to-many)
        modelBuilder.Entity<VenueAmenity>()
            .HasKey(va => new { va.VenueProfileId, va.AmenityId });

        modelBuilder.Entity<VenueAmenity>()
            .HasOne(va => va.VenueProfile)
            .WithMany(v => v.VenueAmenities)
            .HasForeignKey(va => va.VenueProfileId);

        modelBuilder.Entity<VenueAmenity>()
            .HasOne(va => va.Amenity)
            .WithMany(a => a.VenueAmenities)
            .HasForeignKey(va => va.AmenityId);
    }

    private static void ConfigureIndexes(ModelBuilder modelBuilder)
    {
        // User indexes
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        // Song indexes
        modelBuilder.Entity<Song>()
            .HasIndex(s => new { s.Title, s.Artist });

        modelBuilder.Entity<Song>()
            .HasIndex(s => s.GenreId);

        // Event indexes
        modelBuilder.Entity<Event>()
            .HasIndex(e => e.Date);

        modelBuilder.Entity<Event>()
            .HasIndex(e => e.Status);

        modelBuilder.Entity<Event>()
            .HasIndex(e => e.Category);

        // Artist profile indexes
        modelBuilder.Entity<ArtistProfile>()
            .HasIndex(ap => ap.StageName);

        // Venue profile indexes
        modelBuilder.Entity<VenueProfile>()
            .HasIndex(vp => vp.Name);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is Domain.Common.BaseEntity && (
                e.State == EntityState.Added ||
                e.State == EntityState.Modified));

        foreach (var entityEntry in entries)
        {
            var entity = (Domain.Common.BaseEntity)entityEntry.Entity;

            if (entityEntry.State == EntityState.Added)
            {
                entity.CreatedAt = DateTime.UtcNow;
            }

            entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}
