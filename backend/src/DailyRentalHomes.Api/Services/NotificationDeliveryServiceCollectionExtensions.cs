using DailyRentalHomes.Api.Options;

namespace DailyRentalHomes.Api.Services;

public static class NotificationDeliveryServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationDelivery(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(NotificationDeliveryOptions.SectionName);
        services.AddOptions<NotificationDeliveryOptions>()
            .Bind(section)
            .Validate(IsSupportedProvider, "Notification delivery provider must be either Fake or MetaWhatsApp.")
            .Validate(HasValidMetaWhatsAppConfiguration, "MetaWhatsApp notification delivery requires PhoneNumberId, AccessToken, and ApiVersion.")
            .ValidateOnStart();

        var options = section.Get<NotificationDeliveryOptions>() ?? new NotificationDeliveryOptions();
        if (IsMetaWhatsApp(options.Provider))
        {
            services.AddHttpClient<MetaWhatsAppNotificationDeliveryProvider>();
            services.AddScoped<INotificationDeliveryProvider>(provider =>
                provider.GetRequiredService<MetaWhatsAppNotificationDeliveryProvider>());
        }
        else if (IsFake(options.Provider))
        {
            services.AddScoped<INotificationDeliveryProvider, FakeNotificationDeliveryProvider>();
        }
        else
        {
            throw new InvalidOperationException("Notification delivery provider must be either Fake or MetaWhatsApp.");
        }

        services.AddScoped<NotificationDeliveryService>();
        return services;
    }

    private static bool IsSupportedProvider(NotificationDeliveryOptions options) =>
        IsFake(options.Provider) || IsMetaWhatsApp(options.Provider);

    private static bool HasValidMetaWhatsAppConfiguration(NotificationDeliveryOptions options)
    {
        if (!IsMetaWhatsApp(options.Provider)) return true;

        return !string.IsNullOrWhiteSpace(options.MetaWhatsApp.PhoneNumberId) &&
               !string.IsNullOrWhiteSpace(options.MetaWhatsApp.AccessToken) &&
               !string.IsNullOrWhiteSpace(options.MetaWhatsApp.ApiVersion) &&
               options.MetaWhatsApp.ApiVersion.Trim().StartsWith('v');
    }

    private static bool IsFake(string? provider) =>
        string.Equals(provider, NotificationDeliveryOptions.FakeProvider, StringComparison.OrdinalIgnoreCase);

    private static bool IsMetaWhatsApp(string? provider) =>
        string.Equals(provider, NotificationDeliveryOptions.MetaWhatsAppProvider, StringComparison.OrdinalIgnoreCase);
}
