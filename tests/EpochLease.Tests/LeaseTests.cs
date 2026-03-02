using Xunit;

namespace EpochLease.Tests;

public class LeaseTests
{
    [Fact]
    public void Create_SetsEpochToOne()
    {
        var lease = Lease.Create("worker-1", DateTimeOffset.UtcNow.AddMinutes(5));

        Assert.Equal(1, lease.Epoch);
        Assert.Equal("worker-1", lease.Owner);
        Assert.True(lease.IsActive);
    }

    [Fact]
    public void Create_WithNullOwner_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Lease.Create(null!, DateTimeOffset.UtcNow.AddMinutes(5)));
    }

    [Fact]
    public void Create_WithEmptyOwner_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Lease.Create("", DateTimeOffset.UtcNow.AddMinutes(5)));
    }

    [Fact]
    public void Create_WithDefaultExpiresAt_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Lease.Create("worker-1", default));
    }

    [Fact]
    public void Renew_IncrementsEpoch()
    {
        var lease = Lease.Create("worker-1", DateTimeOffset.UtcNow.AddMinutes(5));
        var renewed = lease.Renew("worker-2", DateTimeOffset.UtcNow.AddMinutes(10));

        Assert.Equal(2, renewed.Epoch);
        Assert.Equal("worker-2", renewed.Owner);
        Assert.True(renewed.IsActive);
    }

    [Fact]
    public void Clear_IncrementsEpochAndNullifiesFields()
    {
        var lease = Lease.Create("worker-1", DateTimeOffset.UtcNow.AddMinutes(5));
        var cleared = lease.Clear();

        Assert.Equal(2, cleared.Epoch);
        Assert.Null(cleared.Owner);
        Assert.Null(cleared.ExpiresAt);
        Assert.False(cleared.IsActive);
    }

    [Fact]
    public void IsExpired_WithExpiredLease_ReturnsTrue()
    {
        var expiresAt = new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var lease = Lease.Create("worker-1", expiresAt);
        var currentTime = new DateTimeOffset(2025, 1, 1, 12, 5, 0, TimeSpan.Zero);

        Assert.True(lease.IsExpired(currentTime));
    }

    [Fact]
    public void IsExpired_WithNonExpiredLease_ReturnsFalse()
    {
        var expiresAt = new DateTimeOffset(2025, 1, 1, 12, 10, 0, TimeSpan.Zero);
        var lease = Lease.Create("worker-1", expiresAt);
        var currentTime = new DateTimeOffset(2025, 1, 1, 12, 5, 0, TimeSpan.Zero);

        Assert.False(lease.IsExpired(currentTime));
    }

    [Fact]
    public void IsExpired_WithClearedLease_ReturnsFalse()
    {
        var lease = Lease.Create("worker-1", DateTimeOffset.UtcNow.AddMinutes(5));
        var cleared = lease.Clear();

        Assert.False(cleared.IsExpired(DateTimeOffset.UtcNow.AddMinutes(10)));
    }

    [Fact]
    public void SequenceCreateRenewClear_MaintainsEpochContinuity()
    {
        var lease1 = Lease.Create("worker-1", DateTimeOffset.UtcNow.AddMinutes(5));
        Assert.Equal(1, lease1.Epoch);

        var lease2 = lease1.Clear();
        Assert.Equal(2, lease2.Epoch);

        var lease3 = lease2.Renew("worker-2", DateTimeOffset.UtcNow.AddMinutes(10));
        Assert.Equal(3, lease3.Epoch);
        Assert.Equal("worker-2", lease3.Owner);
    }

    [Fact]
    public void IsActive_WithNullOwnerAndNullExpiresAt_ReturnsFalse()
    {
        var lease = new Lease(null, null, 1);
        Assert.False(lease.IsActive);
    }

    [Fact]
    public void IsActive_WithNullOwner_ReturnsFalse()
    {
        var lease = new Lease(null, DateTimeOffset.UtcNow.AddMinutes(5), 1);
        Assert.False(lease.IsActive);
    }

    [Fact]
    public void IsActive_WithNullExpiresAt_ReturnsFalse()
    {
        var lease = new Lease("worker-1", null, 1);
        Assert.False(lease.IsActive);
    }
}
