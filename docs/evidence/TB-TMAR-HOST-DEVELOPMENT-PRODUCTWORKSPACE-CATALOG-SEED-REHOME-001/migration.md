# Migration — TB-TMAR-HOST-DEVELOPMENT-PRODUCTWORKSPACE-CATALOG-SEED-REHOME-001

## Files created

| Path | Purpose |
| --- | --- |
| `src/backend/Modules/Catalog/Tooba.Catalog.Application/Development/IWorkspaceDemoSeed.cs` | Catalog-owned Development entry abstraction (composition seam only) |
| `src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Development/WorkspaceDemoSeed.cs` | Catalog-owned Development orchestration for the demo dataset |
| `src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Development/WorkspaceDemoProductSeed.cs` | Catalog-owned product/category/brand/attribute/variant/media/SEO/publish seed, copy refresh, Admin R3 preview |
| `src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/Development/WorkspaceDemoMarketplaceSeed.cs` | Catalog-owned two-seller marketplace demo orchestration (Contracts-only to Party/Offer/Pricing/Tax/Inventory) |

## Files modified

| Path | Change |
| --- | --- |
| `src/backend/Host/Tooba.Host/Development/ProductWorkspaceDevelopmentBootstrap.cs` | Business seed authority removed; reduced to schema-migration + module-owned seed **ordering** + `IWorkspaceDemoSeed` invocation |
| `src/backend/Host/Tooba.Host/Program.cs` | `RunLegacyBootstraps=true` path back to `ApplyAsync` (bootstrap retains migration ordering); schema-only path unchanged |
| `src/backend/Modules/Catalog/Tooba.Catalog.Infrastructure/CatalogModule.cs` | Registered `WorkspaceDemoProductSeed`, `WorkspaceDemoMarketplaceSeed`, `IWorkspaceDemoSeed` |
| `src/backend/Modules/Party/Tooba.Party.Contracts/IPartyDevelopmentSeedGateway.cs` | Added `EnsureDevelopmentOrganizationAsync`, `EnsureDevelopmentOrganizationDisplayNamesAsync`, `DevelopmentOrganizationRename` |
| `src/backend/Modules/Party/Tooba.Party.Infrastructure/PartyDevelopmentSeedGateway.cs` | Implemented the two narrow additions |
| `src/backend/Modules/Tax/Tooba.Tax.Contracts/Ports/ITaxDevelopmentSeedGateway.cs` | Added `EnsureDevelopmentRuleAsync` + `DevelopmentTaxRuleKind`/`DevelopmentTaxOverridePolicy` boundary enums |
| `src/backend/Modules/Tax/Tooba.Tax.Infrastructure/Adapters/TaxDevelopmentSeedGateway.cs` | Implemented `EnsureDevelopmentRuleAsync` and mapped boundary enums to Tax domain enums internally |
| `src/backend/Modules/Inventory/Tooba.Inventory.Contracts/Availability/IInventoryDevelopmentSeedGateway.cs` | Added `ReserveDevelopmentHoldAsync` + `SeedDevelopmentStockHold` |
| `src/backend/Modules/Inventory/Tooba.Inventory.Infrastructure/Adapters/InventoryDevelopmentSeedGateway.cs` | Implemented `ReserveDevelopmentHoldAsync` |
| `src/backend/Host/Tooba.Host.Tests/Architecture/HostDevelopmentAmcGuardTests.cs` | Updated `ProductWorkspaceDevelopmentBootstrap.cs` classification to migration-only Wave 2 debt |
| `src/backend/Host/Tooba.Host.Tests/Architecture/HostDevelopmentEnricherClosureGuardTests.cs` | Added Catalog Workspace-seed ownership + Contracts-only + zero-Host-business-authority guards |

## Old → new mapping

| Old (Host) | New (owner) |
| --- | --- |
| `ProductWorkspaceDevelopmentBootstrap.SeedSlug` | `WorkspaceDemoProductSeed.SeedSlug` (Catalog) |
| `ProductWorkspaceDevelopmentBootstrap.EnsureAdminR3PreviewSeedAsync` | `WorkspaceDemoProductSeed.EnsureAdminR3PreviewAsync` (Catalog) |
| `ProductWorkspaceDevelopmentBootstrap.RefreshOperatorFacingCopyAsync` (Catalog half) | `WorkspaceDemoProductSeed.RefreshOperatorFacingCopyAsync` (Catalog) |
| `ProductWorkspaceDevelopmentBootstrap.RefreshOperatorFacingCopyAsync` (Party half) | `PartyDevelopmentSeedGateway.EnsureDevelopmentOrganizationDisplayNamesAsync` (Party) |
| Host product/category/brand/attribute/variant/media/SEO/publish block | `WorkspaceDemoProductSeed.SeedProductAsync` (Catalog) |
| Host two-seller + offer + price + tax + inventory block | `WorkspaceDemoMarketplaceSeed.EnsureMarketplaceAsync` (Catalog orchestration; module-owned gateways) |
| `MediatR.ISender` + `CreateOfferCommand`/`ActivateOfferCommand` | `IOfferDevelopmentSeedGateway.EnsureActiveSellerOfferAsync` (Offer Contracts) |
| `IPriceDirectory.CreatePriceAsync` + `ActivateAsync` | `IPricingDevelopmentSeedGateway.EnsureDevelopmentBasePriceAsync` (Pricing Contracts) |
| `IPartyDirectory.CreateOrganizationAsync` | `IPartyDevelopmentSeedGateway.EnsureDevelopmentOrganizationAsync` (Party Contracts) |
| `ITaxDirectory.CreateRuleAsync` + `ActivateRuleAsync` | `ITaxDevelopmentSeedGateway.EnsureDevelopmentRuleAsync` (Tax Contracts) |
| `IInventoryDirectory.CreateLocationAsync`/`OpenPositionAsync`/`AdjustAsync`/`ReserveAsync` | `IInventoryDevelopmentSeedGateway.EnsureDevelopmentLocationAsync`/`IncreaseDevelopmentStockAsync`/`ReserveDevelopmentHoldAsync` (Inventory Contracts) |
| Host `CatalogDbContext.LocalizedTexts` mutation | `WorkspaceDemoProductSeed` own Catalog persistence |
| Host `PartyDbContext.Parties` mutation | Party-owned gateway |

## Namespace changes

All new files use exact path↔namespace alignment:

- `Tooba.Catalog.Application.Development`
- `Tooba.Catalog.Infrastructure.Development`
- `Tooba.Party.Contracts`
- `Tooba.Inventory.Contracts.Availability`
- `Tooba.Tax.Contracts`

## Persistence / schema

No EF migration. No DDL/schema change. No route change. No endpoint change. No frontend change.
