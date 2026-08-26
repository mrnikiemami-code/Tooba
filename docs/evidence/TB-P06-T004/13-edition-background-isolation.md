# 13 — Edition background isolation (TB-P06-T004)

## Poll target resolution (`ConfiguredOutboxPollTargetSource`)

| Edition | Targets polled | Cross-tenant scan? |
|---|---|---|
| **Marketplace** | Single marketplace connection reference | No tenant list; one shared DB |
| **Single-Store** | One target per **Active** tenant in registry | Only configured tenants — not all PostgreSQL servers |
| **Unset** | Empty list (no background DB work) | N/A |

Workers use `WorkerCommerceContextFactory` to rebuild `CommerceContext` from registry + message/target — **not** from HTTP Host header.

## Single-Store safety

- Each tenant's outbox/cart/inventory data lives in that tenant's connection reference database.
- Failure in tenant A logged; tenant B continues in same poll cycle.
- No tenant credential or connection string exposed in health JSON.

## Marketplace safety

- Single marketplace DB partition; outbox rows carry tenant metadata where applicable.
- Dispatcher test: marketplace DB processed independently of single-store tenant DB (`OutboxPostgresTests.Marketplace_dispatcher_only_reads_marketplace_database`).

## Destructive / expensive jobs

- No default "scan all databases on server" job.
- `MigrationRunner` requires explicit `--tenant`, `--tenants`, or `--all-tenants` — never implicit full-server sweep from Host.

## Cart expiry parity

`CartExpiryHostedService` uses the same `IOutboxPollTargetSource` as outbox — identical edition/tenant targeting rules.

## Credential leakage

Readiness and health endpoints return check labels only (`postgresql=configured`, `messaging=Healthy`) — no connection strings.
