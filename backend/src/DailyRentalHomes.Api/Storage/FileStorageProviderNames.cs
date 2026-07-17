namespace DailyRentalHomes.Api.Storage;

public static class FileStorageProviderNames
{
    public const string Local = "Local";
    public const string S3 = "S3";

    public static bool IsLocal(string? value) => string.Equals(value, Local, StringComparison.OrdinalIgnoreCase);
    public static bool IsS3(string? value) => string.Equals(value, S3, StringComparison.OrdinalIgnoreCase);
}
