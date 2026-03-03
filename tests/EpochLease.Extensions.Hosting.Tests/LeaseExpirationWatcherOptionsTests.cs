using Xunit;

namespace OpenCohesia.EpochLease.Extensions.Hosting.Tests;

public class LeaseExpirationWatcherOptionsTests
{
    [Fact]
    public void DefaultScanInterval_IsTenSeconds()
    {
        var options = new LeaseExpirationWatcherOptions();

        Assert.Equal(TimeSpan.FromSeconds(10), options.ScanInterval);
    }

    [Fact]
    public void ScanInterval_MustBePositive()
    {
        var options = new LeaseExpirationWatcherOptions
        {
            ScanInterval = TimeSpan.Zero,
        };
        var validator = new LeaseExpirationWatcherOptionsValidator();

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
    }

    [Fact]
    public void ScanInterval_ValidValue_PassesValidation()
    {
        var options = new LeaseExpirationWatcherOptions
        {
            ScanInterval = TimeSpan.FromSeconds(5),
        };
        var validator = new LeaseExpirationWatcherOptionsValidator();

        var result = validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }
}
