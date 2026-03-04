# EpochLease Guide

Detailed reference for the EpochLease library. For a quick overview, see the [README](../README.md).

## Core Concepts

### Epoch Counter

The epoch is an incrementing integer that changes on every lease operation. It provides optimistic concurrency — if two workers race to modify the same lease, the epoch mismatch reveals the conflict. Your persistence layer can use the epoch in `WHERE` clauses:

```sql
UPDATE JobItems SET Lease_Owner = @newOwner, Lease_Epoch = @newEpoch
WHERE Id = @id AND Lease_Epoch = @expectedEpoch
```

### Expiration Scanner

The `LeaseExpirationScanner` is the core business logic — no threading or scheduling:

```csharp
var scanner = new LeaseExpirationScanner<Guid, JobItem>(store, handler, TimeProvider.System);
ScanResult result = await scanner.Scan();

if (result.IsFailure)
    Console.WriteLine($"Failed: {result.Error}");
else
    Console.WriteLine($"Reset {result.ResetCount} expired leases");
```

The `LeaseExpirationWatcher` wraps this in a `BackgroundService` with configurable scan intervals and jitter.

## API Reference

### Core Package (`OpenCohesia.EpochLease`)

| Type | Description |
|------|-------------|
| `Lease` | Immutable record with `Owner`, `ExpiresAt`, `Epoch`. Methods: `Create()`, `Renew()`, `Clear()`, `IsExpired()`. Property: `IsActive` |
| `ILeaseable<TId>` | Interface for work items: `Id`, `Lease`, `IsLeaseEligible` |
| `LeaseableExtensions` | `HasExpiredLease()` extension method |
| `ILeaseStore<TId, TItem>` | Storage abstraction: `GetActiveLeasedItems()` |
| `InMemoryLeaseStore<TId, TItem>` | Thread-safe in-memory store for testing/single-process |
| `ILeaseExpirationHandler<TId, TItem>` | Callback: `HandleExpiredLease()` |
| `LeaseExpirationScanner<TId, TItem>` | Core scan logic: `Scan()` returns `ScanResult` |
| `ScanResult` | Result type with `IsSuccess`, `IsFailure`, `Error`, `ResetCount`, `FailureCount` |

### Hosting Package (`OpenCohesia.EpochLease.Extensions.Hosting`)

| Type | Description |
|------|-------------|
| `LeaseExpirationWatcher<TId, TItem>` | `BackgroundService` that runs the scanner on a timer with jitter |
| `LeaseExpirationWatcherOptions` | Configuration: `ScanInterval` (default: 10s) |
| `ServiceCollectionExtensions` | `AddEpochLease<TId, TItem>()`, `AddEpochLeaseWatcher<TId, TItem>()` |

## Configuration

### Via code

```csharp
services.AddEpochLeaseWatcher<Guid, JobItem>(opts =>
    opts.ScanInterval = TimeSpan.FromSeconds(30));
```

### Via appsettings.json

```json
{
  "LeaseExpirationWatcher": {
    "ScanInterval": "00:00:30"
  }
}
```

```csharp
services.AddEpochLeaseWatcher<Guid, JobItem>();
services.Configure<LeaseExpirationWatcherOptions>(
    builder.Configuration.GetSection(LeaseExpirationWatcherOptions.SectionName));
```

### Options

| Option | Default | Description |
|--------|---------|-------------|
| `ScanInterval` | 10 seconds | Base interval between scans. Actual interval includes ±10% jitter to avoid thundering herd. Must be greater than zero. |

## Custom Store Implementations

The `ILeaseStore<TId, TItem>` interface has a single method to implement:

```csharp
public interface ILeaseStore<TId, TItem>
    where TId : notnull
    where TItem : class, ILeaseable<TId>
{
    Task<IReadOnlyList<TItem>> GetActiveLeasedItems();
}
```

### Entity Framework Core Example

```csharp
public class EfJobItemStore : ILeaseStore<Guid, JobItem>
{
    private readonly AppDbContext _db;

    public EfJobItemStore(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<JobItem>> GetActiveLeasedItems()
    {
        return await _db.JobItems
            .Where(j => j.Lease != null
                && j.Lease.Owner != null
                && j.Lease.ExpiresAt != null
                && j.IsLeaseEligible)
            .ToListAsync();
    }
}
```

Configure EF Core to map the `Lease` owned type:

```csharp
modelBuilder.Entity<JobItem>(entity =>
{
    entity.OwnsOne(e => e.Lease, lease =>
    {
        lease.Property(l => l.Owner).HasColumnName("LeaseOwner");
        lease.Property(l => l.ExpiresAt).HasColumnName("LeaseExpiresAt");
        lease.Property(l => l.Epoch).HasColumnName("LeaseEpoch");
    });
});
```
