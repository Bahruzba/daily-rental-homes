using DailyRentalHomes.Api.Common;
using DailyRentalHomes.Api.Contracts.Auth;
using DailyRentalHomes.Api.Controllers;
using DailyRentalHomes.Api.Options;
using DailyRentalHomes.Api.Services;
using DailyRentalHomes.Application.Abstractions.Messaging;
using DailyRentalHomes.Domain.Entities;
using DailyRentalHomes.Domain.Enums;
using DailyRentalHomes.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace DailyRentalHomes.Tests;

public sealed class AuthControllerTests
{
    [Theory]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.Broker)]
    [InlineData(UserRole.Customer)]
    public async Task DevelopmentOtpFlowReturnsTokenAndExistingUserRole(UserRole role)
    {
        await using var context = CreateContext();
        context.Users.Add(new User
        {
            Id = 10,
            FullName = $"Test {role}",
            PhoneNumber = "+994501234567",
            Role = role
        });
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        var sendResult = await controller.Send(new PhoneInput { Phone = "+994501234567" }, default);
        var otp = GetResponse<OtpRequestResponse>(sendResult);
        Assert.NotNull(otp.DevPin);

        var confirmResult = await controller.Confirm(new ConfirmInput
        {
            Phone = "+994501234567",
            Pin = otp.DevPin!
        }, default);
        var session = GetResponse<AuthSessionResponse>(confirmResult);

        Assert.False(string.IsNullOrWhiteSpace(session.AccessToken));
        Assert.True(session.ExpiresAt > DateTime.UtcNow);
        Assert.Equal(role.ToString(), session.User.Role);
        Assert.Equal("+994501234567", session.User.Phone);
    }

    [Fact]
    public async Task ValidOtpCreatesCustomerWhenUserDoesNotExist()
    {
        await using var context = CreateContext();
        var controller = CreateController(context);
        var sendResult = await controller.Send(new PhoneInput { Phone = "+994501234568" }, default);
        var otp = GetResponse<OtpRequestResponse>(sendResult);

        var confirmResult = await controller.Confirm(new ConfirmInput
        {
            Phone = "+994501234568",
            Pin = otp.DevPin!,
            FullName = "New Customer"
        }, default);
        var session = GetResponse<AuthSessionResponse>(confirmResult);

        Assert.Equal(nameof(UserRole.Customer), session.User.Role);
        Assert.Equal("New Customer", session.User.FullName);
        Assert.Equal(UserRole.Customer, (await context.Users.SingleAsync()).Role);
    }

    [Fact]
    public async Task InvalidOtpFailsAndIncrementsTryCount()
    {
        await using var context = CreateContext();
        var controller = CreateController(context);
        await controller.Send(new PhoneInput { Phone = "+994501234569" }, default);

        var result = await controller.Confirm(new ConfirmInput
        {
            Phone = "+994501234569",
            Pin = "000000"
        }, default);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);
        Assert.Equal("Invalid code.", response.Error);
        Assert.Equal(1, (await context.OtpCodes.SingleAsync()).TryCount);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static AuthController CreateController(AppDbContext context)
    {
        var jwtOptions = Options.Create(new JwtOptions
        {
            Issuer = "tests",
            Audience = "test-clients",
            Key = "TEST_SIGNING_KEY_WITH_AT_LEAST_32_BYTES_123456",
            Minutes = 15
        });

        return new AuthController(
            context,
            new TestMessageSender(),
            new AccessTokenBuilder(jwtOptions),
            new TestHostEnvironment());
    }

    private static T GetResponse<T>(IActionResult result)
    {
        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<T>>(ok.Value);
        Assert.True(response.Success);
        return Assert.IsType<T>(response.Data);
    }

    private sealed class TestMessageSender : IMessageSender
    {
        public Task<string?> SendAsync(MessageChannel channel, string to, string text, CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>("test-provider-id");
    }

    private sealed class TestHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "DailyRentalHomes.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
