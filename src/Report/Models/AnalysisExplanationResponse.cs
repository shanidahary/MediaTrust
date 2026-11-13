namespace MediaTrust.Report.Models;

public sealed class AnalysisExplanationResponse
{
    public Guid JobId { get; set; }

    public Guid MediaId { get; set; }

    public double RiskScore { get; set; }

    public string Explanation { get; set; } = default!;
}