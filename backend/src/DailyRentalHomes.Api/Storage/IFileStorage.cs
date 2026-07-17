namespace DailyRentalHomes.Api.Storage;

public interface IFileStorage
{
    Task<StoredFile> SaveAsync(string key, Stream content, string? contentType, CancellationToken cancellationToken);
    Task<StoredFile> SavePrivateAsync(string key, Stream content, string? contentType, CancellationToken cancellationToken);
    Task<StoredFileReadResult?> OpenReadAsync(string keyOrUrl, CancellationToken cancellationToken);
    Task DeleteAsync(string keyOrUrl, CancellationToken cancellationToken);
    string GetPublicUrl(string key);
}

public sealed record StoredFile(string Key, string Url);
public sealed record StoredFileReadResult(Stream Content, string Key);
