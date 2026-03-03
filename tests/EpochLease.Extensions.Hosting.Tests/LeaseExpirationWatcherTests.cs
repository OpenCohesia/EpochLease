using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Xunit;

namespace OpenCohesia.EpochLease.Extensions.Hosting.Tests;

public class LeaseExpirationWatcherTests
{
    private readonly FakeTimeProvider _timeProvider = new(
        new DateTimeOffset(2025, 1, 15, 10, 0, 0, TimeSpan.Zero)
    );

    [Fact]
    public void Constructor_WithNullScanner_Throws()
    {
        var logger = NullLogger<LeaseExpirationWatcher<Guid, TestWorkItem>>.Instance;
        var options = Options.Create(new LeaseExpirationWatcherOptions());

        Assert.Throws<ArgumentNullException>(() =>
            new LeaseExpirationWatcher<Guid, TestWorkItem>(null!, logger, options));
    }

    [Fact]
    public void Constructor_WithNullLogger_Throws()
    {
        var scanner = CreateScanner();
        var options = Options.Create(new LeaseExpirationWatcherOptions());

        Assert.Throws<ArgumentNullException>(() =>
            new LeaseExpirationWatcher<Guid, TestWorkItem>(scanner, null!, options));
    }

    [Fact]
    public void Constructor_WithNullOptions_Throws()
    {
        var scanner = CreateScanner();
        var logger = NullLogger<LeaseExpirationWatcher<Guid, TestWorkItem>>.Instance;

        Assert.Throws<ArgumentNullException>(() =>
            new LeaseExpirationWatcher<Guid, TestWorkItem>(scanner, logger, null!));
    }

    [Fact]
    public async Task ExecuteAsync_ScansAndStopsOnCancellation()
    {
        var store = new Mock<ILeaseStore<Guid, TestWorkItem>>();
        store.Setup(s => s.GetActiveLeasedItems())
            .ReturnsAsync(new List<TestWorkItem>());

        var handler = new Mock<ILeaseExpirationHandler<Guid, TestWorkItem>>();
        var scanner = new LeaseExpirationScanner<Guid, TestWorkItem>(
            store.Object, handler.Object, _timeProvider);

        var options = Options.Create(new LeaseExpirationWatcherOptions
        {
            ScanInterval = TimeSpan.FromMilliseconds(1),
        });

        var logger = NullLogger<LeaseExpirationWatcher<Guid, TestWorkItem>>.Instance;
        var watcher = new LeaseExpirationWatcher<Guid, TestWorkItem>(scanner, logger, options);

        await watcher.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(50, TestContext.Current.CancellationToken);
        await watcher.StopAsync(TestContext.Current.CancellationToken);

        store.Verify(s => s.GetActiveLeasedItems(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_LogsErrorOnScanFailure()
    {
        var expiredItem = new TestWorkItem
        {
            Id = Guid.NewGuid(),
            Lease = Lease.Create("worker-1", _timeProvider.GetUtcNow().AddMinutes(-5)),
            IsLeaseEligible = true,
        };

        var store = new Mock<ILeaseStore<Guid, TestWorkItem>>();
        store.Setup(s => s.GetActiveLeasedItems())
            .ReturnsAsync(new List<TestWorkItem> { expiredItem });

        var handler = new Mock<ILeaseExpirationHandler<Guid, TestWorkItem>>();
        handler.Setup(h => h.HandleExpiredLease(It.IsAny<TestWorkItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var scanner = new LeaseExpirationScanner<Guid, TestWorkItem>(
            store.Object, handler.Object, _timeProvider);

        var options = Options.Create(new LeaseExpirationWatcherOptions
        {
            ScanInterval = TimeSpan.FromMilliseconds(1),
        });

        var logger = new Mock<ILogger<LeaseExpirationWatcher<Guid, TestWorkItem>>>();
        var watcher = new LeaseExpirationWatcher<Guid, TestWorkItem>(
            scanner, logger.Object, options);

        await watcher.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(50, TestContext.Current.CancellationToken);
        await watcher.StopAsync(TestContext.Current.CancellationToken);

        logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    private LeaseExpirationScanner<Guid, TestWorkItem> CreateScanner()
    {
        var store = new Mock<ILeaseStore<Guid, TestWorkItem>>();
        var handler = new Mock<ILeaseExpirationHandler<Guid, TestWorkItem>>();
        return new LeaseExpirationScanner<Guid, TestWorkItem>(
            store.Object, handler.Object, _timeProvider);
    }

    public sealed class TestWorkItem : ILeaseable<Guid>
    {
        public required Guid Id { get; init; }
        public Lease? Lease { get; set; }
        public required bool IsLeaseEligible { get; init; }
    }
}
