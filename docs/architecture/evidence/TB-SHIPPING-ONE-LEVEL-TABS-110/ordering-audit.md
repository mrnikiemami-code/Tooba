# TB-SHIPPING-ONE-LEVEL-TABS-110 — Development migration / post-migration ordering audit

Scope: bounded Development-only orchestration seam. Read-only audit of the real source paths.

## 1. Authority and ownership

| Concern | Owner | Path |
| --- | --- | --- |
| Canonical module migration order (single authoritative source) | BuildingBlocks/Persistence | `src/backend/BuildingBlocks/Tooba.Persistence/ModuleSchemaMigrator.cs` (`ModuleSchemaMigrationOrder`) |
| Neutral migrator / post-migration step seam | BuildingBlocks/Persistence | same file (`IModuleSchemaMigrator`, `IModuleSchemaMigrationStep`, `DelegateModuleSchemaMigrator`, `DelegateModuleSchemaMigrationStep`) |
| Development startup orchestration | Host (composition only, no foreign DbContext, no business seed authority) | `src/backend/Host/Tooba.Host/Development/DevelopmentSchemaMigrator.cs` |
| Module-owned post-migration work | each module's own Infrastructure composition root | `CatalogModule.cs`, `PromotionModule.cs`, `LocalizationModule.cs` |
| Production/explicit migration authority | MigrationRunner CLI (own registry `ModuleMigrationRegistry.All`) | `src/backend/Host/Tooba.MigrationRunner/**` — **not changed, not reachable by Host startup** |

## 2. Registered post-migration steps (all of them)

| Module | AfterOrder | Registered in | Delegate target |
| --- | --- | --- | --- |
| Catalog | 1 | `CatalogModule.cs` | `CatalogDevelopmentSeed.PostMigration.EnsureAsync` |
| Promotion | 12 | `PromotionModule.cs` | `PromotionDevelopmentSeed.EnsureAsync` |
| Localization | 22 | `LocalizationModule.cs` | `LanguageBootstrapDevelopmentSeed.PostMigration.EnsureAsync` |

Migrators: 28 modules register `AddModuleSchemaMigrator` (`Generic EF` for 23, `delegate` for Offer/Pricing/Inventory/Tax/Promotion because their migration entrypoint is a module-owned `I*SchemaMigrator`). Only `Localization` (of the step owners) is a plain EF migrator; every migrator for a module ordered **after** Localization (Content, Media, PageComposition, Story, Notification, AccessControl, Support, Geography, Shipping) is plain DDL `Database.MigrateAsync` and reads no seeded row, so moving a post-migration **step** later cannot affect them.

## 3. Before (defect)

`MigrateOrderedAsync` interleaved a step directly after its own module's migration:

```text
migrate Catalog(1) -> step Catalog(1)   <-- needs Party(6)/Offer(2)/Pricing(3)/Inventory(4)/Tax(5) schemas
migrate Offer(2)   -> (no step)
migrate Pricing(3)
migrate Inventory(4)
migrate Tax(5)
migrate Party(6)
...
migrate Promotion(12) -> step Promotion(12)
...
migrate Localization(22) -> step Localization(22)
...
migrate Shipping(31)
```

Observed runtime failure (TB-109, isolated scratch startup, unchanged behavior at TB-110 base SHA
`072127f7f60a11c0de5583a472b78c27bcf8a5d3`):

```text
Npgsql.PostgresException 42P01: relation "party.parties" does not exist
  CatalogAttributeSchemaSellableEnricher -> PartyDevelopmentSeedGateway
  reached from CatalogDevelopmentSeed.PostMigration.EnsureAsync (AfterOrder == 1)
```

Root cause: a module-owned step may reach **other modules' schemas through its own Contracts-only
Development gateways**, so `AfterOrder` ("immediately after my own schema migration") is not a
sufficient ordering contract for the step body. The Catalog step body is correct and was not changed.

## 4. After (fix)

Two stable phases inside the same seam (still Host composition only, still neutral, no module type named):

```text
PHASE 1 (unchanged canonical order):
  migrate Catalog(1) -> Offer(2) -> Pricing(3) -> Inventory(4) -> Tax(5) -> Party(6)
  -> Identity(7) -> Cart(8) -> Order(9) -> Payment(10) -> Fulfillment(11) -> Promotion(12)
  -> Reviews(14) -> ProductQnA(15) -> BulkInquiry(16) -> Wishlist(17) -> AddressBook(18)
  -> CustomerProfile(19) -> UserPreference(20) -> OperatorProfile(21) -> Localization(22)
  -> Content(23) -> Media(24) -> PageComposition(25) -> Story(26) -> Notification(27)
  -> AccessControl(28) -> Support(29) -> Geography(30) -> Shipping(31)

PHASE 2 (existing deterministic order, unchanged relative order):
  step Catalog(1) -> step Promotion(12) -> step Localization(22)
```

Preserved invariants:

- every registered module schema migration runs before **any** post-migration step;
- migration order = `OrderBy(IModuleSchemaMigrator.Order)` exactly as before; duplicate `Order` is
  still rejected fail-fast **before** anything runs;
- post-migration step order = `OrderBy(AfterOrder).ThenBy(Module, Ordinal)` exactly as before, and the
  three steps keep their previous relative order (Catalog → Promotion → Localization);
- a step whose `AfterOrder` names no registered migrator still never runs (legacy behavior);
- if any migration throws or is canceled, **no** post-migration step runs (strictly stronger than the
  legacy prefix behavior);
- every migrator and every step still executes at most once per startup (sequential loops, no retry);
- startup (fail-fast), cancellation, ABP-style `CreateAsyncScope`, commerce-context assignment in
  `ApplyCoreAsync`, and `seedCatalog: false` (`MigrateSchemaOnlyAsync`) semantics are unchanged;
- Host still knows nothing about any module's DbContext, seed or gateway.

## 5. Changed paths (exact)

| Path | Change |
| --- | --- |
| `src/backend/Host/Tooba.Host/Development/DevelopmentSchemaMigrator.cs` | `MigrateOrderedAsync`: private → internal; single interleaved loop replaced by two sequential phases; comments/docs. No module type, DbContext or seed reference added. |
| `src/backend/Host/Tooba.Host.Tests/Development/DevelopmentSchemaMigratorOrderingTests.cs` | new focused test (7 tests) with synthetic migrators/steps only (no DbContext, no database). |
| `docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-110.md` | task report |
| `docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-110/**` | evidence of this task |

Not changed (explicitly out of scope, verified untouched): `ModuleSchemaMigrator.cs` order constants,
all 28 module composition roots, all `I*SchemaMigrator` implementations, Catalog publish rules,
`ProductVariantDirectory`, Party schema/migrations, Catalog product/offer/price/stock seed data,
`Program.cs`, MigrationRunner, Checkout/Order/Fulfillment/Shipping/Payment/Settlement sources.

## 6. Unresolved / next runtime check (belongs to TB-111)

This task is source-proven and unit-proven only. Still unverified until a separately authorized bounded
scratch Host startup:

1. whether Development startup now proceeds past the `relation "party.parties" does not exist` barrier
   with the Catalog schema demo product published and the enricher succeeding;
2. whether genuine sellable offers / base prices / stock rows are then created for the demo product;
3. whether any next barrier appears (storefront labels, primary-offer selection, one-level tab surface).
