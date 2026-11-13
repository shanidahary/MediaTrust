namespace MediaTrust.Report.Models;

public sealed class DetectorResultDto
{
    public Guid Id { get; set; }

    public Guid MediaId { get; set; }

    public Guid? JobId { get; set; }

    public string DetectorName { get; set; } = null!;

    public double Score { get; set; }

    public string Details { get; set; } = null!;

    public DateTimeOffset CreatedAtUtc { get; set; }
}