using DailyRentalHomes.Api.Options;
using Microsoft.Extensions.Options;

namespace DailyRentalHomes.Api.Services;

public sealed class NotificationDeliveryWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationDeliveryWorker> _logger;
    private readonly NotificationWorkerOptions _options;

    public NotificationDeliveryWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<NotificationWorkerOptions> options,
        ILogger<NotificationDeliveryWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
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
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<NotificationDeliveryService>();
                var summary = await service.ProcessPendingAsync(batchSize, stoppingToken);
                if (summary.Processed > 0)
                {
                    _logger.LogInformation(
                        "Notification delivery processed {Processed} messages: {Sent} sent, {Failed} failed.",
                        summary.Processed,
                        summary.Sent,
                        summary.Failed);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Notification delivery worker iteration failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(pollSeconds), stoppingToken);
        }
    }
}
