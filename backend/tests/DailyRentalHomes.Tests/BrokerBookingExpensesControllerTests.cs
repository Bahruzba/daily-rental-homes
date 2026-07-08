using DailyRentalHomes.Api.Common;
using DailyRentalHomes.Api.Contracts.Broker;
using DailyRentalHomes.Api.Controllers;
using DailyRentalHomes.Api.Security;
using DailyRentalHomes.Domain.Constants;
using DailyRentalHomes.Domain.Entities;
using DailyRentalHomes.Domain.Enums;
using DailyRentalHomes.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace DailyRentalHomes.Tests;

public sealed class BrokerBookingExpensesControllerTests
{
    [Fact]
    public async Task BrokerCanAddExpenseForOwnBooking()
    {
        await using var context = CreateContext();
        await SeedData(context);
        var controller = CreateController(context, brokerId: 10);

        var response = GetData<BrokerBookingExpenseResponse>(await controller.CreateExpense(1001, ValidRequest(), default));

        Assert.Equal(1001, response.BookingId);
        Assert.Equal("cleaning", response.TypeCode);
        Assert.Equal(75m, response.Amount);
        var expense = await context.BookingExpenses.SingleAsync();
        Assert.Equal(10, expense.CreatedByUserId);
    }

    [Fact]
    public async Task BrokerCanUpdateOwnExpense()
    {
        await using var context = CreateContext();
        await SeedData(context);
        await AddExpense(context, 501, 1001);
        var controller = CreateController(context, brokerId: 10);

        var response = GetData<BrokerBookingExpenseResponse>(await controller.UpdateExpense(
            1001,
            501,
            new BrokerBookingExpenseRequest { TypeCode = "repair", Title = "Pool repair", Amount = 120m, Note = "Fixed pump" },
            default));

        Assert.Equal("repair", response.TypeCode);
        Assert.Equal("Pool repair", response.Title);
        Assert.Equal(120m, response.Amount);
        Assert.Equal(10, (await context.BookingExpenses.SingleAsync(item => item.Id == 501)).UpdatedByUserId);
    }

    [Fact]
    public async Task BrokerCanDeleteOwnExpense()
    {
        await using var context = CreateContext();
        await SeedData(context);
        await AddExpense(context, 501, 1001);
        var controller = CreateController(context, brokerId: 10);

        var result = await controller.DeleteExpense(1001, 501, default);

        Assert.IsType<OkObjectResult>(result);
        Assert.True((await context.BookingExpenses.IgnoreQueryFilters().SingleAsync(item => item.Id == 501)).IsDeleted);
    }

    [Fact]
    public async Task AnotherBrokerCannotManageExpense()
    {
        await using var context = CreateContext();
        await SeedData(context);
        await AddExpense(context, 501, 1002);
        var controller = CreateController(context, brokerId: 10);

        Assert.IsType<NotFoundObjectResult>(await controller.GetExpenses(1002, default));
        Assert.IsType<NotFoundObjectResult>(await controller.CreateExpense(1002, ValidRequest(), default));
        Assert.IsType<NotFoundObjectResult>(await controller.UpdateExpense(1002, 501, ValidRequest(), default));
        Assert.IsType<NotFoundObjectResult>(await controller.DeleteExpense(1002, 501, default));
    }

    [Theory]
    [InlineData("", "Cleaning", 75)]
    [InlineData("cleaning", "", 75)]
    [InlineData("cleaning", "Cleaning", 0)]
    [InlineData("cleaning", "Cleaning", -1)]
    public async Task InvalidAmountTypeOrTitleReturnsBadRequest(string typeCode, string title, decimal amount)
    {
        await using var context = CreateContext();
        await SeedData(context);
        var controller = CreateController(context, brokerId: 10);

        var result = await controller.CreateExpense(1001, new BrokerBookingExpenseRequest
        {
            TypeCode = typeCode,
            Title = title,
            Amount = amount
        }, default);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task DeletedExpenseIsNotReturned()
    {
        await using var context = CreateContext();
        await SeedData(context);
        await AddExpense(context, 501, 1001);
        var expense = await context.BookingExpenses.SingleAsync(item => item.Id == 501);
        expense.IsDeleted = true;
        await context.SaveChangesAsync();
        var controller = CreateController(context, brokerId: 10);

        var response = GetData<IReadOnlyList<BrokerBookingExpenseResponse>>(await controller.GetExpenses(1001, default));

        Assert.Empty(response);
    }

    [Fact]
    public async Task SoftDeletedBookingOrHomeCannotBeManaged()
    {
        await using var context = CreateContext();
        await SeedData(context);
        var booking = await context.Bookings.SingleAsync(item => item.Id == 1001);
        booking.IsDeleted = true;
        var home = await context.RentalHomes.SingleAsync(item => item.Id == 102);
        home.IsDeleted = true;
        await context.SaveChangesAsync();
        var brokerOne = CreateController(context, brokerId: 10);
        var brokerTwo = CreateController(context, brokerId: 20);

        Assert.IsType<NotFoundObjectResult>(await brokerOne.CreateExpense(1001, ValidRequest(), default));
        Assert.IsType<NotFoundObjectResult>(await brokerTwo.CreateExpense(1002, ValidRequest(), default));
    }

    [Fact]
    public async Task CustomerAndUnauthenticatedUsersCannotSatisfyBrokerExpensePolicy()
    {
        var authorization = CreateAuthorizationService();

        var unauthenticated = await authorization.AuthorizeAsync(
            new ClaimsPrincipal(new ClaimsIdentity()),
            null,
            AuthorizationPolicies.BrokerOrAdmin);
        var customer = await authorization.AuthorizeAsync(
            Principal(30, UserRole.Customer),
            null,
            AuthorizationPolicies.BrokerOrAdmin);

        Assert.False(unauthenticated.Succeeded);
        Assert.False(customer.Succeeded);
        var attribute = Assert.Single(typeof(BrokerBookingExpensesController).GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        Assert.Equal(AuthorizationPolicies.BrokerOrAdmin, attribute.Policy);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task SeedData(AppDbContext context)
    {
        context.BookingStatuses.Add(new BookingStatus { Id = 1, Name = "Pending", Code = BookingStatusCodes.Pending, SortOrder = 1 });
        context.Users.AddRange(
            new User { Id = 10, FullName = "Broker One", PhoneNumber = "+994501000010", Role = UserRole.Broker },
            new User { Id = 20, FullName = "Broker Two", PhoneNumber = "+994501000020", Role = UserRole.Broker },
            new User { Id = 30, FullName = "Customer", PhoneNumber = "+994501000030", Role = UserRole.Customer });
        context.RentalHomes.AddRange(
            Home(101, 10, "Broker One Home"),
            Home(102, 20, "Broker Two Home"));
        context.Bookings.AddRange(
            Booking(1001, 101),
            Booking(1002, 102));
        await context.SaveChangesAsync();
    }

    private static async Task AddExpense(AppDbContext context, long id, long bookingId)
    {
        context.BookingExpenses.Add(new BookingExpense
        {
            Id = id,
            BookingId = bookingId,
            TypeCode = "cleaning",
            Title = "Cleaning",
            Amount = 75m,
            Note = "After guest checkout"
        });
        await context.SaveChangesAsync();
    }

    private static BrokerBookingExpenseRequest ValidRequest() => new()
    {
        TypeCode = "cleaning",
        Title = "Cleaning",
        Amount = 75m,
        Note = "After guest checkout"
    };

    private static RentalHome Home(long id, long brokerId, string title) => new()
    {
        Id = id,
        BrokerUserId = brokerId,
        Title = title,
        Description = "Test home",
        City = "Qəbələ",
        DailyPrice = 120m,
        RoomCount = 3,
        GuestCount = 6,
        IsPublished = true
    };

    private static Booking Booking(long id, long homeId) => new()
    {
        Id = id,
        RentalHomeId = homeId,
        CustomerFullName = "Test Customer",
        CustomerPhoneNumber = "+994501234567",
        GuestCount = 2,
        DailyPrice = 120m,
        TotalAmount = 240m,
        StatusId = 1,
        Dates = [new BookingDate { Date = new DateOnly(2026, 8, 10) }]
    };

    private static BrokerBookingExpensesController CreateController(AppDbContext context, long brokerId)
    {
        return new BrokerBookingExpensesController(context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = Principal(brokerId, UserRole.Broker) }
            }
        };
    }

    private static ClaimsPrincipal Principal(long userId, UserRole role) =>
        new(new ClaimsIdentity(
        [
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(ClaimTypes.Role, role.ToString())
        ], "test", ClaimTypes.Name, ClaimTypes.Role));

    private static IAuthorizationService CreateAuthorizationService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(AuthorizationPolicies.Configure);
        return services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();
    }

    private static T GetData<T>(IActionResult result)
    {
        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<T>>(ok.Value);
        Assert.True(response.Success);
        return Assert.IsAssignableFrom<T>(response.Data);
    }
}
