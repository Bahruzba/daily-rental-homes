using DailyRentalHomes.Api.Options;
using Microsoft.Extensions.Options;

namespace DailyRentalHomes.Api.Services;

public sealed class DepositDeadlineReminderWorker : BackgroundService
{
    internal static readonly TimeSpan MinimumInterval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DepositDeadlineReminderWorker> _logger;
    private readonly DepositReminderOptions _options;

    public DepositDeadlineReminderWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<DepositReminderOptions> options,
        ILogger<DepositDeadlineReminderWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
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
            var processor = scope.ServiceProvider.GetRequiredService<IDepositDeadlineReminderProcessingService>();
            var result = await processor.ProcessAsync(cancellationToken);

            _logger.LogInformation(
                "Deposit deadline reminder processing evaluated {Evaluated} deposits: {Eligible} eligible, {Queued} queued, {DuplicateSkipped} duplicate skipped.",
                result.Evaluated,
                result.Eligible,
                result.Queued,
                result.DuplicateSkipped);
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
}
