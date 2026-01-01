namespace MediaTrust.Detectors.Models;

public sealed class DetectorResultDto
{
    public Guid MediaId { get; init; }
    public string DetectorName { get; init; } = default!;
    public double Score { get; init; }
    public string Details { get; init; } = default!;
    public DateTimeOffset CreatedAtUtc { get; init; }
}
