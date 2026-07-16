using DailyRentalHomes.Api.Options;
using DailyRentalHomes.Api.Storage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace DailyRentalHomes.Tests;

public sealed class FileStorageTests
{
    [Fact]
    public async Task LocalProviderSavesFilesUnderConfiguredRoot()
    {
        var environment = TestEnvironment.Create();
        var storage = CreateStorage(environment);

        var stored = await storage.SaveAsync("rental-homes/101/home.webp", new MemoryStream([1, 2, 3]), default);

        Assert.Equal("rental-homes/101/home.webp", stored.Key);
        Assert.Equal("/uploads/rental-homes/101/home.webp", stored.Url);
        Assert.True(File.Exists(Path.Combine(environment.WebRootPath, "uploads", "rental-homes", "101", "home.webp")));
    }

    [Theory]
    [InlineData("../outside.webp")]
    [InlineData("rental-homes/../../outside.webp")]
    [InlineData("/outside.webp")]
    [InlineData("C:/outside.webp")]
    public async Task UnsafeTraversalStyleKeysCannotEscapeStorageRoot(string key)
    {
        var storage = CreateStorage(TestEnvironment.Create());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            storage.SaveAsync(key, new MemoryStream([1]), default));
    }

    [Fact]
    public void MissingLocalRootConfigurationFailsClearly()
    {
        var options = Options.Create(new FileStorageOptions
        {
            Provider = "Local",
            Local = new LocalFileStorageOptions { RootPath = "" }
        });

        var exception = Assert.Throws<InvalidOperationException>(() => new LocalFileStorage(options, TestEnvironment.Create()));
        Assert.Contains("FileStorage:Local:RootPath", exception.Message);
    }

    [Fact]
    public async Task DeleteMissingLocalFileIsSafe()
    {
        var storage = CreateStorage(TestEnvironment.Create());

        await storage.DeleteAsync("/uploads/rental-homes/101/missing.webp", default);
    }

    private static LocalFileStorage CreateStorage(IWebHostEnvironment environment) =>
        new(
            Options.Create(new FileStorageOptions
            {
                Provider = "Local",
                Local = new LocalFileStorageOptions
                {
                    RootPath = "uploads",
                    PublicBasePath = "/uploads"
                }
            }),
            environment);

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
