using DailyRentalHomes.Api.Controllers;
using DailyRentalHomes.Api.Security;
using DailyRentalHomes.Api.Services;
using DailyRentalHomes.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
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
}
