namespace DailyRentalHomes.Api.Services;

public interface IDistributedLockManager
{
    Task<IDistributedLockLease?> TryAcquireAsync(string key, TimeSpan leaseDuration, CancellationToken cancellationToken);
    Task<bool> ReleaseAsync(string key, string ownerId, CancellationToken cancellationToken);
}

public interface IDistributedLockLease : IAsyncDisposable
{
    string Key { get; }
    string OwnerId { get; }
}
