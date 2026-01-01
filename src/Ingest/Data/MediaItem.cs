namespace MediaTrust.Ingest.Data;

public sealed class MediaItem
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public long SizeBytes { get; set; }
    public string ObjectKey { get; set; } = default!;
    public DateTimeOffset UploadedAtUtc { get; set; }
}
