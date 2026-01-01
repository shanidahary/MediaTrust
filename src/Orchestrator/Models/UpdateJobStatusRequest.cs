namespace MediaTrust.Orchestrator.Models;

public sealed class UpdateJobStatusRequest
{
    public string Status { get; init; } = default!;
}
