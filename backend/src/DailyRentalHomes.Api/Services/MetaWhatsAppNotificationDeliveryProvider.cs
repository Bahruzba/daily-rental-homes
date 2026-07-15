using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DailyRentalHomes.Api.Options;
using DailyRentalHomes.Domain.Entities;
using Microsoft.Extensions.Options;

namespace DailyRentalHomes.Api.Services;

public sealed class MetaWhatsAppNotificationDeliveryProvider : INotificationDeliveryProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly MetaWhatsAppOptions _options;
    private readonly ILogger<MetaWhatsAppNotificationDeliveryProvider> _logger;

    public MetaWhatsAppNotificationDeliveryProvider(
        HttpClient httpClient,
        IOptions<NotificationDeliveryOptions> options,
        ILogger<MetaWhatsAppNotificationDeliveryProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value.MetaWhatsApp;
        _logger = logger;
    }

    public async Task<NotificationDeliveryResult> SendAsync(OutboundMessage message, CancellationToken cancellationToken)
    {
        var destination = NormalizePhoneNumber(message.To);
        if (destination is null)
        {
            return NotificationDeliveryResult.Failed("Meta WhatsApp destination phone number is missing or invalid.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, BuildMessagesUri());
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);
        request.Content = JsonContent(new
        {
            messaging_product = "whatsapp",
            recipient_type = "individual",
            to = destination,
            type = "text",
            text = new
            {
                preview_url = false,
                body = BuildMessageBody(message)
            }
        });

        HttpResponseMessage response;
        string responseText;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
            responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(exception, "Meta WhatsApp delivery failed for outbound message {OutboundMessageId}.", message.Id);
            return NotificationDeliveryResult.Failed($"Meta WhatsApp request failed: {exception.Message}");
        }

        if (!response.IsSuccessStatusCode)
        {
            var error = TryReadError(responseText);
            _logger.LogWarning(
                "Meta WhatsApp delivery failed for outbound message {OutboundMessageId} with HTTP {StatusCode} and provider code {ProviderErrorCode}.",
                message.Id,
                (int)response.StatusCode,
                error.Code);
            return NotificationDeliveryResult.Failed(BuildFailureMessage(response.StatusCode, error));
        }

        var providerMessageId = TryReadMessageId(responseText);
        if (string.IsNullOrWhiteSpace(providerMessageId))
        {
            return NotificationDeliveryResult.Failed("Meta WhatsApp API response did not include a message id.");
        }

        return NotificationDeliveryResult.Sent(providerMessageId);
    }

    internal static string? NormalizePhoneNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var trimmed = value.Trim();
        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        if (digits.Length == 0) return null;

        if (trimmed.StartsWith("+", StringComparison.Ordinal) && IsValidInternationalNumber(digits))
        {
            return digits;
        }

        if (digits.StartsWith("00", StringComparison.Ordinal))
        {
            digits = digits[2..];
        }

        if (digits.StartsWith("994", StringComparison.Ordinal) && IsValidInternationalNumber(digits))
        {
            return digits;
        }

        if (digits.StartsWith("0", StringComparison.Ordinal) && digits.Length == 10)
        {
            var azNumber = $"994{digits[1..]}";
            return IsValidInternationalNumber(azNumber) ? azNumber : null;
        }

        if (digits.Length == 9 && IsLikelyAzerbaijanMobilePrefix(digits[..2]))
        {
            var azNumber = $"994{digits}";
            return IsValidInternationalNumber(azNumber) ? azNumber : null;
        }

        return IsValidInternationalNumber(digits) ? digits : null;
    }

    private Uri BuildMessagesUri()
    {
        var apiVersion = _options.ApiVersion.Trim().Trim('/');
        var phoneNumberId = Uri.EscapeDataString(_options.PhoneNumberId.Trim());
        return new Uri($"https://graph.facebook.com/{apiVersion}/{phoneNumberId}/messages");
    }

    private static StringContent JsonContent(object value) =>
        new(JsonSerializer.Serialize(value, JsonOptions), Encoding.UTF8, "application/json");

    private static string BuildMessageBody(OutboundMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.Title)) return message.Text;
        if (string.IsNullOrWhiteSpace(message.Text)) return message.Title;
        return $"{message.Title.Trim()}\n\n{message.Text.Trim()}";
    }

    private static string? TryReadMessageId(string responseText)
    {
        try
        {
            using var document = JsonDocument.Parse(responseText);
            if (document.RootElement.TryGetProperty("messages", out var messages) &&
                messages.ValueKind == JsonValueKind.Array &&
                messages.GetArrayLength() > 0 &&
                messages[0].TryGetProperty("id", out var id) &&
                id.ValueKind == JsonValueKind.String)
            {
                return id.GetString();
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    private static MetaWhatsAppError TryReadError(string responseText)
    {
        try
        {
            using var document = JsonDocument.Parse(responseText);
            if (document.RootElement.TryGetProperty("error", out var error))
            {
                return new MetaWhatsAppError(
                    GetString(error, "message"),
                    GetString(error, "type"),
                    GetNumber(error, "code"),
                    GetNumber(error, "error_subcode"));
            }
        }
        catch (JsonException)
        {
            return new MetaWhatsAppError("Malformed Meta WhatsApp error response.", null, null, null);
        }

        return new MetaWhatsAppError("Meta WhatsApp API returned an error response.", null, null, null);
    }

    private static string BuildFailureMessage(HttpStatusCode statusCode, MetaWhatsAppError error)
    {
        var parts = new List<string> { $"Meta WhatsApp API returned HTTP {(int)statusCode}." };
        if (error.Code.HasValue) parts.Add($"Code: {error.Code.Value}.");
        if (error.Subcode.HasValue) parts.Add($"Subcode: {error.Subcode.Value}.");
        if (!string.IsNullOrWhiteSpace(error.Message)) parts.Add($"Message: {error.Message}");
        return string.Join(' ', parts);
    }

    private static string? GetString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static int? GetNumber(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.TryGetInt32(out var value)
            ? value
            : null;

    private static bool IsValidInternationalNumber(string digits) =>
        digits.Length is >= 8 and <= 15 && digits.All(char.IsDigit);

    private static bool IsLikelyAzerbaijanMobilePrefix(string prefix) =>
        prefix is "50" or "51" or "55" or "70" or "77" or "99";

    private sealed record MetaWhatsAppError(string? Message, string? Type, int? Code, int? Subcode);
}
