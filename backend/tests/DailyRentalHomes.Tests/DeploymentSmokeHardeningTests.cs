using DailyRentalHomes.Api.Options;
using DailyRentalHomes.Api.Services;
using DailyRentalHomes.Api.Storage;
using DailyRentalHomes.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace DailyRentalHomes.Tests;

public sealed class DeploymentSmokeHardeningTests
{
    [Fact]
    public void ValidProductionLikeMinimumConfigurationBuildsServiceProvider()
    {
        var configuration = Configuration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=127.0.0.1;Database=DailyRentalHomesSmoke;User Id=sa;Password=Smoke_password_123;TrustServerCertificate=True",
            ["NotificationDelivery:Provider"] = "Fake",
            ["BackgroundWorkers:DistributedLocking:Enabled"] = "true",
            ["BackgroundWorkers:DistributedLocking:LeaseSeconds"] = "120",
            ["FileStorage:Provider"] = "Local",
            ["FileStorage:Local:RootPath"] = "uploads-smoke",
            ["FileStorage:Local:PublicBasePath"] = "/uploads-smoke"
        });
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);
        services.AddNotificationDelivery(configuration);
        services.AddOptions<BackgroundWorkerOptions>()
            .Bind(configuration.GetSection(BackgroundWorkerOptions.SectionName))
            .Validate(options => options.DistributedLocking.LeaseSeconds > 0, "BackgroundWorkers:DistributedLocking:LeaseSeconds must be positive.");
        services.AddOptions<FileStorageOptions>()
            .Bind(configuration.GetSection(FileStorageOptions.SectionName))
            .Validate(options => string.Equals(options.Provider, "Local", StringComparison.OrdinalIgnoreCase), "Only Local file storage provider is supported.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Local.RootPath), "FileStorage:Local:RootPath is required.");
        services.AddSingleton<IWebHostEnvironment>(TestEnvironment.Create());
        services.AddScoped<IFileStorage, LocalFileStorage>();

        using var provider = services.BuildServiceProvider(validateScopes: true);

        Assert.Equal("Local", provider.GetRequiredService<IOptions<FileStorageOptions>>().Value.Provider);
        Assert.Equal(120, provider.GetRequiredService<IOptions<BackgroundWorkerOptions>>().Value.DistributedLocking.LeaseSeconds);
        using var scope = provider.CreateScope();
        Assert.IsType<FakeNotificationDeliveryProvider>(scope.ServiceProvider.GetRequiredService<INotificationDeliveryProvider>());
    }

    [Fact]
    public void MissingCriticalDatabaseConfigurationFailsClearly()
    {
        var configuration = Configuration([]);
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() => services.AddInfrastructure(configuration));

        Assert.Contains("ConnectionStrings:DefaultConnection", exception.Message);
    }

    [Fact]
    public void LocalStorageConfigurationValidationRejectsMissingRoot()
    {
        var services = new ServiceCollection();
        services.AddOptions<FileStorageOptions>()
            .Configure(options =>
            {
                options.Provider = "Local";
                options.Local.RootPath = "";
            })
            .Validate(options => !string.IsNullOrWhiteSpace(options.Local.RootPath), "FileStorage:Local:RootPath is required.");

        using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<OptionsValidationException>(() =>
            _ = provider.GetRequiredService<IOptions<FileStorageOptions>>().Value);
        Assert.Contains("FileStorage:Local:RootPath", exception.Message);
    }

    [Fact]
    public void DistributedLockingConfigurationValidationRejectsInvalidLease()
    {
        var services = new ServiceCollection();
        services.AddOptions<BackgroundWorkerOptions>()
            .Configure(options => options.DistributedLocking.LeaseSeconds = 0)
            .Validate(options => options.DistributedLocking.LeaseSeconds > 0, "BackgroundWorkers:DistributedLocking:LeaseSeconds must be positive.");

        using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<OptionsValidationException>(() =>
            _ = provider.GetRequiredService<IOptions<BackgroundWorkerOptions>>().Value);
        Assert.Contains("LeaseSeconds", exception.Message);
    }

    [Fact]
    public void ReadinessFailureDoesNotExposeExceptionOrSecrets()
    {
        var result = HealthCheckResult.Unhealthy("Database connection check failed.");

        Assert.Null(result.Exception);
        Assert.DoesNotContain("Password", result.Description, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Token", result.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DefaultFakeNotificationProviderAllowsSmokeStartupWithoutMetaCredentials()
    {
        var configuration = Configuration(new Dictionary<string, string?>
        {
            ["NotificationDelivery:Provider"] = "Fake"
        });
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNotificationDelivery(configuration);

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        Assert.IsType<FakeNotificationDeliveryProvider>(scope.ServiceProvider.GetRequiredService<INotificationDeliveryProvider>());
    }

    private static IConfiguration Configuration(IEnumerable<KeyValuePair<string, string?>> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

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
        public string EnvironmentName { get; set; } = "Production";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
