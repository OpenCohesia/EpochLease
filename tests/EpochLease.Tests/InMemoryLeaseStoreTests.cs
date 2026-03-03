using Xunit;

namespace OpenCohesia.EpochLease.Tests;

public class InMemoryLeaseStoreTests
{
    [Fact]
    public async Task GetActiveLeasedItems_ReturnsOnlyEligibleItems()
    {
        var store = new InMemoryLeaseStore<Guid, TestWorkItem>();

        var eligible = new TestWorkItem
        {
            Id = Guid.NewGuid(),
            Lease = Lease.Create("worker-1", DateTimeOffset.UtcNow.AddMinutes(5)),
            IsLeaseEligible = true,
        };
        var notEligible = new TestWorkItem
        {
            Id = Guid.NewGuid(),
            Lease = Lease.Create("worker-1", DateTimeOffset.UtcNow.AddMinutes(5)),
            IsLeaseEligible = false,
        };
        var noLease = new TestWorkItem
        {
            Id = Guid.NewGuid(),
            Lease = null,
            IsLeaseEligible = true,
        };

        store.Add(eligible);
        store.Add(notEligible);
        store.Add(noLease);

        var items = await store.GetActiveLeasedItems();

        var item = Assert.Single(items);
        Assert.Equal(eligible.Id, item.Id);
    }

    [Fact]
    public async Task GetActiveLeasedItems_WhenEmpty_ReturnsEmptyCollection()
    {
        var store = new InMemoryLeaseStore<Guid, TestWorkItem>();

        var items = await store.GetActiveLeasedItems();

        Assert.Empty(items);
    }

    [Fact]
    public void Add_DuplicateId_ReturnsFalse()
    {
        var store = new InMemoryLeaseStore<Guid, TestWorkItem>();
        var id = Guid.NewGuid();
        var item1 = new TestWorkItem { Id = id, Lease = null, IsLeaseEligible = true };
        var item2 = new TestWorkItem { Id = id, Lease = null, IsLeaseEligible = true };

        Assert.True(store.Add(item1));
        Assert.False(store.Add(item2));
    }

    [Fact]
    public void Remove_ExistingItem_ReturnsTrue()
    {
        var store = new InMemoryLeaseStore<Guid, TestWorkItem>();
        var item = new TestWorkItem
        {
            Id = Guid.NewGuid(),
            Lease = null,
            IsLeaseEligible = true,
        };
        store.Add(item);

        Assert.True(store.Remove(item.Id));
    }

    [Fact]
    public void Remove_NonExistentItem_ReturnsFalse()
    {
        var store = new InMemoryLeaseStore<Guid, TestWorkItem>();

        Assert.False(store.Remove(Guid.NewGuid()));
    }

    private sealed class TestWorkItem : ILeaseable<Guid>
    {
        public required Guid Id { get; init; }
        public Lease? Lease { get; set; }
        public required bool IsLeaseEligible { get; init; }
    }
}
