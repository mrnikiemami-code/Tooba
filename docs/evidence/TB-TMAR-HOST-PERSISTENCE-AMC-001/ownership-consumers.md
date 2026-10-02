# ownership-consumers — TB-TMAR-HOST-PERSISTENCE-AMC-001

## Interface ownership

| Item | Owner |
| --- | --- |
| `IDatabaseConnectionResolver` | `Tooba.BuildingBlocks` (`PersistenceSeams.cs`) |
| Contract role | Neutral persistence/platform seam |
| Documented fail-closed | `PlatformHttpException` + `platform.connection.unconfigured` |
| Active Host implementation count | **1** — `Tooba.Host.Persistence.DatabaseConnectionResolver` |
| Other production implementations | **ZERO** (tests may use fixed fakes) |

## Program DI

```text
AddSingleton<IDatabaseConnectionResolver, DatabaseConnectionResolver>()
```

| Concern | Assessment |
| --- | --- |
| Lifetime | Singleton — intentional |
| Options | `IOptions<ToobaPlatformOptions>` snapshot at ctor (`options.Value`) |
| Thread safety | Read-only after startup; no mutation in resolver |
| Reload | `IOptionsMonitor` **not** used — runtime config reload intentionally absent |

## Consumer inventory (production)

### Host platform

| Consumer | Path class | Notes |
| --- | --- | --- |
| `TenantResolutionMiddleware` | Host HTTP request path | Resolves tenant store connection |
| `MessagingRegistration` | Host startup / bus composition | SQL transport connection via messaging `ConnectionReference` |
| `OutboxDispatcher` | Host background worker | Per-poll-target resolve |
| `MigrationRunner` / `MigrationOrchestrator` | Host tooling | Concrete `DatabaseConnectionResolver` registration (process-local) |

### Neutral infrastructure

| Consumer | Notes |
| --- | --- |
| `Tooba.Persistence.ToobaNpgsql.ResolveForContext` | Modules obtain connection strings only through this + `ICurrentCommerceContext` |

### Module infrastructure adapters (foreign module consumer of seam — legal)

Wide set of module `*Module` DI registrations call `GetRequiredService<IDatabaseConnectionResolver>()` (or via `ToobaNpgsql`) including Catalog, Order, Offer, Payment, Cart, Identity, AccessControl, Party, Content, Media, Localization, Story, Reviews, Wishlist, Support, Wallet, Tax, Pricing, Inventory, Promotion, Fulfillment, Settlement, Returns, Notification, BulkInquiry, ProductQnA, CustomerProfile, OperatorProfile, UserPreference, AddressBook, PageComposition, PlatformProbe, etc.

Classification: **foreign module consumer of neutral contract** — modules never read Host `ToobaPlatformOptions` / raw ConnectionReferences.

### Tests

Host foundation/HTTP/outbox/messaging tests + MigrationRunner tests + Payment fixed fake. Not production authority.

## Classification summary

| Class | Count pattern |
| --- | --- |
| Host platform | 4 production surfaces |
| Neutral infra | `ToobaNpgsql` |
| Module consumers | Many — Contracts-only seam usage |
| Direct public HTTP endpoint owning resolver | ZERO (resolver is infra; HTTP sees mapped `PlatformHttpException`) |
