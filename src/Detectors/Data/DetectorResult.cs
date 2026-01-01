namespace MediaTrust.Detectors.Data;

public sealed class DetectorResult
{
    public Guid Id { get; set; }
    public Guid MediaId { get; set; }
    public string DetectorName { get; set; } = default!;
    public double Score { get; set; }
    public string Details { get; set; } = default!;
    public DateTimeOffset CreatedAtUtc { get; set; }
}
