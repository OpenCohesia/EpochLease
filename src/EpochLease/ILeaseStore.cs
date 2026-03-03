namespace OpenCohesia.EpochLease;

/// <summary>
/// Storage abstraction for leaseable work items.
/// Implement this interface to integrate with your persistence layer
/// (database, in-memory, etc.).
/// </summary>
/// <typeparam name="TId">The type of the work item identifier.</typeparam>
/// <typeparam name="TItem">The type of the work item.</typeparam>
public interface ILeaseStore<TId, TItem>
    where TId : notnull
    where TItem : class, ILeaseable<TId>
{
    /// <summary>
    /// Returns all items that currently have an active lease and are in an eligible state.
    /// Used by the expiration scanner to find candidates for reset.
    /// </summary>
    Task<IReadOnlyList<TItem>> GetActiveLeasedItems();
}
