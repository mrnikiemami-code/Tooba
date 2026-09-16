# Messaging Audit — TB-P11-T001

| Topic | Status | Evidence |
| --- | --- | --- |
| MassTransit version | **8.5.10** locked | `Tooba.Host.csproj`, arch doc 34 |
| Transport | **PostgreSQL SQL Transport** (`UsingPostgres`, `MassTransit.SqlTransport.PostgreSQL`) | `MessagingRegistration.cs` |
| RabbitMQ packages | **Absent** in Host csproj; tests assert forbidden | `MassTransitFoundationTests.cs` |
| Config | `MessagingHostOptions` rejects non-PostgreSql; RabbitMQ/AMQP forbidden | `MessagingHostOptions.cs` |
| Outbox | Module transactional outbox + `OutboxDispatcher` → `IIntegrationEventPublisher` | `OutboxDispatcher.cs`; EF Outbox not added (comment in MessagingRegistration) |
| Admin dependency | Settlement/notification/order lifecycle events are background; Admin UIs read state — no Admin RabbitMQ | settlement P09 evidence |
| Remnants | Docs/ops mention RabbitMQ only as **FORBIDDEN** | `docs/operations/messaging.md` |
| Direct publish bypass | Publisher seam required; no silent fallback | `AddToobaIntegrationPublisher` |

**Verdict:** Locked architecture intact. No RabbitMQ runtime remnant in `src/`. Remnants are documentation/forbid-assertions only. No messaging redesign in T001.
