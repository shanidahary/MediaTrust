using Microsoft.EntityFrameworkCore;

namespace MediaTrust.Ingest.Data;

public sealed class IngestDbContext : DbContext
{
    public IngestDbContext(DbContextOptions<IngestDbContext> options) : base(options) { }

    public DbSet<MediaItem> MediaItems => Set<MediaItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MediaItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.FileName).HasMaxLength(512).IsRequired();
            e.Property(x => x.ContentType).HasMaxLength(256).IsRequired();
            e.Property(x => x.ObjectKey).HasMaxLength(1024).IsRequired();
            e.Property(x => x.UploadedAtUtc).IsRequired();
        });
    }
}
