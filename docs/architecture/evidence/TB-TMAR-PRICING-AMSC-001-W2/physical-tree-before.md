# TB-TMAR-PRICING-AMSC-001-W2 — physical tree BEFORE (tooba-architecture-structure)

- Module: `Pricing`
- Starting HEAD: `069f77d2` (W1 Migrate, `HEAD == origin/main`)
- Task: `TB-TMAR-PRICING-AMSC-001-W2` (Structure)
- Produced by: `git ls-tree -r --name-only 069f77d2 -- src/backend/Modules/Pricing` (tracked files only; `obj/`, `bin/` excluded)

## 1. Classified defects visible in the BEFORE tree

| # | Defect | Exact offending paths |
| --- | --- | --- |
| D1 | `Contracts` **root dump** — three boundary files live at the project root instead of capability folders | `Tooba.Pricing.Contracts/SellerOfferPricingContracts.cs` |
| D2 | `PATH_NAMESPACE_ALIGNMENT` violation — the file sits in a capability folder but declares the project-level namespace | `Tooba.Pricing.Contracts/Dtos/CurrencyCode.cs` → `namespace Tooba.Pricing.Contracts;` |
| D3 | `PATH_NAMESPACE_ALIGNMENT` violation — same project-level declaration for the six ports | `Tooba.Pricing.Contracts/Ports/{CampaignCartPriceAuthority,IPriceDirectory,IPricingDevelopmentSeedGateway,IPricingSchemaMigrator,PriceLookupContracts,PriceQueryContracts}.cs` |
| D4 | `PATH_NAMESPACE_ALIGNMENT` violation across the whole Domain (7 files) | `Tooba.Pricing.Domain/{Aggregates/AuthoredPrice,Enums/PriceChannel,Enums/PriceEnums,Events/PricingDomainEvents,ValueObjects/AuthoredCurrency,ValueObjects/MarketCode,ValueObjects/Money}.cs` |
| D5 | `PATH_NAMESPACE_ALIGNMENT` violation in Infrastructure (3 files) | `Tooba.Pricing.Infrastructure/{Adapters/PriceDirectory,DependencyInjection/PricingModule,Outbox/PricingOutboxRegistration}.cs` |
| D6 | Localization surface physically owned by **Endpoints** while the stable-code + resource-set identity is Contracts-owned (split code/text boundary; a microservice extraction would leave the text behind) | `Tooba.Pricing.Endpoints/Errors/PricingErrorCatalogContributor.cs`, `Tooba.Pricing.Endpoints/Resources/{PricingErrorResources.cs,PricingErrors.resx,PricingErrors.fa.resx}` |
| D7 | Domain god-folder split hazard: the capability-first Domain split required a namespace bridge, which did not exist yet | `Tooba.Pricing.Domain` (no `GlobalUsings.cs`) |

Non-defects recorded honestly (unchanged, no W2 obligation):

- `Application` was already `PROFESSIONAL_SHALLOW` (`Composition/PricingOperation.cs`, `Ports/IPricingUseCaseGuard.cs`); Pricing owns zero endpoint-reachable requests so no CQRS/request tree exists and none was invented.
- `Infrastructure` was already on the canonical integration folders (`Adapters/`, `DependencyInjection/`, `Events/`, `Outbox/`, `Persistence/Migrations/`).
- `Endpoints` root already held only the composition entry `PricingEndpointModule.cs`.
- `/Modules/Pricing/` solution grouping already listed all six projects.
- `Tooba.Pricing.Contracts` root already carried no `Errors/` folder; W1 had created `Errors/PricingErrorCodes.cs` (the single canonical home) and the path-derived namespace `Tooba.Pricing.Contracts.Errors` was already exact.

## 2. BEFORE tree (tracked, generated paths excluded)

```text
src/backend/Modules/Pricing/Tooba.Pricing.Application/Composition/PricingOperation.cs
src/backend/Modules/Pricing/Tooba.Pricing.Application/Ports/IPricingUseCaseGuard.cs
src/backend/Modules/Pricing/Tooba.Pricing.Application/Tooba.Pricing.Application.csproj
src/backend/Modules/Pricing/Tooba.Pricing.Contracts/Dtos/CurrencyCode.cs
src/backend/Modules/Pricing/Tooba.Pricing.Contracts/Errors/PricingErrorCodes.cs
src/backend/Modules/Pricing/Tooba.Pricing.Contracts/Ports/CampaignCartPriceAuthority.cs
src/backend/Modules/Pricing/Tooba.Pricing.Contracts/Ports/IPriceDirectory.cs
src/backend/Modules/Pricing/Tooba.Pricing.Contracts/Ports/IPricingDevelopmentSeedGateway.cs
src/backend/Modules/Pricing/Tooba.Pricing.Contracts/Ports/IPricingSchemaMigrator.cs
src/backend/Modules/Pricing/Tooba.Pricing.Contracts/Ports/PriceLookupContracts.cs
src/backend/Modules/Pricing/Tooba.Pricing.Contracts/Ports/PriceQueryContracts.cs
src/backend/Modules/Pricing/Tooba.Pricing.Contracts/SellerOfferPricingContracts.cs
src/backend/Modules/Pricing/Tooba.Pricing.Contracts/Tooba.Pricing.Contracts.csproj
src/backend/Modules/Pricing/Tooba.Pricing.Domain/Aggregates/AuthoredPrice.cs
src/backend/Modules/Pricing/Tooba.Pricing.Domain/Enums/PriceChannel.cs
src/backend/Modules/Pricing/Tooba.Pricing.Domain/Enums/PriceEnums.cs
src/backend/Modules/Pricing/Tooba.Pricing.Domain/Events/PricingDomainEvents.cs
src/backend/Modules/Pricing/Tooba.Pricing.Domain/Tooba.Pricing.Domain.csproj
src/backend/Modules/Pricing/Tooba.Pricing.Domain/ValueObjects/AuthoredCurrency.cs
src/backend/Modules/Pricing/Tooba.Pricing.Domain/ValueObjects/MarketCode.cs
src/backend/Modules/Pricing/Tooba.Pricing.Domain/ValueObjects/Money.cs
src/backend/Modules/Pricing/Tooba.Pricing.Endpoints/Errors/PricingErrorCatalogContributor.cs
src/backend/Modules/Pricing/Tooba.Pricing.Endpoints/PricingEndpointModule.cs
src/backend/Modules/Pricing/Tooba.Pricing.Endpoints/Resources/PricingErrorResources.cs
src/backend/Modules/Pricing/Tooba.Pricing.Endpoints/Resources/PricingErrors.fa.resx
src/backend/Modules/Pricing/Tooba.Pricing.Endpoints/Resources/PricingErrors.resx
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

## 3. Classification states (before)

| Axis | Before |
| --- | --- |
| Folder-Granularity-State | `PROFESSIONAL_SHALLOW` (no over-foldered request leaf; no technical-axis-first request tree — Pricing owns zero requests) |
| Solution-Explorer-State | `CANONICAL` (`/Modules/Pricing/`, 6 projects, matches disk) |
| Path-Namespace-State | `MISMATCH` (D2–D5: 18 production files) |
| Physical-Copy-State | `CLEAN` |
| Root-Allowlist-State | `VIOLATION` (D1: `Contracts` root dump; no manifest entry existed yet) |
| File-Cohesion-State | `COHESIVE` (29 production `.cs`; largest `Adapters/PriceDirectory.cs` 477 LOC single four-port adapter) |
| Structure-State | `REPAIR_REQUIRED` |
