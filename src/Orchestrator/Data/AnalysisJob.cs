namespace MediaTrust.Orchestrator.Data;

public sealed class AnalysisJob
{
    public Guid Id { get; set; }
    public Guid MediaId { get; set; }
    public string ObjectKey { get; set; } = default!;
    public string Status { get; set; } = default!;
    public DateTimeOffset CreatedAtUtc { get; set; }
}
