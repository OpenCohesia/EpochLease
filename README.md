# <img src="assets/logo.svg" alt="EpochLease logo" width="36" height="36" style="vertical-align: middle; margin-bottom: 4px"> EpochLease

[![Build and Test](https://github.com/OpenCohesia/EpochLease/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/OpenCohesia/EpochLease/actions/workflows/build-and-test.yml)
[![NuGet](https://img.shields.io/nuget/v/EpochLease.svg)](https://www.nuget.org/packages/EpochLease)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Lightweight lease-based distributed work coordination with epoch-based optimistic concurrency for .NET.

## When Do You Need This?

If you have **multiple workers competing for work items from a shared queue** and need automatic recovery when workers crash, this library provides the building blocks.

Common scenarios:
- Job queues where workers pick tasks and process them
- Task schedulers with distributed agents
- Message consumers that need at-least-once delivery
- Any system where distributed agents claim and process work items

## Quick Start

Install the packages:

```bash
dotnet add package EpochLease
dotnet add package EpochLease.Extensions.Hosting
```

### 1. Define your work item

```csharp
using EpochLease;

public class Job : ILeaseable<Guid>
{
    public Guid Id { get; init; }
    public Lease? Lease { get; set; }
    public bool IsLeaseEligible => Status is JobStatus.Processing;
    public JobStatus Status { get; set; }
}

public enum JobStatus { Available, Processing, Completed, Failed }
```

### 2. Implement the expiration handler

```csharp
using EpochLease;

public class JobLeaseExpirationHandler : ILeaseExpirationHandler<Guid, Job>
{
    public Task<bool> HandleExpiredLease(Job job, CancellationToken ct)
    {
        job.Status = JobStatus.Available;
        job.Lease = job.Lease?.Clear();
        return Task.FromResult(true);
    }
}
```

### 3. Implement the lease store

```csharp
using EpochLease;

public class JobStore : ILeaseStore<Guid, Job>
{
    private readonly MyDbContext _db;

    public JobStore(MyDbContext db) => _db = db;

    public async Task<IReadOnlyList<Job>> GetActiveLeasedItems()
    {
        return await _db.Jobs
            .Where(j => j.Lease != null && j.Lease.Owner != null
                && j.Lease.ExpiresAt != null && j.IsLeaseEligible)
            .ToListAsync();
    }
}
```

### 4. Register in DI

```csharp
using EpochLease.Extensions.Hosting;

builder.Services.AddSingleton<ILeaseStore<Guid, Job>, JobStore>();
builder.Services.AddSingleton<ILeaseExpirationHandler<Guid, Job>, JobLeaseExpirationHandler>();
builder.Services.AddEpochLease<Guid, Job>();
builder.Services.AddEpochLeaseWatcher<Guid, Job>(opts =>
    opts.ScanInterval = TimeSpan.FromSeconds(5));
```

The background watcher automatically scans for expired leases and resets them.

## Core Concepts

### Lease Lifecycle

```
  ┌──────────┐    Acquire     ┌──────────┐    Release     ┌──────────┐
  │ No Lease │ ──────────────►│  Active   │ ──────────────►│ Cleared  │
  │ (null)   │   Create()     │ (epoch=1) │   Clear()      │ (epoch=2)│
  └──────────┘                └────┬──────┘                └──────────┘
                                   │  Renew()
                                   │  (epoch increments)
                                   ▼
                              ┌──────────┐
                              │  Active   │
                              │ (epoch=N) │
                              └──────────┘
```

- **Create** — A worker acquires a lease on a work item (sets owner + expiration, epoch=1)
- **Renew** — The worker extends the lease while still processing (epoch increments)
- **Clear** — The worker releases the lease when done (owner/expiresAt nullified, epoch increments)
- **Expire** — If the worker crashes, the background scanner detects the expired lease and resets the item

### Epoch Counter

The epoch is an incrementing integer that changes on every lease operation. It provides optimistic concurrency — if two workers race to modify the same lease, the epoch mismatch reveals the conflict. Your persistence layer can use the epoch in `WHERE` clauses:

```sql
UPDATE Jobs SET Lease_Owner = @newOwner, Lease_Epoch = @newEpoch
WHERE Id = @id AND Lease_Epoch = @expectedEpoch
```

### Expiration Scanner

The `LeaseExpirationScanner` is the core business logic — no threading or scheduling:

```csharp
var scanner = new LeaseExpirationScanner<Guid, Job>(store, handler, TimeProvider.System);
ScanResult result = await scanner.Scan();

if (result.IsFailure)
    Console.WriteLine($"Failed: {result.Error}");
else
    Console.WriteLine($"Reset {result.ResetCount} expired leases");
```

The `LeaseExpirationWatcher` wraps this in a `BackgroundService` with configurable scan intervals and jitter.

## API Reference

### Core Package (`EpochLease`)

| Type | Description |
|------|-------------|
| `Lease` | Immutable record with `Owner`, `ExpiresAt`, `Epoch`. Methods: `Create()`, `Renew()`, `Clear()`, `IsExpired()`, `IsActive` |
| `ILeaseable<TId>` | Interface for work items: `Id`, `Lease`, `IsLeaseEligible` |
| `LeaseableExtensions` | `HasExpiredLease()` extension method |
| `ILeaseStore<TId, TItem>` | Storage abstraction: `GetActiveLeasedItems()` |
| `InMemoryLeaseStore<TId, TItem>` | Thread-safe in-memory store for testing/single-process |
| `ILeaseExpirationHandler<TId, TItem>` | Callback: `HandleExpiredLease()` |
| `LeaseExpirationScanner<TId, TItem>` | Core scan logic: `Scan()` returns `ScanResult` |
| `ScanResult` | Result type with `IsSuccess`, `IsFailure`, `Error`, `ResetCount`, `FailureCount` |

### Hosting Package (`EpochLease.Extensions.Hosting`)

| Type | Description |
|------|-------------|
| `LeaseExpirationWatcher<TId, TItem>` | `BackgroundService` that runs the scanner on a timer with jitter |
| `LeaseExpirationWatcherOptions` | Configuration: `ScanInterval` (default: 10s) |
| `ServiceCollectionExtensions` | `AddEpochLease<TId, TItem>()`, `AddEpochLeaseWatcher<TId, TItem>()` |

## Configuration

### Via code

```csharp
services.AddEpochLeaseWatcher<Guid, Job>(opts =>
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
services.AddEpochLeaseWatcher<Guid, Job>();
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
public class EfJobStore : ILeaseStore<Guid, Job>
{
    private readonly AppDbContext _db;

    public EfJobStore(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<Job>> GetActiveLeasedItems()
    {
        return await _db.Jobs
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
modelBuilder.Entity<Job>(entity =>
{
    entity.OwnsOne(e => e.Lease, lease =>
    {
        lease.Property(l => l.Owner).HasColumnName("LeaseOwner");
        lease.Property(l => l.ExpiresAt).HasColumnName("LeaseExpiresAt");
        lease.Property(l => l.Epoch).HasColumnName("LeaseEpoch");
    });
});
```

## License

MIT — see [LICENSE](LICENSE) for details.
