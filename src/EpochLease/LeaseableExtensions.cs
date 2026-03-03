namespace OpenCohesia.EpochLease;

/// <summary>
/// Extension methods for <see cref="ILeaseable{TId}"/>.
/// </summary>
public static class LeaseableExtensions
{
    /// <summary>
    /// Checks if the work item has an expired lease and is in an eligible state for reset.
    /// </summary>
    public static bool HasExpiredLease<TId>(
        this ILeaseable<TId> item,
        DateTimeOffset currentTime
    ) where TId : notnull =>
        item.Lease is not null
        && item.IsLeaseEligible
        && item.Lease.IsExpired(currentTime);
}
