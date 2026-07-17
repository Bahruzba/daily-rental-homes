using DailyRentalHomes.Api.Options;
using DailyRentalHomes.Api.Services;
using DailyRentalHomes.Domain.Entities;
using DailyRentalHomes.Domain.Enums;
using DailyRentalHomes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DailyRentalHomes.Tests;

public sealed class DistributedLockingTests
{
    [Fact]
    public async Task FirstInstanceAcquiresLockSuccessfully()
    {
        await using var context = CreateContext();
        var manager = Manager(context);

        await using var lease = await manager.TryAcquireAsync("notification-delivery-worker", TimeSpan.FromMinutes(2), default);

        Assert.NotNull(lease);
        Assert.Equal("notification-delivery-worker", lease.Key);
    }

    [Fact]
    public async Task SecondInstanceCannotAcquireSameActiveLock()
    {
        var databaseName = Guid.NewGuid().ToString();
        var root = new InMemoryDatabaseRoot();
        await using var firstContext = CreateContext(databaseName, root);
        await using var secondContext = CreateContext(databaseName, root);
        var first = Manager(firstContext);
        var second = Manager(secondContext);

        await using var firstLease = await first.TryAcquireAsync("notification-delivery-worker", TimeSpan.FromMinutes(2), default);
        var secondLease = await second.TryAcquireAsync("notification-delivery-worker", TimeSpan.FromMinutes(2), default);

        Assert.NotNull(firstLease);
        Assert.Null(secondLease);
    }

    [Fact]
    public async Task DifferentLockKeysCanBeAcquiredIndependently()
    {
        var databaseName = Guid.NewGuid().ToString();
        var root = new InMemoryDatabaseRoot();
        await using var firstContext = CreateContext(databaseName, root);
        await using var secondContext = CreateContext(databaseName, root);
        var first = Manager(firstContext);
        var second = Manager(secondContext);

        await using var firstLease = await first.TryAcquireAsync("notification-delivery-worker", TimeSpan.FromMinutes(2), default);
        await using var secondLease = await second.TryAcquireAsync("deposit-deadline-reminder-worker", TimeSpan.FromMinutes(2), default);

        Assert.NotNull(firstLease);
        Assert.NotNull(secondLease);
    }

    [Fact]
    public async Task ExpiredLockCanBeAcquiredByAnotherOwner()
    {
        var databaseName = Guid.NewGuid().ToString();
        var root = new InMemoryDatabaseRoot();
        await using var firstContext = CreateContext(databaseName, root);
        await using var secondContext = CreateContext(databaseName, root);
        var first = Manager(firstContext);
        var second = Manager(secondContext);

        await using var firstLease = await first.TryAcquireAsync("notification-delivery-worker", TimeSpan.FromMilliseconds(1), default);
        var stored = await firstContext.DistributedLocks.SingleAsync();
        stored.ExpiresAt = DateTime.UtcNow.AddSeconds(-1);
        await firstContext.SaveChangesAsync();

        await using var secondLease = await second.TryAcquireAsync("notification-delivery-worker", TimeSpan.FromMinutes(2), default);

        Assert.NotNull(firstLease);
        Assert.NotNull(secondLease);
        Assert.NotEqual(firstLease.OwnerId, secondLease.OwnerId);
    }

    [Fact]
    public async Task LockOwnerCanReleaseOwnLock()
    {
        await using var context = CreateContext();
        var manager = Manager(context);
        await using var lease = await manager.TryAcquireAsync("notification-delivery-worker", TimeSpan.FromMinutes(2), default);

        var released = await manager.ReleaseAsync(lease!.Key, lease.OwnerId, default);

        Assert.True(released);
        Assert.True((await context.DistributedLocks.IgnoreQueryFilters().SingleAsync()).IsDeleted);
    }

    [Fact]
    public async Task OneOwnerCannotReleaseAnotherOwnersActiveLock()
    {
        await using var context = CreateContext();
        var manager = Manager(context);
        await using var lease = await manager.TryAcquireAsync("notification-delivery-worker", TimeSpan.FromMinutes(2), default);

        var released = await manager.ReleaseAsync(lease!.Key, "another-owner", default);

        Assert.False(released);
        Assert.False((await context.DistributedLocks.IgnoreQueryFilters().SingleAsync()).IsDeleted);
    }

    [Fact]
    public async Task NotificationWorkerSkipsProcessingWhenLockIsUnavailable()
    {
        await using var context = CreateContext();
        context.OutboundMessages.Add(Message(1));
        await context.SaveChangesAsync();
        var lockManager = new SequenceLockManager(false);
        var provider = new RecordingNotificationDeliveryProvider(NotificationDeliveryResult.Sent("unused"));
        var worker = NotificationWorker(context, provider, lockManager);

        await worker.RunCycleAsync(20, default);

        Assert.Equal(0, provider.Calls);
        Assert.Equal(MessageStatus.Pending, (await context.OutboundMessages.SingleAsync()).Status);
    }

    [Fact]
    public async Task DepositReminderWorkerSkipsProcessingWhenLockIsUnavailable()
    {
        var processor = new FakeReminderProcessor();
        var worker = DepositReminderWorker(processor, new SequenceLockManager(false));

        await worker.RunCycleAsync(default);

        Assert.Equal(0, processor.Calls);
    }

    [Fact]
    public async Task WorkerContinuesNormallyOnLaterCycleAfterLockBecomesAvailable()
    {
        await using var context = CreateContext();
        context.OutboundMessages.Add(Message(1));
        await context.SaveChangesAsync();
        var lockManager = new SequenceLockManager(false, true);
        var provider = new RecordingNotificationDeliveryProvider(NotificationDeliveryResult.Sent("provider-1"));
        var worker = NotificationWorker(context, provider, lockManager);

        await worker.RunCycleAsync(20, default);
        await worker.RunCycleAsync(20, default);

        Assert.Equal(1, provider.Calls);
        Assert.Equal(MessageStatus.Sent, (await context.OutboundMessages.SingleAsync()).Status);
    }

    [Fact]
    public async Task LockFailureDoesNotCrashWorkerCycle()
    {
        var processor = new FakeReminderProcessor();
        var worker = DepositReminderWorker(processor, new ThrowingLockManager());

        await worker.RunCycleAsync(default);

        Assert.Equal(0, processor.Calls);
    }

    private static AppDbContext CreateContext() => CreateContext(Guid.NewGuid().ToString(), new InMemoryDatabaseRoot());

    private static AppDbContext CreateContext(string databaseName, InMemoryDatabaseRoot root)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName, root)
            .Options;
        return new AppDbContext(options);
    }

    private static DatabaseDistributedLockManager Manager(AppDbContext context) =>
        new(context, NullLogger<DatabaseDistributedLockManager>.Instance);

    private static NotificationDeliveryWorker NotificationWorker(
        AppDbContext context,
        INotificationDeliveryProvider provider,
        IDistributedLockManager lockManager)
    {
        var services = new ServiceCollection();
        services.AddSingleton(context);
        services.AddSingleton(provider);
        services.AddScoped<NotificationDeliveryService>();
        services.AddSingleton(lockManager);
        var serviceProvider = services.BuildServiceProvider();
        return new NotificationDeliveryWorker(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new NotificationWorkerOptions { WorkerEnabled = true, BatchSize = 20, PollSeconds = 1 }),
            Options.Create(new BackgroundWorkerOptions { DistributedLocking = new DistributedLockingOptions { Enabled = true, LeaseSeconds = 120 } }),
            NullLogger<NotificationDeliveryWorker>.Instance);
    }

    private static DepositDeadlineReminderWorker DepositReminderWorker(
        IDepositDeadlineReminderProcessingService processor,
        IDistributedLockManager lockManager)
    {
        var services = new ServiceCollection();
        services.AddSingleton(processor);
        services.AddSingleton(lockManager);
        var serviceProvider = services.BuildServiceProvider();
        return new DepositDeadlineReminderWorker(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new DepositReminderOptions { ProcessingIntervalMinutes = 15 }),
            Options.Create(new BackgroundWorkerOptions { DistributedLocking = new DistributedLockingOptions { Enabled = true, LeaseSeconds = 120 } }),
            NullLogger<DepositDeadlineReminderWorker>.Instance);
    }

    private static OutboundMessage Message(long id) => new()
    {
        Id = id,
        Channel = MessageChannel.WhatsApp,
        Status = MessageStatus.Pending,
        TypeCode = "test",
        Title = "Title",
        To = "+994501234567",
        Text = "Text",
        ScheduledAt = DateTime.UtcNow.AddMinutes(-1)
    };

    private sealed class SequenceLockManager : IDistributedLockManager
    {
        private readonly Queue<bool> _results;

        public SequenceLockManager(params bool[] results)
        {
            _results = new Queue<bool>(results);
        }

        public Task<IDistributedLockLease?> TryAcquireAsync(string key, TimeSpan leaseDuration, CancellationToken cancellationToken)
        {
            var result = _results.Count == 0 || _results.Dequeue();
            return Task.FromResult<IDistributedLockLease?>(result ? new FakeLease(key) : null);
        }

        public Task<bool> ReleaseAsync(string key, string ownerId, CancellationToken cancellationToken) => Task.FromResult(true);
    }

    private sealed class RecordingNotificationDeliveryProvider : INotificationDeliveryProvider
    {
        private readonly NotificationDeliveryResult _result;

        public RecordingNotificationDeliveryProvider(NotificationDeliveryResult result)
        {
            _result = result;
        }

        public int Calls { get; private set; }

        public Task<NotificationDeliveryResult> SendAsync(OutboundMessage message, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(_result);
        }
    }

    private sealed class ThrowingLockManager : IDistributedLockManager
    {
        public Task<IDistributedLockLease?> TryAcquireAsync(string key, TimeSpan leaseDuration, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Lock backend unavailable.");

        public Task<bool> ReleaseAsync(string key, string ownerId, CancellationToken cancellationToken) => Task.FromResult(false);
    }

    private sealed class FakeLease : IDistributedLockLease
    {
        public FakeLease(string key)
        {
            Key = key;
        }

        public string Key { get; }
        public string OwnerId { get; } = "fake-owner";
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class FakeReminderProcessor : IDepositDeadlineReminderProcessingService
    {
        public int Calls { get; private set; }

        public Task<DepositDeadlineReminderProcessingResult> ProcessAsync(CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(new DepositDeadlineReminderProcessingResult(1, 1, 1, 0));
        }
    }
}
