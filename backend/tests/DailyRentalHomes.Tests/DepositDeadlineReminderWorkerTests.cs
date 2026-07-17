using DailyRentalHomes.Api.Options;
using DailyRentalHomes.Api.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DailyRentalHomes.Tests;

public sealed class DepositDeadlineReminderWorkerTests
{
    [Fact]
    public async Task WorkerInvokesExistingReminderProcessor()
    {
        var processor = new FakeReminderProcessor();
        var worker = Worker(processor);

        await worker.RunCycleAsync(default);

        Assert.Equal(1, processor.Calls);
    }

    [Fact]
    public async Task ProcessingExceptionDoesNotStopLaterCycles()
    {
        var processor = new FakeReminderProcessor { ThrowOnCall = 1 };
        var worker = Worker(processor);

        await worker.RunCycleAsync(default);
        await worker.RunCycleAsync(default);

        Assert.Equal(2, processor.Calls);
    }

    [Fact]
    public async Task CancellationStopsWorkerCycleCleanly()
    {
        var processor = new FakeReminderProcessor { ThrowCancellation = true };
        var worker = Worker(processor);
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() => worker.RunCycleAsync(source.Token));
    }

    [Fact]
    public void TooSmallProcessingIntervalUsesSafeMinimum()
    {
        var processor = new FakeReminderProcessor();
        var worker = Worker(processor, processingIntervalMinutes: 0);

        Assert.Equal(DepositDeadlineReminderWorker.MinimumInterval, worker.GetProcessingInterval());
    }

    private static DepositDeadlineReminderWorker Worker(FakeReminderProcessor processor, int processingIntervalMinutes = 15)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDepositDeadlineReminderProcessingService>(processor);
        var provider = services.BuildServiceProvider();
        return new DepositDeadlineReminderWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new DepositReminderOptions { ProcessingIntervalMinutes = processingIntervalMinutes }),
            Options.Create(new BackgroundWorkerOptions { DistributedLocking = new DistributedLockingOptions { Enabled = false, LeaseSeconds = 120 } }),
            NullLogger<DepositDeadlineReminderWorker>.Instance);
    }

    private sealed class FakeReminderProcessor : IDepositDeadlineReminderProcessingService
    {
        public int Calls { get; private set; }
        public int ThrowOnCall { get; set; }
        public bool ThrowCancellation { get; set; }

        public Task<DepositDeadlineReminderProcessingResult> ProcessAsync(CancellationToken cancellationToken)
        {
            Calls++;
            if (ThrowCancellation)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            if (ThrowOnCall == Calls)
            {
                throw new InvalidOperationException("Test failure");
            }

            return Task.FromResult(new DepositDeadlineReminderProcessingResult(1, 1, 1, 0));
        }
    }
}
