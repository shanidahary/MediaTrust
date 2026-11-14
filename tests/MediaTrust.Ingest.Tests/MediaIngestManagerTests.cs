using MediaTrust.Ingest.Accessors;
using MediaTrust.Ingest.Data;
using MediaTrust.Ingest.Managers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Moq;

namespace MediaTrust.Ingest.Tests;

public sealed class MediaIngestManagerTests
{
    private readonly Mock<IMediaRepository> _repository = new(MockBehavior.Strict);
    private readonly Mock<IStorageAccessor> _storage = new(MockBehavior.Strict);
    private readonly Mock<IHttpClientFactory> _http = new(MockBehavior.Strict);

    private MediaIngestManager CreateManager() => new(
        _repository.Object, _storage.Object, _http.Object,
        NullLogger<MediaIngestManager>.Instance);

    [Theory]
    [InlineData("example.png")]
    [InlineData("uploads/example.png")]
    public async Task HandleUploadAsync_DuplicateFile_ReturnsExistingMediaWithoutCreatingOrUploading(
        string uploadedFileName)
    {
        // Arrange
        using var content = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        using var cancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        var request = CreateUploadRequest(content, uploadedFileName);
        var existing = new MediaItem
        {
            Id = Guid.NewGuid(),
            FileName = "example.png",
            ObjectKey = "2026/01/01/example.png",
            ContentType = "image/png",
            SizeBytes = 32768,
            UploadedAtUtc = DateTimeOffset.Parse("2026-01-01T10:00:00Z")
        };
        _repository.Setup(repository => repository.GetByFileNameAsync("example.png", token))
            .ReturnsAsync(existing);

        // Act
        var result = await CreateManager().HandleUploadAsync(request, token);

        // Assert: return the persisted metadata, even if the incoming file differs in size.
        Assert.True(result.IsDuplicate);
        Assert.Equal(existing.Id, result.MediaId);
        Assert.Equal(existing.ObjectKey, result.ObjectKey);
        Assert.Equal(existing.FileName, result.FileName);
        Assert.Equal(existing.ContentType, result.ContentType);
        Assert.Equal(existing.SizeBytes, result.SizeBytes);
        Assert.Equal(existing.UploadedAtUtc, result.UploadedAtUtc);
        _repository.Verify(repository => repository.GetByFileNameAsync("example.png", token), Times.Once);
        _repository.Verify(repository => repository.AddAsync(
            It.IsAny<MediaItem>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.VerifyNoOtherCalls();
        _storage.VerifyNoOtherCalls();
        // The duplicate path must not even create an HTTP client to enqueue another job.
        _http.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleUploadAsync_NotMultipart_ThrowsBeforeCallingDependencies()
    {
        // Arrange
        var request = new DefaultHttpContext().Request;
        request.ContentType = "application/json";

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateManager().HandleUploadAsync(request, CancellationToken.None));

        // Assert
        Assert.Equal("Expected multipart/form-data", exception.Message);
        VerifyNoDependencyCalls();
    }

    [Fact]
    public async Task HandleUploadAsync_MissingFile_ThrowsBeforeCallingDependencies()
    {
        // Arrange
        var request = new DefaultHttpContext().Request;
        request.ContentType = "multipart/form-data; boundary=unit-test";
        request.Form = new FormCollection(new Dictionary<string, StringValues>());

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateManager().HandleUploadAsync(request, CancellationToken.None));

        // Assert
        Assert.Equal("Missing file", exception.Message);
        VerifyNoDependencyCalls();
    }

    [Fact]
    public async Task HandleUploadAsync_EmptyFile_ThrowsBeforeCallingDependencies()
    {
        // Arrange
        using var content = new MemoryStream();
        var request = CreateUploadRequest(content, "empty.png");

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateManager().HandleUploadAsync(request, CancellationToken.None));

        // Assert
        Assert.Equal("Missing file", exception.Message);
        VerifyNoDependencyCalls();
    }

    private void VerifyNoDependencyCalls()
    {
        _repository.VerifyNoOtherCalls();
        _storage.VerifyNoOtherCalls();
        _http.VerifyNoOtherCalls();
    }

    private static HttpRequest CreateUploadRequest(Stream content, string fileName)
    {
        var request = new DefaultHttpContext().Request;
        request.ContentType = "multipart/form-data; boundary=unit-test";
        var file = new FormFile(content, 0, content.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };
        request.Form = new FormCollection(
            new Dictionary<string, StringValues>(), new FormFileCollection { file });
        return request;
    }
}
