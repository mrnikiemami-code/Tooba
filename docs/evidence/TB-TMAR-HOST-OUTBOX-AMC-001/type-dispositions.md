# type-dispositions — TB-TMAR-HOST-OUTBOX-AMC-001

| Type | Disposition | Rationale |
|---|---|---|
| OutboxDispatcher | **KEEP_AS_GLOBAL_HOST_OUTBOX_PLATFORM** | Process-level poll/claim/publish/retry orchestration across module outbox tables; Host platform ownership. |
| OutboxDispatcherHostedService | **KEEP_AS_GLOBAL_HOST_BACKGROUND_WORKER** | BackgroundService loop; readiness-independent; belongs with Host Outbox platform. |
| OutboxHostOptions | **KEEP_AS_GLOBAL_HOST_OUTBOX_PLATFORM** | Host deployment options (`Tooba:Outbox`). |
| ConfiguredOutboxPollTargetSource | **KEEP_AS_THIN_HOST_WORKER_CONTEXT_ADAPTER** | Enumerates Active tenants / Marketplace connection from `ControlPlaneRegistry` only. |
| WorkerCommerceContextFactory | **KEEP_AS_THIN_HOST_WORKER_CONTEXT_ADAPTER** | Rebuilds `CommerceContext` from registry + outbox/poll identity; no HTTP headers; anti-spoof. Not StoreContext move (technical commerce, not store commerce). |
| WorkerStoreCommerceContextFactory | **KEEP_AS_THIN_HOST_WORKER_CONTEXT_ADAPTER** | Selects `StoreCommerceContext` from registry; Contracts-only StoreContext boundary; no Market/Currency/SalesChannel defaults. |

No type is DEAD / MOVE_TO_PERSISTENCE / MOVE_TO_STORECONTEXT / BLOCKED.

Folder-level: retain Host/Outbox; migrate namespace + file cohesion in W1.
