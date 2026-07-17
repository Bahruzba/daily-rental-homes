using DailyRentalHomes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DailyRentalHomes.Api.Services;

public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly AppDbContext _db;

    public DatabaseHealthCheck(AppDbContext db) => _db = db;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _db.Database.CanConnectAsync(cancellationToken)
                ? await CheckMigrationsAsync(cancellationToken)
                : HealthCheckResult.Unhealthy("Database connection is unavailable.");
        }
        catch
        {
            return HealthCheckResult.Unhealthy("Database connection check failed.");
        }
    }

    private async Task<HealthCheckResult> CheckMigrationsAsync(CancellationToken cancellationToken)
    {
        var pendingMigrations = await _db.Database.GetPendingMigrationsAsync(cancellationToken);
        return pendingMigrations.Any()
            ? HealthCheckResult.Unhealthy("Database has pending migrations.")
            : HealthCheckResult.Healthy("Database connection and schema are available.");
    }
}
