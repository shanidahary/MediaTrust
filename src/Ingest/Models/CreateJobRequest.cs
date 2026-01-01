namespace MediaTrust.Ingest.Models;

public sealed class CreateJobRequest
{
    public Guid MediaId { get; set; }
    public string ObjectKey { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public long SizeBytes { get; set; }
}