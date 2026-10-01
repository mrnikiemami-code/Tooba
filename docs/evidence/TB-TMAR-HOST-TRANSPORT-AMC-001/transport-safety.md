# Transport safety — Host/Transport AMC-001

## Envelope

`ToobaIntegrationTransportMessage` fields remain generic transport metadata only (EventType, Version, EventId, OccurredAt, TenantId, Edition, DeploymentId, CorrelationId, PayloadJson). No module-specific fields. No `$type` / AssemblyQualifiedName coupling.

## Consumer

- Deserializes via `IIntegrationEventSerializer` into generic integration event.
- Reconstructs commerce context via `WorkerCommerceContextFactory` + `ICommerceContextAssigner`.
- Dispatches `IIntegrationEventHandler<>` via DI `GetServices` (no concrete module event switch).
- Logs EventType/TenantId/Edition/DeploymentId/EventId/HandlersPresent only — **PayloadJson never logged**.
- Tenant integrity check preserved as generic platform invariant.
- Unknown handler absence: existing behavior preserved (`HandlersPresent=false`, still counts consumed).

## SQL mapper

- Npgsql connection-string mapping into MassTransit `SqlTransportOptions` only.
- Schema parameter is infrastructure schema input (e.g. transport), not module schema authority.
- **No ILogger / no connection-string or password logging.**
