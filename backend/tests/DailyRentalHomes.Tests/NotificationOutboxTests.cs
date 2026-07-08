using DailyRentalHomes.Api.Controllers;
using DailyRentalHomes.Api.Contracts.Notifications;
using DailyRentalHomes.Api.Options;
using DailyRentalHomes.Api.Security;
using DailyRentalHomes.Api.Services;
using DailyRentalHomes.Api.Common;
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

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static NotificationDeliveryService DeliveryService(AppDbContext context) =>
        new(context, new FakeNotificationDeliveryProvider());

    private static AdminNotificationsController AdminController(AppDbContext context) => new(context, DeliveryService(context))
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
}
