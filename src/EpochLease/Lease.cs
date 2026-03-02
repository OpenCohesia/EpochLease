namespace EpochLease;

/// <summary>
/// Represents a time-limited lease on a work item.
/// Uses nullable Owner/ExpiresAt to preserve epoch history after release.
/// Epoch increments on every Create, Renew, or Clear operation for optimistic concurrency.
/// </summary>
public record Lease(string? Owner, DateTimeOffset? ExpiresAt, int Epoch)
{
    /// <summary>
    /// True if lease is active (has both owner and expiration).
    /// </summary>
    public bool IsActive => Owner is not null && ExpiresAt is not null;

    /// <summary>
    /// Creates initial lease with epoch 1.
    /// </summary>
    public static Lease Create(string owner, DateTimeOffset expiresAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner, nameof(owner));
        if (expiresAt == default)
            throw new ArgumentException("ExpiresAt must be set", nameof(expiresAt));

        return new Lease(owner, expiresAt, Epoch: 1);
    }

    /// <summary>
    /// Renews lease with new owner and expiration, incrementing epoch.
    /// </summary>
    public Lease Renew(string newOwner, DateTimeOffset newExpiresAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newOwner, nameof(newOwner));
        if (newExpiresAt == default)
            throw new ArgumentException("ExpiresAt must be set", nameof(newExpiresAt));

        return this with
        {
            Owner = newOwner,
            ExpiresAt = newExpiresAt,
            Epoch = Epoch + 1,
        };
    }

    /// <summary>
    /// Clears lease (sets Owner and ExpiresAt to null) while incrementing epoch.
    /// Preserves lease history for optimistic concurrency.
    /// </summary>
    public Lease Clear() => new(null, null, Epoch + 1);

    /// <summary>
    /// Checks if lease has expired at given time.
    /// Only active leases can expire.
    /// </summary>
    public bool IsExpired(DateTimeOffset currentTime) =>
        IsActive && ExpiresAt!.Value <= currentTime;
}
