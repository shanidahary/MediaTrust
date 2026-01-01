using MediaTrust.Ingest.Data;

namespace MediaTrust.Ingest.Accessors;

public interface IMediaRepository
{
    Task AddAsync(MediaItem item, CancellationToken ct);
    Task<bool> ExistsByFileNameAsync(string fileName, CancellationToken ct);
}
