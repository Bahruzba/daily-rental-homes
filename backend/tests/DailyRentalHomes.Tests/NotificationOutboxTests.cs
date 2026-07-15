using DailyRentalHomes.Api.Controllers;
using DailyRentalHomes.Api.Contracts.Notifications;
using DailyRentalHomes.Api.Options;
using DailyRentalHomes.Api.Security;
using DailyRentalHomes.Api.Services;
using DailyRentalHomes.Api.Common;
using DailyRentalHomes.Domain.Constants;
using DailyRentalHomes.Domain.Entities;
using DailyRentalHomes.Infrastructure.Persistence;
using DailyRentalHomes.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace DailyRentalHomes.Tests;

public sealed class NotificationOutboxTests
{
    [Fact]
    public void ReminderMoreThanThreeHoursAwayIsScheduledTwoHoursBeforeDeadline()
    {
        var now = new DateTime(2026, 7, 3, 10, 0, 0, DateTimeKind.Utc);
        var deadline = now.AddHours(5);

        Assert.Equal(deadline.AddHours(-2), NotificationOutboxService.GetReminderAt(deadline, now));
    }

    [Fact]
    public void ReminderWithinThreeHoursIsScheduledThirtyMinutesBeforeDeadline()
    {
        var now = new DateTime(2026, 7, 3, 10, 0, 0, DateTimeKind.Utc);
        var deadline = now.AddHours(2);

        Assert.Equal(deadline.AddMinutes(-30), NotificationOutboxService.GetReminderAt(deadline, now));
    }

    [Fact]
    public void ReminderIsSkippedWhenDeadlineIsTooClose()
    {
        var now = new DateTime(2026, 7, 3, 10, 0, 0, DateTimeKind.Utc);

        Assert.Null(NotificationOutboxService.GetReminderAt(now.AddMinutes(20), now));
    }

    [Fact]
    public async Task CustomerCannotReadAdminNotificationEndpoint()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(AuthorizationPolicies.Configure);
        var authorization = services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();
        var customer = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, nameof(UserRole.Customer))],
            "test", ClaimTypes.Name, ClaimTypes.Role));

        var result = await authorization.AuthorizeAsync(customer, null, AuthorizationPolicies.AdminOnly);

        Assert.False(result.Succeeded);
        var attribute = Assert.Single(typeof(AdminNotificationsController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>());
        Assert.Equal(AuthorizationPolicies.AdminOnly, attribute.Policy);
    }

    [Fact]
    public async Task PendingDueNotificationIsMarkedSentByFakeProvider()
    {
        await using var context = CreateContext();
        context.OutboundMessages.Add(Message(1, "Normal title", "Normal text", DateTime.UtcNow.AddMinutes(-1)));
        await context.SaveChangesAsync();
        var service = DeliveryService(context);

        var summary = await service.ProcessPendingAsync(20, default);

        var message = await context.OutboundMessages.SingleAsync();
        Assert.Equal(1, summary.Processed);
        Assert.Equal(1, summary.Sent);
        Assert.Equal(MessageStatus.Sent, message.Status);
        Assert.NotNull(message.SentAt);
        Assert.Equal("fake-1", message.ProviderMessageId);
        Assert.Null(message.ErrorMessage);
    }

    [Fact]
    public async Task QueuedNotificationIsDeliveredThroughProviderAbstraction()
    {
        await using var context = CreateContext();
        context.OutboundMessages.Add(Message(4, "Provider test", "Text", DateTime.UtcNow.AddMinutes(-1)));
        await context.SaveChangesAsync();
        var provider = new RecordingNotificationDeliveryProvider(NotificationDeliveryResult.Sent("provider-123"));
        var service = DeliveryService(context, provider);

        var summary = await service.ProcessPendingAsync(20, default);

        var message = await context.OutboundMessages.SingleAsync();
        Assert.Equal(1, provider.Calls);
        Assert.Equal(1, summary.Sent);
        Assert.Equal(MessageStatus.Sent, message.Status);
        Assert.Equal("provider-123", message.ProviderMessageId);
    }

    [Fact]
    public async Task ProviderFailureResultFollowsExistingFailureFlow()
    {
        await using var context = CreateContext();
        context.OutboundMessages.Add(Message(5, "Provider failure", "Text", DateTime.UtcNow.AddMinutes(-1)));
        await context.SaveChangesAsync();
        var service = DeliveryService(context, new RecordingNotificationDeliveryProvider(
            NotificationDeliveryResult.Failed("Provider rejected message.")));

        var summary = await service.ProcessPendingAsync(20, default);

        var message = await context.OutboundMessages.SingleAsync();
        Assert.Equal(1, summary.Failed);
        Assert.Equal(MessageStatus.Failed, message.Status);
        Assert.Equal("Provider rejected message.", message.ErrorMessage);
        Assert.Null(message.ProviderMessageId);
        Assert.Null(message.SentAt);
    }

    [Fact]
    public async Task ProviderExceptionDoesNotCrashProcessingPass()
    {
        await using var context = CreateContext();
        context.OutboundMessages.Add(Message(6, "Provider exception", "Text", DateTime.UtcNow.AddMinutes(-1)));
        await context.SaveChangesAsync();
        var service = DeliveryService(context, new RecordingNotificationDeliveryProvider(
            NotificationDeliveryResult.Sent("unused"),
            throwOnSend: true));

        var summary = await service.ProcessPendingAsync(20, default);

        var message = await context.OutboundMessages.SingleAsync();
        Assert.Equal(1, summary.Processed);
        Assert.Equal(1, summary.Failed);
        Assert.Equal(1, summary.Retried);
        Assert.Equal(MessageStatus.Pending, message.Status);
        Assert.Equal(1, message.DeliveryAttemptCount);
        Assert.NotNull(message.NextAttemptAt);
        Assert.Contains("unexpected error", message.ErrorMessage);
    }

    [Fact]
    public async Task RetryableFailureSchedulesAnotherAttempt()
    {
        await using var context = CreateContext();
        context.OutboundMessages.Add(Message(20, "Retry", "Text", DateTime.UtcNow.AddMinutes(-1)));
        await context.SaveChangesAsync();
        var service = DeliveryService(context, new RecordingNotificationDeliveryProvider(
            NotificationDeliveryResult.RetryableFailed("Temporary provider failure.")));

        var summary = await service.ProcessPendingAsync(20, default);

        var message = await context.OutboundMessages.SingleAsync();
        Assert.Equal(1, summary.Failed);
        Assert.Equal(1, summary.Retried);
        Assert.Equal(MessageStatus.Pending, message.Status);
        Assert.Equal(1, message.DeliveryAttemptCount);
        Assert.NotNull(message.LastAttemptAt);
        Assert.NotNull(message.NextAttemptAt);
        Assert.True(message.NextAttemptAt > message.LastAttemptAt);
        Assert.Equal("Temporary provider failure.", message.ErrorMessage);
    }

    [Fact]
    public async Task BackoffIncreasesBetweenAttempts()
    {
        await using var context = CreateContext();
        context.OutboundMessages.Add(Message(21, "Retry", "Text", DateTime.UtcNow.AddMinutes(-1)));
        await context.SaveChangesAsync();
        var service = DeliveryService(context, new RecordingNotificationDeliveryProvider(
            NotificationDeliveryResult.RetryableFailed("Temporary provider failure.")));

        await service.ProcessPendingAsync(20, default);
        var first = await context.OutboundMessages.SingleAsync();
        first.NextAttemptAt = DateTime.UtcNow.AddMinutes(-1);
        await context.SaveChangesAsync();

        await service.ProcessPendingAsync(20, default);

        var second = await context.OutboundMessages.SingleAsync();
        Assert.Equal(2, second.DeliveryAttemptCount);
        Assert.NotNull(second.LastAttemptAt);
        Assert.NotNull(second.NextAttemptAt);
        Assert.True(second.NextAttemptAt.Value - second.LastAttemptAt.Value >= TimeSpan.FromMinutes(4));
    }

    [Fact]
    public void BackoffIsCappedAtConfiguredMaximum()
    {
        var now = new DateTime(2026, 7, 15, 10, 0, 0, DateTimeKind.Utc);
        var options = new NotificationRetryOptions
        {
            InitialDelayMinutes = 10,
            MaxDelayMinutes = 15
        };

        var nextAttempt = NotificationDeliveryService.CalculateNextAttemptAt(now, attemptCount: 5, options);

        Assert.Equal(now.AddMinutes(15), nextAttempt);
    }

    [Fact]
    public async Task PendingRetryIsNotProcessedBeforeNextAttemptTime()
    {
        await using var context = CreateContext();
        var message = Message(22, "Retry later", "Text", DateTime.UtcNow.AddMinutes(-10));
        message.NextAttemptAt = DateTime.UtcNow.AddHours(1);
        context.OutboundMessages.Add(message);
        await context.SaveChangesAsync();
        var provider = new RecordingNotificationDeliveryProvider(NotificationDeliveryResult.Sent("unused"));
        var service = DeliveryService(context, provider);

        var summary = await service.ProcessPendingAsync(20, default);

        Assert.Equal(0, summary.Processed);
        Assert.Equal(0, provider.Calls);
        Assert.Equal(MessageStatus.Pending, (await context.OutboundMessages.SingleAsync()).Status);
    }

    [Fact]
    public async Task SuccessfulRetryMarksMessageSentAndClearsRetrySchedule()
    {
        await using var context = CreateContext();
        var message = Message(23, "Retry success", "Text", DateTime.UtcNow.AddMinutes(-10));
        message.DeliveryAttemptCount = 1;
        message.NextAttemptAt = DateTime.UtcNow.AddMinutes(-1);
        message.ErrorMessage = "Previous failure.";
        context.OutboundMessages.Add(message);
        await context.SaveChangesAsync();
        var service = DeliveryService(context, new RecordingNotificationDeliveryProvider(NotificationDeliveryResult.Sent("provider-retry-ok")));

        var summary = await service.ProcessPendingAsync(20, default);

        var processed = await context.OutboundMessages.SingleAsync();
        Assert.Equal(1, summary.Sent);
        Assert.Equal(MessageStatus.Sent, processed.Status);
        Assert.Equal(2, processed.DeliveryAttemptCount);
        Assert.Null(processed.NextAttemptAt);
        Assert.Null(processed.ErrorMessage);
        Assert.Equal("provider-retry-ok", processed.ProviderMessageId);
    }

    [Fact]
    public async Task PermanentFailureIsNotAutomaticallyRetried()
    {
        await using var context = CreateContext();
        context.OutboundMessages.Add(Message(24, "Permanent", "Text", DateTime.UtcNow.AddMinutes(-1)));
        await context.SaveChangesAsync();
        var service = DeliveryService(context, new RecordingNotificationDeliveryProvider(
            NotificationDeliveryResult.Failed("Permanent provider failure.")));

        var summary = await service.ProcessPendingAsync(20, default);

        var message = await context.OutboundMessages.SingleAsync();
        Assert.Equal(1, summary.Failed);
        Assert.Equal(0, summary.Retried);
        Assert.Equal(MessageStatus.Failed, message.Status);
        Assert.Equal(1, message.DeliveryAttemptCount);
        Assert.Null(message.NextAttemptAt);
    }

    [Fact]
    public async Task MaximumAttemptsStopFurtherAutomaticRetries()
    {
        await using var context = CreateContext();
        var message = Message(25, "Max", "Text", DateTime.UtcNow.AddMinutes(-10));
        message.DeliveryAttemptCount = 4;
        context.OutboundMessages.Add(message);
        await context.SaveChangesAsync();
        var service = DeliveryService(context, new RecordingNotificationDeliveryProvider(
            NotificationDeliveryResult.RetryableFailed("Still temporary.")));

        var summary = await service.ProcessPendingAsync(20, default);

        var processed = await context.OutboundMessages.SingleAsync();
        Assert.Equal(1, summary.Failed);
        Assert.Equal(0, summary.Retried);
        Assert.Equal(5, processed.DeliveryAttemptCount);
        Assert.Equal(MessageStatus.Failed, processed.Status);
        Assert.Null(processed.NextAttemptAt);
    }

    [Fact]
    public async Task WorkerContinuesProcessingOtherMessagesAfterRetryableFailure()
    {
        await using var context = CreateContext();
        context.OutboundMessages.AddRange(
            Message(26, "Retry", "Text", DateTime.UtcNow.AddMinutes(-1)),
            Message(27, "Success", "Text", DateTime.UtcNow.AddMinutes(-1)));
        await context.SaveChangesAsync();
        var provider = new SequenceNotificationDeliveryProvider(
            NotificationDeliveryResult.RetryableFailed("Temporary."),
            NotificationDeliveryResult.Sent("provider-success"));
        var service = DeliveryService(context, provider);

        var summary = await service.ProcessPendingAsync(20, default);

        Assert.Equal(2, summary.Processed);
        Assert.Equal(1, summary.Sent);
        Assert.Equal(1, summary.Failed);
        Assert.Equal(1, summary.Retried);
        Assert.Contains(await context.OutboundMessages.ToListAsync(), item => item.Id == 26 && item.Status == MessageStatus.Pending);
        Assert.Contains(await context.OutboundMessages.ToListAsync(), item => item.Id == 27 && item.Status == MessageStatus.Sent);
    }

    [Fact]
    public async Task ExistingNotificationTypesContinueProcessingUnchanged()
    {
        await using var context = CreateContext();
        var message = Message(7, "Deposit requested", "Text", DateTime.UtcNow.AddMinutes(-1));
        message.TypeCode = NotificationTypeCodes.DepositRequested;
        context.OutboundMessages.Add(message);
        await context.SaveChangesAsync();
        var service = DeliveryService(context);

        await service.ProcessPendingAsync(20, default);

        var processed = await context.OutboundMessages.SingleAsync();
        Assert.Equal(NotificationTypeCodes.DepositRequested, processed.TypeCode);
        Assert.Equal(MessageStatus.Sent, processed.Status);
    }

    [Fact]
    public async Task FakeProviderDoesNotRequireExternalNetworkConfiguration()
    {
        var provider = new FakeNotificationDeliveryProvider();
        var message = Message(8, "Normal", "Text", DateTime.UtcNow);

        var result = await provider.SendAsync(message, default);

        Assert.True(result.Success);
        Assert.Equal("fake-8", result.ProviderMessageId);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task FailureMarkerMarksNotificationFailed()
    {
        await using var context = CreateContext();
        context.OutboundMessages.Add(Message(2, "FAIL_FAKE_PROVIDER", "Normal text", DateTime.UtcNow.AddMinutes(-1)));
        await context.SaveChangesAsync();
        var service = DeliveryService(context);

        var summary = await service.ProcessPendingAsync(20, default);

        var message = await context.OutboundMessages.SingleAsync();
        Assert.Equal(1, summary.Processed);
        Assert.Equal(1, summary.Failed);
        Assert.Equal(MessageStatus.Failed, message.Status);
        Assert.Null(message.SentAt);
        Assert.Null(message.ProviderMessageId);
        Assert.Contains("Fake provider failure", message.ErrorMessage);
    }

    [Fact]
    public async Task FutureScheduledPendingNotificationIsNotProcessed()
    {
        await using var context = CreateContext();
        context.OutboundMessages.Add(Message(3, "Future", "Text", DateTime.UtcNow.AddHours(1)));
        await context.SaveChangesAsync();
        var service = DeliveryService(context);

        var summary = await service.ProcessPendingAsync(20, default);

        var message = await context.OutboundMessages.SingleAsync();
        Assert.Equal(0, summary.Processed);
        Assert.Equal(MessageStatus.Pending, message.Status);
    }

    [Fact]
    public async Task ManualEndpointProcessesLimitedBatch()
    {
        await using var context = CreateContext();
        context.OutboundMessages.AddRange(
            Message(10, "One", "Text", DateTime.UtcNow.AddMinutes(-1)),
            Message(11, "Two", "Text", DateTime.UtcNow.AddMinutes(-1)),
            Message(12, "Three", "Text", DateTime.UtcNow.AddMinutes(-1)));
        await context.SaveChangesAsync();
        var controller = AdminController(context);

        var response = GetData<ProcessPendingNotificationsResponse>(await controller.ProcessPending(
            new ProcessPendingNotificationsRequest { BatchSize = 2 },
            default));

        Assert.Equal(2, response.Processed);
        Assert.Equal(2, response.Sent);
        Assert.Equal(2, await context.OutboundMessages.CountAsync(item => item.Status == MessageStatus.Sent));
        Assert.Equal(1, await context.OutboundMessages.CountAsync(item => item.Status == MessageStatus.Pending));
    }

    [Fact]
    public async Task ManualEndpointUsesProviderAbstraction()
    {
        await using var context = CreateContext();
        context.OutboundMessages.Add(Message(13, "Manual provider", "Text", DateTime.UtcNow.AddMinutes(-1)));
        await context.SaveChangesAsync();
        var provider = new RecordingNotificationDeliveryProvider(NotificationDeliveryResult.Sent("manual-provider-13"));
        var controller = AdminController(context, provider);

        var response = GetData<ProcessPendingNotificationsResponse>(await controller.ProcessPending(
            new ProcessPendingNotificationsRequest { BatchSize = 20 },
            default));

        var message = await context.OutboundMessages.SingleAsync();
        Assert.Equal(1, provider.Calls);
        Assert.Equal(1, response.Sent);
        Assert.Equal("manual-provider-13", message.ProviderMessageId);
    }

    [Fact]
    public async Task ManualRetryUsesProviderDeliveryPathForFailedMessage()
    {
        await using var context = CreateContext();
        var message = Message(14, "Manual retry", "Text", DateTime.UtcNow.AddMinutes(-10));
        message.Status = MessageStatus.Failed;
        message.DeliveryAttemptCount = 5;
        message.ErrorMessage = "Exhausted.";
        context.OutboundMessages.Add(message);
        await context.SaveChangesAsync();
        var provider = new RecordingNotificationDeliveryProvider(NotificationDeliveryResult.Sent("manual-retry-14"));
        var controller = AdminController(context, provider);

        var response = GetData<ProcessPendingNotificationsResponse>(await controller.Retry(14, default));

        var processed = await context.OutboundMessages.SingleAsync();
        Assert.Equal(1, provider.Calls);
        Assert.Equal(1, response.Sent);
        Assert.Equal(MessageStatus.Sent, processed.Status);
        Assert.Equal("manual-retry-14", processed.ProviderMessageId);
        Assert.Equal(1, processed.DeliveryAttemptCount);
    }

    [Fact]
    public async Task ManualEndpointRejectsInvalidBatchSize()
    {
        await using var context = CreateContext();
        var controller = AdminController(context);

        var result = await controller.ProcessPending(new ProcessPendingNotificationsRequest { BatchSize = 101 }, default);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ManualEndpointRequiresAdmin()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(AuthorizationPolicies.Configure);
        var authorization = services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();
        var broker = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, nameof(UserRole.Broker))],
            "test", ClaimTypes.Name, ClaimTypes.Role));

        var result = await authorization.AuthorizeAsync(broker, null, AuthorizationPolicies.AdminOnly);

        Assert.False(result.Succeeded);
        var attribute = Assert.Single(typeof(AdminNotificationsController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>());
        Assert.Equal(AuthorizationPolicies.AdminOnly, attribute.Policy);
    }

    [Fact]
    public void NotificationWorkerIsDisabledByDefault()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection([]).Build();
        var options = new NotificationWorkerOptions();

        configuration.GetSection(NotificationWorkerOptions.SectionName).Bind(options);

        Assert.False(options.WorkerEnabled);
        Assert.Equal(30, options.PollSeconds);
        Assert.Equal(20, options.BatchSize);
    }

    [Fact]
    public void NotificationDeliveryProviderDefaultsToFake()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection([]).Build();
        var options = new NotificationDeliveryOptions();

        configuration.GetSection(NotificationDeliveryOptions.SectionName).Bind(options);

        Assert.Equal(NotificationDeliveryOptions.FakeProvider, options.Provider);
    }

    [Fact]
    public void InvalidRetryConfigurationFailsOptionsValidation()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNotificationDelivery(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{NotificationDeliveryOptions.SectionName}:Retry:MaxAttempts"] = "0",
                [$"{NotificationDeliveryOptions.SectionName}:Retry:InitialDelayMinutes"] = "2",
                [$"{NotificationDeliveryOptions.SectionName}:Retry:MaxDelayMinutes"] = "1"
            })
            .Build());
        using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<NotificationDeliveryOptions>>().Value);

        Assert.Contains("Notification retry configuration", exception.Message);
    }

    [Fact]
    public void MetaWhatsAppProviderRequiresWebhookVerifyToken()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNotificationDelivery(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{NotificationDeliveryOptions.SectionName}:Provider"] = NotificationDeliveryOptions.MetaWhatsAppProvider,
                [$"{NotificationDeliveryOptions.SectionName}:MetaWhatsApp:PhoneNumberId"] = "123456789",
                [$"{NotificationDeliveryOptions.SectionName}:MetaWhatsApp:AccessToken"] = "test-access-token",
                [$"{NotificationDeliveryOptions.SectionName}:MetaWhatsApp:ApiVersion"] = "v22.0",
                [$"{NotificationDeliveryOptions.SectionName}:MetaWhatsApp:AppSecret"] = "test-app-secret"
            })
            .Build());
        using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<NotificationDeliveryOptions>>().Value);

        Assert.Contains("WebhookVerifyToken", exception.Message);
    }

    [Fact]
    public void NotificationRetryOptionsUseProductionSafeDefaults()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection([]).Build();
        var options = new NotificationDeliveryOptions();

        configuration.GetSection(NotificationDeliveryOptions.SectionName).Bind(options);

        Assert.Equal(5, options.Retry.MaxAttempts);
        Assert.Equal(2, options.Retry.InitialDelayMinutes);
        Assert.Equal(60, options.Retry.MaxDelayMinutes);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static NotificationDeliveryService DeliveryService(AppDbContext context) =>
        new(context, new FakeNotificationDeliveryProvider());

    private static NotificationDeliveryService DeliveryService(AppDbContext context, INotificationDeliveryProvider provider) =>
        new(context, provider);

    private static AdminNotificationsController AdminController(AppDbContext context) => new(context, DeliveryService(context))
    {
        ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = AdminPrincipal() }
        }
    };

    private static AdminNotificationsController AdminController(AppDbContext context, INotificationDeliveryProvider provider) => new(context, DeliveryService(context, provider))
    {
        ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = AdminPrincipal() }
        }
    };

    private static ClaimsPrincipal AdminPrincipal() => new(new ClaimsIdentity(
        [new Claim(JwtRegisteredClaimNames.Sub, "1"), new Claim(ClaimTypes.Role, nameof(UserRole.Admin))],
        "test", ClaimTypes.Name, ClaimTypes.Role));

    private static OutboundMessage Message(long id, string title, string text, DateTime? scheduledAt) => new()
    {
        Id = id,
        Channel = MessageChannel.WhatsApp,
        Status = MessageStatus.Pending,
        TypeCode = "test",
        Title = title,
        To = "+994501234567",
        Text = text,
        ScheduledAt = scheduledAt
    };

    private static T GetData<T>(IActionResult result)
    {
        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<T>>(ok.Value);
        Assert.True(response.Success);
        return Assert.IsAssignableFrom<T>(response.Data);
    }

    private sealed class RecordingNotificationDeliveryProvider : INotificationDeliveryProvider
    {
        private readonly NotificationDeliveryResult _result;
        private readonly bool _throwOnSend;

        public RecordingNotificationDeliveryProvider(NotificationDeliveryResult result, bool throwOnSend = false)
        {
            _result = result;
            _throwOnSend = throwOnSend;
        }

        public int Calls { get; private set; }

        public Task<NotificationDeliveryResult> SendAsync(OutboundMessage message, CancellationToken cancellationToken)
        {
            Calls++;
            if (_throwOnSend)
            {
                throw new InvalidOperationException("Provider exploded.");
            }

            return Task.FromResult(_result);
        }
    }

    private sealed class SequenceNotificationDeliveryProvider : INotificationDeliveryProvider
    {
        private readonly Queue<NotificationDeliveryResult> _results;

        public SequenceNotificationDeliveryProvider(params NotificationDeliveryResult[] results)
        {
            _results = new Queue<NotificationDeliveryResult>(results);
        }

        public Task<NotificationDeliveryResult> SendAsync(OutboundMessage message, CancellationToken cancellationToken) =>
            Task.FromResult(_results.Dequeue());
    }
}
