# 11 — Background shutdown proof (TB-P06-T004)

## stoppingToken honored

Both poll workers check `stoppingToken.IsCancellationRequested` in their main loops:

- `OutboxDispatcherHostedService.ExecuteAsync`
- `CartExpiryHostedService.ExecuteAsync`

`OperationCanceledException` during `Task.Delay` or dispatch exits the loop cleanly.

## No new claims during shutdown

When cancellation is requested:

- Loop condition fails before next `DispatchOnceAsync` / `ReconcileOnceAsync`.
- No additional outbox `ClaimAsync` or cart SKIP LOCKED batch after exit.

In-flight work: current poll cycle may complete one batch; no new cycle starts.

## MassTransit graceful stop

`MessagingRegistration` configures:

```csharp
options.StopTimeout = TimeSpan.FromSeconds(30);
options.WaitUntilStarted = true;
```

Host shutdown allows up to **30 seconds** for in-flight consumer handling before force stop.

## DB transaction cancellation

- Outbox store operations accept `CancellationToken` from the hosted service stopping token.
- Cart/inventory expiry batches use the same token on EF transactions and commands.

## Lease release on crash vs graceful stop

- **Graceful:** in-flight claim either completes (processed/retry/dead-letter) or transaction rolls back on cancel.
- **Crash:** `locked_until` passive expiry recovers orphaned outbox claims (see 04).

## AuthorizationSchemaHostedService

One-shot startup; `StopAsync` returns immediately — no long-running work at shutdown.
