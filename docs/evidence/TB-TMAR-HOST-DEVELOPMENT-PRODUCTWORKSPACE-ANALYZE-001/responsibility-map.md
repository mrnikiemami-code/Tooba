# TB-TMAR-HOST-DEVELOPMENT-PRODUCTWORKSPACE-ANALYZE-001 — Responsibility Map

## Target

`src/backend/Host/Tooba.Host/Development/ProductWorkspaceDevelopmentBootstrap.cs`

| Fact | Value |
| --- | --- |
| Lines | 428 (accepted baseline) |
| SHA256 (read-only baseline for this analysis) | `96F3DEDDEF8B5FD1A003C34CE24AE9E78BA389E2240C507A6D942D351C77DD28` |
| Namespace | `Tooba.Host.Development` |
| Kind | `internal static class` (4 members: `SeedSlug`, `MigrateSchemaOnlyAsync`, `ApplyAsync`, `ApplyCoreAsync`, plus 4 private helpers) |
| Call sites | `Program.cs:340` (`ApplyAsync`), `Program.cs:360` (`MigrateSchemaOnlyAsync`) — Development-only branch |

Skill applied: `.cursor/skills/tooba-architecture-analyze/SKILL.md` (analysis only; no migrate, no certify).

## File-Cohesion-State

`MULTI_RESPONSIBILITY_COHESION_VIOLATION`

One file holds four unrelated reasons to change:
1. Host platform composition (tenant/commerce-context assignment, guard, ordering),
2. module schema-migration orchestration for 28 module schemas,
3. Catalog-owned demo **business** product/category/brand/attribute/variant creation,
4. foreign-module development-seed composition **plus** two Host-owned dev seeds
   (`Host/Wishlist/WishlistDevelopmentSeed.cs`, `Host/Settings/SettingsFoundationDevelopmentSeed.cs`)
   that themselves perform foreign `DbContext` business reads/writes.

## Responsibility inventory (exhaustive, every code region)

| # | Responsibility | Current dependency (exact) | True owner | Current legality | Target mechanism | Existing reusable seed/contract? | New contract? | May remain Host? | Migration risk | Exact target path if moved |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | Development tenant resolution + Active-tenant guard (`ControlPlaneRegistry`, tenant `store-alpha`) | `ControlPlaneRegistry` (Host) | HOST platform composition | LEGAL | unchanged | — | no | YES | LOW | stays |
| 2 | `CommerceContext` assignment + trace label (`workspace-dev-seed` / `workspace-dev-schema`) | `ICommerceContextAssigner` (BuildingBlocks) | HOST platform composition | LEGAL | unchanged | `DevelopmentTenantCommerceContext` seam already exists | no | YES | LOW | stays |
| 3 | Catalog schema migration | `CatalogDbContext.Database.MigrateAsync` | Catalog (schema) | **ILLEGAL (Host-owned foreign persistence orchestration)** | module migrator port (precedent `IOfferSchemaMigrator`) | no port today | yes (platform-neutral, Wave 2) | NO | MEDIUM | Wave 2 seam |
| 4 | Offer schema migration | `IOfferSchemaMigrator` (Offer.Contracts/Infra) | Offer | LEGAL (Contracts port) | unchanged | yes | no | invocation only | LOW | stays |
| 5 | Pricing schema migration | `IPricingSchemaMigrator` | Pricing | LEGAL | unchanged | yes | no | invocation only | LOW | stays |
| 6 | Inventory schema migration | `IInventorySchemaMigrator` | Inventory | LEGAL | unchanged | yes | no | invocation only | LOW | stays |
| 7 | Tax schema migration | `ITaxSchemaMigrator` | Tax | LEGAL | unchanged | yes | no | invocation only | LOW | stays |
| 8 | Promotion schema migration | `IPromotionSchemaMigrator` | Promotion | LEGAL | unchanged | yes | no | invocation only | LOW | stays |
| 9 | Party schema migration | `PartyDbContext` | Party | **ILLEGAL** | Wave 2 seam | no | yes | NO | MEDIUM | Wave 2 seam |
| 10 | Identity schema migration | `IdentityDbContext` | Identity | **ILLEGAL** | Wave 2 seam | no | yes | NO | MEDIUM | Wave 2 seam |
| 11 | Cart schema migration | `CartDbContext` | Cart | **ILLEGAL** | Wave 2 seam | no | yes | NO | MEDIUM | Wave 2 seam |
| 12 | Order schema migration | `OrderDbContext` | Order | **ILLEGAL** | Wave 2 seam | no | yes | NO | MEDIUM | Wave 2 seam |
| 13 | Payment schema migration | `PaymentDbContext` | Payment | **ILLEGAL** | Wave 2 seam | no | yes | NO | MEDIUM | Wave 2 seam |
| 14 | Fulfillment schema migration | `FulfillmentDbContext` | Fulfillment | **ILLEGAL** | Wave 2 seam | no | yes | NO | MEDIUM | Wave 2 seam |
| 15 | PlatformProbe / Reviews / ProductQnA / BulkInquiry / Wishlist / AddressBook / CustomerProfile / UserPreference / OperatorProfile / Content / Media / PageComposition / Story / Notification / AccessControl / Support schema migrations (16 contexts) | 16 foreign `DbContext` types | each owning module | **ILLEGAL (bulk)** | Wave 2 seam | no | yes | NO | MEDIUM | Wave 2 seam |
| 16 | Promotion AMAZING campaign type + campaign seed even in schema-only mode | `IMerchandisingCampaignDirectory`, `MerchandisingCampaignDevelopmentSeed` | Promotion | LEGAL (Promotion-owned dev seed invoked by Host) | unchanged | yes | no | invocation only | LOW | stays |
| 17 | **Catalog demo product seed** (root/mid/leaf categories, brand `tooba-live`, attribute `color`+option, product `workspace-live-shirt`, 4 media refs, SEO prep, publish, variant `LIVE-SHIRT-BLK`) | `ICatalogDirectory` (Catalog.Application) + localized name dictionaries | Catalog | **ILLEGAL (Host-owned Catalog business seed data)** | Catalog-owned development seed | `CatalogDemoSeedService` proves the pattern but is a different matrix/seam | no new contract needed (Catalog↔Catalog) | NO | MEDIUM | Wave 1: `Tooba.Catalog.Infrastructure/Development/WorkspaceDemoProductSeed.cs` |
| 18 | Variant ownership | `CreateVariantAsync` via Catalog directory | Catalog | **ILLEGAL (same as 17)** | same seed | no | no | NO | LOW | Wave 1 |
| 19 | Seller-party create/reuse (2 orgs: `فروشگاه آرمان`, `دیجی‌استایل نمونه`) | `IPartyDirectory` (Party.Application) + `PartyDbContext` writes | Party | **ILLEGAL (foreign Application + foreign persistence)** | Party development-seed port | `IPartyDevelopmentSeedGateway` created in ENRICHER-CLOSURE-001 | reuse existing | NO | MEDIUM | Wave 1 via Contracts |
| 20 | Offer create + activate (2 offers `ARM-LN-01`, `DGS-LN-01`) | `ISender` + `Tooba.Offer.Application.Commands.CreateOffer/ActivateOffer` | Offer | **ILLEGAL (Host sends foreign Application commands)** | Offer development-seed port | `IOfferDevelopmentSeedGateway.EnsureActiveSellerOfferAsync` created in ENRICHER-CLOSURE-001 | reuse existing | NO | MEDIUM | Wave 1 via Contracts |
| 21 | Price create + activate (1,850,000 / 1,790,000 IRR) | `IPriceDirectory` (Pricing.Application) | Pricing | **ILLEGAL (foreign Application)** | Pricing development-seed port | `IPricingDevelopmentSeedGateway` created in ENRICHER-CLOSURE-001 | reuse existing | NO | MEDIUM | Wave 1 via Contracts |
| 22 | Tax category + offer-category assignment | `ITaxDirectory` + `Tax.Domain` (`TaxRuleKind`, `TaxOverridePolicy`) | Tax | **ILLEGAL (foreign Application + foreign Domain)** | Tax development-seed port | `ITaxDevelopmentSeedGateway` created in ENRICHER-CLOSURE-001 | reuse existing | NO | MEDIUM | Wave 1 via Contracts |
| 23 | Inventory locations (3) + positions (3) + stock increase + reservation | `IInventoryDirectory` + `Inventory.Domain` (`StockAdjustmentKind`) + `Inventory.Application.Ports` | Inventory | **ILLEGAL (foreign Application + foreign Domain)** | Inventory development-seed port | `IInventoryDevelopmentSeedGateway` created in ENRICHER-CLOSURE-001 | reuse existing | NO | MEDIUM | Wave 1 via Contracts |
| 24 | Existing-product refresh path (`RefreshOperatorFacingCopyAsync`): rewrites Catalog `LocalizedTexts`, rewrites Party `Parties.DisplayName`, then `SaveChangesAsync` on two foreign DbContexts | `CatalogDbContext`, `PartyDbContext` direct write | Catalog (localized copy) + Party (display name) | **ILLEGAL (foreign DbContext write + cross-module mutation in one unit)** | Catalog seed (copy refresh) + Party dev-seed port (display name) | reuse gateways | no | NO | MEDIUM (data-rewrite semantics must be preserved exactly) | Wave 1 |
| 25 | Admin R3 preview seed: gallery enrichment (5 media refs), draft `admin-r3-draft-scarf`, archived `admin-r3-archived-hat`, category copy, SEO, publish+archive | `ICatalogDirectory` + `CatalogDbContext` reads | Catalog | **ILLEGAL (Host-owned Catalog business seed + foreign persistence read)** | same Catalog seed | no | no | NO | MEDIUM | Wave 1 |
| 26 | Seller/Admin development actor bootstrap | `SellerDevActorBootstrap` (`Host/Seller`), `AdminDevActorBootstrap` (`Host/Admin/Development`) | HOST development runtime seam (accepted) | LEGAL (already classified `ALLOWED_DEVELOPMENT_RUNTIME_SEAM`) | unchanged | yes | no | YES (invocation) | LOW | stays |
| 27 | Reviews seed | `ReviewsDevelopmentSeed.ApplyAsync` (Reviews.Infrastructure) | Reviews | LEGAL (module-owned seed) | unchanged | yes | no | invocation only | LOW | stays |
| 28 | Wishlist seed | `WishlistDevelopmentSeed.ApplyAsync` — **class physically lives in `Host/Wishlist/`** and itself reads `CatalogDbContext` + writes `WishlistDbContext` | Wishlist (+ Catalog read) | **ILLEGAL (Host-owned dev seed with foreign persistence)** — but file is in `Host/Wishlist`, **outside this bounded task's Development folder** | future module-owned seed | no | later bounded task | NO | MEDIUM | `OPEN_FOR_FUTURE_BOUNDED_TASK` (separate debt, not this file) |
| 29 | AddressBook seed | `AddressBookDevelopmentSeed.ApplyAsync` (AddressBook.Infrastructure) | AddressBook | LEGAL | unchanged | yes | no | invocation only | LOW | stays |
| 30 | CustomerProfile seed | `CustomerProfileDevelopmentSeed.ApplyAsync` | CustomerProfile | LEGAL | unchanged | yes | no | invocation only | LOW | stays |
| 31 | Settings seed | `SettingsFoundationDevelopmentSeed.ApplyAsync` — **class in `Host/Settings/`**, reads `PartyDbContext`, writes UserPreference/OperatorProfile via ports | Party/UserPreference/OperatorProfile (+Host) | **ILLEGAL (Host-owned dev seed with foreign persistence)** — outside this folder | future module-owned seeds | no | later bounded task | NO | MEDIUM | `OPEN_FOR_FUTURE_BOUNDED_TASK` (separate debt) |
| 32 | Content seed | `ContentDevelopmentSeed.ApplyAsync` | Content | LEGAL | unchanged | yes | no | invocation only | LOW | stays |
| 33 | PageComposition seed | `PageCompositionDevelopmentSeed.ApplyAsync` | PageComposition | LEGAL | unchanged | yes | no | invocation only | LOW | stays |
| 34 | Story seed | `StoryDevelopmentSeed.ApplyAsync` | Story | LEGAL | unchanged | yes | no | invocation only | LOW | stays |
| 35 | LandingPage seed | `LandingPageDevelopmentSeed.ApplyAsync` | Catalog | LEGAL (Catalog-owned seed invoked by Host) | unchanged | yes | no | invocation only | LOW | stays |
| 36 | StoreMenu seed | `StoreMenuDevelopmentSeed.ApplyAsync` | Catalog | LEGAL | unchanged | yes | no | invocation only | LOW | stays |
| 37 | Helper `MigrateAsync(DbContext)` | generic | platform | **ILLEGAL as written** (typed `DbContext` = foreign persistence handle) | replaced by Wave 2 seam | no | yes | NO | LOW | Wave 2 |

No coverage gaps: every type member, both public entry points (`MigrateSchemaOnlyAsync`, `ApplyAsync`),
`ApplyCoreAsync`, and all four private helpers (`MigrateAsync`, `EnsureAdminR3PreviewSeedAsync`,
`RefreshOperatorFacingCopyAsync`, and the inline seed branch) are represented above.

## Ownership-State

`MUST_SPLIT`

## Oversized/God-File-State

| File | LOC | Classification |
| --- | --- | --- |
| `Development/ProductWorkspaceDevelopmentBootstrap.cs` | 428 | `MULTI_RESPONSIBILITY_COHESION_VIOLATION` (>2 unrelated ownership domains; `MUST_SPLIT`) |
| `Host/Wishlist/WishlistDevelopmentSeed.cs` | ~32 | `MULTI_RESPONSIBILITY_COHESION_VIOLATION` (Host-wide debt, out of bounded scope) |
| `Host/Settings/SettingsFoundationDevelopmentSeed.cs` | ~126 | `MULTI_RESPONSIBILITY_COHESION_VIOLATION` (Host-wide debt, out of bounded scope) |

## Foundation-State (per target destination)

| Destination | State |
| --- | --- |
| `Tooba.Catalog.Infrastructure/Development/` | `FOUNDATION_READY` (Catalog is structure-certified; `Development/` already holds `CatalogDemo/*`, `LandingPageDevelopmentSeed.cs`, `StoreMenuDevelopmentSeed.cs`, `CatalogAttributeSchemaSellableEnricher.cs`) |
| `Tooba.Party.Contracts` | `FOUNDATION_READY` (`IPartyDevelopmentSeedGateway` present) |
| `Tooba.Offer.Contracts` | `FOUNDATION_READY` (`IOfferDevelopmentSeedGateway` with `EnsureActiveAsync` + `EnsureActiveSellerOfferAsync`) |
| `Tooba.Pricing.Contracts` | `FOUNDATION_READY` (`IPricingDevelopmentSeedGateway`) |
| `Tooba.Inventory.Contracts` | `FOUNDATION_READY` (`IInventoryDevelopmentSeedGateway`) |
| `Tooba.Tax.Contracts` | `FOUNDATION_READY` (`ITaxDevelopmentSeedGateway`) |
| BuildingBlocks (platform-neutral schema-migrator seam for Wave 2) | `FOUNDATION_PARTIAL` — needs one new neutral port; module implementations are minimal adapters |

## Behavior-Preservation-Risk

`MEDIUM` — no HTTP route, DTO, schema, or frontend surface is involved, but the seed is
**data-creating and data-rewriting** (idempotency guards by slug, localized-copy rewrites on two
DbContexts, publish/archive lifecycle, reservation hold `workspace-live-hold`) and is observed by
live verification scripts (`scripts/capture-t014-evidence.mjs`) and by `CatalogDemoSeam`
junk-slug/publish-count expectations. Seed values, ordering, idempotency keys and the two
`RunLegacyBootstraps` modes must be preserved bit-for-bit.
