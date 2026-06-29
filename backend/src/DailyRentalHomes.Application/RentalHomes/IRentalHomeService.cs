using DailyRentalHomes.Application.Common;

namespace DailyRentalHomes.Application.RentalHomes;

public interface IRentalHomeService
{
    Task<OperationResult<long>> CreateAsync(CreateRentalHomeDto request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RentalHomeListItemDto>> GetListAsync(string? city, CancellationToken cancellationToken = default);
}
