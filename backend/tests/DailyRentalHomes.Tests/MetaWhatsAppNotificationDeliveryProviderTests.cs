using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using DailyRentalHomes.Api.Options;
using DailyRentalHomes.Api.Services;
using DailyRentalHomes.Domain.Constants;
using DailyRentalHomes.Domain.Entities;
using DailyRentalHomes.Domain.Enums;
using DailyRentalHomes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DailyRentalHomes.Tests;

public sealed class MetaWhatsAppNotificationDeliveryProviderTests
{
    private const string AccessToken = "test-meta-access-token";

    [Fact]
    public void MetaWhatsAppProviderIsSelectedWhenConfigured()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNotificationDelivery(ConfigurationForProvider(NotificationDeliveryOptions.MetaWhatsAppProvider));
        using var provider = services.BuildServiceProvider();

        var deliveryProvider = provider.GetRequiredService<INotificationDeliveryProvider>();

        Assert.IsType<MetaWhatsAppNotificationDeliveryProvider>(deliveryProvider);
    }

    [Fact]
    public void FakeProviderRemainsDefault()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNotificationDelivery(new ConfigurationBuilder().AddInMemoryCollection([]).Build());
        using var provider = services.BuildServiceProvider();

        var deliveryProvider = provider.GetRequiredService<INotificationDeliveryProvider>();

        Assert.IsType<FakeNotificationDeliveryProvider>(deliveryProvider);
    }

    [Fact]
    public async Task SuccessfulMetaApiResponseReturnsProviderReference()
    {
        var handler = new CaptureHandler(_ => JsonResponse(HttpStatusCode.OK, SuccessJson("wamid.success")));
        var provider = Provider(handler);

        var result = await provider.SendAsync(Message(to: "+994 50 555 12 12"), default);

        Assert.True(result.Success);
        Assert.Equal("wamid.success", result.ProviderMessageId);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task SendsCorrectDestinationPayloadAndAuthorizationHeader()
    {
        var handler = new CaptureHandler(_ => JsonResponse(HttpStatusCode.OK, SuccessJson("wamid.payload")));
        var provider = Provider(handler);

        await provider.SendAsync(Message(title: "Başlıq", text: "Mətn", to: "050-555-12-12"), default);

        Assert.Equal(new Uri("https://graph.facebook.com/v22.0/123456789/messages"), handler.LastRequestUri);
        Assert.Equal("Bearer", handler.LastAuthorization?.Scheme);
        Assert.Equal(AccessToken, handler.LastAuthorization?.Parameter);
        using var document = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("whatsapp", document.RootElement.GetProperty("messaging_product").GetString());
        Assert.Equal("individual", document.RootElement.GetProperty("recipient_type").GetString());
        Assert.Equal("994505551212", document.RootElement.GetProperty("to").GetString());
        Assert.Equal("text", document.RootElement.GetProperty("type").GetString());
        Assert.False(document.RootElement.GetProperty("text").GetProperty("preview_url").GetBoolean());
        Assert.Equal("Başlıq\n\nMətn", document.RootElement.GetProperty("text").GetProperty("body").GetString());
    }

    [Fact]
    public async Task DepositDeadlineReminderUsesConfiguredTemplate()
    {
        var handler = new CaptureHandler(_ => JsonResponse(HttpStatusCode.OK, SuccessJson("wamid.template-reminder")));
        var provider = Provider(handler, configure: options =>
        {
            options.MetaWhatsApp.Templates[NotificationTypeCodes.DepositDeadlineReminder] = "custom_deadline_reminder_template";
        });

        await provider.SendAsync(Message(
            typeCode: NotificationTypeCodes.DepositDeadlineReminder,
            payloadJson: Payload(deadlineText: "20.07.2026 18:00")), default);

        using var document = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("template", document.RootElement.GetProperty("type").GetString());
        Assert.Equal("custom_deadline_reminder_template", document.RootElement.GetProperty("template").GetProperty("name").GetString());
        Assert.Equal("az", document.RootElement.GetProperty("template").GetProperty("language").GetProperty("code").GetString());
        Assert.Equal("20.07.2026 18:00", BodyParameterText(document, 0));
    }

    [Fact]
    public async Task DepositDeadlineExtendedUsesConfiguredTemplateWithExtensionReason()
    {
        var handler = new CaptureHandler(_ => JsonResponse(HttpStatusCode.OK, SuccessJson("wamid.template-extended")));
        var provider = Provider(handler, configure: options =>
        {
            options.MetaWhatsApp.DefaultLanguageCode = "az_AZ";
            options.MetaWhatsApp.Templates[NotificationTypeCodes.DepositDeadlineExtended] = "custom_deadline_extended_template";
        });

        await provider.SendAsync(Message(
            typeCode: NotificationTypeCodes.DepositDeadlineExtended,
            payloadJson: Payload(deadlineText: "21.07.2026 19:30", reason: "Müştəri əlavə vaxt istədi"),
            to: "050 555 12 12"), default);

        using var document = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("994505551212", document.RootElement.GetProperty("to").GetString());
        Assert.Equal("template", document.RootElement.GetProperty("type").GetString());
        Assert.Equal("custom_deadline_extended_template", document.RootElement.GetProperty("template").GetProperty("name").GetString());
        Assert.Equal("az_AZ", document.RootElement.GetProperty("template").GetProperty("language").GetProperty("code").GetString());
        Assert.Equal("21.07.2026 19:30", BodyParameterText(document, 0));
        Assert.Equal("Müştəri əlavə vaxt istədi", BodyParameterText(document, 1));
    }

    [Fact]
    public async Task EmptyConfiguredTemplateFailsSafely()
    {
        var handler = new CaptureHandler(_ => JsonResponse(HttpStatusCode.OK, SuccessJson("wamid.unused")));
        var provider = Provider(handler, configure: options =>
        {
            options.MetaWhatsApp.Templates[NotificationTypeCodes.DepositDeadlineReminder] = "";
        });

        var result = await provider.SendAsync(Message(
            typeCode: NotificationTypeCodes.DepositDeadlineReminder,
            payloadJson: Payload(deadlineText: "20.07.2026 18:00")), default);

        Assert.False(result.Success);
        Assert.False(result.IsRetryable);
        Assert.Equal(0, handler.RequestCount);
        Assert.Contains("template mapping", result.ErrorMessage);
    }

    [Fact]
    public async Task ConfiguredTemplateMissingRequiredDeadlineFailsSafely()
    {
        var handler = new CaptureHandler(_ => JsonResponse(HttpStatusCode.OK, SuccessJson("wamid.unused")));
        var provider = Provider(handler, configure: options =>
        {
            options.MetaWhatsApp.Templates[NotificationTypeCodes.DepositDeadlineReminder] = "deposit_deadline_reminder";
        });

        var result = await provider.SendAsync(Message(typeCode: NotificationTypeCodes.DepositDeadlineReminder), default);

        Assert.False(result.Success);
        Assert.False(result.IsRetryable);
        Assert.Equal(0, handler.RequestCount);
        Assert.Contains("requires a deposit deadline", result.ErrorMessage);
    }

    [Fact]
    public async Task UnsupportedNotificationTypeUsesPlainTextFallback()
    {
        var handler = new CaptureHandler(_ => JsonResponse(HttpStatusCode.OK, SuccessJson("wamid.text-fallback")));
        var provider = Provider(handler, configure: options =>
        {
            options.MetaWhatsApp.Templates[NotificationTypeCodes.DepositDeadlineReminder] = "deposit_deadline_reminder";
        });

        await provider.SendAsync(Message(typeCode: NotificationTypeCodes.BookingCreated, title: "Title", text: "Text"), default);

        using var document = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("text", document.RootElement.GetProperty("type").GetString());
        Assert.Equal("Title\n\nText", document.RootElement.GetProperty("text").GetProperty("body").GetString());
    }

    [Fact]
    public async Task NonSuccessHttpResponseReturnsProviderFailure()
    {
        var handler = new CaptureHandler(_ => JsonResponse(HttpStatusCode.BadRequest, ErrorJson(131000, "Invalid parameter")));
        var provider = Provider(handler);

        var result = await provider.SendAsync(Message(), default);

        Assert.False(result.Success);
        Assert.False(result.IsRetryable);
        Assert.Contains("HTTP 400", result.ErrorMessage);
        Assert.Contains("131000", result.ErrorMessage);
        Assert.Contains("Invalid parameter", result.ErrorMessage);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    public async Task TransientMetaHttpResponsesAreRetryable(HttpStatusCode statusCode)
    {
        var handler = new CaptureHandler(_ => JsonResponse(statusCode, ErrorJson(131000, "Temporary Meta failure")));
        var provider = Provider(handler);

        var result = await provider.SendAsync(Message(), default);

        Assert.False(result.Success);
        Assert.True(result.IsRetryable);
        Assert.Contains($"HTTP {(int)statusCode}", result.ErrorMessage);
    }

    [Fact]
    public async Task MetaErrorResponseIsMappedIntoUsefulFailureInformation()
    {
        var handler = new CaptureHandler(_ => JsonResponse(HttpStatusCode.Forbidden, ErrorJson(190, "Invalid OAuth access token.")));
        var provider = Provider(handler);

        var result = await provider.SendAsync(Message(), default);

        Assert.False(result.Success);
        Assert.False(result.IsRetryable);
        Assert.Contains("HTTP 403", result.ErrorMessage);
        Assert.Contains("190", result.ErrorMessage);
        Assert.Contains("Invalid OAuth access token", result.ErrorMessage);
    }

    [Fact]
    public async Task NetworkExceptionReturnsFailure()
    {
        var handler = new CaptureHandler(_ => throw new HttpRequestException("Network unavailable."));
        var provider = Provider(handler);

        var result = await provider.SendAsync(Message(), default);

        Assert.False(result.Success);
        Assert.True(result.IsRetryable);
        Assert.Contains("Network unavailable", result.ErrorMessage);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("123")]
    public async Task InvalidOrMissingPhoneNumberFailsSafely(string? phone)
    {
        var handler = new CaptureHandler(_ => JsonResponse(HttpStatusCode.OK, SuccessJson("wamid.unused")));
        var provider = Provider(handler);

        var result = await provider.SendAsync(Message(to: phone), default);

        Assert.False(result.Success);
        Assert.False(result.IsRetryable);
        Assert.Equal(0, handler.RequestCount);
        Assert.Contains("phone number", result.ErrorMessage);
    }

    [Fact]
    public async Task AccessTokenIsNotWrittenToFailureLogs()
    {
        var handler = new CaptureHandler(_ => JsonResponse(HttpStatusCode.Unauthorized, ErrorJson(190, "Invalid token.")));
        var logger = new CaptureLogger<MetaWhatsAppNotificationDeliveryProvider>();
        var provider = Provider(handler, logger);

        await provider.SendAsync(Message(), default);

        Assert.DoesNotContain(AccessToken, string.Join(Environment.NewLine, logger.Messages));
    }

    [Fact]
    public async Task DeliveryServicePersistsMetaSuccessResult()
    {
        await using var context = CreateContext();
        context.OutboundMessages.Add(Message(id: 99, to: "+994505551212"));
        await context.SaveChangesAsync();
        var handler = new CaptureHandler(_ => JsonResponse(HttpStatusCode.OK, SuccessJson("wamid.persisted")));
        var provider = Provider(handler);
        var service = new NotificationDeliveryService(context, provider);

        var summary = await service.ProcessPendingAsync(20, default);

        var message = await context.OutboundMessages.SingleAsync();
        Assert.Equal(1, summary.Sent);
        Assert.Equal(MessageStatus.Sent, message.Status);
        Assert.Equal("wamid.persisted", message.ProviderMessageId);
        Assert.Null(message.ErrorMessage);
    }

    [Fact]
    public async Task DeliveryServicePersistsMetaTemplateSuccessResult()
    {
        await using var context = CreateContext();
        context.OutboundMessages.Add(Message(
            id: 100,
            to: "+994505551212",
            typeCode: NotificationTypeCodes.DepositDeadlineReminder,
            payloadJson: Payload(deadlineText: "20.07.2026 18:00")));
        await context.SaveChangesAsync();
        var handler = new CaptureHandler(_ => JsonResponse(HttpStatusCode.OK, SuccessJson("wamid.template-persisted")));
        var provider = Provider(handler, configure: options =>
        {
            options.MetaWhatsApp.Templates[NotificationTypeCodes.DepositDeadlineReminder] = "deposit_deadline_reminder";
        });
        var service = new NotificationDeliveryService(context, provider);

        var summary = await service.ProcessPendingAsync(20, default);

        var message = await context.OutboundMessages.SingleAsync();
        Assert.Equal(1, summary.Sent);
        Assert.Equal(MessageStatus.Sent, message.Status);
        Assert.Equal("wamid.template-persisted", message.ProviderMessageId);
        Assert.Null(message.ErrorMessage);
    }

    [Fact]
    public async Task MetaTemplateApiFailureUsesExistingFailureBehavior()
    {
        var handler = new CaptureHandler(_ => JsonResponse(HttpStatusCode.BadRequest, ErrorJson(132000, "Template parameter mismatch")));
        var provider = Provider(handler, configure: options =>
        {
            options.MetaWhatsApp.Templates[NotificationTypeCodes.DepositDeadlineReminder] = "deposit_deadline_reminder";
        });

        var result = await provider.SendAsync(Message(
            typeCode: NotificationTypeCodes.DepositDeadlineReminder,
            payloadJson: Payload(deadlineText: "20.07.2026 18:00")), default);

        Assert.False(result.Success);
        Assert.False(result.IsRetryable);
        Assert.Contains("HTTP 400", result.ErrorMessage);
        Assert.Contains("132000", result.ErrorMessage);
        Assert.Contains("Template parameter mismatch", result.ErrorMessage);
    }

    private static MetaWhatsAppNotificationDeliveryProvider Provider(
        CaptureHandler handler,
        ILogger<MetaWhatsAppNotificationDeliveryProvider>? logger = null,
        Action<NotificationDeliveryOptions>? configure = null)
    {
        var options = new NotificationDeliveryOptions
        {
            Provider = NotificationDeliveryOptions.MetaWhatsAppProvider,
            MetaWhatsApp = new MetaWhatsAppOptions
            {
                PhoneNumberId = "123456789",
                AccessToken = AccessToken,
                ApiVersion = "v22.0",
                DefaultLanguageCode = "az"
            }
        };
        configure?.Invoke(options);
        return new MetaWhatsAppNotificationDeliveryProvider(
            new HttpClient(handler),
            Options.Create(options),
            logger ?? new CaptureLogger<MetaWhatsAppNotificationDeliveryProvider>());
    }

    private static IConfiguration ConfigurationForProvider(string provider) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{NotificationDeliveryOptions.SectionName}:Provider"] = provider,
                [$"{NotificationDeliveryOptions.SectionName}:MetaWhatsApp:PhoneNumberId"] = "123456789",
                [$"{NotificationDeliveryOptions.SectionName}:MetaWhatsApp:AccessToken"] = AccessToken,
                [$"{NotificationDeliveryOptions.SectionName}:MetaWhatsApp:ApiVersion"] = "v22.0",
                [$"{NotificationDeliveryOptions.SectionName}:MetaWhatsApp:AppSecret"] = "test-app-secret"
            })
            .Build();

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static OutboundMessage Message(
        long id = 1,
        string title = "Title",
        string text = "Text",
        string? to = "+994501234567",
        string typeCode = "test",
        string? payloadJson = null) => new()
    {
        Id = id,
        Channel = MessageChannel.WhatsApp,
        Status = MessageStatus.Pending,
        TypeCode = typeCode,
        Title = title,
        To = to ?? string.Empty,
        Text = text,
        ScheduledAt = DateTime.UtcNow.AddMinutes(-1),
        PayloadJson = payloadJson
    };

    private static string Payload(string? deadlineText = null, string? reason = null) =>
        JsonSerializer.Serialize(new
        {
            bookingId = 1001,
            depositId = 501,
            deadlineText,
            deadlineExtensionReason = reason
        });

    private static string? BodyParameterText(JsonDocument document, int index) =>
        document.RootElement
            .GetProperty("template")
            .GetProperty("components")[0]
            .GetProperty("parameters")[index]
            .GetProperty("text")
            .GetString();

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string content) =>
        new(statusCode)
        {
            Content = new StringContent(content)
        };

    private static string SuccessJson(string messageId) =>
        $$"""
        {
          "messaging_product": "whatsapp",
          "contacts": [{ "input": "994501234567", "wa_id": "994501234567" }],
          "messages": [{ "id": "{{messageId}}" }]
        }
        """;

    private static string ErrorJson(int code, string message) =>
        $$"""
        {
          "error": {
            "message": "{{message}}",
            "type": "OAuthException",
            "code": {{code}},
            "fbtrace_id": "trace"
          }
        }
        """;

    private sealed class CaptureHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public CaptureHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        public int RequestCount { get; private set; }
        public Uri? LastRequestUri { get; private set; }
        public AuthenticationHeaderValue? LastAuthorization { get; private set; }
        public string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            LastRequestUri = request.RequestUri;
            LastAuthorization = request.Headers.Authorization;
            LastRequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return _responseFactory(request);
        }
    }

    private sealed class CaptureLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
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
