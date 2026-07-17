using DailyRentalHomes.Api.Options;
using Microsoft.Extensions.Options;

namespace DailyRentalHomes.Api.Storage;

public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _rootPath;
    private readonly string _privateRootPath;
    private readonly string _publicBasePath;

    public LocalFileStorage(IOptions<FileStorageOptions> options, IWebHostEnvironment environment)
    {
        var storageOptions = options.Value;
        if (!string.Equals(storageOptions.Provider, "Local", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only Local file storage provider is supported.");
        }

        var rootPath = storageOptions.Local.RootPath?.Trim();
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new InvalidOperationException("FileStorage:Local:RootPath is required.");
        }

        var webRoot = string.IsNullOrWhiteSpace(environment.WebRootPath)
            ? Path.Combine(environment.ContentRootPath, "wwwroot")
            : environment.WebRootPath;
        _rootPath = Path.GetFullPath(Path.IsPathRooted(rootPath)
            ? rootPath
            : Path.Combine(webRoot, rootPath));
        var privateRootPath = string.IsNullOrWhiteSpace(storageOptions.Local.PrivateRootPath)
            ? "private-uploads"
            : storageOptions.Local.PrivateRootPath.Trim();
        _privateRootPath = Path.GetFullPath(Path.IsPathRooted(privateRootPath)
            ? privateRootPath
            : Path.Combine(environment.ContentRootPath, privateRootPath));
        _publicBasePath = NormalizePublicBasePath(storageOptions.Local.PublicBasePath);
        Directory.CreateDirectory(_rootPath);
        Directory.CreateDirectory(_privateRootPath);
    }

    public async Task<StoredFile> SaveAsync(string key, Stream content, CancellationToken cancellationToken)
    {
        var normalizedKey = NormalizeKey(key);
        var fullPath = GetFullPath(normalizedKey);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var output = File.Create(fullPath);
        await content.CopyToAsync(output, cancellationToken);

        return new StoredFile(normalizedKey, GetPublicUrl(normalizedKey));
    }

    public async Task<StoredFile> SavePrivateAsync(string key, Stream content, CancellationToken cancellationToken)
    {
        var normalizedKey = NormalizeKey(key);
        var fullPath = GetPrivateFullPath(normalizedKey);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var output = File.Create(fullPath);
        await content.CopyToAsync(output, cancellationToken);

        return new StoredFile(normalizedKey, normalizedKey);
    }

    public Task<StoredFileReadResult?> OpenReadAsync(string keyOrUrl, CancellationToken cancellationToken)
    {
        var location = ResolveReadLocation(keyOrUrl);
        if (!File.Exists(location.FullPath))
        {
            return Task.FromResult<StoredFileReadResult?>(null);
        }

        Stream stream = File.OpenRead(location.FullPath);
        return Task.FromResult<StoredFileReadResult?>(new StoredFileReadResult(stream, location.Key));
    }

    public Task DeleteAsync(string keyOrUrl, CancellationToken cancellationToken)
    {
        var locations = ResolveDeleteLocations(keyOrUrl);
        foreach (var location in locations)
        {
            if (File.Exists(location.FullPath))
            {
                File.Delete(location.FullPath);
            }
        }

        return Task.CompletedTask;
    }

    public string GetPublicUrl(string key) => $"{_publicBasePath}/{NormalizeKey(key)}";

    private string GetFullPath(string normalizedKey)
    {
        return GetSafeFullPath(_rootPath, normalizedKey);
    }

    private string GetPrivateFullPath(string normalizedKey)
    {
        return GetSafeFullPath(_privateRootPath, normalizedKey);
    }

    private static string GetSafeFullPath(string rootPath, string normalizedKey)
    {
        var fullPath = Path.GetFullPath(Path.Combine(rootPath, normalizedKey.Replace('/', Path.DirectorySeparatorChar)));
        if (!fullPath.StartsWith(rootPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(fullPath, rootPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("File storage key resolves outside the configured root.");
        }

        return fullPath;
    }

    private string NormalizeKeyOrUrl(string keyOrUrl)
    {
        var value = keyOrUrl.Trim();
        if (value.StartsWith(_publicBasePath + "/", StringComparison.OrdinalIgnoreCase))
        {
            value = value[(_publicBasePath.Length + 1)..];
        }
        else if (value.StartsWith("/", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("File storage URL is outside the configured public base path.");
        }

        return NormalizeKey(value);
    }

    private StorageLocation ResolveReadLocation(string keyOrUrl)
    {
        var normalizedKey = NormalizeKeyOrUrl(keyOrUrl);
        var isPublicUrl = keyOrUrl.Trim().StartsWith(_publicBasePath + "/", StringComparison.OrdinalIgnoreCase);
        var fullPath = isPublicUrl
            ? GetFullPath(normalizedKey)
            : File.Exists(GetPrivateFullPath(normalizedKey))
                ? GetPrivateFullPath(normalizedKey)
                : GetFullPath(normalizedKey);
        return new StorageLocation(normalizedKey, fullPath);
    }

    private IReadOnlyList<StorageLocation> ResolveDeleteLocations(string keyOrUrl)
    {
        var normalizedKey = NormalizeKeyOrUrl(keyOrUrl);
        var isPublicUrl = keyOrUrl.Trim().StartsWith(_publicBasePath + "/", StringComparison.OrdinalIgnoreCase);
        if (isPublicUrl)
        {
            return [new StorageLocation(normalizedKey, GetFullPath(normalizedKey))];
        }

        return
        [
            new StorageLocation(normalizedKey, GetPrivateFullPath(normalizedKey)),
            new StorageLocation(normalizedKey, GetFullPath(normalizedKey))
        ];
    }

    private static string NormalizeKey(string key)
    {
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

    private static string NormalizePublicBasePath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "/uploads";
        var normalized = "/" + value.Trim().Replace('\\', '/').Trim('/');
        return normalized == "/" ? "/uploads" : normalized;
    }

    private sealed record StorageLocation(string Key, string FullPath);
}
