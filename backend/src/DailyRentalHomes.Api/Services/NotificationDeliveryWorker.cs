using DailyRentalHomes.Api.Options;
using Microsoft.Extensions.Options;

namespace DailyRentalHomes.Api.Services;

public sealed class NotificationDeliveryWorker : BackgroundService
{
    internal const string LockKey = "notification-delivery-worker";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationDeliveryWorker> _logger;
    private readonly NotificationWorkerOptions _options;
    private readonly DistributedLockingOptions _lockOptions;

    public NotificationDeliveryWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<NotificationWorkerOptions> options,
        IOptions<BackgroundWorkerOptions> backgroundWorkerOptions,
        ILogger<NotificationDeliveryWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
        _lockOptions = backgroundWorkerOptions.Value.DistributedLocking;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.WorkerEnabled)
        {
            _logger.LogInformation("Notification delivery worker is disabled.");
            return;
        }

        var pollSeconds = Math.Max(1, _options.PollSeconds);
        var batchSize = Math.Clamp(_options.BatchSize, 1, 100);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunCycleAsync(batchSize, stoppingToken);

            await Task.Delay(TimeSpan.FromSeconds(pollSeconds), stoppingToken);
        }
    }

    internal async Task RunCycleAsync(int batchSize, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            if (_lockOptions.Enabled)
            {
                var lockManager = scope.ServiceProvider.GetRequiredService<IDistributedLockManager>();
                await using var lease = await lockManager.TryAcquireAsync(
                    LockKey,
                    TimeSpan.FromSeconds(Math.Max(1, _lockOptions.LeaseSeconds)),
                    cancellationToken);
                if (lease is null)
                {
                    _logger.LogDebug("Notification delivery worker lock is held by another instance; skipping cycle.");
                    return;
                }

                await ProcessAsync(scope.ServiceProvider, batchSize, cancellationToken);
                return;
            }

            await ProcessAsync(scope.ServiceProvider, batchSize, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Notification delivery worker iteration failed.");
        }
    }

    private async Task ProcessAsync(IServiceProvider serviceProvider, int batchSize, CancellationToken cancellationToken)
    {
        var service = serviceProvider.GetRequiredService<NotificationDeliveryService>();
        var summary = await service.ProcessPendingAsync(batchSize, cancellationToken);
        if (summary.Processed > 0)
        {
            _logger.LogInformation(
                "Notification delivery processed {Processed} messages: {Sent} sent, {Failed} failed.",
                summary.Processed,
                summary.Sent,
                summary.Failed);
        }
    }
}
