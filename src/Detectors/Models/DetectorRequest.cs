namespace MediaTrust.Detectors.Models;

public sealed class DetectorRequest
{
    public Guid JobId { get; set; }
    public Guid MediaId { get; set; }
    public string ObjectKey { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public long SizeBytes { get; set; }
}
