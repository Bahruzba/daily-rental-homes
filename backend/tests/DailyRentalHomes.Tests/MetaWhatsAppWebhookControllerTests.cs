using System.Text.Json;
using DailyRentalHomes.Api.Controllers;
using DailyRentalHomes.Api.Options;
using DailyRentalHomes.Api.Services;
using DailyRentalHomes.Domain.Entities;
using DailyRentalHomes.Domain.Enums;
using DailyRentalHomes.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DailyRentalHomes.Tests;

public sealed class MetaWhatsAppWebhookControllerTests
{
    private const string VerifyToken = "verify-secret";

    [Fact]
    public void ValidWebhookVerificationReturnsChallenge()
    {
        using var context = CreateContext();
        var controller = Controller(context);

        var result = Assert.IsType<ContentResult>(controller.Verify("subscribe", VerifyToken, "challenge-123"));

        Assert.Equal("challenge-123", result.Content);
        Assert.Equal("text/plain", result.ContentType);
    }

    [Fact]
    public void InvalidVerificationTokenIsRejected()
    {
        using var context = CreateContext();
        var controller = Controller(context);

        var result = Assert.IsType<StatusCodeResult>(controller.Verify("subscribe", "wrong-token", "challenge-123"));

        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
    }

    [Fact]
    public async Task SentStatusUpdatesCorrectOutboxMessage()
    {
        await using var context = CreateContext();
        context.OutboundMessages.Add(Message("wamid.sent", includeSentAt: false));
        await context.SaveChangesAsync();

        await Post(context, StatusPayload("wamid.sent", "sent", "1720000000"));

        var message = await context.OutboundMessages.SingleAsync();
        Assert.Equal("sent", message.ProviderDeliveryStatus);
        Assert.Equal(MessageStatus.Sent, message.Status);
        Assert.Equal(Unix("1720000000"), message.SentAt);
        Assert.Equal(Unix("1720000000"), message.ProviderStatusUpdatedAt);
    }

    [Fact]
    public async Task DeliveredStatusUpdatesCorrectOutboxMessage()
    {
        await using var context = CreateContext();
        context.OutboundMessages.Add(Message("wamid.delivered"));
        await context.SaveChangesAsync();

        await Post(context, StatusPayload("wamid.delivered", "delivered", "1720000100"));

        var message = await context.OutboundMessages.SingleAsync();
        Assert.Equal("delivered", message.ProviderDeliveryStatus);
        Assert.Equal(MessageStatus.Sent, message.Status);
        Assert.Equal(Unix("1720000100"), message.DeliveredAt);
    }

    [Fact]
    public async Task ReadStatusUpdatesCorrectOutboxMessage()
    {
        await using var context = CreateContext();
        context.OutboundMessages.Add(Message("wamid.read"));
        await context.SaveChangesAsync();

        await Post(context, StatusPayload("wamid.read", "read", "1720000200"));

        var message = await context.OutboundMessages.SingleAsync();
        Assert.Equal("read", message.ProviderDeliveryStatus);
        Assert.Equal(MessageStatus.Sent, message.Status);
        Assert.Equal(Unix("1720000200"), message.DeliveredAt);
        Assert.Equal(Unix("1720000200"), message.ReadAt);
    }

    [Fact]
    public async Task FailedStatusStoresUsefulFailureInformation()
    {
        await using var context = CreateContext();
        context.OutboundMessages.Add(Message("wamid.failed"));
        await context.SaveChangesAsync();

        await Post(context, FailedStatusPayload("wamid.failed"));

        var message = await context.OutboundMessages.SingleAsync();
        Assert.Equal("failed", message.ProviderDeliveryStatus);
        Assert.Equal(MessageStatus.Failed, message.Status);
        Assert.Contains("131000", message.ErrorMessage);
        Assert.Contains("Internal error", message.ErrorMessage);
    }

    [Fact]
    public async Task UnknownProviderMessageIdIsIgnoredSafely()
    {
        await using var context = CreateContext();
        context.OutboundMessages.Add(Message("wamid.known"));
        await context.SaveChangesAsync();

        var result = await Post(context, StatusPayload("wamid.unknown", "delivered", "1720000000"));

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("ignored = 1", ok.Value!.ToString());
        var message = await context.OutboundMessages.SingleAsync();
        Assert.Null(message.ProviderDeliveryStatus);
    }

    [Fact]
    public async Task DuplicateWebhookStatusIsIdempotent()
    {
        await using var context = CreateContext();
        context.OutboundMessages.Add(Message("wamid.duplicate"));
        await context.SaveChangesAsync();
        var payload = StatusPayload("wamid.duplicate", "delivered", "1720000100");

        await Post(context, payload);
        await Post(context, payload);

        var message = await context.OutboundMessages.SingleAsync();
        Assert.Equal("delivered", message.ProviderDeliveryStatus);
        Assert.Equal(Unix("1720000100"), message.DeliveredAt);
    }

    [Fact]
    public async Task EarlierOutOfOrderStatusDoesNotRegressLaterStatus()
    {
        await using var context = CreateContext();
        context.OutboundMessages.Add(Message("wamid.order"));
        await context.SaveChangesAsync();

        await Post(context, StatusPayload("wamid.order", "read", "1720000200"));
        await Post(context, StatusPayload("wamid.order", "sent", "1720000000"));

        var message = await context.OutboundMessages.SingleAsync();
        Assert.Equal("read", message.ProviderDeliveryStatus);
        Assert.Equal(Unix("1720000200"), message.ReadAt);
        Assert.Equal(Unix("1720000200"), message.ProviderStatusUpdatedAt);
    }

    [Fact]
    public async Task MultipleStatusEntriesInOnePayloadAreProcessedSafely()
    {
        await using var context = CreateContext();
        context.OutboundMessages.AddRange(Message("wamid.one"), Message("wamid.two"));
        await context.SaveChangesAsync();

        await Post(context, MultiStatusPayload());

        Assert.Equal(2, await context.OutboundMessages.CountAsync(item => item.ProviderDeliveryStatus != null));
        Assert.Contains(await context.OutboundMessages.ToListAsync(), item => item.ProviderMessageId == "wamid.one" && item.ProviderDeliveryStatus == "sent");
        Assert.Contains(await context.OutboundMessages.ToListAsync(), item => item.ProviderMessageId == "wamid.two" && item.ProviderDeliveryStatus == "delivered");
    }

    [Fact]
    public async Task UnrelatedInboundMessagePayloadIsIgnoredSafely()
    {
        await using var context = CreateContext();
        context.OutboundMessages.Add(Message("wamid.inbound-ignore"));
        await context.SaveChangesAsync();

        var result = await Post(context, InboundMessagePayload());

        Assert.IsType<OkObjectResult>(result);
        Assert.Null((await context.OutboundMessages.SingleAsync()).ProviderDeliveryStatus);
    }

    [Fact]
    public void VerificationSecretIsNotExposedInLogs()
    {
        using var context = CreateContext();
        var logger = new CaptureLogger<MetaWhatsAppWebhookController>();
        var controller = Controller(context, logger);

        controller.Verify("subscribe", "wrong-token", "challenge-123");

        Assert.DoesNotContain(VerifyToken, string.Join(Environment.NewLine, logger.Messages));
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static MetaWhatsAppWebhookController Controller(
        AppDbContext context,
        ILogger<MetaWhatsAppWebhookController>? logger = null) =>
        new(
            Options.Create(new NotificationDeliveryOptions
            {
                MetaWhatsApp = new MetaWhatsAppOptions
                {
                    WebhookVerifyToken = VerifyToken
                }
            }),
            new MetaWhatsAppWebhookService(context, new CaptureLogger<MetaWhatsAppWebhookService>()),
            logger ?? new CaptureLogger<MetaWhatsAppWebhookController>());

    private static Task<IActionResult> Post(AppDbContext context, string json) =>
        Controller(context).Receive(JsonDocument.Parse(json).RootElement, default);

    private static OutboundMessage Message(string providerMessageId, bool includeSentAt = true) => new()
    {
        Channel = MessageChannel.WhatsApp,
        Status = MessageStatus.Sent,
        TypeCode = "test",
        Title = "Title",
        To = "+994501234567",
        Text = "Text",
        ProviderMessageId = providerMessageId,
        SentAt = includeSentAt ? DateTime.UtcNow.AddMinutes(-5) : null
    };

    private static DateTime Unix(string seconds) =>
        DateTimeOffset.FromUnixTimeSeconds(long.Parse(seconds)).UtcDateTime;

    private static string StatusPayload(string providerMessageId, string status, string timestamp) =>
        $$"""
        {
          "object": "whatsapp_business_account",
          "entry": [
            {
              "id": "waba-id",
              "changes": [
                {
                  "field": "messages",
                  "value": {
                    "messaging_product": "whatsapp",
                    "statuses": [
                      {
                        "id": "{{providerMessageId}}",
                        "status": "{{status}}",
                        "timestamp": "{{timestamp}}",
                        "recipient_id": "994501234567"
                      }
                    ]
                  }
                }
              ]
            }
          ]
        }
        """;

    private static string FailedStatusPayload(string providerMessageId) =>
        $$"""
        {
          "object": "whatsapp_business_account",
          "entry": [
            {
              "changes": [
                {
                  "field": "messages",
                  "value": {
                    "statuses": [
                      {
                        "id": "{{providerMessageId}}",
                        "status": "failed",
                        "timestamp": "1720000300",
                        "errors": [
                          {
                            "code": 131000,
                            "title": "Internal error",
                            "message": "Failure due to an internal error."
                          }
                        ]
                      }
                    ]
                  }
                }
              ]
            }
          ]
        }
        """;

    private static string MultiStatusPayload() =>
        """
        {
          "entry": [
            {
              "changes": [
                {
                  "value": {
                    "statuses": [
                      { "id": "wamid.one", "status": "sent", "timestamp": "1720000000" },
                      { "id": "wamid.two", "status": "delivered", "timestamp": "1720000100" },
                      { "id": "wamid.unknown", "status": "read", "timestamp": "1720000200" }
                    ]
                  }
                }
              ]
            }
          ]
        }
        """;

    private static string InboundMessagePayload() =>
        """
        {
          "entry": [
            {
              "changes": [
                {
                  "field": "messages",
                  "value": {
                    "messages": [
                      {
                        "from": "994501234567",
                        "id": "wamid.inbound",
                        "timestamp": "1720000000",
                        "text": { "body": "Hello" },
                        "type": "text"
                      }
                    ]
                  }
                }
              ]
            }
          ]
        }
        """;

    private sealed class CaptureLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
            if (exception is not null) Messages.Add(exception.Message);
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose()
        {
        }
    }
}
