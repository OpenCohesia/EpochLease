namespace EpochLease;

/// <summary>
/// Result of a lease expiration scan operation.
/// </summary>
public sealed class ScanResult
{
    private ScanResult(bool isSuccess, string? error, int resetCount, int failureCount)
    {
        IsSuccess = isSuccess;
        Error = error;
        ResetCount = resetCount;
        FailureCount = failureCount;
    }

    /// <summary>
    /// Whether the scan completed without any failures.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Whether the scan had at least one failure.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Error message describing failures, if any.
    /// </summary>
    public string? Error { get; }

    /// <summary>
    /// Number of items successfully reset.
    /// </summary>
    public int ResetCount { get; }

    /// <summary>
    /// Number of items that failed to reset.
    /// </summary>
    public int FailureCount { get; }

    /// <summary>
    /// Creates a successful scan result.
    /// </summary>
    public static ScanResult Success(int resetCount = 0) =>
        new(true, null, resetCount, 0);

    /// <summary>
    /// Creates a failed scan result.
    /// </summary>
    public static ScanResult Failure(string error, int resetCount = 0, int failureCount = 0) =>
        new(false, error, resetCount, failureCount);
}
