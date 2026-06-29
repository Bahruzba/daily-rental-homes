namespace DailyRentalHomes.Api.Common;

public static class TextRules
{
    public static bool Empty(string? value) => string.IsNullOrWhiteSpace(value);
    public static string Clean(string value) => value.Trim();
    public static string? CleanOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
