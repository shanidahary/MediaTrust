namespace MediaTrust.Report.Accessors;

public interface IAiAccessor
{
    Task<string> GenerateExplanationAsync(
        string detectorName,
        double score,
        string details,
        CancellationToken ct);
}