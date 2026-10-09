# TB-TMAR-TAX-AMSC-001-W2 — physical tree

## Before (`fa87201a`, W1 Migrate)

```text
src/backend/Modules/Tax/
  Tooba.Tax.Contracts/                        namespace Tooba.Tax.Contracts
    Dtos/TaxOutcome.cs
    Errors/TaxErrorCatalogContributor.cs
    Errors/TaxErrorCodes.cs
    Errors/TaxErrorResourceSet.cs
    Ports/ITaxDevelopmentSeedGateway.cs
    Ports/ITaxSchemaMigrator.cs
    Ports/TaxCalculatorContracts.cs
    Ports/TaxQueryContracts.cs
    Resources/TaxErrors.resx
    Resources/TaxErrors.fa.resx
  Tooba.Tax.Domain/                           namespace Tooba.Tax.Domain
    Aggregates/{TaxRule,TaxCategory,TaxOfferClassification}.cs
    Enums/TaxRuleEnums.cs
    Events/TaxDomainEvents.cs
    Policies/TaxRounding.cs
  Tooba.Tax.Application/                      namespace Tooba.Tax.Application
    Composition/TaxOperation.cs
    Ports/ITaxDirectory.cs                    (bundles ITaxUseCaseGuard)
  Tooba.Tax.Infrastructure/                   namespace Tooba.Tax.Infrastructure
    Adapters/{TaxDirectory,TaxDevelopmentSeedGateway,TaxModuleMigration,TaxSchemaMigrator}.cs
    DependencyInjection/TaxModule.cs
    Events/TaxEvents.cs
    Outbox/TaxOutboxRegistration.cs
    Persistence/TaxDbContext.cs
    Persistence/Configurations/{TaxCategory,TaxOfferClassification,TaxRule}Configuration.cs
    Persistence/Migrations/20260823190000_InitialTax(.Designer).cs
    Persistence/Migrations/TaxDbContextModelSnapshot.cs
  Tooba.Tax.Endpoints/                        CEREMONIAL
    TaxEndpointModule.cs                      (empty MapGroup("/v1/tax"))
  Tooba.Tax.Tests/
    Architecture/TaxArchitectureGuardTests.cs
    Contracts/TaxCalculationShapeTests.cs
    Domain/TaxRuleInvariantTests.cs
    Endpoints/TaxEndpointModuleTests.cs       (tested the ceremony)
    Infrastructure/TaxDbContextOwnershipTests.cs
```

Solution (`src/backend/Tooba.slnx`): `/Modules/Tax/` with **6** projects (Endpoints included).

## After (this commit)

```text
src/backend/Modules/Tax/
  Tooba.Tax.Contracts/                        namespaces Tooba.Tax.Contracts.{Dtos,Errors,Ports}
    Dtos/TaxOutcome.cs                        -> Tooba.Tax.Contracts.Dtos
    Errors/TaxErrorCatalogContributor.cs      -> Tooba.Tax.Contracts.Errors
    Errors/TaxErrorCodes.cs                   -> Tooba.Tax.Contracts.Errors
    Errors/TaxErrorResourceSet.cs             -> Tooba.Tax.Contracts.Errors
    Ports/ITaxDevelopmentSeedGateway.cs       -> Tooba.Tax.Contracts.Ports
    Ports/ITaxSchemaMigrator.cs               -> Tooba.Tax.Contracts.Ports
    Ports/TaxCalculatorContracts.cs           -> Tooba.Tax.Contracts.Ports
    Ports/TaxQueryContracts.cs                -> Tooba.Tax.Contracts.Ports
    Resources/TaxErrors.resx
    Resources/TaxErrors.fa.resx
  Tooba.Tax.Domain/                           namespaces Tooba.Tax.Domain.{Aggregates,Enums,Events,Policies}
    GlobalUsings.cs                           (NEW — namespace bridge, no namespace, no type)
    Aggregates/{TaxRule,TaxCategory,TaxOfferClassification}.cs
    Enums/TaxRuleEnums.cs
    Events/TaxDomainEvents.cs
    Policies/TaxRounding.cs
  Tooba.Tax.Application/                      namespaces Tooba.Tax.Application.{Composition,Ports}
    Composition/TaxOperation.cs
    Ports/ITaxDirectory.cs
    Ports/ITaxUseCaseGuard.cs                 (NEW — split out for Inventory/Pricing parity)
  Tooba.Tax.Infrastructure/                   namespaces Tooba.Tax.Infrastructure.{Adapters,DependencyInjection,Events,Outbox,Persistence}
    Adapters/{TaxDirectory,TaxDevelopmentSeedGateway,TaxModuleMigration,TaxSchemaMigrator}.cs
    DependencyInjection/TaxModule.cs
    Events/TaxEvents.cs
    Outbox/TaxOutboxRegistration.cs
    Persistence/TaxDbContext.cs
    Persistence/Configurations/{TaxCategory,TaxOfferClassification,TaxRule}Configuration.cs
    Persistence/Migrations/20260823190000_InitialTax(.Designer).cs
    Persistence/Migrations/TaxDbContextModelSnapshot.cs
  Tooba.Tax.Tests/
    Architecture/TaxArchitectureGuardTests.cs
    Contracts/TaxCalculationShapeTests.cs
    Domain/TaxRuleInvariantTests.cs
    Infrastructure/TaxDbContextOwnershipTests.cs
```

Solution (`src/backend/Tooba.slnx`): `/Modules/Tax/` with **5** projects.

## Deleted

```text
src/backend/Modules/Tax/Tooba.Tax.Endpoints/TaxEndpointModule.cs
src/backend/Modules/Tax/Tooba.Tax.Endpoints/Tooba.Tax.Endpoints.csproj
src/backend/Modules/Tax/Tooba.Tax.Tests/Endpoints/TaxEndpointModuleTests.cs
```

`Physical-Copy-State = CLEAN` — no leftover path carries a duplicate of any moved type, and no
solution entry points at a deleted path.
