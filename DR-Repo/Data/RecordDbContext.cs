using Microsoft.EntityFrameworkCore;

namespace DR.Data;

public class RecordDbContext : DbContext
{
    public RecordDbContext(DbContextOptions<RecordDbContext> options) : base(options)
    {
    }

    public DbSet<Record> Records => Set<Record>();
    public DbSet<Track> Tracks => Set<Track>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Record>(entity =>
        {
            entity.ToTable("Records");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Name).IsRequired();
            entity.Property(r => r.ReleaseYear).IsRequired();
            entity.Property(r => r.Genre).IsRequired();
            entity.Property(r => r.Artist).IsRequired();
            entity.Property(r => r.trackCount).IsRequired();
            entity.Property(d => d.Duration).IsRequired();
        });

        modelBuilder.Entity<Track>(entity =>
        {
            entity.ToTable("tracks");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Name).IsRequired();
            entity.Property(t => t.Artist).IsRequired();
            entity.Property(t => t.PlayedAt).IsRequired();
            entity.Property(t => t.Channel).IsRequired();
        });
    }
}
