namespace EpochLease;

/// <summary>
/// Scans for expired leases and invokes the handler to reset them.
/// This is the core business logic — no threading or scheduling concerns.
/// </summary>
/// <typeparam name="TId">The type of the work item identifier.</typeparam>
/// <typeparam name="TItem">The type of the work item.</typeparam>
public sealed class LeaseExpirationScanner<TId, TItem>
    where TId : notnull
    where TItem : class, ILeaseable<TId>
{
    private readonly ILeaseStore<TId, TItem> _store;
    private readonly ILeaseExpirationHandler<TId, TItem> _handler;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Creates a new scanner instance.
    /// </summary>
    public LeaseExpirationScanner(
        ILeaseStore<TId, TItem> store,
        ILeaseExpirationHandler<TId, TItem> handler,
        TimeProvider timeProvider)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <summary>
    /// Scans for work items with expired leases and resets them.
    /// </summary>
    public async Task<ScanResult> Scan(CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var items = await _store.GetActiveLeasedItems();

        var expiredItems = items
            .Where(item => item.HasExpiredLease(now))
            .ToList();

        if (expiredItems.Count == 0)
            return ScanResult.Success();

        var results = await Task.WhenAll(
            expiredItems.Select(async item =>
            {
                var success = await _handler.HandleExpiredLease(item, cancellationToken);
                return (item.Id, success);
            }));

        var failures = results.Where(r => !r.success).ToList();

        if (failures.Count == 0)
            return ScanResult.Success(expiredItems.Count);

        var failedIds = string.Join(", ", failures.Select(f => f.Id));
        return ScanResult.Failure(
            $"Failed to reset {failures.Count} item(s): {failedIds}",
            resetCount: results.Count(r => r.success),
            failureCount: failures.Count);
    }
}
