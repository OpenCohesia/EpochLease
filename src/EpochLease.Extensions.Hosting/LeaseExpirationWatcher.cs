using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OpenCohesia.EpochLease.Extensions.Hosting;

/// <summary>
/// Background service that periodically scans for expired leases and resets them.
/// Uses jittered intervals (±10%) to avoid thundering herd in multi-instance deployments.
/// </summary>
/// <typeparam name="TId">The type of the work item identifier.</typeparam>
/// <typeparam name="TItem">The type of the work item.</typeparam>
public sealed class LeaseExpirationWatcher<TId, TItem> : BackgroundService
    where TId : notnull
    where TItem : class, ILeaseable<TId>
{
    private readonly LeaseExpirationScanner<TId, TItem> _scanner;
    private readonly ILogger<LeaseExpirationWatcher<TId, TItem>> _logger;
    private readonly LeaseExpirationWatcherOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="LeaseExpirationWatcher{TId, TItem}"/> class.
    /// </summary>
    public LeaseExpirationWatcher(
        LeaseExpirationScanner<TId, TItem> scanner,
        ILogger<LeaseExpirationWatcher<TId, TItem>> logger,
        IOptions<LeaseExpirationWatcherOptions> options)
    {
        _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Lease expiration watcher starting with scan interval {Interval}",
            _options.ScanInterval);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var result = await _scanner.Scan(stoppingToken);
                if (result.IsFailure)
                {
                    _logger.LogError(
                        "Lease expiration scan failed: {Error}", result.Error);
                }

                var delay = GetJitteredDelay(_options.ScanInterval);
                await Task.Delay(delay, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Lease expiration watcher stopping (cancellation requested)");
        }

        _logger.LogInformation("Lease expiration watcher stopped");
    }

    private static TimeSpan GetJitteredDelay(TimeSpan baseInterval)
    {
        double factor = 1.0 + ((Random.Shared.NextDouble() - 0.5) * 0.2);
        double ms = Math.Max(0, baseInterval.TotalMilliseconds * factor);
        return TimeSpan.FromMilliseconds(ms);
    }
}
