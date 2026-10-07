# TB-TMAR-PRICING-AMSC-001-W2 — physical tree AFTER (tooba-architecture-structure)

- Module: `Pricing`
- Starting HEAD: `069f77d2` (W1 Migrate, `HEAD == origin/main`)
- Task: `TB-TMAR-PRICING-AMSC-001-W2` (Structure)
- Produced by: disk enumeration of `src/backend/Modules/Pricing` with `obj/`, `bin/` excluded

## 1. Repairs applied

| # | Repair | Mechanics |
| --- | --- | --- |
| R1 | `Contracts` root dump retired | `git mv Tooba.Pricing.Contracts/SellerOfferPricingContracts.cs → Tooba.Pricing.Contracts/Seller/SellerOfferPricingContracts.cs`; namespace → `Tooba.Pricing.Contracts.Seller` |
| R2 | `Contracts/Dtos` path↔namespace exact | `CurrencyCode.cs` → `namespace Tooba.Pricing.Contracts.Dtos;` |
| R3 | `Contracts/Ports` path↔namespace exact (6 files) | `namespace Tooba.Pricing.Contracts.Ports;` |
| R4 | `Domain` path↔namespace exact (7 files) | `namespace Tooba.Pricing.Domain.{Aggregates,Enums,Events,ValueObjects};` |
| R5 | `Domain` namespace bridge | new root `GlobalUsings.cs` (no namespace declaration, no type) — the certified Catalog/Order Domain precedent |
| R6 | `Infrastructure` path↔namespace exact (3 files) | `namespace Tooba.Pricing.Infrastructure.{Adapters,DependencyInjection,Outbox};` |
| R7 | Localization surface re-homed to the code owner | `git mv Tooba.Pricing.Endpoints/Errors/PricingErrorCatalogContributor.cs → Tooba.Pricing.Contracts/Errors/`; `git mv Tooba.Pricing.Endpoints/Resources/PricingErrorResources.cs → Tooba.Pricing.Contracts/Errors/PricingErrorResourceSet.cs`; `git mv Tooba.Pricing.Endpoints/Resources/PricingErrors{,.fa}.resx → Tooba.Pricing.Contracts/Resources/`; `Endpoints/Errors/` + `Endpoints/Resources/` deleted from disk |
| R8 | Single authoritative localization home enforced | `PricingErrorResourceSet` owns the `pricing.` keyspace and resolves through the Contracts-assembly resource manager `Tooba.Pricing.Contracts.Resources.PricingErrors`; `PricingErrorResources` (the resource-manager marker) lives in the same Contracts `Errors/` file |

Every move was performed with `git mv` so history is preserved. No route, DTO shape, descriptor, resource key, stable code value, DI lifetime or schema element changed.

## 2. AFTER tree (disk, generated paths excluded)

```text
src/backend/Modules/Pricing/Tooba.Pricing.Application/Composition/PricingOperation.cs
src/backend/Modules/Pricing/Tooba.Pricing.Application/Ports/IPricingUseCaseGuard.cs
src/backend/Modules/Pricing/Tooba.Pricing.Application/Tooba.Pricing.Application.csproj
src/backend/Modules/Pricing/Tooba.Pricing.Contracts/Dtos/CurrencyCode.cs
src/backend/Modules/Pricing/Tooba.Pricing.Contracts/Errors/PricingErrorCatalogContributor.cs
src/backend/Modules/Pricing/Tooba.Pricing.Contracts/Errors/PricingErrorCodes.cs
src/backend/Modules/Pricing/Tooba.Pricing.Contracts/Errors/PricingErrorResourceSet.cs
src/backend/Modules/Pricing/Tooba.Pricing.Contracts/Ports/CampaignCartPriceAuthority.cs
src/backend/Modules/Pricing/Tooba.Pricing.Contracts/Ports/IPriceDirectory.cs
src/backend/Modules/Pricing/Tooba.Pricing.Contracts/Ports/IPricingDevelopmentSeedGateway.cs
src/backend/Modules/Pricing/Tooba.Pricing.Contracts/Ports/IPricingSchemaMigrator.cs
src/backend/Modules/Pricing/Tooba.Pricing.Contracts/Ports/PriceLookupContracts.cs
src/backend/Modules/Pricing/Tooba.Pricing.Contracts/Ports/PriceQueryContracts.cs
src/backend/Modules/Pricing/Tooba.Pricing.Contracts/Resources/PricingErrors.fa.resx
src/backend/Modules/Pricing/Tooba.Pricing.Contracts/Resources/PricingErrors.resx
src/backend/Modules/Pricing/Tooba.Pricing.Contracts/Seller/SellerOfferPricingContracts.cs
src/backend/Modules/Pricing/Tooba.Pricing.Contracts/Tooba.Pricing.Contracts.csproj
src/backend/Modules/Pricing/Tooba.Pricing.Domain/Aggregates/AuthoredPrice.cs
src/backend/Modules/Pricing/Tooba.Pricing.Domain/Enums/PriceChannel.cs
src/backend/Modules/Pricing/Tooba.Pricing.Domain/Enums/PriceEnums.cs
src/backend/Modules/Pricing/Tooba.Pricing.Domain/Events/PricingDomainEvents.cs
src/backend/Modules/Pricing/Tooba.Pricing.Domain/GlobalUsings.cs
src/backend/Modules/Pricing/Tooba.Pricing.Domain/Tooba.Pricing.Domain.csproj
src/backend/Modules/Pricing/Tooba.Pricing.Domain/ValueObjects/AuthoredCurrency.cs
src/backend/Modules/Pricing/Tooba.Pricing.Domain/ValueObjects/MarketCode.cs
src/backend/Modules/Pricing/Tooba.Pricing.Domain/ValueObjects/Money.cs
src/backend/Modules/Pricing/Tooba.Pricing.Endpoints/PricingEndpointModule.cs
src/backend/Modules/Pricing/Tooba.Pricing.Endpoints/Tooba.Pricing.Endpoints.csproj
src/backend/Modules/Pricing/Tooba.Pricing.Infrastructure/Adapters/PriceDirectory.cs
src/backend/Modules/Pricing/Tooba.Pricing.Infrastructure/Adapters/PricingDevelopmentSeedGateway.cs
src/backend/Modules/Pricing/Tooba.Pricing.Infrastructure/Adapters/PricingModuleMigration.cs
src/backend/Modules/Pricing/Tooba.Pricing.Infrastructure/Adapters/PricingSchemaMigrator.cs
src/backend/Modules/Pricing/Tooba.Pricing.Infrastructure/DependencyInjection/PricingModule.cs
src/backend/Modules/Pricing/Tooba.Pricing.Infrastructure/Events/PricingEvents.cs
src/backend/Modules/Pricing/Tooba.Pricing.Infrastructure/Outbox/PricingOutboxRegistration.cs
src/backend/Modules/Pricing/Tooba.Pricing.Infrastructure/Persistence/Configurations/AuthoredPriceConfiguration.cs
src/backend/Modules/Pricing/Tooba.Pricing.Infrastructure/Persistence/Migrations/20260823085546_InitialPricing.Designer.cs
src/backend/Modules/Pricing/Tooba.Pricing.Infrastructure/Persistence/Migrations/20260823085546_InitialPricing.cs
src/backend/Modules/Pricing/Tooba.Pricing.Infrastructure/Persistence/Migrations/PricingDbContextModelSnapshot.cs
src/backend/Modules/Pricing/Tooba.Pricing.Infrastructure/Persistence/PricingDbContext.cs
src/backend/Modules/Pricing/Tooba.Pricing.Infrastructure/Tooba.Pricing.Infrastructure.csproj
src/backend/Modules/Pricing/Tooba.Pricing.Tests/Architecture/PricingArchitectureGuardTests.cs
src/backend/Modules/Pricing/Tooba.Pricing.Tests/Contracts/PriceQuoteShapeTests.cs
src/backend/Modules/Pricing/Tooba.Pricing.Tests/Domain/AuthoredPriceInvariantTests.cs
src/backend/Modules/Pricing/Tooba.Pricing.Tests/Endpoints/PricingEndpointModuleTests.cs
src/backend/Modules/Pricing/Tooba.Pricing.Tests/Infrastructure/PricingDbContextOwnershipTests.cs
src/backend/Modules/Pricing/Tooba.Pricing.Tests/Observability/PricingErrorCatalogTests.cs
src/backend/Modules/Pricing/Tooba.Pricing.Tests/Tooba.Pricing.Tests.csproj
```

## 3. Per-project folder shape after

| Project | Root `*.cs` | Top-level folders |
| --- | --- | --- |
| `Tooba.Pricing.Contracts` | *(none)* | `Dtos`, `Errors`, `Ports`, `Resources`, `Seller` |
| `Tooba.Pricing.Domain` | `GlobalUsings.cs` (bridge only) | `Aggregates`, `Enums`, `Events`, `ValueObjects` |
| `Tooba.Pricing.Application` | *(none)* | `Composition`, `Ports` |
| `Tooba.Pricing.Infrastructure` | *(none)* | `Adapters`, `DependencyInjection`, `Events`, `Outbox`, `Persistence` |
| `Tooba.Pricing.Endpoints` | `PricingEndpointModule.cs` (composition entry) | *(none)* |

## 4. Classification states (after)

| Axis | Before | After |
| --- | --- | --- |
| Folder-Granularity-State | `PROFESSIONAL_SHALLOW` | `PROFESSIONAL_SHALLOW` |
| Solution-Explorer-State | `CANONICAL` | `CANONICAL` (unchanged; all six projects verified in `/Modules/Pricing/`) |
| Path-Namespace-State | `MISMATCH` | `EXACT` |
| Physical-Copy-State | `CLEAN` | `CLEAN` (one authoritative home for the relocated localization surface; `Endpoints/Errors` + `Endpoints/Resources` deleted) |
| Root-Allowlist-State | `VIOLATION` | `ENFORCED` (manifest entry added, allowlists/forbidden lists matched against disk) |
| File-Cohesion-State | `COHESIVE` | `COHESIVE` (no file grew beyond its single responsibility; no new god-file) |
| Structure-State | `REPAIR_REQUIRED` | `READY_FOR_CERTIFY` |
