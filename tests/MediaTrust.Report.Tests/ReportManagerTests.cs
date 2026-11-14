using MediaTrust.Report.Accessors;
using MediaTrust.Report.Data;
using MediaTrust.Report.Managers;
using MediaTrust.Report.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MediaTrust.Report.Tests;

public sealed class ReportManagerTests
{
    private readonly Mock<IReportRepository> _repository = new(MockBehavior.Strict);
    private readonly Mock<IDetectorsAccessor> _detectors = new(MockBehavior.Strict);
    private readonly Mock<IAiAccessor> _ai = new(MockBehavior.Strict);

    private ReportManager CreateManager() => new(
        _repository.Object, _detectors.Object, _ai.Object,
        NullLogger<ReportManager>.Instance);

    [Fact]
    public async Task BuildExplanationAsync_RequestedJob_UsesItsResultAndReturnsExplanationFields()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        using var cancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        var result = CreateDetectorResult(jobId, 0.6);
        const string explanation = "The header does not match the declared media type.";
        _detectors.Setup(accessor => accessor.GetByJobIdAsync(jobId, token))
            .ReturnsAsync(new[] { result });
        _ai.Setup(accessor => accessor.GenerateExplanationAsync(
                result.DetectorName, result.Score, result.Details, token))
            .ReturnsAsync(explanation);

        // Act
        var response = await CreateManager().BuildExplanationAsync(jobId, token);

        // Assert
        Assert.Equal(jobId, response.JobId);
        Assert.Equal(result.MediaId, response.MediaId);
        Assert.Equal(result.Score, response.RiskScore);
        Assert.Equal(explanation, response.Explanation);
        _detectors.Verify(accessor => accessor.GetByJobIdAsync(jobId, token), Times.Once);
        _ai.Verify(accessor => accessor.GenerateExplanationAsync(
            result.DetectorName, result.Score, result.Details, token), Times.Once);
        _detectors.VerifyNoOtherCalls();
        _ai.VerifyNoOtherCalls();
        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task BuildExplanationAsync_DifferentJobsForSameMedia_KeepResultsSeparate()
    {
        // Arrange
        var mediaId = Guid.NewGuid();
        var firstJobId = Guid.NewGuid();
        var secondJobId = Guid.NewGuid();
        var first = CreateDetectorResult(firstJobId, 0.0);
        var second = CreateDetectorResult(secondJobId, 1.0);
        first.MediaId = second.MediaId = mediaId;
        first.Details = "First job findings";
        second.Details = "Second job findings";
        _detectors.Setup(accessor => accessor.GetByJobIdAsync(firstJobId, CancellationToken.None))
            .ReturnsAsync(new[] { first });
        _detectors.Setup(accessor => accessor.GetByJobIdAsync(secondJobId, CancellationToken.None))
            .ReturnsAsync(new[] { second });
        _ai.Setup(accessor => accessor.GenerateExplanationAsync(
                first.DetectorName, first.Score, first.Details, CancellationToken.None))
            .ReturnsAsync("First job explanation");
        _ai.Setup(accessor => accessor.GenerateExplanationAsync(
                second.DetectorName, second.Score, second.Details, CancellationToken.None))
            .ReturnsAsync("Second job explanation");
        var manager = CreateManager();

        // Act: request the second job, then the first, to catch reuse of latest results.
        var secondResponse = await manager.BuildExplanationAsync(secondJobId, CancellationToken.None);
        var firstResponse = await manager.BuildExplanationAsync(firstJobId, CancellationToken.None);

        // Assert
        Assert.Equal(secondJobId, secondResponse.JobId);
        Assert.Equal(firstJobId, firstResponse.JobId);
        Assert.Equal(mediaId, secondResponse.MediaId);
        Assert.Equal(mediaId, firstResponse.MediaId);
        Assert.Equal(1.0, secondResponse.RiskScore);
        Assert.Equal(0.0, firstResponse.RiskScore);
        Assert.Equal("Second job explanation", secondResponse.Explanation);
        Assert.Equal("First job explanation", firstResponse.Explanation);
        _detectors.VerifyAll();
        _ai.VerifyAll();
        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task BuildExplanationAsync_UnorderedResults_UsesNewestResultWithinRequestedJob()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var oldest = CreateDetectorResult(jobId, 0.0);
        var newest = CreateDetectorResult(jobId, 0.4);
        var middle = CreateDetectorResult(jobId, 0.6);
        oldest.CreatedAtUtc = DateTimeOffset.Parse("2026-01-01T10:00:00Z");
        newest.CreatedAtUtc = oldest.CreatedAtUtc.AddHours(2);
        middle.CreatedAtUtc = oldest.CreatedAtUtc.AddHours(1);
        _detectors.Setup(accessor => accessor.GetByJobIdAsync(jobId, CancellationToken.None))
            .ReturnsAsync(new[] { oldest, newest, middle });
        _ai.Setup(accessor => accessor.GenerateExplanationAsync(
                newest.DetectorName, newest.Score, newest.Details, CancellationToken.None))
            .ReturnsAsync("Newest findings explained");

        // Act
        var response = await CreateManager().BuildExplanationAsync(jobId, CancellationToken.None);

        // Assert
        Assert.Equal(newest.MediaId, response.MediaId);
        Assert.Equal(newest.Score, response.RiskScore);
        Assert.Equal("Newest findings explained", response.Explanation);
        _ai.Verify(accessor => accessor.GenerateExplanationAsync(
            newest.DetectorName, newest.Score, newest.Details, CancellationToken.None), Times.Once);
        _ai.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task BuildExplanationAsync_NoJobSpecificResults_ThrowsKeyNotFoundWithoutCallingAi()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        _detectors.Setup(accessor => accessor.GetByJobIdAsync(jobId, CancellationToken.None))
            .ReturnsAsync(Array.Empty<DetectorResultDto>());

        // Act
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            CreateManager().BuildExplanationAsync(jobId, CancellationToken.None));

        // Assert
        Assert.Equal($"No detector result found for JobId {jobId}", exception.Message);
        _detectors.Verify(accessor => accessor.GetByJobIdAsync(jobId, CancellationToken.None), Times.Once);
        _ai.VerifyNoOtherCalls();
        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task BuildExplanationAsync_AiFails_PropagatesExistingException()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var result = CreateDetectorResult(jobId, 0.4);
        var failure = new HttpRequestException("Simulated AI service failure");
        _detectors.Setup(accessor => accessor.GetByJobIdAsync(jobId, CancellationToken.None))
            .ReturnsAsync(new[] { result });
        _ai.Setup(accessor => accessor.GenerateExplanationAsync(
                result.DetectorName, result.Score, result.Details, CancellationToken.None))
            .ThrowsAsync(failure);

        // Act
        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            CreateManager().BuildExplanationAsync(jobId, CancellationToken.None));

        // Assert
        Assert.Same(failure, exception);
    }

    [Theory]
    [InlineData(0.0, "OK")]
    [InlineData(0.299999, "OK")]
    [InlineData(0.3, "Warning")]
    [InlineData(0.699999, "Warning")]
    [InlineData(0.7, "Critical")]
    [InlineData(1.0, "Critical")]
    public async Task BuildReportAsync_RiskScore_UsesExistingStatusThresholds(
        double score, string expectedStatus)
    {
        // Arrange
        var mediaId = Guid.NewGuid();
        var result = CreateReportResult(mediaId, score);
        _repository.Setup(repository => repository.GetByMediaIdAsync(mediaId, CancellationToken.None))
            .ReturnsAsync(new[] { result });

        // Act
        var report = await CreateManager().BuildReportAsync(mediaId, CancellationToken.None);

        // Assert
        Assert.Equal(mediaId, report.MediaId);
        Assert.Equal(score, report.RiskScore);
        Assert.Equal(expectedStatus, report.OverallStatus);
        _detectors.VerifyNoOtherCalls();
        _ai.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task BuildReportAsync_MultipleResults_UsesMaximumRiskAndPreservesDetectorDetails()
    {
        // Arrange
        var mediaId = Guid.NewGuid();
        using var cancellation = new CancellationTokenSource();
        var results = new[]
        {
            CreateReportResult(mediaId, 0.2),
            CreateReportResult(mediaId, 0.8),
            CreateReportResult(mediaId, 0.4)
        };
        _repository.Setup(repository => repository.GetByMediaIdAsync(mediaId, cancellation.Token))
            .ReturnsAsync(results);

        // Act
        var report = await CreateManager().BuildReportAsync(mediaId, cancellation.Token);

        // Assert
        Assert.Equal(mediaId, report.MediaId);
        Assert.Equal(0.8, report.RiskScore);
        Assert.Equal("Critical", report.OverallStatus);
        Assert.Equal(results.Length, report.Results.Count);
        for (var index = 0; index < results.Length; index++)
        {
            Assert.Equal(results[index].DetectorName, report.Results[index].DetectorName);
            Assert.Equal(results[index].Score, report.Results[index].Score);
            Assert.Equal(results[index].Details, report.Results[index].Details);
        }
        _repository.Verify(repository => repository.GetByMediaIdAsync(mediaId, cancellation.Token), Times.Once);
    }

    [Fact]
    public async Task BuildReportAsync_NoResults_ReturnsZeroRiskAndOkStatus()
    {
        // Arrange
        var mediaId = Guid.NewGuid();
        _repository.Setup(repository => repository.GetByMediaIdAsync(mediaId, CancellationToken.None))
            .ReturnsAsync(Array.Empty<DetectorResult>());

        // Act
        var report = await CreateManager().BuildReportAsync(mediaId, CancellationToken.None);

        // Assert
        Assert.Equal(mediaId, report.MediaId);
        Assert.Equal(0.0, report.RiskScore);
        Assert.Equal("OK", report.OverallStatus);
        Assert.Empty(report.Results);
    }

    private static DetectorResultDto CreateDetectorResult(Guid jobId, double score) => new()
    {
        Id = Guid.NewGuid(),
        JobId = jobId,
        MediaId = Guid.NewGuid(),
        DetectorName = "ContentIntegrityDetector",
        Score = score,
        Details = $"Findings for score {score}",
        CreatedAtUtc = DateTimeOffset.Parse("2026-01-01T10:00:00Z")
    };

    private static DetectorResult CreateReportResult(Guid mediaId, double score) => new()
    {
        Id = Guid.NewGuid(),
        MediaId = mediaId,
        DetectorName = $"Detector-{score}",
        Score = score,
        Details = $"Findings for score {score}",
        CreatedAtUtc = DateTimeOffset.Parse("2026-01-01T10:00:00Z")
    };
}
