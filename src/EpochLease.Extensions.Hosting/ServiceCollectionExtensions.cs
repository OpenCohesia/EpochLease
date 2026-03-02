using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace EpochLease.Extensions.Hosting;

/// <summary>
/// Extension methods for registering EpochLease services in the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the <see cref="LeaseExpirationScanner{TId,TItem}"/> in the DI container.
    /// Also registers <see cref="TimeProvider.System"/> as a fallback if no TimeProvider
    /// is already registered. Override by registering your own TimeProvider before calling this.
    /// Requires <see cref="ILeaseStore{TId,TItem}"/> and
    /// <see cref="ILeaseExpirationHandler{TId,TItem}"/> to be registered.
    /// </summary>
    public static IServiceCollection AddEpochLease<TId, TItem>(
        this IServiceCollection services)
        where TId : notnull
        where TItem : class, ILeaseable<TId>
    {
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<LeaseExpirationScanner<TId, TItem>>();
        return services;
    }

    /// <summary>
    /// Registers the <see cref="LeaseExpirationWatcher{TId,TItem}"/> background service.
    /// Call <see cref="AddEpochLease{TId,TItem}"/> first.
    /// </summary>
    public static IServiceCollection AddEpochLeaseWatcher<TId, TItem>(
        this IServiceCollection services,
        Action<LeaseExpirationWatcherOptions>? configure = null)
        where TId : notnull
        where TItem : class, ILeaseable<TId>
    {
        var optionsBuilder = services
            .AddOptions<LeaseExpirationWatcherOptions>()
            .ValidateOnStart();

        if (configure is not null)
            optionsBuilder.Configure(configure);

        services.TryAddSingleton<IValidateOptions<LeaseExpirationWatcherOptions>,
            LeaseExpirationWatcherOptionsValidator>();

        services.AddHostedService<LeaseExpirationWatcher<TId, TItem>>();
        return services;
    }
}
