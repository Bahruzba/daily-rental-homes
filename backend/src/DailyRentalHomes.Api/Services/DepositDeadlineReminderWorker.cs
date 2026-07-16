using DailyRentalHomes.Api.Options;
using Microsoft.Extensions.Options;

namespace DailyRentalHomes.Api.Services;

public sealed class DepositDeadlineReminderWorker : BackgroundService
{
    internal const string LockKey = "deposit-deadline-reminder-worker";
    internal static readonly TimeSpan MinimumInterval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DepositDeadlineReminderWorker> _logger;
    private readonly DepositReminderOptions _options;
    private readonly DistributedLockingOptions _lockOptions;

    public DepositDeadlineReminderWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<DepositReminderOptions> options,
        IOptions<BackgroundWorkerOptions> backgroundWorkerOptions,
        ILogger<DepositDeadlineReminderWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
        _lockOptions = backgroundWorkerOptions.Value.DistributedLocking;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await RunCycleAsync(stoppingToken);

            try
            {
                await Task.Delay(GetProcessingInterval(), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    internal async Task RunCycleAsync(CancellationToken cancellationToken)
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
                    _logger.LogDebug("Deposit deadline reminder worker lock is held by another instance; skipping cycle.");
                    return;
                }

                await ProcessAsync(scope.ServiceProvider, cancellationToken);
                return;
            }

            await ProcessAsync(scope.ServiceProvider, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Deposit deadline reminder worker iteration failed.");
        }
    }

    internal TimeSpan GetProcessingInterval()
    {
        var configured = TimeSpan.FromMinutes(_options.ProcessingIntervalMinutes);
        return configured < MinimumInterval ? MinimumInterval : configured;
    }

    private async Task ProcessAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var processor = serviceProvider.GetRequiredService<IDepositDeadlineReminderProcessingService>();
        var result = await processor.ProcessAsync(cancellationToken);

        _logger.LogInformation(
            "Deposit deadline reminder processing evaluated {Evaluated} deposits: {Eligible} eligible, {Queued} queued, {DuplicateSkipped} duplicate skipped.",
            result.Evaluated,
            result.Eligible,
            result.Queued,
            result.DuplicateSkipped);
    }
}
