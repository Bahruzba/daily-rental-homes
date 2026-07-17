namespace DailyRentalHomes.Api.Options;

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    public string Provider { get; set; } = "Local";
    public LocalFileStorageOptions Local { get; set; } = new();
    public S3FileStorageOptions S3 { get; set; } = new();
}

public sealed class LocalFileStorageOptions
{
    public string RootPath { get; set; } = "uploads";
    public string PrivateRootPath { get; set; } = "private-uploads";
    public string PublicBasePath { get; set; } = "/uploads";
}

public sealed class S3FileStorageOptions
{
    public string BucketName { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string ServiceUrl { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string PublicBaseUrl { get; set; } = string.Empty;
    public bool ForcePathStyle { get; set; }
}
