using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using DailyRentalHomes.Api.Options;
using DailyRentalHomes.Api.Services;
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
    public async Task NonSuccessHttpResponseReturnsProviderFailure()
    {
        var handler = new CaptureHandler(_ => JsonResponse(HttpStatusCode.BadRequest, ErrorJson(131000, "Invalid parameter")));
        var provider = Provider(handler);

        var result = await provider.SendAsync(Message(), default);

        Assert.False(result.Success);
        Assert.Contains("HTTP 400", result.ErrorMessage);
        Assert.Contains("131000", result.ErrorMessage);
        Assert.Contains("Invalid parameter", result.ErrorMessage);
    }

    [Fact]
    public async Task MetaErrorResponseIsMappedIntoUsefulFailureInformation()
    {
        var handler = new CaptureHandler(_ => JsonResponse(HttpStatusCode.Forbidden, ErrorJson(190, "Invalid OAuth access token.")));
        var provider = Provider(handler);

        var result = await provider.SendAsync(Message(), default);

        Assert.False(result.Success);
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

    private static MetaWhatsAppNotificationDeliveryProvider Provider(
        CaptureHandler handler,
        ILogger<MetaWhatsAppNotificationDeliveryProvider>? logger = null) =>
        new(
            new HttpClient(handler),
            Options.Create(new NotificationDeliveryOptions
            {
                Provider = NotificationDeliveryOptions.MetaWhatsAppProvider,
                MetaWhatsApp = new MetaWhatsAppOptions
                {
                    PhoneNumberId = "123456789",
                    AccessToken = AccessToken,
                    ApiVersion = "v22.0"
                }
            }),
            logger ?? new CaptureLogger<MetaWhatsAppNotificationDeliveryProvider>());

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
        string? to = "+994501234567") => new()
    {
        Id = id,
        Channel = MessageChannel.WhatsApp,
        Status = MessageStatus.Pending,
        TypeCode = "test",
        Title = title,
        To = to ?? string.Empty,
        Text = text,
        ScheduledAt = DateTime.UtcNow.AddMinutes(-1)
    };

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
