using Microsoft.EntityFrameworkCore;

namespace MediaTrust.Report.Data;

public sealed class ReportDbContext : DbContext
{
    public ReportDbContext(DbContextOptions<ReportDbContext> options)
        : base(options) { }

    public DbSet<DetectorResult> DetectorResults => Set<DetectorResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DetectorResult>(e =>
        {
            e.HasKey(x => x.Id);
            e.ToTable("DetectorResults");
        });
    }
}