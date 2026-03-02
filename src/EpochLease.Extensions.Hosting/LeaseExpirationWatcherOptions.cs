using Microsoft.Extensions.Options;

namespace EpochLease.Extensions.Hosting;

/// <summary>
/// Configuration options for the <see cref="LeaseExpirationWatcher{TId,TItem}"/> background service.
/// </summary>
public sealed class LeaseExpirationWatcherOptions
{
    /// <summary>
    /// Configuration section name for binding from appsettings.json.
    /// </summary>
    public const string SectionName = "LeaseExpirationWatcher";

    /// <summary>
    /// Base interval between scans. Actual interval includes jitter of ±10%.
    /// Default: 10 seconds.
    /// </summary>
    public TimeSpan ScanInterval { get; set; } = TimeSpan.FromSeconds(10);
}

/// <summary>
/// Validates <see cref="LeaseExpirationWatcherOptions"/>.
/// </summary>
public sealed class LeaseExpirationWatcherOptionsValidator
    : IValidateOptions<LeaseExpirationWatcherOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, LeaseExpirationWatcherOptions options)
    {
        if (options.ScanInterval <= TimeSpan.Zero)
            return ValidateOptionsResult.Fail("ScanInterval must be greater than zero.");

        return ValidateOptionsResult.Success;
    }
}
