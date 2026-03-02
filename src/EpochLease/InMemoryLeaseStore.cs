using System.Collections.Concurrent;

namespace EpochLease;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="ILeaseStore{TId,TItem}"/>.
/// Suitable for single-process scenarios or testing.
/// </summary>
public sealed class InMemoryLeaseStore<TId, TItem> : ILeaseStore<TId, TItem>
    where TId : notnull
    where TItem : class, ILeaseable<TId>
{
    private readonly ConcurrentDictionary<TId, TItem> _items = new();

    /// <summary>
    /// Adds a work item to the store. Returns false if an item with the same ID already exists.
    /// </summary>
    public bool Add(TItem item) => _items.TryAdd(item.Id, item);

    /// <summary>
    /// Removes a work item from the store by ID. Returns false if the item was not found.
    /// </summary>
    public bool Remove(TId id) => _items.TryRemove(id, out _);

    /// <inheritdoc />
    public Task<IReadOnlyList<TItem>> GetActiveLeasedItems()
    {
        IReadOnlyList<TItem> result = _items.Values
            .Where(item => item.Lease is not null && item.Lease.IsActive && item.IsLeaseEligible)
            .ToList();

        return Task.FromResult(result);
    }
}
