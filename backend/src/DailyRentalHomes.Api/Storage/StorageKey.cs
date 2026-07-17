namespace DailyRentalHomes.Api.Storage;

public static class StorageKey
{
    public static string Normalize(string key)
    {
        if (key is null)
        {
            throw new InvalidOperationException("File storage key is required.");
        }

        var rawValue = key.Trim();
        if (rawValue.StartsWith('/') || rawValue.StartsWith('\\'))
        {
            throw new InvalidOperationException("File storage key must be relative.");
        }

        var value = rawValue.Replace('\\', '/').Trim('/');
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException("File storage key is required.");
        }

        var segments = value.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Any(segment => segment is "." or ".." || segment.Contains(':')))
        {
            throw new InvalidOperationException("File storage key contains unsafe path segments.");
        }

        return string.Join('/', segments);
    }
}
