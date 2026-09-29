# Analyze — TB-TMAR-HOST-DEVELOPMENT-PRODUCTWORKSPACE-CATALOG-SEED-REHOME-001

## Scope

Wave 1 only of the two-wave closure plan from
`TB-TMAR-HOST-DEVELOPMENT-PRODUCTWORKSPACE-ANALYZE-001`.

Active Host folder: `src/backend/Host/Tooba.Host/Development`
Active target: `ProductWorkspaceDevelopmentBootstrap.cs`

## Skill order applied

1. `.cursor/skills/tooba-architecture-analyze/SKILL.md`
2. `.cursor/skills/tooba-architecture-migrate/SKILL.md`
3. `.cursor/skills/tooba-architecture-certify/SKILL.md`

## Pre-state (HEAD = 648a282f)

| Item | Value |
| --- | --- |
| `ProductWorkspaceDevelopmentBootstrap.cs` LOC | 428 |
| SHA256 | `96F3DEDDEF8B5FD1A003C34CE24AE9E78BA389E2240C507A6D942D351C77DD28` |
| Host/Development production file count | 5 |
| Foreign DbContext types migrated from Host | 28 |
| Foreign DbContexts written from Host | 2 (`CatalogDbContext`, `PartyDbContext`) |
| Foreign MediatR requests from Host | 2 (`CreateOfferCommand`, `ActivateOfferCommand`) |
| Cross-module joins | NONE |

## Responsibility classification (Wave 1 slice)

Responsibilities 17–25 of the accepted responsibility map are closed by this task.

| # | Responsibility | True owner | Disposition |
| --- | --- | --- | --- |
| 17 | Catalog demo product create (workspace-live-shirt) | Catalog | MOVED → `WorkspaceDemoProductSeed.SeedProductAsync` |
| 18 | Category hierarchy + brand + colour attribute/option | Catalog | MOVED → same file |
| 19 | Media reference attach + SEO + publish + variant | Catalog | MOVED → same file |
| 20 | Operator-facing copy refresh (Catalog copy) | Catalog | MOVED → `WorkspaceDemoProductSeed.RefreshOperatorFacingCopyAsync` |
| 21 | Operator-facing copy refresh (Party organization display names) | Party | MOVED → `PartyDevelopmentSeedGateway.EnsureDevelopmentOrganizationDisplayNamesAsync` (Contracts seam) |
| 22 | Two-seller marketplace demo (party + offer + pricing) | Catalog orchestration; Party/Offer/Pricing own data | MOVED → `WorkspaceDemoMarketplaceSeed.EnsureMarketplaceAsync` |
| 23 | Tax category + active rule for demo offers | Tax | MOVED → `TaxDevelopmentSeedGateway.EnsureDevelopmentRuleAsync` (Contracts seam) |
| 24 | Inventory locations + stock + workspace-live-hold reservation | Inventory | MOVED → `InventoryDevelopmentSeedGateway.ReserveDevelopmentHoldAsync` (Contracts seam) |
| 25 | Admin R3 preview enrichment (gallery, draft, archived) | Catalog | MOVED → `WorkspaceDemoProductSeed.EnsureAdminR3PreviewAsync` |

Responsibilities 3 and 9–16 and 37 (the 28-context migration list and schema orchestration) stay in Host
as explicit **Wave 2 debt** and were intentionally not touched.

## Destination readiness

| Destination | Classification | Result |
| --- | --- | --- |
| `Tooba.Catalog.Infrastructure/Development/` | OPEN_FOR_CURRENT_TASK | FOUNDATION_READY (existing module-owned Development folder + full Catalog.Contracts project references to Party/Offer/Pricing/Inventory/Tax Contracts) |
| `Tooba.Catalog.Application/Development/` | OPEN_FOR_CURRENT_TASK | FOUNDATION_READY (existing Development capability folder) |
| `Tooba.Party.Contracts` / `Tooba.Party.Infrastructure` | OPEN_FOR_CURRENT_TASK (narrow additive) | FOUNDATION_READY |
| `Tooba.Tax.Contracts` / `Tooba.Tax.Infrastructure` | OPEN_FOR_CURRENT_TASK (narrow additive) | FOUNDATION_READY |
| `Tooba.Inventory.Contracts` / `Tooba.Inventory.Infrastructure` | OPEN_FOR_CURRENT_TASK (narrow additive) | FOUNDATION_READY |
| `Tooba.Offer.Contracts` / `Tooba.Offer.Infrastructure` | LOCKED_BY_ACCEPTED_DISPOSITION | NOT TOUCHED (existing `EnsureActiveSellerOfferAsync` sufficed) |

No new module or project was created.
