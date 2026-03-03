using Microsoft.Extensions.Time.Testing;
using Moq;
using Xunit;

namespace OpenCohesia.EpochLease.Tests;

public class LeaseExpirationScannerTests
{
    private readonly FakeTimeProvider _timeProvider = new(
        new DateTimeOffset(2025, 1, 15, 10, 0, 0, TimeSpan.Zero)
    );

    [Fact]
    public void Constructor_WithNullStore_Throws()
    {
        var handler = new Mock<ILeaseExpirationHandler<Guid, TestWorkItem>>();

        Assert.Throws<ArgumentNullException>(() =>
            new LeaseExpirationScanner<Guid, TestWorkItem>(null!, handler.Object, _timeProvider));
    }

    [Fact]
    public void Constructor_WithNullHandler_Throws()
    {
        var store = new Mock<ILeaseStore<Guid, TestWorkItem>>();

        Assert.Throws<ArgumentNullException>(() =>
            new LeaseExpirationScanner<Guid, TestWorkItem>(store.Object, null!, _timeProvider));
    }

    [Fact]
    public void Constructor_WithNullTimeProvider_Throws()
    {
        var store = new Mock<ILeaseStore<Guid, TestWorkItem>>();
        var handler = new Mock<ILeaseExpirationHandler<Guid, TestWorkItem>>();

        Assert.Throws<ArgumentNullException>(() =>
            new LeaseExpirationScanner<Guid, TestWorkItem>(store.Object, handler.Object, null!));
    }

    [Fact]
    public async Task Scan_WithExpiredLeases_CallsHandlerForEachExpired()
    {
        var expired1 = CreateItem(
            Lease.Create("worker-1", _timeProvider.GetUtcNow().AddMinutes(-5)), isEligible: true);
        var expired2 = CreateItem(
            Lease.Create("worker-2", _timeProvider.GetUtcNow().AddMinutes(-1)), isEligible: true);
        var valid = CreateItem(
            Lease.Create("worker-3", _timeProvider.GetUtcNow().AddMinutes(5)), isEligible: true);

        var store = new Mock<ILeaseStore<Guid, TestWorkItem>>();
        store.Setup(s => s.GetActiveLeasedItems())
            .ReturnsAsync(new List<TestWorkItem> { expired1, expired2, valid });

        var handler = new Mock<ILeaseExpirationHandler<Guid, TestWorkItem>>();
        handler.Setup(h => h.HandleExpiredLease(It.IsAny<TestWorkItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var scanner = new LeaseExpirationScanner<Guid, TestWorkItem>(
            store.Object, handler.Object, _timeProvider);

        var result = await scanner.Scan(TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        handler.Verify(h => h.HandleExpiredLease(expired1, It.IsAny<CancellationToken>()), Times.Once);
        handler.Verify(h => h.HandleExpiredLease(expired2, It.IsAny<CancellationToken>()), Times.Once);
        handler.Verify(h => h.HandleExpiredLease(valid, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Scan_WithEmptyStore_ReturnsSuccess()
    {
        var store = new Mock<ILeaseStore<Guid, TestWorkItem>>();
        store.Setup(s => s.GetActiveLeasedItems())
            .ReturnsAsync(new List<TestWorkItem>());

        var handler = new Mock<ILeaseExpirationHandler<Guid, TestWorkItem>>();

        var scanner = new LeaseExpirationScanner<Guid, TestWorkItem>(
            store.Object, handler.Object, _timeProvider);

        var result = await scanner.Scan(TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        handler.Verify(
            h => h.HandleExpiredLease(It.IsAny<TestWorkItem>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Scan_WhenHandlerFails_ReturnsFailureWithErrors()
    {
        var expired = CreateItem(
            Lease.Create("worker-1", _timeProvider.GetUtcNow().AddMinutes(-5)), isEligible: true);

        var store = new Mock<ILeaseStore<Guid, TestWorkItem>>();
        store.Setup(s => s.GetActiveLeasedItems())
            .ReturnsAsync(new List<TestWorkItem> { expired });

        var handler = new Mock<ILeaseExpirationHandler<Guid, TestWorkItem>>();
        handler.Setup(h => h.HandleExpiredLease(expired, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var scanner = new LeaseExpirationScanner<Guid, TestWorkItem>(
            store.Object, handler.Object, _timeProvider);

        var result = await scanner.Scan(TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Scan_WithMixedResults_ReportsPartialFailure()
    {
        var expired1 = CreateItem(
            Lease.Create("worker-1", _timeProvider.GetUtcNow().AddMinutes(-5)), isEligible: true);
        var expired2 = CreateItem(
            Lease.Create("worker-2", _timeProvider.GetUtcNow().AddMinutes(-1)), isEligible: true);

        var store = new Mock<ILeaseStore<Guid, TestWorkItem>>();
        store.Setup(s => s.GetActiveLeasedItems())
            .ReturnsAsync(new List<TestWorkItem> { expired1, expired2 });

        var handler = new Mock<ILeaseExpirationHandler<Guid, TestWorkItem>>();
        handler.Setup(h => h.HandleExpiredLease(expired1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        handler.Setup(h => h.HandleExpiredLease(expired2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var scanner = new LeaseExpirationScanner<Guid, TestWorkItem>(
            store.Object, handler.Object, _timeProvider);

        var result = await scanner.Scan(TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Contains(expired2.Id.ToString(), result.Error);
    }

    [Fact]
    public async Task Scan_IgnoresNonEligibleItems()
    {
        var expired = CreateItem(
            Lease.Create("worker-1", _timeProvider.GetUtcNow().AddMinutes(-5)), isEligible: false);

        var store = new Mock<ILeaseStore<Guid, TestWorkItem>>();
        store.Setup(s => s.GetActiveLeasedItems())
            .ReturnsAsync(new List<TestWorkItem> { expired });

        var handler = new Mock<ILeaseExpirationHandler<Guid, TestWorkItem>>();

        var scanner = new LeaseExpirationScanner<Guid, TestWorkItem>(
            store.Object, handler.Object, _timeProvider);

        var result = await scanner.Scan(TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        handler.Verify(
            h => h.HandleExpiredLease(It.IsAny<TestWorkItem>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private TestWorkItem CreateItem(Lease? lease, bool isEligible) =>
        new()
        {
            Id = Guid.NewGuid(),
            Lease = lease,
            IsLeaseEligible = isEligible,
        };

    public sealed class TestWorkItem : ILeaseable<Guid>
    {
        public required Guid Id { get; init; }
        public Lease? Lease { get; set; }
        public required bool IsLeaseEligible { get; init; }
    }
}
