using MediaTrust.Orchestrator.Accessors;
using MediaTrust.Orchestrator.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace MediaTrust.Orchestrator.Tests;

public sealed class AnalysisJobRepositoryTests
{
    [Fact]
    public async Task GetLatestByMediaIdAsync_SingleMatchingJob_ReturnsThatJob()
    {
        // Arrange
        await using var database = CreateDatabase();
        var job = CreateJob(Guid.NewGuid(), DateTimeOffset.Parse("2026-01-01T10:00:00Z"));
        database.AnalysisJobs.Add(job);
        await database.SaveChangesAsync();
        database.ChangeTracker.Clear();
        var repository = CreateRepository(database);

        // Act
        var result = await repository.GetLatestByMediaIdAsync(job.MediaId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(job.Id, result.Id);
        Assert.Equal(job.MediaId, result.MediaId);
    }

    [Fact]
    public async Task GetLatestByMediaIdAsync_UnorderedJobs_ReturnsNewestCreatedJobForRequestedMedia()
    {
        // Arrange
        await using var database = CreateDatabase();
        var mediaId = Guid.NewGuid();
        var created = DateTimeOffset.Parse("2026-01-01T10:00:00Z");
        var oldest = CreateJob(mediaId, created);
        var newest = CreateJob(mediaId, created.AddHours(2));
        var middle = CreateJob(mediaId, created.AddHours(1));
        var otherMedia = CreateJob(Guid.NewGuid(), created.AddDays(1));
        // A later update to an older job must not change which job was created last.
        oldest.UpdatedAtUtc = created.AddDays(2);
        newest.Status = "Processing";
        database.AnalysisJobs.AddRange(oldest, newest, middle, otherMedia);
        await database.SaveChangesAsync();
        database.ChangeTracker.Clear();
        var repository = CreateRepository(database);

        // Act
        var result = await repository.GetLatestByMediaIdAsync(mediaId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(newest.Id, result.Id);
        Assert.Equal(mediaId, result.MediaId);
        Assert.Equal(newest.CreatedAtUtc, result.CreatedAtUtc);
        Assert.Equal("Processing", result.Status);
    }

    [Fact]
    public async Task GetLatestByMediaIdAsync_OnlyOtherMediaExists_ReturnsNull()
    {
        // Arrange
        await using var database = CreateDatabase();
        database.AnalysisJobs.Add(CreateJob(Guid.NewGuid(), DateTimeOffset.Parse("2026-01-01T10:00:00Z")));
        await database.SaveChangesAsync();
        var repository = CreateRepository(database);

        // Act
        var result = await repository.GetLatestByMediaIdAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetLatestByMediaIdAsync_EmptyHistory_ReturnsNull()
    {
        // Arrange
        await using var database = CreateDatabase();
        var repository = CreateRepository(database);

        // Act
        var result = await repository.GetLatestByMediaIdAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    private static OrchestratorDbContext CreateDatabase() => new(
        new DbContextOptionsBuilder<OrchestratorDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static AnalysisJobRepository CreateRepository(OrchestratorDbContext database) => new(
        database, NullLogger<AnalysisJobRepository>.Instance);

    private static AnalysisJob CreateJob(Guid mediaId, DateTimeOffset createdAt) => new()
    {
        Id = Guid.NewGuid(),
        MediaId = mediaId,
        ObjectKey = "uploads/example.png",
        Status = "Completed",
        CreatedAtUtc = createdAt
    };
}
