namespace DailyRentalHomes.Api.Contracts.Deposits;

public sealed record DepositDeadlineReminderProcessingResponse(
    int Evaluated,
    int Eligible,
    int Queued,
    int DuplicateSkipped);
