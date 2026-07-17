using Amazon.S3;
using Amazon.S3.Model;
using System.Net;

namespace DailyRentalHomes.Api.Storage;

public sealed class AwsS3StorageClient : IS3StorageClient
{
    private readonly IAmazonS3 _s3;

    public AwsS3StorageClient(IAmazonS3 s3)
    {
        _s3 = s3;
    }

    public Task PutObjectAsync(
        string bucketName,
        string key,
        Stream content,
        string? contentType,
        CancellationToken cancellationToken)
    {
        var request = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = key,
            InputStream = content,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType
        };

        return _s3.PutObjectAsync(request, cancellationToken);
    }

    public async Task<Stream?> OpenReadAsync(string bucketName, string key, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _s3.GetObjectAsync(bucketName, key, cancellationToken);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound ||
                                                 string.Equals(exception.ErrorCode, "NoSuchKey", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }
    }

    public async Task DeleteObjectAsync(string bucketName, string key, CancellationToken cancellationToken)
    {
        try
        {
            await _s3.DeleteObjectAsync(bucketName, key, cancellationToken);
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound ||
                                                 string.Equals(exception.ErrorCode, "NoSuchKey", StringComparison.OrdinalIgnoreCase))
        {
            // S3-compatible providers usually make delete idempotent, but keep missing objects safe.
        }
    }
}
