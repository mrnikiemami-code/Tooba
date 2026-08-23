# Tooba — MassTransit PostgreSQL SQL Transport

Status:

```text
P01 foundation — candidate layout; not an ADR; not a P01 Gate
```

Task:

```text
TB-P01-T007
```

Locked versions:

```text
MassTransit = 8.5.10
MassTransit.SqlTransport.PostgreSQL = 8.5.10
MassTransit.EntityFrameworkCore = NOT ADDED
MassTransit.RabbitMQ = NOT USED
RabbitMQ.Client = NOT USED
MassTransit v9 = NOT USED
```

```text
MassTransit is infrastructure.
Business/application contracts must remain MassTransit-neutral.
SQL Transport != module business persistence
one messaging transport database per deployment
T006 module-owned transactional Outbox is retained
at-least-once; not exactly-once
no global ordering guarantee
consumer tenant is not Host
```

## Why 8.5.10 and not v9

Architect locked **8.5.10** as the project MassTransit version. v9 APIs and packages must not be substituted. Cursor must not pick `latest`.

## Why PostgreSQL SQL Transport (not RabbitMQ)

Initial durable integration transport is MassTransit SQL Transport on PostgreSQL already in the platform.

```text
RabbitMQ = NO
external broker service = NO
```

SQL Transport provides queues, topics, subscriptions, competing consumers, retry/redelivery, and error/dead-letter queues without a separate broker process. It is **not** the module write-model and **not** the T006 outbox table.

Later transport swap (for example RabbitMQ) should stay behind the same Tooba publisher/consumer contracts.

## Deployment messaging database

```text
Marketplace deployment → 1 messaging transport connection
SingleStore shared deployment → 1 shared messaging transport connection
→ many isolated tenant business databases
```

Do **not** create one bus or one transport database per Single-Store tenant.

Configuration:

```text
Tooba:Messaging:Enabled
Tooba:Messaging:ConnectionReference
Tooba:Messaging:Schema   (default: transport)
```

Credentials resolve through the existing `ConnectionReference` map. Do not derive messaging DB from Host, TenantId, the first tenant database, or the current HTTP tenant connection. Connection strings are never logged.

## Transport schema

Instant CLR values on module DbContexts use DateTimeOffset conversion rather than a process-wide Npgsql NodaTime plugin, because that plugin makes MassTransit SQL Transport Dapper fail on `timestamptz` (`enqueue_time`).

MassTransit objects must not live in `catalog`, `identity`, `pricing`, `platform_probe`, or other module-owned business schemas.

## Infrastructure migration / credentials

Development and Testcontainers use `AddPostgresMigrationHostedService` with `CreateDatabase = false` and `CreateInfrastructure = true` against an already-created messaging database.

Today admin credentials equal the application user from the same ConnectionReference. Production should split:

```text
application runtime credentials
transport infrastructure/migration credentials
```

Do not keep elevated production privileges permanently if avoidable. Destructive database deletion is not enabled outside isolated tests.

## T006 outbox retained

```text
KEEP T006 transactional Outbox in each module/tenant business database
REPLACE the temporary in-process publisher
WITH MassTransit PostgreSQL SQL Transport
```

MassTransit EF Bus Outbox / Inbox packages are **not** added. A generic EF hosted outbox cannot safely enumerate dynamic Single-Store tenant databases.

```text
Do not create Custom Outbox + MassTransit EF Outbox duplicate persistence
for the same outgoing message.
```

If a future task proves EF outbox can replace T006 under DB-per-tenant, that is an Architect redesign — not this task.

Flow:

```text
Module-local transaction
→ module-owned transactional Outbox (T006)
→ tenant-aware dispatcher
→ IIntegrationEventPublisher
→ MassTransit 8.5.10
→ PostgreSQL SQL Transport
→ ToobaIntegrationTransportConsumer
→ IIntegrationEventHandler<T>
```

Outbox `ProcessedAt` means **published to transport**, not **consumer succeeded**.

## Publisher / consumer boundaries

Tooba-owned:

```text
IIntegrationEventPublisher
IIntegrationEventHandler<T>
IIntegrationEvent
```

MassTransit types (`IBus`, `IPublishEndpoint`, `IConsumer<T>`) stay in `Tooba.Host` adapters.

Production publisher: `MassTransitIntegrationEventPublisher`.

Explicit Testing double: `InProcessIntegrationEventPublisher` only when `Tooba:Messaging:UseInProcessTestDouble=true` **and** environment is `Testing`.

If messaging is disabled: `MessagingDisabledPublisher` throws. There is no silent in-process fallback.

Receive endpoint name is stable and shared:

```text
tooba-integration
```

Not machine-name based, not tenant-hostname based, not one endpoint per tenant.

## Tenant metadata / partition

The shared Single-Store transport carries durable:

```text
TenantId
Edition
DeploymentId
EventId / EventType / Version / Correlation
```

on `ToobaIntegrationTransportMessage` and transport headers. Payload JSON cannot spoof TenantId (serializer still applies column/envelope metadata).

Consumer reconstructs `CommerceContext` via `WorkerCommerceContextFactory` from that metadata + config registry. It does **not** read HTTP Host, current web request, default tenant, or first tenant.

Partition-key **convention** (not forced ordered receive mode): persist `TenantId` on the envelope and headers so tenant-scoped messages *can* use a transport partition key later. This task does **not** enable partitioned/ordered SQL receive mode. **No global ordering guarantee.**

Unknown/disabled tenant context fails the consume (transport retry / error queue).

## Retry vs redelivery vs dead-letter

```text
Outbox delivery retry
= failure publishing from business outbox to MassTransit transport
→ T006 attempt_count / NextAttemptAt / outbox dead-letter

Consumer processing retry
= failure consuming an already transported message
→ MassTransit UseMessageRetry on tooba-integration
→ SQL Transport _error / dead-letter queues
```

These domains are not mixed. A consumer handler throw must not dead-letter the business outbox row after a successful publish.

Message scheduler / Quartz / Hangfire are not introduced. Delayed redelivery scheduler is deferred; this task uses immediate consumer retry only.

## Serialization / versioning

Explicit Tooba type map (`platform_probe.record_created.v1` → CLR). Envelope + payload JSON. No Newtonsoft `TypeNameHandling`, no unrestricted polymorphic CLR deserialize, no assembly-qualified type names as the version strategy.

`Domain Event != Integration Event` remains.

## OpenTelemetry

Existing `ToobaTelemetry` plus MassTransit activity source name `MassTransit`. Tags: event type, event id, tenant id, edition, deployment id, endpoint name. No payloads, no connection strings.

## Inbox / idempotency

T006 `IInboxProcessedStore` remains a seam. No full generic Inbox product here. Consumers stay at-least-once; handlers must be idempotent. MassTransit EF inbox was evaluated and **not** added because it would couple consume-side dedup to a non-module-owned EF model and would not match tenant DB isolation.

## Health / startup

Invalid enabled messaging configuration fails start (`ValidateOnStart`).

`/ready`:

- messaging disabled → `{ status: ready }` (no bus)
- bus `Unhealthy` → 503
- `Healthy` / `Degraded` → ready
- does **not** require zero queue backlog
- does **not** require empty outbox

## Tests

Isolated Testcontainers PostgreSQL: business DBs plus a dedicated `tooba_messaging` database. Real bus, real SQL Transport, no InMemory harness as the proof.

## Carry-forward

```text
OpenTelemetry contrib package alignment later
/__platform-* diagnostic endpoints before public deploy
config-backed tenant registry is not production control plane
Npgsql package alignment (EF Instant conversion vs MassTransit Dapper timestamptz)
PlatformProbe disposable
durable Inbox/dedup not complete
SQL Transport admin vs runtime credentials should be split in production
MassTransit delayed redelivery / SQL scheduler deferred
process-wide Npgsql NodaTime plugin remains incompatible with MassTransit SQL Transport Dapper
```

## Future replacement / extraction

Keep `IIntegrationEventPublisher` / `IIntegrationEventHandler<T>` if SQL Transport is later replaced by RabbitMQ or another MassTransit transport. Do not leak `IPublishEndpoint` into modules.

## PlatformProbe

Disposable proof only: local transaction → T006 outbox → SQL Transport → probe handler. Not a business module.
