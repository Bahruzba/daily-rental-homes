using DailyRentalHomes.Api.Options;
using Microsoft.Extensions.Options;

namespace DailyRentalHomes.Api.Storage;

public sealed class S3FileStorage : IFileStorage
{
    private readonly IS3StorageClient _client;
    private readonly S3FileStorageOptions _options;

    public S3FileStorage(IS3StorageClient client, IOptions<FileStorageOptions> options)
    {
        _client = client;
        _options = options.Value.S3;

        if (!FileStorageProviderNames.IsS3(options.Value.Provider))
        {
            throw new InvalidOperationException("S3 file storage can only be used when FileStorage:Provider is S3.");
        }

        if (string.IsNullOrWhiteSpace(_options.BucketName))
        {
            throw new InvalidOperationException("FileStorage:S3:BucketName is required when FileStorage:Provider is S3.");
        }
    }

    public async Task<StoredFile> SaveAsync(string key, Stream content, string? contentType, CancellationToken cancellationToken)
    {
        var normalizedKey = StorageKey.Normalize(key);
        await _client.PutObjectAsync(_options.BucketName.Trim(), normalizedKey, content, contentType, cancellationToken);
        return new StoredFile(normalizedKey, GetPublicUrl(normalizedKey));
    }

    public async Task<StoredFile> SavePrivateAsync(string key, Stream content, string? contentType, CancellationToken cancellationToken)
    {
        var normalizedKey = StorageKey.Normalize(key);
        await _client.PutObjectAsync(_options.BucketName.Trim(), normalizedKey, content, contentType, cancellationToken);
        return new StoredFile(normalizedKey, normalizedKey);
    }

    public async Task<StoredFileReadResult?> OpenReadAsync(string keyOrUrl, CancellationToken cancellationToken)
    {
        var normalizedKey = NormalizeKeyOrPublicUrl(keyOrUrl);
        var stream = await _client.OpenReadAsync(_options.BucketName.Trim(), normalizedKey, cancellationToken);
        return stream is null ? null : new StoredFileReadResult(stream, normalizedKey);
    }

    public Task DeleteAsync(string keyOrUrl, CancellationToken cancellationToken)
    {
        var normalizedKey = NormalizeKeyOrPublicUrl(keyOrUrl);
        return _client.DeleteObjectAsync(_options.BucketName.Trim(), normalizedKey, cancellationToken);
    }

    public string GetPublicUrl(string key)
    {
        var normalizedKey = StorageKey.Normalize(key);
        var publicBaseUrl = _options.PublicBaseUrl?.Trim();
        if (!string.IsNullOrWhiteSpace(publicBaseUrl))
        {
            return $"{publicBaseUrl.TrimEnd('/')}/{EscapeKey(normalizedKey)}";
        }

        if (!string.IsNullOrWhiteSpace(_options.ServiceUrl))
        {
            var serviceUrl = _options.ServiceUrl.Trim().TrimEnd('/');
            return _options.ForcePathStyle
                ? $"{serviceUrl}/{Uri.EscapeDataString(_options.BucketName.Trim())}/{EscapeKey(normalizedKey)}"
                : $"{serviceUrl}/{EscapeKey(normalizedKey)}";
        }

        var region = string.IsNullOrWhiteSpace(_options.Region) ? "us-east-1" : _options.Region.Trim();
        return $"https://{_options.BucketName.Trim()}.s3.{region}.amazonaws.com/{EscapeKey(normalizedKey)}";
    }

    private string NormalizeKeyOrPublicUrl(string keyOrUrl)
    {
        if (string.IsNullOrWhiteSpace(keyOrUrl))
        {
            throw new InvalidOperationException("File storage key is required.");
        }

        var value = keyOrUrl.Trim();
        var publicBaseUrl = _options.PublicBaseUrl?.Trim();
        if (!string.IsNullOrWhiteSpace(publicBaseUrl) &&
            value.StartsWith(publicBaseUrl.TrimEnd('/') + "/", StringComparison.OrdinalIgnoreCase))
        {
            var keyPart = value[(publicBaseUrl.TrimEnd('/').Length + 1)..];
            value = Uri.UnescapeDataString(keyPart);
        }
        else if (Uri.TryCreate(value, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException("File storage URL is outside the configured S3 public base URL.");
        }

        return StorageKey.Normalize(value);
    }

    private static string EscapeKey(string key) =>
        string.Join('/', StorageKey.Normalize(key).Split('/').Select(Uri.EscapeDataString));
}
