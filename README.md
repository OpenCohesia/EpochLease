# <img src="assets/logo.svg" height="30px"> EpochLease

[![Build and Test](https://github.com/OpenCohesia/EpochLease/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/OpenCohesia/EpochLease/actions/workflows/build-and-test.yml)
[![NuGet](https://img.shields.io/nuget/v/OpenCohesia.EpochLease.svg)](https://www.nuget.org/packages/OpenCohesia.EpochLease)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Lightweight lease-based distributed work coordination with epoch-based optimistic concurrency for .NET.

## When Do You Need This?

If you have **multiple workers competing for work items from a shared queue** and need automatic recovery when workers crash, this library provides the building blocks.

## Quick Start

```bash
dotnet add package OpenCohesia.EpochLease
dotnet add package OpenCohesia.EpochLease.Extensions.Hosting
```

### 1. Define your work item

```csharp
using OpenCohesia.EpochLease;

public class JobItem : ILeaseable<Guid>
{
    public Guid Id { get; init; }
    public Lease? Lease { get; set; }
    public bool IsLeaseEligible => Status is JobItemStatus.Processing;
    public JobItemStatus Status { get; set; }
}

public enum JobItemStatus { Available, Processing, Completed, Failed }
```

### 2. Implement the expiration handler

```csharp
using OpenCohesia.EpochLease;

public class JobItemLeaseExpirationHandler : ILeaseExpirationHandler<Guid, JobItem>
{
    public Task<bool> HandleExpiredLease(JobItem jobItem, CancellationToken ct)
    {
        jobItem.Status = JobItemStatus.Available;
        jobItem.Lease = jobItem.Lease?.Clear();
        return Task.FromResult(true);
    }
}
```

### 3. Implement the lease store

> For quick prototyping or single-process scenarios, you can skip this step and use the built-in `InMemoryLeaseStore<TId, TItem>` instead.

```csharp
using OpenCohesia.EpochLease;

public class JobItemStore : ILeaseStore<Guid, JobItem>
{
    private readonly MyDbContext _db;

    public JobItemStore(MyDbContext db) => _db = db;

    public async Task<IReadOnlyList<JobItem>> GetActiveLeasedItems()
    {
        return await _db.JobItems
            .Where(j => j.Lease != null && j.Lease.Owner != null
                && j.Lease.ExpiresAt != null && j.IsLeaseEligible)
            .ToListAsync();
    }
}
```

### 4. Register in DI

```csharp
using OpenCohesia.EpochLease.Extensions.Hosting;

builder.Services.AddSingleton<ILeaseStore<Guid, JobItem>, JobItemStore>();
builder.Services.AddSingleton<ILeaseExpirationHandler<Guid, JobItem>, JobItemLeaseExpirationHandler>();
builder.Services.AddEpochLease<Guid, JobItem>();
builder.Services.AddEpochLeaseWatcher<Guid, JobItem>(opts =>
    opts.ScanInterval = TimeSpan.FromSeconds(5));
```

The background watcher automatically scans for expired leases and resets them.

## Lease Lifecycle

```
  ┌──────────┐    Acquire     ┌──────────┐    Release     ┌──────────┐
  │ No Lease │ ──────────────►│  Active  │ ──────────────►│ Cleared  │
  │ (null)   │   Create()     │ (epoch=1)│   Clear()      │ (epoch=2)│
  └──────────┘                └────┬─────┘                └──────────┘
                                   │  Renew()
                                   │  (epoch increments)
                                   ▼
                              ┌──────────┐
                              │  Active  │
                              │ (epoch=N)│
                              └──────────┘
```

- **Create** — Acquire a lease (sets owner + expiration, epoch=1)
- **Renew** — Extend the lease while still processing (epoch increments)
- **Clear** — Release the lease when done (epoch increments)
- **Expire** — Background scanner detects expired leases and resets items

For API reference, configuration, and custom store examples, see the [detailed guide](docs/guide.md).

## License

MIT — see [LICENSE](LICENSE) for details.
