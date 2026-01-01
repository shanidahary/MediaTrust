using Microsoft.EntityFrameworkCore;

namespace MediaTrust.Orchestrator.Data;

public sealed class OrchestratorDbContext : DbContext
{
    public OrchestratorDbContext(DbContextOptions<OrchestratorDbContext> options)
        : base(options) { }

    public DbSet<AnalysisJob> AnalysisJobs => Set<AnalysisJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AnalysisJob>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).IsRequired().HasMaxLength(64);
            e.Property(x => x.ObjectKey).IsRequired().HasMaxLength(1024);
            e.Property(x => x.CreatedAtUtc).IsRequired();
        });
    }
}

