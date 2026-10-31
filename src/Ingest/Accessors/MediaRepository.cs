using MediaTrust.Ingest.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MediaTrust.Ingest.Accessors;

public sealed class MediaRepository : IMediaRepository
{
    private readonly IngestDbContext _db;
    private readonly ILogger<MediaRepository> _logger;

    public MediaRepository(
        IngestDbContext db,
        ILogger<MediaRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task AddAsync(MediaItem item, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation(
                "Saving media item {MediaId} to database",
                item.Id);

            _db.MediaItems.Add(item);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Media item {MediaId} saved successfully",
                item.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to save media item {MediaId} to database",
                item.Id);

            throw;
        }
    }

    public async Task<bool> ExistsByFileNameAsync(
    string fileName,
    CancellationToken ct)
    {
        return await _db.MediaItems
            .AnyAsync(x => x.FileName == fileName, ct);
    }

    public async Task<MediaItem?> GetByFileNameAsync(
    string fileName,
    CancellationToken ct)
{
    try
    {
        return await _db.MediaItems
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.FileName == fileName,
                ct);
    }
    catch (Exception ex)
    {
        _logger.LogError(
            ex,
            "Failed to get media item by file name {FileName}",
            fileName);

        throw;
    }
}
}