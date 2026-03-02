using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
