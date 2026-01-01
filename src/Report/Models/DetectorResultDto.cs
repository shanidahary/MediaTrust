namespace MediaTrust.Report.Models;

public sealed class DetectorResultDto
{
    public string DetectorName { get; set; } = null!;
    public double Score { get; set; }
    public string Details { get; set; } = null!;
}
