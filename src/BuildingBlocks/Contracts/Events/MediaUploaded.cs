namespace MediaTrust.Contracts.Events;

public sealed record MediaUploaded(
    Guid MediaId,
    string ObjectKey,
    string FileName,
    string ContentType,
    long SizeBytes,
    DateTimeOffset UploadedAtUtc
);
