# analyze — TB-TMAR-HOST-OUTBOX-AMC-001

## Mode

ANALYSIS_ONLY — production change ZERO.

## Exact tree

```text
src/backend/Host/Tooba.Host/Outbox/
  OutboxDispatcher.cs
  OutboxHostOptions.cs
  OutboxWorkerSeams.cs
```

| Metric | Value |
|---|---|
| Production `.cs` count | **3** |
| Top-level production types | **6** |
| Nested types | 0 |
| Namespace (current) | `Tooba.Host` |
| Path-derived namespace | `Tooba.Host.Outbox` |
| Path↔namespace | **VIOLATION** |
| Visibility | all `internal sealed` |

### Types by file

| File | Types |
|---|---|
| OutboxDispatcher.cs | `OutboxDispatcher`, `OutboxDispatcherHostedService` |
| OutboxHostOptions.cs | `OutboxHostOptions` |
| OutboxWorkerSeams.cs | `ConfiguredOutboxPollTargetSource`, `WorkerCommerceContextFactory`, `WorkerStoreCommerceContextFactory` |

## Consumers

- `Program.cs`: options bind, singleton store/targets/factories/dispatcher, hosted service.
- `Transport/ToobaIntegrationTransportConsumer.cs`: `WorkerCommerceContextFactory` concrete.
- Module workers (Cart/Payment/Order): `IWorkerCommerceContextFactory` / `IWorkerStoreCommerceContextFactory` contracts only.
- Tests: `OutboxFoundationTests`, `OutboxPostgresTests`, `OutboxTestSupport`.

## Protected certifications preserved

HOST_OBSERVABILITY / MESSAGING / HEALTH / MULTITENANCY / ERRORS / SECURITY / ADMIN — untouched (docs-only analyze).

## Recommended next

`TB-TMAR-HOST-OUTBOX-AMC-001-W1` then `W2-CERT` (see migration-plan.md).
