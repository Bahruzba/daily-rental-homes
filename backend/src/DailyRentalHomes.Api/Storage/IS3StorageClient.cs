namespace DailyRentalHomes.Api.Storage;

public interface IS3StorageClient
{
    Task PutObjectAsync(string bucketName, string key, Stream content, string? contentType, CancellationToken cancellationToken);
    Task<Stream?> OpenReadAsync(string bucketName, string key, CancellationToken cancellationToken);
    Task DeleteObjectAsync(string bucketName, string key, CancellationToken cancellationToken);
}
