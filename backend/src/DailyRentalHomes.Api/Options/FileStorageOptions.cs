namespace DailyRentalHomes.Api.Options;

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    public string Provider { get; set; } = "Local";
    public LocalFileStorageOptions Local { get; set; } = new();
}

public sealed class LocalFileStorageOptions
{
    public string RootPath { get; set; } = "uploads";
    public string PrivateRootPath { get; set; } = "private-uploads";
    public string PublicBasePath { get; set; } = "/uploads";
}
