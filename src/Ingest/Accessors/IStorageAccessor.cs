namespace MediaTrust.Ingest.Accessors;

public interface IStorageAccessor
{
    Task UploadAsync(
        string objectKey,
        Stream data,
        long size,
        string contentType,
        CancellationToken ct);
}
