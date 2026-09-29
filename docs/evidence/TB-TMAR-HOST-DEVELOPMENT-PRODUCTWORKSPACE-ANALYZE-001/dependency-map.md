# TB-TMAR-HOST-DEVELOPMENT-PRODUCTWORKSPACE-ANALYZE-001 — Dependency Map

All direct references of `ProductWorkspaceDevelopmentBootstrap.cs`, classified.
Source of truth: the file's 64 `using` directives (lines 1–65) plus every call site.

## A. Direct foreign `.Application` references (ILLEGAL today)

| # | Reference | True owner | Exact replacement | Reusable today? |
| --- | --- | --- | --- | --- |
| A1 | `Tooba.Catalog.Application` (`ICatalogDirectory`, `ProductPublishPrep`) | Catalog | none needed — Catalog seed may consume its own Application | n/a (intra-module) |
| A2 | `Tooba.Inventory.Application.Ports` (`IInventoryDirectory`) | Inventory | `Tooba.Inventory.Contracts.Availability.IInventoryDevelopmentSeedGateway` | YES (created in ENRICHER-CLOSURE-001) |
| A3 | `Tooba.Inventory.Application.Checkout` | Inventory | drop (no dev-seed need) | YES (drop) |
| A4 | `Tooba.Inventory.Application.Orders` | Inventory | drop (no dev-seed need) | YES (drop) |
| A5 | `Tooba.Offer.Application.Ports` | Offer | `IOfferDevelopmentSeedGateway` (Offer.Contracts) | YES |
| A6 | `Tooba.Party.Application` (`IPartyDirectory`) | Party | `IPartyDevelopmentSeedGateway` (Party.Contracts) | YES |
| A7 | `Tooba.Pricing.Application` (`IPriceDirectory`) | Pricing | `IPricingDevelopmentSeedGateway` (Pricing.Contracts) | YES |
| A8 | `Tooba.Promotion.Application.Ports` / `.Checkout` / `.Merchandising` | Promotion | Promotion-owned dev seed entry (already exists) + existing Contracts only | PARTIAL (Promotion seed yes; directory port not needed) |
| A9 | `Tooba.Tax.Application` (`ITaxDirectory`) | Tax | `ITaxDevelopmentSeedGateway` (Tax.Contracts) | YES |
| A10 | `Tooba.Order.Application.Storefront.Services` (reached indirectly via `Host/Wishlist` + `Host/Settings` seeds) | Order | keep as read-only constant usage until those Host files are rehomed (separate bounded debt) | LATER |

## B. Direct foreign `.Infrastructure` references (ILLEGAL today)

| # | Reference | True owner | Exact replacement |
| --- | --- | --- | --- |
| B1 | `Tooba.Cart.Infrastructure.Persistence` (`CartDbContext`) | Cart | platform-neutral schema-migrator seam (Wave 2) |
| B2 | `Tooba.Catalog.Infrastructure.Development` (`LandingPageDevelopmentSeed`, `StoreMenuDevelopmentSeed`, `CatalogDemoSeam`) | Catalog | legitimate once the seed itself is Catalog-owned |
| B3 | `Tooba.Catalog.Infrastructure.Persistence` (`CatalogDbContext`) | Catalog | legitimate once Catalog-owned (NoCrossModuleJoin) |
| B4 | `Tooba.Identity.Infrastructure.Persistence` | Identity | Wave 2 seam |
| B5 | `Tooba.Offer.Infrastructure.*` (via `IOfferSchemaMigrator` registration) | Offer | already resolved through Contracts port |
| B6 | `Tooba.Order.Infrastructure.Persistence` | Order | Wave 2 seam |
| B7 | `Tooba.Party.Infrastructure.Persistence` (`PartyDbContext`) | Party | Wave 2 seam + Party dev-seed port for the copy refresh |
| B8 | `Tooba.Payment.Infrastructure.Persistence` | Payment | Wave 2 seam |
| B9 | `Tooba.Fulfillment.Infrastructure.Persistence` | Fulfillment | Wave 2 seam |
| B10 | `Tooba.PlatformProbe.Infrastructure.Persistence` | PlatformProbe | Wave 2 seam |
| B11 | `Tooba.Promotion.Infrastructure.Development` (`MerchandisingCampaignDevelopmentSeed`) | Promotion | legitimate module-owned dev seed invocation |
| B12 | `Tooba.Reviews.Infrastructure.Persistence` + `.Infrastructure` | Reviews | Wave 2 seam (Persistence) |
| B13 | `Tooba.ProductQnA.Infrastructure.Persistence` | ProductQnA | Wave 2 seam |
| B14 | `Tooba.BulkInquiry.Infrastructure.Persistence` | BulkInquiry | Wave 2 seam |
| B15 | `Tooba.Wishlist.Infrastructure.Persistence` | Wishlist | Wave 2 seam |
| B16 | `Tooba.AddressBook.Infrastructure.Adapters` + `.Persistence` | AddressBook | seed invocation legal; Persistence → Wave 2 seam |
| B17 | `Tooba.CustomerProfile.Infrastructure.Development` + `.Persistence` | CustomerProfile | seed legal; Persistence → Wave 2 seam |
| B18 | `Tooba.UserPreference.Infrastructure.Persistence` | UserPreference | Wave 2 seam |
| B19 | `Tooba.OperatorProfile.Infrastructure.Persistence` | OperatorProfile | Wave 2 seam |
| B20 | `Tooba.Content.Infrastructure*` + `Media.Infrastructure.Persistence` | Content/Media | seed legal; Media Persistence → Wave 2 seam |
| B21 | `Tooba.PageComposition.Infrastructure*` | PageComposition | seed legal; Persistence → Wave 2 seam |
| B22 | `Tooba.Story.Infrastructure*` | Story | seed legal; Persistence → Wave 2 seam |
| B23 | `Tooba.Notification.Infrastructure.Persistence` | Notification | Wave 2 seam |
| B24 | `Tooba.AccessControl.Infrastructure.Persistence` | AccessControl | Wave 2 seam |
| B25 | `Tooba.Support.Infrastructure.Persistence` (fully qualified, not imported) | Support | Wave 2 seam |

## C. Direct foreign `.Domain` references (ILLEGAL today)

| # | Reference | True owner | Exact replacement | Reusable today? |
| --- | --- | --- | --- | --- |
| C1 | `Tooba.Inventory.Domain.Aggregates` / `.ValueObjects` / `.Events` (`StockAdjustmentKind`, reservation types) | Inventory | `IInventoryDevelopmentSeedGateway.IncreaseDevelopmentStockAsync` + `SeedDevelopmentStock` Contracts DTO | YES |
| C2 | `Tooba.Tax.Domain` (`TaxRuleKind`, `TaxOverridePolicy`) | Tax | `ITaxDevelopmentSeedGateway.EnsureDevelopmentOfferCategoryAsync` (+ Contracts rule DTO) | YES |
| C3 | `Tooba.Catalog.Domain` (`CatalogProductKind`, `CatalogAttributeValueKind`, `CatalogLocalizedOwnerKind`, `CatalogPublicationStatus`) | Catalog | legitimate once Catalog-owned | n/a (intra-module) |

## D. Direct foreign `DbContext` / `DbSet` access (ILLEGAL today)

| # | Context | Accessed as | Member accessed | True owner | Replacement |
| --- | --- | --- | --- | --- | --- |
| D1 | `CatalogDbContext` | `GetRequiredService` + `MigrateAsync` + `Products.AnyAsync` + `MediaReferences.AnyAsync` + `ProductCategories` + `LocalizedTexts` iteration + `SaveChangesAsync` | Catalog | Catalog-owned seed + Wave 2 seam |
| D2 | `PartyDbContext` | `MigrateAsync` + `Parties` iteration + `SaveChangesAsync` | Party | Wave 2 seam + Party dev-seed port |
| D3–D18 | 16 further contexts (see B1–B25 minus the promotions/Catalog/Party ones) | `MigrateAsync` only | each owner | Wave 2 seam |

No cross-module **EF/SQL join** exists in this file: every query is single-context.
`RefreshOperatorFacingCopyAsync` mutates two contexts but does **not** join them — it writes
Catalog localized text and Party display names independently, then saves both.

## E. MediatR requests owned by another module (ILLEGAL today)

| # | Request | True owner | Exact replacement |
| --- | --- | --- | --- |
| E1 | `Tooba.Offer.Application.Commands.CreateOffer.CreateOfferCommand` | Offer | `IOfferDevelopmentSeedGateway.EnsureActiveSellerOfferAsync` |
| E2 | `Tooba.Offer.Application.Commands.ActivateOffer.ActivateOfferCommand` | Offer | same port (activation folded in) |

`ISender` itself is resolved as a service (`mediatR.ISender`), i.e. Host uses the MediatR pipeline to
dispatch foreign module commands. After Wave 1 the seed must not need `ISender` at all.

## F. Module-specific directories/ports not in Contracts (ILLEGAL today)

`ICatalogDirectory`, `IPartyDirectory`, `IPriceDirectory`, `IInventoryDirectory`, `ITaxDirectory`,
`IMerchandisingCampaignDirectory` all live in `*.Application`. Of these, `ICatalogDirectory` becomes
legal (same module); the other five are replaced by existing `*.Contracts` development-seed gateways
(four already exist; Promotion needs no directory because its own seed is the entry point).

## G. Legitimate Host composition (KEEP)

| # | Dependency | Classification |
| --- | --- | --- |
| G1 | `ControlPlaneRegistry`, `ICommerceContextAssigner`, `CommerceContext`, `EditionContext`, `TenantContext`, `ToobaEdition`, `TenantStatus` (BuildingBlocks/Host) | `HOST_COMPOSITION_ROOT` |
| G2 | `IHostEnvironment` (via passed provider) | platform |
| G3 | `SellerDevActorBootstrap`, `AdminDevActorBootstrap` (Host dev runtime seams, already classified `ALLOWED_DEVELOPMENT_RUNTIME_SEAM`) | Host dev seam invocation |
| G4 | 5 `I*SchemaMigrator` Contracts ports (Offer/Pricing/Inventory/Tax/Promotion) | `LEGAL_CONTRACTS_ONLY` |
| G5 | 6 module-owned `*DevelopmentSeed.ApplyAsync` entry points (Reviews, AddressBook, CustomerProfile, Content, PageComposition, Story, LandingPage, StoreMenu, MerchandisingCampaign) | module-owned seeds invoked by Host composition |

## H. Stale / redundant / drain-only classification

| # | Item | Verdict |
| --- | --- | --- |
| H1 | Duplicate `MerchandisingCampaignDevelopmentSeed.EnsureAsync` (called once in the migration phase for LOCK-SF-402 and once at the end of both seed branches) | `NOT_STALE` — intentional so Development AMAZING campaigns exist in schema-only mode too; **must be preserved** |
| H2 | `MigrateSchemaOnlyAsync` distinct entry point | `NOT_STALE` — required by `RunLegacyBootstraps=false`; preserve as an explicit mode |
| H3 | Unused imports (e.g. `Tooba.Inventory.Application.Checkout`, `Tooba.Inventory.Application.Orders`, `Tooba.Inventory.Contracts.Seller`, `Tooba.Offer.Contracts.Dtos`, several `*.Contracts.Errors`) | `DRAIN_ONLY` — removed automatically by the move; no behavior impact |
| H4 | `Host/Wishlist/WishlistDevelopmentSeed.cs` and `Host/Settings/SettingsFoundationDevelopmentSeed.cs` | `LEGACY_PATTERN_DRAIN_TARGETS` — they are **outside** the active folder; this task must **not** move them, only stop depending on them from the product-workspace seed |

## Cross-Module-Coupling-State

`ILLEGAL`

## Cross-Module-Join-State

`NONE`

## Persistence-Ownership-State

`FOREIGN_ACCESS` (18 foreign contexts orchestrated/mutated from one Host file)

## Endpoint-Ownership-State

`MODULE_OWNED` — no HTTP endpoint, route, CQRS ceremony, or validator is involved in this file
(`N/A` for endpoint ownership; recorded for completeness).

## Localization-State

`HARDCODED_TEXT` (development-only seed literals: `"پیراهن مردانه لینن"`, `"فروشگاه آرمان"`,
`"انبار مرکزی تهران"`, …). These are **development seed values, not user-facing API messages** —
the canonical-localization gate does not apply to seed data, but the values must be preserved exactly.

## API-Result-Pattern-State

`N/A` (no endpoints).

## Stable-Error-Code-State

`N/A` — seed failures throw `InvalidOperationException(firstError.Code)`; no new transport code is introduced or required.

## Logging-State

`CANONICAL` (no logging is performed at all in this file; callers in `Program.cs` use `ILogger`).

## Sensitive-Logging-State

`NONE`

## OpenTelemetry-State

`N/A` — only `CommerceContext.TraceId` labels are set (`workspace-dev-seed`, `workspace-dev-schema`); these labels must be preserved.

## Correlation-Trace-State

`CANONICAL` (uses the existing `ICommerceContextAssigner` seam).

## CQRS-State

`N/A` for this file, **but** it dispatches two foreign MediatR commands (`CreateOffer`, `ActivateOffer`) — classified as illegal cross-module command ownership, replaced by the Offer development-seed port.

## Validator-Coverage-State

`N/A` (no endpoint-reachable request).

## Schema-Migration-State

`DRIFT_RISK` — the file owns a 28-context migration order that must stay aligned with
`Host/Tooba.MigrationRunner/ModuleMigrationRegistry.cs`. Any Wave 2 migration work must prove the
Development order still matches the registry order (Catalog, Offer, Pricing, Inventory, Tax, Party,
Identity, Cart, Order, Payment, Fulfillment, …, Promotion, PlatformProbe, Reviews, …).
