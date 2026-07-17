using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using DailyRentalHomes.Api.Options;
using Microsoft.Extensions.Options;

namespace DailyRentalHomes.Api.Storage;

public static class FileStorageServiceCollectionExtensions
{
    public static IServiceCollection AddFileStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<FileStorageOptions>()
            .Bind(configuration.GetSection(FileStorageOptions.SectionName))
            .Validate(ValidateProvider, "FileStorage:Provider must be either Local or S3.")
            .Validate(ValidateLocalOptions, "FileStorage:Local:RootPath is required when FileStorage:Provider is Local.")
            .Validate(ValidateS3Options, "FileStorage:S3:BucketName is required when FileStorage:Provider is S3.")
            .Validate(ValidateS3Credentials, "FileStorage:S3:AccessKey and FileStorage:S3:SecretKey must be provided together.")
            .ValidateOnStart();

        services.AddSingleton<IAmazonS3>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<FileStorageOptions>>().Value.S3;
            var config = new AmazonS3Config
            {
                ForcePathStyle = options.ForcePathStyle
            };

            if (!string.IsNullOrWhiteSpace(options.ServiceUrl))
            {
                config.ServiceURL = options.ServiceUrl.Trim();
            }
            else if (!string.IsNullOrWhiteSpace(options.Region))
            {
                config.RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region.Trim());
            }

            var hasAccessKey = !string.IsNullOrWhiteSpace(options.AccessKey);
            var hasSecretKey = !string.IsNullOrWhiteSpace(options.SecretKey);
            return hasAccessKey && hasSecretKey
                ? new AmazonS3Client(new BasicAWSCredentials(options.AccessKey.Trim(), options.SecretKey.Trim()), config)
                : new AmazonS3Client(config);
        });

        services.AddSingleton<IS3StorageClient, AwsS3StorageClient>();
        services.AddScoped<LocalFileStorage>();
        services.AddScoped<S3FileStorage>();
        services.AddScoped<IFileStorage>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<FileStorageOptions>>().Value;
            if (FileStorageProviderNames.IsLocal(options.Provider))
            {
                return serviceProvider.GetRequiredService<LocalFileStorage>();
            }

            if (FileStorageProviderNames.IsS3(options.Provider))
            {
                return serviceProvider.GetRequiredService<S3FileStorage>();
            }

            throw new InvalidOperationException("FileStorage:Provider must be either Local or S3.");
        });

        return services;
    }

    private static bool ValidateProvider(FileStorageOptions options) =>
        FileStorageProviderNames.IsLocal(options.Provider) || FileStorageProviderNames.IsS3(options.Provider);

    private static bool ValidateLocalOptions(FileStorageOptions options) =>
        !FileStorageProviderNames.IsLocal(options.Provider) || !string.IsNullOrWhiteSpace(options.Local.RootPath);

    private static bool ValidateS3Options(FileStorageOptions options) =>
        !FileStorageProviderNames.IsS3(options.Provider) || !string.IsNullOrWhiteSpace(options.S3.BucketName);

    private static bool ValidateS3Credentials(FileStorageOptions options)
    {
        if (!FileStorageProviderNames.IsS3(options.Provider)) return true;
        var hasAccessKey = !string.IsNullOrWhiteSpace(options.S3.AccessKey);
        var hasSecretKey = !string.IsNullOrWhiteSpace(options.S3.SecretKey);
        return hasAccessKey == hasSecretKey;
    }
}
