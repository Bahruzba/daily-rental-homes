using DailyRentalHomes.Api.Storage;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DailyRentalHomes.Api.Services;

public sealed class FileStorageHealthCheck : IHealthCheck
{
    private readonly IFileStorage _fileStorage;

    public FileStorageHealthCheck(IFileStorage fileStorage)
    {
        _fileStorage = fileStorage;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _ = _fileStorage.GetPublicUrl("health/check.txt");
            return Task.FromResult(HealthCheckResult.Healthy("File storage configuration is available."));
        }
        catch
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("File storage configuration check failed."));
        }
    }
}
