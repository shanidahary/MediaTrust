namespace MediaTrust.Orchestrator.Models;

public sealed class AnalysisJobDto
{
    public Guid JobId { get; init; }
    public Guid MediaId { get; init; }
    public string ObjectKey { get; init; } = default!;
    public string Status { get; init; } = default!;
    public DateTimeOffset CreatedAtUtc { get; init; }
}
