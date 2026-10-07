# TB-TMAR-PRICING-AMSC-001-W3-R2 — project inventory before/after

- Module: `Pricing`
- Before: `2e664bb3` (W3-R1, ceremonial Endpoints retained, 6 solution projects)
- After: W3-R2 working tree (INTERNAL_ONLY, 5 solution projects)

## 1. Solution Explorer (`src/backend/Tooba.slnx`) — `/Modules/Pricing/`

| Project | Before | After |
| --- | --- | --- |
| `Tooba.Pricing.Application` | present | present |
| `Tooba.Pricing.Contracts` | present | present |
| `Tooba.Pricing.Domain` | present | present |
| `Tooba.Pricing.Endpoints` | **present** | **REMOVED** |
| `Tooba.Pricing.Infrastructure` | present | present |
| `Tooba.Pricing.Tests` | present | present |
| **count** | **6** | **5** |

## 2. Physical project directories (`src/backend/Modules/Pricing/`)

```text
BEFORE (6 dirs)                         AFTER (5 dirs)
Tooba.Pricing.Application               Tooba.Pricing.Application
Tooba.Pricing.Contracts                 Tooba.Pricing.Contracts
Tooba.Pricing.Domain                    Tooba.Pricing.Domain
Tooba.Pricing.Endpoints        <-- gone
Tooba.Pricing.Infrastructure            Tooba.Pricing.Infrastructure
Tooba.Pricing.Tests                     Tooba.Pricing.Tests
```

## 3. Files removed

| File | Reason |
| --- | --- |
| `Tooba.Pricing.Endpoints/Tooba.Pricing.Endpoints.csproj` | ceremonial project (no HTTP surface) |
| `Tooba.Pricing.Endpoints/PricingEndpointModule.cs` | `MapPricingModule()` empty `/v1/pricing` group + `AddPricingEndpointPresentation()` |
| `Tooba.Pricing.Tests/Endpoints/PricingEndpointModuleTests.cs` | tested the removed ceremony |

No other production file was deleted or moved. All other Pricing files are byte-identical.

## 4. Production project file inventory (unchanged set)

| Project | Root `.cs` | Capability folders |
| --- | --- | --- |
| `Tooba.Pricing.Contracts` | none | `Dtos/`, `Errors/`, `Ports/`, `Resources/`, `Seller/` |
| `Tooba.Pricing.Domain` | `GlobalUsings.cs` (allowlisted namespace bridge) | `Aggregates/`, `Enums/`, `Events/`, `ValueObjects/` |
| `Tooba.Pricing.Application` | none | `Composition/`, `Ports/` |
| `Tooba.Pricing.Infrastructure` | none | `Adapters/`, `DependencyInjection/`, `Events/`, `Outbox/`, `Persistence/` (`Persistence/Configurations/`, `Persistence/Migrations/`) |

`Tooba.Pricing.Tests` holds `Architecture/`, `Contracts/`, `Domain/`, `Infrastructure/`, `Observability/`;
the retired `Endpoints/` test folder must not resurrect.

## 5. `PricingModule.cs` composition surface (registration move)

```text
BEFORE  Tooba.Pricing.Endpoints/PricingEndpointModule.cs
        AddPricingEndpointPresentation():
          services.AddSingleton<IErrorCatalogContributor, PricingErrorCatalogContributor>();
          services.AddSingleton<IErrorResourceSet, PricingErrorResourceSet>();
        Host: builder.Services.AddPricingEndpointPresentation();

AFTER   Tooba.Pricing.Infrastructure/DependencyInjection/PricingModule.cs
        AddServices(...):
          services.AddSingleton<IErrorCatalogContributor, PricingErrorCatalogContributor>();
          services.AddSingleton<IErrorResourceSet, PricingErrorResourceSet>();
        Host: no call (the IToobaModule composition root already runs)
```

Registration occurrences of each interface: **exactly 1** before and **exactly 1** after. The concrete
types stay Contracts-owned (`Tooba.Pricing.Contracts.Errors`). No DI lifetime, ordering or semantics
change.

## 6. Manifest project inventory (`docs/architecture/tmar-module-structure-manifests.json`)

| Manifest state | Before | After |
| --- | --- | --- |
| Array | `modules[]` (`structureCertified: true`) | `preCertModules[]` (`structureCertified: false`, `structureState: READY_FOR_CERTIFY`) |
| Project entries | `Contracts`, `Domain`, `Application`, `Infrastructure`, `Endpoints` (5) | `Contracts`, `Domain`, `Application`, `Infrastructure`, `Tests` (5) |
| Endpoints entry | present | **removed** |
| `Tooba.Pricing.Tests` entry | absent | present, `forbiddenTopLevelFolders: ["Endpoints"]` |
| `modules[]` certified count | **26** | **25** |
| `structureLock.certifiedModules` count | **25** (Pricing present) | **24** (Pricing temporarily absent) |

The per-project `rootAllowlist` / `forbiddenRootFiles` / `forbiddenTopLevelFolders` values for the four
production projects are unchanged from W2/W3; only the Endpoints entry was dropped and the
`Tooba.Pricing.Infrastructure` `rootAllowlistJustification` was updated to state that
`DependencyInjection/PricingModule.cs` now also carries the error catalog/resource registration.

## 7. Untouched surfaces (identity preserved)

- 11 stable codes, 11 descriptors, 11 EN + 11 FA resource keys/text — byte-identical.
- `Tooba.Pricing.Contracts/Errors/{PricingErrorCodes,PricingErrorCatalogContributor,PricingErrorResourceSet}.cs`
  and `Tooba.Pricing.Contracts/Resources/PricingErrors{,.fa}.resx` — unchanged content and path.
- `pricing` schema, `PricingDbContext`, `PricingDbContextFactory`, the single migration
  `20260823085546_InitialPricing` (+ `.Designer.cs` + `PricingDbContextModelSnapshot.cs`),
  `PricingOutboxRegistration`, `IPricingSchemaMigrator` / `PricingModuleMigration` — unchanged.
- All project `.csproj` files except the two edits below:
  - `Tooba.Host/Tooba.Host.csproj` — one `ProjectReference` removed;
  - `Tooba.Pricing.Tests/Tooba.Pricing.Tests.csproj` — one `ProjectReference` removed.
