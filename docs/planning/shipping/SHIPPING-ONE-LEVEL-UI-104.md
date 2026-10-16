# SHIPPING-ONE-LEVEL-UI-104 — narrow sellable-offer path: decision and proposal (BLOCKED)

Task `TB-SHIPPING-ONE-LEVEL-TABS-104` (parent 103) · Channel `tooba-main`
Mode `BOUNDED_CATALOG_SEED_LATENCY_AND_NARROW_SELLABLE_OFFER_PATH` · Stop-State `STOP_FOR_ARCHITECT`
**Status: BLOCKED (`BLOCKED_NARROW_SELLABLE_SEED_PATH`)** — report-only: no source change, no seed call, no scratch
environment started. Evidence: `docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-104/source-route-inventory.txt`.

## 1. Source-first findings (Mission A, ~11 minutes, read-only)

**The demo HTTP API is draft-only by construction — it can never yield a sellable offer.**
`POST /v1/admin/catalog/demo/reset-and-seed` and `POST /v1/admin/catalog/demo/seed-only`
(`CatalogDemoDevEndpoints.cs` lines 19–20) both run `CatalogDemoSeedService.SeedAsync`, and both then enforce
`ValidateIntegrity` (`CatalogDemoResetAndSeedHost.cs` line 179: roots == 15, brands ≥ 20, tags ≥ 30,
**420 ≤ products ≤ 720**, line 206) and `EnsureProductLifecycleCleanAsync` (line 214: **Published == 0**,
Archived == 0, line 235). `seed-only` therefore does **not** avoid the 545-product expansion, and no offer, price
or stock is created anywhere in that service. This explains 103's `offers=0 / prices=0 / stock=0` as **design, not
as a client-timeout artefact** — so a longer timeout would have produced nothing sellable. Per the Architect's
ruling, that run was not repeated.

**The only Development code that creates sellable data (all bounded, all Catalog-owned).**

| Seam | What it creates | Where |
| --- | --- | --- |
| `CatalogAttributeSchemaSellableEnricher` | 1 product, 1 offer per variant, base price 12,500,000 IRR, 5 stock each | lines 67, 105, 111, 126, 134 |
| `IWorkspaceDemoSeed` → `WorkspaceDemoMarketplaceSeed` | 1 live demo product, 2 seller offers, 2 prices, tax rule, inventory location/hold/stock | lines 123, 128, 135, 143, 151, 167, 191, 221 |
| `StorefrontDemo/StorefrontDemoCatalogBootstrap` | self-contained bounded template catalog: own category tree, products, publish (line 247), then offers/prices/inventory (256–298) | lines 184–298, 397–403 |

**…and none of them is reachable on an empty scratch database.** All three hang off the Development startup chain:
`Program.cs` line 365 `if (RunLegacyBootstraps) DevelopmentSchemaMigrator.ApplyAsync(...)`, and inside
`DevelopmentSchemaMigrator.ApplyCoreAsync` the migration steps run **first** (`MigrateOrderedAsync`, line 119)
and only then `IWorkspaceDemoSeed` (line 78). One of those steps — `CatalogModule.cs` line 154 →
`CatalogDevelopmentSeed.PostMigration.EnsureAsync` (line 21, gated by the same `RunLegacyBootstraps` at line 32) →
`CatalogAttributeSchemaDevelopmentSeed.ApplyAsync` **line 99 `PublishProductAsync`** — throws an unhandled
`InvalidOperationException: محصول برای انتشار آماده نیست` on a genuinely empty database, aborting the whole chain
(verbatim stack from 102, unchanged at HEAD `ce1b9c2`: `ProductPublishReadinessReader` rejection →
`UnwrapLifecycle` line 849 → `ApplyAsync` line 99 → `PostMigration` line 38 → `MigrateOrderedAsync` line 143).

**No narrowed configuration exists.** `CatalogDemoSeedOptions` (section `Tooba:CatalogDemo`) exposes exactly two
booleans — `AllowResetAndSeed` (line 14) and `RunLegacyBootstraps` (line 20). There is no workspace-demo-only flag,
no single-product/bounded-size parameter and no seed selector.

Route scan for any other seed/dev surface found only unrelated `demo-preview` routes (Wallet, Support) and the
dev-context reads — no Offer/Pricing/Inventory seed endpoint exists.

## 2. Why this is Path B, not Path A

Path A required an *existing official Development-only route or supported parameter* that is demonstrably bounded
and usable once in scratch. Neither part exists at HEAD: the only HTTP seed API is draft-only by construction, and
the only bounded sellable seeds sit behind a startup chain that cannot complete on an empty database, with no
configuration to select them individually. Inventing raw SQL, pre-creating rows to slip past the seed's
early-return, or disabling the failing step would all be either forbidden or a scope expansion, so Path B applies:
**no seed call was made and no long seed was launched.**

## 3. Minimal Catalog-owned proposal for the Architect (report-only)

**P1 — one-line diagnosis enabler (recommended first, smallest, zero behaviour change).**
Surface the already-computed readiness gaps at the failure point instead of the opaque Persian message.
`ProductPublishReadinessReader.GetAsync` already returns `ProductPublishReadiness.Missing[]` with per-requirement
codes (`category`, `identity`, `attributes`, `variants`, `media`, `seo`), but `CatalogDirectory.UnwrapLifecycle`
(line 849) discards them and throws only `MessageNotReadyFa`. Making the Development seed log
`missing.Select(m => m.Code + ":" + m.MessageFa)` before that throw (or including `missing[]` in the thrown message
in Development only) names the exact failing prerequisite on a scratch database in one run — turning the current
guess into a fact and making P2 a targeted fix rather than a redesign.

**P2 — bounded sellable fixture builder using existing public application commands (no framework, no raw SQL).**
Add one Development-only, Catalog-owned entry point (e.g. a third `CatalogDemoSeedOptions` flag such as
`RunWorkspaceDemoSeedOnly`, or an admin-gated Development `POST /v1/admin/catalog/demo/sellable-fixture`) that
invokes **only** the existing bounded seams — `IWorkspaceDemoSeed.SeedNewProductAsync` (one live product + variant
+ 2 seller offers + prices + stock) or, for the schema-product case, the existing
`CatalogAttributeSchemaSellableEnricher` sequence (`EnsureActiveSellerOfferAsync`,
`EnsureDevelopmentBasePriceAsync`, `EnsureDevelopmentOfferCategoryAsync`, `IncreaseDevelopmentStockAsync`,
`ResolveDevelopmentSellerPartyAsync`) — and never `CatalogDemoSeedService.SeedAsync`. Development-only, opt-in,
default-false, idempotent, no production logic touched, and it must be verified on a disposable scratch database.

**P3 — sequence guard, optional.** Make the attribute-schema product publish non-fatal (or move it behind the
enricher's guarded publish, which runs the identical media+SEO+publish sequence at line 67 only when the piece is
actually missing/rejected) so one sick seed step cannot abort the Development chain that hosts the sellable seeds.
Catalog Development seed ownership only; verify against `docs/ai/tasks`/Host tests before applying.

Supporting observation: shared `tooba_alpha` holds 567 products and **3223 offers**, i.e. the
Offer/Pricing/Inventory Development gateways demonstrably work on a populated database. The gap is purely
empty-database ordering/reachability, not gateway capability or cross-module contract failure.

## 4. Timebox, cleanup, parity

Mission A took ~11 minutes; the task stopped well inside the 30-minute hard limit, with nothing to tear down
(no container, no host, no secret, no temporary file was created for this task — listeners on 5292 and 55443 are 0
and only `postgres-db`/`rabbitmq` containers exist). Shared stack verified read-only and unchanged: `:5088`
`/health` 200, provenance `dev-singlestore / store-alpha / tenant-alpha`, gate unchanged, `tooba_alpha`
7/14/50/78/14/26 (and 567/3223). No shared write, no DML, no process or gate change. The two pre-existing dirty
frontend screens were preserved untouched. As instructed, this result claims nothing about Checkout cutover or
commercial shipping readiness.
