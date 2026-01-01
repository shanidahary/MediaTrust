namespace MediaTrust.Report.Data;

public sealed class DetectorResult
{
    public Guid Id { get; set; }
    public Guid MediaId { get; set; }
    public string DetectorName { get; set; } = null!;
    public double Score { get; set; }
    public string Details { get; set; } = null!;
    public DateTimeOffset CreatedAtUtc { get; set; }
}