namespace MediaTrust.Orchestrator.Models;

public sealed class CreateJobHttpRequest
{
    public Guid MediaId { get; set; }
    public string ObjectKey { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public long SizeBytes { get; set; }
}