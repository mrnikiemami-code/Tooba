# worker-scope-trust — TB-TMAR-HOST-OUTBOX-AMC-001-W1

Per-message worker scope preserved:

- `IServiceScopeFactory.CreateAsyncScope()`
- scoped `ICommerceContextAssigner`
- scoped `IStoreCommerceContextAssigner`
- scoped `IIntegrationEventPublisher`

No WorkerScopeRunner abstraction. No singleton injection of scoped services.

Worker commerce trust preserved:

- Marketplace uses registry deployment connection
- SingleStore TenantId must resolve Active in registry
- connection reference from registry record
- HTTP Host unused
- FromPollTarget operator text English only

Store commerce factory Contracts-only; Marketplace -> DeploymentStoreCommerce; SingleStore -> active tenant StoreCommerce.
