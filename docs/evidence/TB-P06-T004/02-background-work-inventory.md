# 02 — Background work inventory (TB-P06-T004)

| Worker / job | Owner | Purpose | Trigger | Data source | Lock / lease | Retry | Idempotency | Shutdown | Observability | Stuck recovery |
|---|---|---|---|---|---|---|---|---|---|---|
| **OutboxDispatcherHostedService** | Host / all modules | Poll module outbox tables → publish integration events | Continuous loop; `Tooba:Outbox:PollIntervalSeconds` (default 2s) | Per-tenant DB via `ConfiguredOutboxPollTargetSource` | `locked_until` + `FOR UPDATE SKIP LOCKED` | Exponential backoff; max 5 attempts → dead-letter | `processed_at` marker | Honors `stoppingToken`; no new poll after cancel | Metrics: `tooba.outbox.*`; OTel `tooba.outbox.dispatch`; `BackgroundWorkerRegistry` | Expired `locked_until` re-claimable |
| **CartExpiryHostedService** | Host / Cart + Inventory | Expire due carts; release held inventory | Continuous loop; `Tooba:CartExpiry:PollIntervalSeconds` (min 5s) | Same poll targets as outbox | Cart + inventory rows: `FOR UPDATE SKIP LOCKED` | Next poll cycle (no per-row retry table) | Expired cart no-op; inventory `Released` no-op | Honors `stoppingToken` | Metrics: `tooba.cart_expiry.*`; OTel `tooba.cart_expiry.reconcile`; registry | Stale claims released at transaction end |
| **AuthorizationSchemaHostedService** | Host / Authorization | Apply SpiceDB schema when configured | One-shot on startup (`IHostedService.StartAsync`) | SpiceDB endpoint | N/A (external API) | Operator re-run / restart | Bootstrap guard in bootstrapper | Immediate `StopAsync` | Logs only | Manual re-bootstrap |
| **MassTransit SQL Transport** | Host / Messaging | Consume `tooba-integration` endpoint | Event-driven (transport queue) | Dedicated messaging PostgreSQL DB | MassTransit delivery semantics | 2 immediate + 5s/15s/30s intervals | Handler-level (e.g. order payment inbox) | `StopTimeout` 30s | Metrics: `tooba.messaging.*`; MassTransit OTel source | Transport redelivery + SQL transport ops |
| **PostgresMigrationHostedService** | MassTransit registration | Create SQL transport infrastructure on startup | Startup when messaging enabled | Messaging connection ref | N/A | Startup retry via host | Idempotent DDL | Part of MassTransit host stop | Logs | Re-run on restart |
| **MigrationRunner CLI** | `Tooba.MigrationRunner` | `status` / `plan` / `apply` EF migrations per tenant | Manual CLI invocation | Tenant connection refs from config | PostgreSQL advisory lock per DB | Operator retry | Migration history table | Process exit | CLI stdout | Release advisory lock on dispose |

## Not present

- No Quartz, Hangfire, or other scheduler framework.
- No RabbitMQ consumers.
- No in-memory-only durable work queues.
