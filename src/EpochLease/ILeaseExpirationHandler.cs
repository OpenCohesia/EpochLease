namespace EpochLease;

/// <summary>
/// Handles expired leases detected by the scanner.
/// Implement this to define how your application resets work items
/// when their lease expires (e.g., mark as available, notify, retry).
/// </summary>
/// <typeparam name="TId">The type of the work item identifier.</typeparam>
/// <typeparam name="TItem">The type of the work item.</typeparam>
public interface ILeaseExpirationHandler<TId, TItem>
    where TId : notnull
    where TItem : class, ILeaseable<TId>
{
    /// <summary>
    /// Called when a work item's lease has expired.
    /// Should reset the item to an available state so another worker can pick it up.
    /// </summary>
    /// <param name="item">The work item with the expired lease.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the reset was successful, false otherwise.</returns>
    Task<bool> HandleExpiredLease(TItem item, CancellationToken cancellationToken = default);
}
