using Xunit;

namespace EpochLease.Tests;

public class LeaseableExtensionsTests
{
    [Fact]
    public void HasExpiredLease_WithExpiredLeaseAndEligibleStatus_ReturnsTrue()
    {
        var item = new TestWorkItem
        {
            Id = Guid.NewGuid(),
            Lease = Lease.Create("worker-1", DateTimeOffset.UtcNow.AddMinutes(-1)),
            IsLeaseEligible = true,
        };

        Assert.True(item.HasExpiredLease(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void HasExpiredLease_WithValidLease_ReturnsFalse()
    {
        var item = new TestWorkItem
        {
            Id = Guid.NewGuid(),
            Lease = Lease.Create("worker-1", DateTimeOffset.UtcNow.AddMinutes(5)),
            IsLeaseEligible = true,
        };

        Assert.False(item.HasExpiredLease(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void HasExpiredLease_WithNullLease_ReturnsFalse()
    {
        var item = new TestWorkItem
        {
            Id = Guid.NewGuid(),
            Lease = null,
            IsLeaseEligible = true,
        };

        Assert.False(item.HasExpiredLease(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void HasExpiredLease_WithClearedLease_ReturnsFalse()
    {
        var item = new TestWorkItem
        {
            Id = Guid.NewGuid(),
            Lease = Lease.Create("worker-1", DateTimeOffset.UtcNow.AddMinutes(-5)).Clear(),
            IsLeaseEligible = true,
        };

        Assert.False(item.HasExpiredLease(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void HasExpiredLease_WithExpiredLeaseButNotEligible_ReturnsFalse()
    {
        var item = new TestWorkItem
        {
            Id = Guid.NewGuid(),
            Lease = Lease.Create("worker-1", DateTimeOffset.UtcNow.AddMinutes(-1)),
            IsLeaseEligible = false,
        };

        Assert.False(item.HasExpiredLease(DateTimeOffset.UtcNow));
    }

    private sealed class TestWorkItem : ILeaseable<Guid>
    {
        public required Guid Id { get; init; }
        public Lease? Lease { get; set; }
        public required bool IsLeaseEligible { get; init; }
    }
}
