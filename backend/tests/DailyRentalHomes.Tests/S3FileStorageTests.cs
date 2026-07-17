using DailyRentalHomes.Api.Options;
using DailyRentalHomes.Api.Storage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace DailyRentalHomes.Tests;

public sealed class S3FileStorageTests
{
    [Fact]
    public async Task S3ProviderUploadsToConfiguredBucketWithNormalizedKeyAndContentType()
    {
        var client = new RecordingS3StorageClient();
        var storage = CreateS3Storage(client);

        var stored = await storage.SaveAsync("rental-homes\\101\\main image.webp", new MemoryStream([1, 2, 3]), "image/webp", default);

        Assert.Equal("rental-homes/101/main image.webp", stored.Key);
        Assert.Equal("https://cdn.example.test/media/rental-homes/101/main%20image.webp", stored.Url);
        var upload = Assert.Single(client.Uploads);
        Assert.Equal("daily-homes", upload.BucketName);
        Assert.Equal("rental-homes/101/main image.webp", upload.Key);
        Assert.Equal("image/webp", upload.ContentType);
        Assert.Equal([1, 2, 3], upload.Bytes);
    }

    [Fact]
    public async Task S3PrivateUploadDoesNotReturnPermanentPublicUrlAndCanBeOpenedByKey()
    {
        var client = new RecordingS3StorageClient();
        client.StoredObjects[("daily-homes", "deposit-receipts/receipt.png")] = [4, 5, 6];
        var storage = CreateS3Storage(client);

        var stored = await storage.SavePrivateAsync("deposit-receipts/receipt.png", new MemoryStream([4, 5, 6]), "image/png", default);
        var read = await storage.OpenReadAsync(stored.Url, default);

        Assert.Equal("deposit-receipts/receipt.png", stored.Key);
        Assert.Equal("deposit-receipts/receipt.png", stored.Url);
        Assert.NotNull(read);
        Assert.Equal("deposit-receipts/receipt.png", read.Key);
        await read.Content.DisposeAsync();
    }

    [Fact]
    public async Task S3DeleteTargetsConfiguredBucketAndKey()
    {
        var client = new RecordingS3StorageClient();
        var storage = CreateS3Storage(client);

        await storage.DeleteAsync("https://cdn.example.test/media/rental-homes/101/main.webp", default);

        var delete = Assert.Single(client.Deletes);
        Assert.Equal("daily-homes", delete.BucketName);
        Assert.Equal("rental-homes/101/main.webp", delete.Key);
    }

    [Fact]
    public async Task S3DeleteMissingObjectIsSafe()
    {
        var client = new RecordingS3StorageClient();
        var storage = CreateS3Storage(client);

        await storage.DeleteAsync("rental-homes/101/missing.webp", default);

        Assert.Single(client.Deletes);
    }

    [Fact]
    public async Task S3RejectsUnsafeKeys()
    {
        var storage = CreateS3Storage(new RecordingS3StorageClient());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            storage.SaveAsync("../outside.webp", new MemoryStream([1]), "image/webp", default));
    }

    [Fact]
    public void LocalProviderResolvesWithoutS3Configuration()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IWebHostEnvironment>(TestEnvironment.Create());
        services.AddFileStorage(Configuration(new Dictionary<string, string?>
        {
            ["FileStorage:Provider"] = "Local",
            ["FileStorage:Local:RootPath"] = "uploads",
            ["FileStorage:Local:PrivateRootPath"] = "private-uploads",
            ["FileStorage:Local:PublicBasePath"] = "/uploads"
        }));

        using var provider = services.BuildServiceProvider();
        var storage = provider.GetRequiredService<IFileStorage>();

        Assert.IsType<LocalFileStorage>(storage);
    }

    [Fact]
    public void S3ProviderResolvesWhenConfigured()
    {
        var services = new ServiceCollection();
        var client = new RecordingS3StorageClient();
        services.AddFileStorage(Configuration(new Dictionary<string, string?>
        {
            ["FileStorage:Provider"] = "S3",
            ["FileStorage:S3:BucketName"] = "daily-homes",
            ["FileStorage:S3:Region"] = "eu-central-1",
            ["FileStorage:S3:PublicBaseUrl"] = "https://cdn.example.test/media"
        }));
        services.AddSingleton<IS3StorageClient>(client);

        using var provider = services.BuildServiceProvider();
        var storage = provider.GetRequiredService<IFileStorage>();

        Assert.IsType<S3FileStorage>(storage);
    }

    [Fact]
    public void S3ProviderFailsClearlyWhenBucketIsMissing()
    {
        var services = new ServiceCollection();
        services.AddFileStorage(Configuration(new Dictionary<string, string?>
        {
            ["FileStorage:Provider"] = "S3"
        }));

        using var provider = services.BuildServiceProvider();
        var exception = Assert.Throws<OptionsValidationException>(() =>
            _ = provider.GetRequiredService<IOptions<FileStorageOptions>>().Value);
        Assert.Contains("FileStorage:S3:BucketName", exception.Message);
    }

    private static S3FileStorage CreateS3Storage(RecordingS3StorageClient client) => new(
        client,
        Options.Create(new FileStorageOptions
        {
            Provider = "S3",
            S3 = new S3FileStorageOptions
            {
                BucketName = "daily-homes",
                PublicBaseUrl = "https://cdn.example.test/media",
                Region = "eu-central-1"
            }
        }));

    private static IConfiguration Configuration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private sealed class RecordingS3StorageClient : IS3StorageClient
    {
        public List<S3Upload> Uploads { get; } = [];
        public List<S3Delete> Deletes { get; } = [];
        public Dictionary<(string BucketName, string Key), byte[]> StoredObjects { get; } = [];

        public async Task PutObjectAsync(string bucketName, string key, Stream content, string? contentType, CancellationToken cancellationToken)
        {
            using var memory = new MemoryStream();
            await content.CopyToAsync(memory, cancellationToken);
            var bytes = memory.ToArray();
            Uploads.Add(new S3Upload(bucketName, key, contentType, bytes));
            StoredObjects[(bucketName, key)] = bytes;
        }

        public Task<Stream?> OpenReadAsync(string bucketName, string key, CancellationToken cancellationToken)
        {
            return Task.FromResult<Stream?>(StoredObjects.TryGetValue((bucketName, key), out var bytes)
                ? new MemoryStream(bytes)
                : null);
        }

        public Task DeleteObjectAsync(string bucketName, string key, CancellationToken cancellationToken)
        {
            Deletes.Add(new S3Delete(bucketName, key));
            StoredObjects.Remove((bucketName, key));
            return Task.CompletedTask;
        }
    }

    private sealed record S3Upload(string BucketName, string Key, string? ContentType, byte[] Bytes);
    private sealed record S3Delete(string BucketName, string Key);

    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public static TestEnvironment Create()
        {
            var root = Path.Combine(Path.GetTempPath(), "daily-rental-homes-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var webRoot = Path.Combine(root, "wwwroot");
            Directory.CreateDirectory(webRoot);
            return new TestEnvironment { ContentRootPath = root, WebRootPath = webRoot };
        }

        public string ApplicationName { get; set; } = "DailyRentalHomes.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
