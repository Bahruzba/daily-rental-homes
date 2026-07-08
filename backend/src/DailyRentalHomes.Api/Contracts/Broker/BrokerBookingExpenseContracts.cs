using DailyRentalHomes.Domain.Entities;

namespace DailyRentalHomes.Api.Contracts.Broker;

public sealed class BrokerBookingExpenseRequest
{
    public string TypeCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Note { get; set; }
}

public sealed record BrokerBookingExpenseResponse(
    long Id,
    long BookingId,
    string TypeCode,
    string Title,
    decimal Amount,
    string? Note,
    DateTime CreatedAt)
{
    public static BrokerBookingExpenseResponse FromEntity(BookingExpense expense) => new(
        expense.Id,
        expense.BookingId,
        expense.TypeCode,
        expense.Title,
        expense.Amount,
        expense.Note,
        expense.CreatedAt);
}
