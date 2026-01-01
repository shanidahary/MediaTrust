namespace MediaTrust.Ingest.Storage;

public sealed class MinioOptions
{
    public string Endpoint { get; set; } = "minio:9000";
    public string AccessKey { get; set; } = "minio";
    public string SecretKey { get; set; } = "minio12345";
    public string Bucket { get; set; } = "mediatrust";
    public bool UseSsl { get; set; } = false;
}
