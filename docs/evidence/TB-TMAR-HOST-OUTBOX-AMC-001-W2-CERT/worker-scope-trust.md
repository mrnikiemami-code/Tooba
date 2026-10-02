# worker-scope-trust — TB-TMAR-HOST-OUTBOX-AMC-001-W2-CERT

Per-message scope certified:

- singleton dispatcher injects `IServiceScopeFactory`
- `CreateAsyncScope` per message
- scoped resolve of `ICommerceContextAssigner`, `IStoreCommerceContextAssigner`, `IIntegrationEventPublisher`

Label: `LEGITIMATE_PER_MESSAGE_WORKER_SCOPE_COMPOSITION_CERTIFIED`

Trust certified:

- registry authority for edition/deployment/connection
- SingleStore Active tenant required
- HTTP Host ignored
- StoreCommerce Contracts-only

Label: `ANTI_SPOOF_REGISTRY_AUTHORITY_CERTIFIED`
