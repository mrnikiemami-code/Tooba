# Tooba — Outbox, Domain Events & Background Foundation

Status:

```text
P01 foundation — candidate layout; not an ADR; not a P01 Gate
```

Task:

```text
TB-P01-T006
```

```text
Domain Event != Integration Event
Outbox != Message Broker
No cross-module transaction
At-least-once; not exactly-once
No global ordering guarantee
Worker tenant is not Host
/ready does not require an empty outbox
```

## What was implemented

- Domain and Integration event contracts in `Tooba.BuildingBlocks` (no EF, no ASP.NET).
- Module-owned `outbox_messages` mapped per schema via `OutboxMessage` CLR type + `OutboxMessageMapping` (not one global physical table for all modules).
- `OutboxSaveChangesInterceptor`: collect domain events, translate only registered mappings, insert outbox rows in the same `SaveChanges` transaction, never invoke consumers.
- `IOutboxDispatcherStore` with PostgreSQL `FOR UPDATE SKIP LOCKED`.
- `IIntegrationEventPublisher` boundary (T006 in-process publisher is now an explicit Testing double; production default is MassTransit SQL Transport in T007).
- `IIntegrationEventHandler<T>` consumed after claim.
- Hosted dispatcher from `Tooba:Outbox` (poll interval, batch size, retry base delay, max attempts).
- `IOutboxModuleRegistration` so PlatformProbe registers as sample DI; generic interceptor/store do not hard-code probe names.
- Inbox seam: `IInboxProcessedStore` interface only (full inbox table deferred).
- Disposable PlatformProbe sample: domain + integration event, schema `platform_probe.outbox_messages`, EF migration.
- JSON payload with explicit event-type map; metadata taken from columns so payload cannot spoof TenantId.
- LastError sanitizer: no secrets, stack, or payload.
- Observability via existing `ToobaTelemetry` (event type / tenant / schema tags only).

PlatformProbe remains disposable convention proof. This is not Catalog, Identity, SpiceDB, or a bus.

## Domain vs Integration

```text
Domain Event = internal fact inside one module
Integration Event = versioned externalized fact
```

Not every domain event is published. PlatformProbe raises `ProbeRecordCreatedDomainEvent` (translated) and can raise `ProbeInternalNoteDomainEvent` (no translation → no outbox row).

## Outbox vs broker

```text
SaveChanges persists outbox rows
dispatcher later claims and publishes
consumers are never called from SaveChanges
```

Delivery is at-least-once. Handlers must be idempotent or later use Inbox. There is no global order across tenants, modules, or databases.

## Module ownership

Each module owns `schema.outbox_messages`. No cross-module transaction. No shared mega outbox table. Host composes registrations; it does not own probe tables.

## Tenant-aware worker

The worker does **not** resolve tenant from HTTP Host.

```text
Single-Store: enumerate Active configured tenants → ConnectionReference → poll each database separately
Marketplace: poll marketplace database only
one tenant failure must not corrupt another
no cross-tenant DbContext / connection reuse
```

Handler `CommerceContext` is reconstructed from durable outbox columns (`TenantId`, `Edition`, `DeploymentId`) plus the config registry.

## Serialization

Explicit type mapping (`platform_probe.record_created.v1` → CLR type). No `TypeNameHandling`, no `Type.GetType` on payload, no polymorphic CLR dump.

## Health

`/health` and `/ready` stay independent of pending outbox depth.

## Deferred

- Full Inbox table and consumer idempotency store (seam only; T007 did not add MassTransit EF inbox)
- RabbitMQ / MassTransit v9 / per-tenant bus
- Business modules, Identity, SpiceDB, Catalog
- Tenant migration orchestrator
- Exactly-once and global ordering (explicitly out of scope)
