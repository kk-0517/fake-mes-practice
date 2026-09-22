using Microsoft.EntityFrameworkCore;

namespace FakeMes.Api.Domain;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Station> Stations => Set<Station>();
    public DbSet<TrackRecord> TrackRecords => Set<TrackRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Station>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Code).HasMaxLength(32);
            e.Property(x => x.Name).HasMaxLength(64);
        });

        modelBuilder.Entity<TrackRecord>(e =>
        {
            e.Property(x => x.StationCode).HasMaxLength(32);
            e.Property(x => x.Barcode).HasMaxLength(64);
            e.HasIndex(x => new { x.Barcode, x.Time });
        });
    }
}
