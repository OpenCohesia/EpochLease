using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace EpochLease.Extensions.Hosting.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEpochLease_RegistersScanner()
    {
        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ILeaseStore<Guid, TestWorkItem>>(
            new InMemoryLeaseStore<Guid, TestWorkItem>());
        services.AddSingleton<ILeaseExpirationHandler<Guid, TestWorkItem>>(
            new TestHandler());

        services.AddEpochLease<Guid, TestWorkItem>();

        var provider = services.BuildServiceProvider();
        var scanner = provider.GetService<LeaseExpirationScanner<Guid, TestWorkItem>>();
        Assert.NotNull(scanner);
    }

    [Fact]
    public void AddEpochLeaseWatcher_RegistersHostedService()
    {
        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ILeaseStore<Guid, TestWorkItem>>(
            new InMemoryLeaseStore<Guid, TestWorkItem>());
        services.AddSingleton<ILeaseExpirationHandler<Guid, TestWorkItem>>(
            new TestHandler());
        services.AddLogging();

        services.AddEpochLease<Guid, TestWorkItem>();
        services.AddEpochLeaseWatcher<Guid, TestWorkItem>();

        var provider = services.BuildServiceProvider();
        var hostedServices = provider.GetServices<IHostedService>();
        Assert.Contains(hostedServices,
            s => s is LeaseExpirationWatcher<Guid, TestWorkItem>);
    }

    [Fact]
    public void AddEpochLeaseWatcher_WithConfigure_AppliesConfiguration()
    {
        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ILeaseStore<Guid, TestWorkItem>>(
            new InMemoryLeaseStore<Guid, TestWorkItem>());
        services.AddSingleton<ILeaseExpirationHandler<Guid, TestWorkItem>>(
            new TestHandler());
        services.AddLogging();

        var expectedInterval = TimeSpan.FromSeconds(42);

        services.AddEpochLease<Guid, TestWorkItem>();
        services.AddEpochLeaseWatcher<Guid, TestWorkItem>(
            opts => opts.ScanInterval = expectedInterval);

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<LeaseExpirationWatcherOptions>>();
        Assert.Equal(expectedInterval, options.Value.ScanInterval);
    }

    public sealed class TestWorkItem : ILeaseable<Guid>
    {
        public Guid Id { get; init; }
        public Lease? Lease { get; set; }
        public bool IsLeaseEligible => true;
    }

    private sealed class TestHandler : ILeaseExpirationHandler<Guid, TestWorkItem>
    {
        public Task<bool> HandleExpiredLease(
            TestWorkItem item, CancellationToken ct) => Task.FromResult(true);
    }
}
