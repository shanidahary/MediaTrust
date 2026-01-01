using Microsoft.EntityFrameworkCore;

namespace MediaTrust.Detectors.Data;

public sealed class DetectorsDbContext : DbContext
{
    public DetectorsDbContext(DbContextOptions<DetectorsDbContext> options)
        : base(options) { }

    public DbSet<DetectorResult> DetectorResults => Set<DetectorResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DetectorResult>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.DetectorName).IsRequired().HasMaxLength(128);
            e.Property(x => x.Details).IsRequired().HasMaxLength(1024);
            e.Property(x => x.CreatedAtUtc).IsRequired();
        });
    }
}
