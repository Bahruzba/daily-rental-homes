namespace DailyRentalHomes.Api.Contracts.Broker;

public sealed record BrokerReportSummaryResponse(
    int BookingCount,
    int RevenueBookingCount,
    decimal TotalBookingAmount,
    decimal TotalExpenses,
    decimal EstimatedProfit,
    decimal TotalCleaningCost,
    decimal TotalOwnerPayout,
    decimal TotalOtherExpenses,
    DateOnly? From,
    DateOnly? To);
