using DailyRentalHomes.Application.Abstractions.Messaging;
using DailyRentalHomes.Domain.Enums;

namespace DailyRentalHomes.Infrastructure.Messaging;

public sealed class DevelopmentMessageSender : IMessageSender
{
    public Task<string?> SendAsync(MessageChannel channel, string to, string text, CancellationToken cancellationToken = default)
    {
        var providerId = $"dev-{channel.ToString().ToLowerInvariant()}-{Guid.NewGuid():N}";
        return Task.FromResult<string?>(providerId);
    }
}
