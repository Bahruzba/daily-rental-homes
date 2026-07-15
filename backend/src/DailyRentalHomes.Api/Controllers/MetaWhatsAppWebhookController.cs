using System.IO;
using System.Text.Json;
using DailyRentalHomes.Api.Options;
using DailyRentalHomes.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DailyRentalHomes.Api.Controllers;

[ApiController]
[Route("api/webhooks/meta-whatsapp")]
public sealed class MetaWhatsAppWebhookController : ControllerBase
{
    private readonly MetaWhatsAppOptions _options;
    private readonly MetaWhatsAppWebhookService _webhookService;
    private readonly ILogger<MetaWhatsAppWebhookController> _logger;

    public MetaWhatsAppWebhookController(
        IOptions<NotificationDeliveryOptions> options,
        MetaWhatsAppWebhookService webhookService,
        ILogger<MetaWhatsAppWebhookController> logger)
    {
        _options = options.Value.MetaWhatsApp;
        _webhookService = webhookService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Verify(
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? verifyToken,
        [FromQuery(Name = "hub.challenge")] string? challenge)
    {
        if (string.Equals(mode, "subscribe", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(_options.WebhookVerifyToken) &&
            string.Equals(verifyToken, _options.WebhookVerifyToken, StringComparison.Ordinal) &&
            !string.IsNullOrWhiteSpace(challenge))
        {
            return Content(challenge, "text/plain");
        }

        _logger.LogWarning("Invalid Meta WhatsApp webhook verification attempt.");
        return StatusCode(StatusCodes.Status403Forbidden);
    }

    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.AppSecret))
        {
            _logger.LogError("Meta WhatsApp webhook AppSecret is not configured.");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        await using var buffer = new MemoryStream();
        await Request.Body.CopyToAsync(buffer, cancellationToken);
        var rawBody = buffer.ToArray();
        var signature = Request.Headers["X-Hub-Signature-256"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(signature))
        {
            _logger.LogWarning("Meta WhatsApp webhook request rejected because signature header is missing.");
            return Unauthorized();
        }

        if (!signature.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Meta WhatsApp webhook request rejected because signature header is malformed.");
            return Unauthorized();
        }

        if (!MetaWhatsAppWebhookSignatureValidator.TryValidate(signature, _options.AppSecret, rawBody))
        {
            _logger.LogWarning("Meta WhatsApp webhook request rejected because signature validation failed.");
            return Unauthorized();
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(rawBody);
        }
        catch (JsonException)
        {
            return BadRequest();
        }

        using (document)
        {
            var payload = document.RootElement;
            if (payload.ValueKind is not JsonValueKind.Object)
            {
                return BadRequest();
            }

            var result = await _webhookService.ProcessAsync(payload, cancellationToken);
            return Ok(new
            {
                received = result.Received,
                updated = result.Updated,
                ignored = result.Ignored
            });
        }
    }
}
