namespace MediaTrust.Ingest.Models;

public sealed class UploadMediaResult
{
    public Guid MediaId { get; init; }
    public string ObjectKey { get; init; } = default!;
    public string FileName { get; init; } = default!;
    public string ContentType { get; init; } = default!;
    public long SizeBytes { get; init; }
    public DateTimeOffset UploadedAtUtc { get; init; }
}
