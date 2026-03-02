namespace EpochLease;

/// <summary>
/// Represents a work item that can be leased by a worker.
/// </summary>
/// <typeparam name="TId">The type of the work item identifier.</typeparam>
public interface ILeaseable<TId> where TId : notnull
{
    /// <summary>
    /// Unique identifier for this work item.
    /// </summary>
    TId Id { get; }

    /// <summary>
    /// Current lease information. Null if never leased.
    /// </summary>
    Lease? Lease { get; }

    /// <summary>
    /// Whether this item is in a state where lease expiration should be checked.
    /// For example, only items in "processing" states (not completed/failed) are eligible.
    /// </summary>
    bool IsLeaseEligible { get; }
}
