namespace DailyRentalHomes.Application.Common;

public sealed class OperationResult<T>
{
    public bool IsSuccess { get; init; }
    public T? Value { get; init; }
    public string? Message { get; init; }

    public static OperationResult<T> Ok(T value) => new() { IsSuccess = true, Value = value };
    public static OperationResult<T> Fail(string message) => new() { IsSuccess = false, Message = message };
}
