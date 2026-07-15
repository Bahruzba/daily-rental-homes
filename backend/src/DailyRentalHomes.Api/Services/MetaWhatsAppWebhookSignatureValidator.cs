using System.Security.Cryptography;
using System.Text;

namespace DailyRentalHomes.Api.Services;

public static class MetaWhatsAppWebhookSignatureValidator
{
    private const string SignaturePrefix = "sha256=";

    public static bool TryValidate(string? signatureHeader, string appSecret, byte[] requestBody)
    {
        if (string.IsNullOrWhiteSpace(signatureHeader) ||
            string.IsNullOrWhiteSpace(appSecret) ||
            !signatureHeader.StartsWith(SignaturePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var signatureHex = signatureHeader[SignaturePrefix.Length..];
        byte[] providedSignature;
        try
        {
            providedSignature = Convert.FromHexString(signatureHex);
        }
        catch (FormatException)
        {
            return false;
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appSecret));
        var expectedSignature = hmac.ComputeHash(requestBody);
        return providedSignature.Length == expectedSignature.Length &&
               CryptographicOperations.FixedTimeEquals(providedSignature, expectedSignature);
    }

    public static string SignForTests(string appSecret, string requestBody)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appSecret));
        return $"{SignaturePrefix}{Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(requestBody))).ToLowerInvariant()}";
    }
}
