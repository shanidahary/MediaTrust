namespace MediaTrust.Report.Models;

public sealed class MediaReportDto
{
    public Guid MediaId { get; set; }
    public string OverallStatus { get; set; } = null!;
    public double RiskScore { get; set; }
    public IReadOnlyList<DetectorResultDto> Results { get; set; } = [];
}