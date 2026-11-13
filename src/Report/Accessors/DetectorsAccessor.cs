using System.Net.Http.Json;
using MediaTrust.Report.Models;

namespace MediaTrust.Report.Accessors;

public sealed class DetectorsAccessor : IDetectorsAccessor
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DetectorsAccessor> _logger;

    public DetectorsAccessor(
        IHttpClientFactory httpClientFactory,
        ILogger<DetectorsAccessor> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DetectorResultDto>> GetByJobIdAsync(
        Guid jobId,
        CancellationToken ct)
    {
        try
        {
            var client =
                _httpClientFactory.CreateClient("detectors");

            var results =
                await client.GetFromJsonAsync<List<DetectorResultDto>>(
                    $"detectors/results/job/{jobId}",
                    ct);

            return results ?? new List<DetectorResultDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to get detector results for JobId {JobId}",
                jobId);

            throw;
        }
    }
}