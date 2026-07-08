using DailyRentalHomes.Api.Common;
using DailyRentalHomes.Api.Contracts.Broker;
using DailyRentalHomes.Api.Security;
using DailyRentalHomes.Domain.Entities;
using DailyRentalHomes.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DailyRentalHomes.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.BrokerOrAdmin)]
[Route("api/broker/bookings/{bookingId:long}/expenses")]
public sealed class BrokerBookingExpensesController : ControllerBase
{
    private readonly AppDbContext _db;

    public BrokerBookingExpensesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetExpenses(long bookingId, CancellationToken cancellationToken)
    {
        if (!await BookingExistsInScope(bookingId, cancellationToken))
        {
            return NotFound(ApiResponse<object>.Fail("Booking not found."));
        }

        var expenses = await _db.BookingExpenses
            .AsNoTracking()
            .Where(expense => expense.BookingId == bookingId)
            .OrderByDescending(expense => expense.CreatedAt)
            .ThenByDescending(expense => expense.Id)
            .ToListAsync(cancellationToken);

        var response = expenses.Select(BrokerBookingExpenseResponse.FromEntity).ToList();

        return Ok(ApiResponse<IReadOnlyList<BrokerBookingExpenseResponse>>.Ok(response));
    }

    [HttpPost]
    public async Task<IActionResult> CreateExpense(
        long bookingId,
        BrokerBookingExpenseRequest request,
        CancellationToken cancellationToken)
    {
        var validation = Validate(request);
        if (validation is not null)
        {
            return BadRequest(ApiResponse<object>.Fail(validation));
        }

        if (!await BookingExistsInScope(bookingId, cancellationToken))
        {
            return NotFound(ApiResponse<object>.Fail("Booking not found."));
        }

        var expense = new BookingExpense
        {
            BookingId = bookingId,
            TypeCode = TextRules.Clean(request.TypeCode).ToLowerInvariant(),
            Title = TextRules.Clean(request.Title),
            Amount = request.Amount,
            Note = TextRules.CleanOptional(request.Note),
            CreatedByUserId = User.GetUserId()
        };

        _db.BookingExpenses.Add(expense);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<BrokerBookingExpenseResponse>.Ok(BrokerBookingExpenseResponse.FromEntity(expense)));
    }

    [HttpPut("{expenseId:long}")]
    public async Task<IActionResult> UpdateExpense(
        long bookingId,
        long expenseId,
        BrokerBookingExpenseRequest request,
        CancellationToken cancellationToken)
    {
        var validation = Validate(request);
        if (validation is not null)
        {
            return BadRequest(ApiResponse<object>.Fail(validation));
        }

        if (!await BookingExistsInScope(bookingId, cancellationToken))
        {
            return NotFound(ApiResponse<object>.Fail("Booking not found."));
        }

        var expense = await _db.BookingExpenses
            .FirstOrDefaultAsync(item => item.Id == expenseId && item.BookingId == bookingId, cancellationToken);
        if (expense is null)
        {
            return NotFound(ApiResponse<object>.Fail("Expense not found."));
        }

        expense.TypeCode = TextRules.Clean(request.TypeCode).ToLowerInvariant();
        expense.Title = TextRules.Clean(request.Title);
        expense.Amount = request.Amount;
        expense.Note = TextRules.CleanOptional(request.Note);
        expense.UpdatedByUserId = User.GetUserId();

        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<BrokerBookingExpenseResponse>.Ok(BrokerBookingExpenseResponse.FromEntity(expense)));
    }

    [HttpDelete("{expenseId:long}")]
    public async Task<IActionResult> DeleteExpense(long bookingId, long expenseId, CancellationToken cancellationToken)
    {
        if (!await BookingExistsInScope(bookingId, cancellationToken))
        {
            return NotFound(ApiResponse<object>.Fail("Booking not found."));
        }

        var expense = await _db.BookingExpenses
            .FirstOrDefaultAsync(item => item.Id == expenseId && item.BookingId == bookingId, cancellationToken);
        if (expense is null)
        {
            return NotFound(ApiResponse<object>.Fail("Expense not found."));
        }

        expense.IsDeleted = true;
        expense.UpdatedByUserId = User.GetUserId();
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { expense.Id }));
    }

    private async Task<bool> BookingExistsInScope(long bookingId, CancellationToken cancellationToken)
    {
        var query = _db.Bookings.AsNoTracking()
            .Where(booking => booking.Id == bookingId && booking.RentalHome != null);

        if (!User.IsAdmin())
        {
            var userId = User.GetUserId();
            query = query.Where(booking => booking.RentalHome!.BrokerUserId == userId);
        }

        return await query.AnyAsync(cancellationToken);
    }

    private static string? Validate(BrokerBookingExpenseRequest request)
    {
        if (TextRules.Empty(request.TypeCode))
        {
            return "Expense type is required.";
        }

        if (TextRules.Clean(request.TypeCode).Length > 50)
        {
            return "Expense type is too long.";
        }

        if (TextRules.Empty(request.Title))
        {
            return "Expense title is required.";
        }

        if (TextRules.Clean(request.Title).Length > 150)
        {
            return "Expense title is too long.";
        }

        if (request.Amount <= 0)
        {
            return "Expense amount must be greater than 0.";
        }

        if (!string.IsNullOrWhiteSpace(request.Note) && TextRules.Clean(request.Note).Length > 1000)
        {
            return "Expense note is too long.";
        }

        return null;
    }
}
