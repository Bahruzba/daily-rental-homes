using DailyRentalHomes.Domain.Enums;

namespace DailyRentalHomes.Application.Abstractions.Messaging;

public interface IMessageSender
{
    Task<string?> SendAsync(MessageChannel channel, string to, string text, CancellationToken cancellationToken = default);
}
