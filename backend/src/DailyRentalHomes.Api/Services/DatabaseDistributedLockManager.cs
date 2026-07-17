using DailyRentalHomes.Domain.Entities;
using DailyRentalHomes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DailyRentalHomes.Api.Services;

public sealed class DatabaseDistributedLockManager : IDistributedLockManager
{
    private readonly AppDbContext _db;
    private readonly ILogger<DatabaseDistributedLockManager> _logger;
    private readonly string _ownerId = $"{Environment.MachineName}-{Guid.NewGuid():N}";

    public DatabaseDistributedLockManager(AppDbContext db, ILogger<DatabaseDistributedLockManager> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IDistributedLockLease?> TryAcquireAsync(string key, TimeSpan leaseDuration, CancellationToken cancellationToken)
    {
        var normalizedKey = NormalizeKey(key);
        var now = DateTime.UtcNow;
        var expiresAt = now.Add(leaseDuration <= TimeSpan.Zero ? TimeSpan.FromSeconds(120) : leaseDuration);

        try
        {
            if (_db.Database.IsRelational())
            {
                await using var transaction = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
                var lease = await TryAcquireCoreAsync(normalizedKey, now, expiresAt, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return lease;
            }

            return await TryAcquireCoreAsync(normalizedKey, now, expiresAt, cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            _logger.LogWarning(exception, "Distributed lock {LockKey} could not be acquired because of a concurrent update.", normalizedKey);
            return null;
        }
    }

    private async Task<IDistributedLockLease?> TryAcquireCoreAsync(
        string key,
        DateTime acquiredAt,
        DateTime expiresAt,
        CancellationToken cancellationToken)
    {
        var current = await _db.DistributedLocks
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(item => item.Key == key, cancellationToken);

        if (current is not null && !current.IsDeleted && current.ExpiresAt > acquiredAt && current.OwnerId != _ownerId)
        {
            return null;
        }

        if (current is null)
        {
            current = new DistributedLock
            {
                Key = key,
                OwnerId = _ownerId,
                AcquiredAt = acquiredAt,
                ExpiresAt = expiresAt
            };
            _db.DistributedLocks.Add(current);
        }
        else
        {
            current.IsDeleted = false;
            current.OwnerId = _ownerId;
            current.AcquiredAt = acquiredAt;
            current.ExpiresAt = expiresAt;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new DatabaseDistributedLockLease(this, key, _ownerId);
    }

    public async Task<bool> ReleaseAsync(string key, string ownerId, CancellationToken cancellationToken)
    {
        var normalizedKey = NormalizeKey(key);
        var current = await _db.DistributedLocks
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(item => item.Key == normalizedKey && item.OwnerId == ownerId, cancellationToken);
        if (current is null || current.IsDeleted)
        {
            return false;
        }

        current.IsDeleted = true;
        current.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string NormalizeKey(string key)
    {
        var normalized = key.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > 200)
        {
            throw new InvalidOperationException("Distributed lock key is required and must be 200 characters or less.");
        }

        return normalized;
    }

    private sealed class DatabaseDistributedLockLease : IDistributedLockLease
    {
        private readonly DatabaseDistributedLockManager _manager;
        private bool _released;

        public DatabaseDistributedLockLease(DatabaseDistributedLockManager manager, string key, string ownerId)
        {
            _manager = manager;
            Key = key;
            OwnerId = ownerId;
        }

        public string Key { get; }
        public string OwnerId { get; }

        public async ValueTask DisposeAsync()
        {
            if (_released) return;
            _released = true;
            await _manager.ReleaseAsync(Key, OwnerId, CancellationToken.None);
        }
    }
}
