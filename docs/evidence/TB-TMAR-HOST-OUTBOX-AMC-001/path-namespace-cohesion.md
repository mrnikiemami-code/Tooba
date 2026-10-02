# path-namespace-cohesion — TB-TMAR-HOST-OUTBOX-AMC-001

## Path ↔ namespace

| Current | Target |
|---|---|
| `namespace Tooba.Host;` | `namespace Tooba.Host.Outbox;` |

State: **VIOLATION_Tooba.Host_vs_Outbox**

Program/tests/Transport must gain `using Tooba.Host.Outbox;` after W1 (no alias/shim).

## File cohesion

| File | Top-level types | Decision |
|---|---|---|
| OutboxDispatcher.cs | 2 | **MUST_SPLIT** → Dispatcher + HostedService |
| OutboxHostOptions.cs | 1 | OK |
| OutboxWorkerSeams.cs | 3 | **MUST_SPLIT** → one type per file |

Canonical target tree (W1):

```text
Outbox/
  OutboxDispatcher.cs
  OutboxDispatcherHostedService.cs
  OutboxHostOptions.cs
  OutboxHostOptionsValidator.cs   # if validator added in same wave
  ConfiguredOutboxPollTargetSource.cs
  WorkerCommerceContextFactory.cs
  WorkerStoreCommerceContextFactory.cs
```

State: `MULTI_TYPE_FILES_MUST_SPLIT_ONE_TOP_LEVEL_TYPE_PER_FILE`
