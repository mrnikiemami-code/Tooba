# TB-TMAR-PRICING-AMSC-001-W2 — structure (tooba-architecture-structure)

- Module: `Pricing`
- Skill: `tooba-architecture-structure`
- Starting HEAD: `069f77d2` (W1 Migrate, `HEAD == origin/main`)
- Structure-Handoff-State: `READY_FOR_CERTIFY`
- Host final closure: preserved (`HOST_TMAR_EVACUATION_FINAL_CLOSURE_CERTIFIED` / `HOST_ROOT_FINAL_CERTIFIED` untouched; zero Host production file added, moved or widened)
- Behavior: `PRESERVED` (pure physical reorganization plus namespace/using repointing; zero route/DTO/descriptor/resource-key/stable-code/DI/schema change)

## 1. Classification states

| Axis | Before | After |
| --- | --- | --- |
| Folder-Granularity-State | `PROFESSIONAL_SHALLOW` | `PROFESSIONAL_SHALLOW` |
| Solution-Explorer-State | `CANONICAL` (`/Modules/Pricing/`, 6 projects) | `CANONICAL` (unchanged) |
| Path-Namespace-State | `MISMATCH` (18 production files) | `EXACT` |
| Physical-Copy-State | `CLEAN` | `CLEAN` |
| Root-Allowlist-State | `VIOLATION` (no manifest entry, `Contracts` root dump) | `ENFORCED` |
| File-Cohesion-State | `COHESIVE` | `COHESIVE` |
| Structure-State | `REPAIR_REQUIRED` | `READY_FOR_CERTIFY` |

## 2. The defects that were repaired

### 2.1 `PATH_NAMESPACE_ALIGNMENT` violation — 18 production files

Every capability folder in `Contracts`, `Domain` and `Infrastructure` declared the **project-level**
namespace instead of the path-derived one:

| Project | Files | Before namespace | After namespace |
| --- | --- | --- | --- |
| `Tooba.Pricing.Contracts` | `Dtos/CurrencyCode.cs` | `Tooba.Pricing.Contracts` | `Tooba.Pricing.Contracts.Dtos` |
| `Tooba.Pricing.Contracts` | 6 × `Ports/*.cs` | `Tooba.Pricing.Contracts` | `Tooba.Pricing.Contracts.Ports` |
| `Tooba.Pricing.Contracts` | `SellerOfferPricingContracts.cs` | `Tooba.Pricing.Contracts` | `Tooba.Pricing.Contracts.Seller` |
| `Tooba.Pricing.Domain` | `Aggregates/AuthoredPrice.cs` | `Tooba.Pricing.Domain` | `Tooba.Pricing.Domain.Aggregates` |
| `Tooba.Pricing.Domain` | `Enums/PriceChannel.cs`, `Enums/PriceEnums.cs` | `Tooba.Pricing.Domain` | `Tooba.Pricing.Domain.Enums` |
| `Tooba.Pricing.Domain` | `Events/PricingDomainEvents.cs` | `Tooba.Pricing.Domain` | `Tooba.Pricing.Domain.Events` |
| `Tooba.Pricing.Domain` | `ValueObjects/{AuthoredCurrency,MarketCode,Money}.cs` | `Tooba.Pricing.Domain` | `Tooba.Pricing.Domain.ValueObjects` |
| `Tooba.Pricing.Infrastructure` | `Adapters/PriceDirectory.cs` | `Tooba.Pricing.Infrastructure` | `Tooba.Pricing.Infrastructure.Adapters` |
| `Tooba.Pricing.Infrastructure` | `DependencyInjection/PricingModule.cs` | `Tooba.Pricing.Infrastructure` | `Tooba.Pricing.Infrastructure.DependencyInjection` |
| `Tooba.Pricing.Infrastructure` | `Outbox/PricingOutboxRegistration.cs` | `Tooba.Pricing.Infrastructure` | `Tooba.Pricing.Infrastructure.Outbox` |

### 2.2 `ROOT_DUMP` — `Contracts` root boundary file

`Tooba.Pricing.Contracts/SellerOfferPricingContracts.cs` was a capability boundary file living at the
project root. It moved to `Tooba.Pricing.Contracts/Seller/SellerOfferPricingContracts.cs` and the root
dump is now explicitly forbidden by the manifest.

### 2.3 Split code/text localization boundary

The stable-code identity was Contracts-owned (W1) while its **user-facing text** was Endpoints-owned:

```text
Tooba.Pricing.Endpoints/Errors/PricingErrorCatalogContributor.cs   ← 11 ErrorDescriptors
Tooba.Pricing.Endpoints/Resources/PricingErrorResources.cs          ← IErrorResourceSet
Tooba.Pricing.Endpoints/Resources/PricingErrors.resx                ← en text
Tooba.Pricing.Endpoints/Resources/PricingErrors.fa.resx             ← fa text
```

Extracting Pricing as a microservice would have left its own text behind in `Tooba.Pricing.Endpoints`.
W2 re-homed the whole surface to the code owner so `Tooba.Pricing.Contracts` is one self-contained
code + text boundary (the certified Inventory/Payment/Media/Party precedent, all of which keep
`Contracts/Errors` **and** `Contracts/Resources`):

```text
Tooba.Pricing.Contracts/Errors/PricingErrorCodes.cs                 ← 11 declared codes + KnownCodes/IsKnown (W1)
Tooba.Pricing.Contracts/Errors/PricingErrorCatalogContributor.cs    ← 11 ErrorDescriptors
Tooba.Pricing.Contracts/Errors/PricingErrorResourceSet.cs           ← IErrorResourceSet + ResourceManager marker
Tooba.Pricing.Contracts/Resources/PricingErrors.resx                ← en text (byte-identical)
Tooba.Pricing.Contracts/Resources/PricingErrors.fa.resx             ← fa text (byte-identical)
```

`PricingErrorResourceSet` resolves through `new ResourceManager("Tooba.Pricing.Contracts.Resources.PricingErrors",
typeof(PricingErrorResources).Assembly)`, so the `.resx` pair is embedded from the Contracts assembly with
an explicit logical name (`EmbeddedResource Update` in `Tooba.Pricing.Contracts.csproj`) — the assembly
that owns the codes also owns the text. The descriptor set is registered exactly once by
`PricingEndpointModule.AddPricingEndpointPresentation()` (unchanged registration site, unchanged single
`IErrorCatalogContributor` / single `IErrorResourceSet`).

### 2.4 Domain capability split needed a namespace bridge

Splitting the Domain into `Aggregates/`, `Enums/`, `Events/`, `ValueObjects/` with path-derived
namespaces made the internal domain types stop resolving each other. W2 added the root
`Tooba.Pricing.Domain/GlobalUsings.cs` — a pure import aggregation with **no** namespace declaration and
**no** type — exactly as the certified `Catalog`/`Order` Domain precedent. It is the single allowlisted
root source file of the Domain.

## 3. Canonical folders kept deliberately

- `Application` stays `Composition/` + `Ports/` only. Pricing owns **zero** endpoint-reachable requests
  (W0), so no `Commands/`, `Queries/`, `Validators/`, `Models/`, `Handlers/` or `Requests/` folder was
  invented — inventing CQRS ceremony would have been the structural defect, not the repair.
- `Infrastructure` stays on `Adapters/`, `DependencyInjection/`, `Events/`, `Outbox/`, `Persistence/`
  (`Persistence/Migrations/` for the single unchanged migration).
- `Endpoints` root holds only the composition entry `PricingEndpointModule.cs`.
- `/Modules/Pricing/` keeps all six projects (`Contracts`, `Domain`, `Application`, `Infrastructure`,
  `Endpoints`, `Tests`) matching disk — no decorative or stale solution folder was introduced.

## 4. Behavior preservation

Routes (the deliberately empty `/v1/pricing` group), descriptor set (11), stable-code string values (11),
bilingual resource keys (11 × 2) and their text, `IPriceDirectory` member set and implementation, DI
lifetimes, the `pricing` schema, the single migration `20260823085546_InitialPricing` (+ designer +
snapshot) and the outbox translation are **byte-identical**. W2 is pure physical reorganization plus
namespace/using repointing. `Persistence/Migrations/*` was touched only for the EF model type name
(`Tooba.Pricing.Domain.AuthoredPrice` → `Tooba.Pricing.Domain.Aggregates.AuthoredPrice`), which is the
mechanical consequence of the Domain namespace split and changes no table, column, index, constraint,
migration id or Up/Down semantics.

## 5. Durable guards added / updated

`src/backend/Host/Tooba.Host.Tests/Architecture/PricingModuleAmsc001W2StructureGuardTests.cs` (**new**, 9 tests)

- `Contracts_and_Domain_are_capability_first_with_no_root_dump`
- `Application_stays_shallow_with_no_technical_axis_or_use_case_leaves`
- `Infrastructure_uses_canonical_integration_folders`
- `Endpoints_root_holds_only_the_composition_entry`
- `Relocated_localization_surface_has_a_single_authoritative_home`
- `Path_derived_namespaces_are_exact`
- `Root_allowlists_and_forbidden_lists_match_the_manifest`
- `Solution_grouping_is_canonical_modules_pricing`
- `Endpoints_import_hygiene_stays_application_contracts_and_buildingblocks_only`

`src/backend/Host/Tooba.Host.Tests/Architecture/PricingModuleAmsc001W1MigrateGuardTests.cs` (**W1 semantics preserved, not weakened**)

- `Pricing_Contracts_surface_uses_path_derived_namespaces` now pins the post-W2 physical truth
  (`Contracts/Seller/SellerOfferPricingContracts.cs` + `namespace Tooba.Pricing.Contracts.Seller`) while
  keeping every W1 assertion (single code home, no re-inlined literals, no message-text classification,
  Contracts-only boundary, no `Tooba.Pricing.Application` reachable from Promotion).

`src/backend/Modules/Pricing/Tooba.Pricing.Tests/Architecture/PricingArchitectureGuardTests.cs`

- Localization assertions re-pointed to `Contracts/Errors` + `Contracts/Resources`; the seller boundary
  path assertion re-pointed to `Contracts/Seller/SellerOfferPricingContracts.cs`.

`src/backend/Modules/Pricing/Tooba.Pricing.Tests/{Contracts/PriceQuoteShapeTests,Domain/AuthoredPriceInvariantTests,
Infrastructure/PricingDbContextOwnershipTests,Observability/PricingErrorCatalogTests}.cs` and
`src/backend/Host/Tooba.Host.Tests/{ContractsW5CharacterizationTests,ContractsW6CharacterizationTests,
PricingFoundationTests}.cs` — namespace/path literals repointed to the post-W2 tree. `ContractsW5`/`W6`
assert the path-derived namespace (`Tooba.Pricing.Contracts.Ports`) **and** keep asserting the assembly
identity (`Tooba.Pricing.Contracts`), so the boundary ownership claim is preserved rather than relaxed.

## 6. Manifest interaction

`docs/architecture/tmar-module-structure-manifests.json` → **new** `preCertModules[Pricing]` entry
(`structureCertified: false`, `lockVersion: ARCH-COMPLETE-002`), disk-accurate per project:

| Project | `rootAllowlist` | Notable `forbiddenRootFiles` | `forbiddenTopLevelFolders` |
| --- | --- | --- | --- |
| `Tooba.Pricing.Contracts` | `[]` | `PricingErrorCodes.cs`, `SellerOfferPricingContracts.cs`, `PriceLookupContracts.cs`, `PriceQueryContracts.cs`, `CurrencyCode.cs`, `IPriceDirectory.cs` | `[]` |
| `Tooba.Pricing.Domain` | `["GlobalUsings.cs"]` | the 7 split domain files | `["Errors"]` |
| `Tooba.Pricing.Application` | `[]` | `PricingContracts.cs`, `PricingHandlers.cs`, `PricingRequests.cs`, `PricingOperation.cs`, `PricingErrorCodes.cs` | `["Commands","Queries","Validators","Models","Handlers","Requests"]` |
| `Tooba.Pricing.Infrastructure` | `[]` | `PricingModule.cs`, `PriceDirectory.cs`, `PricingDbContext.cs`, `PricingEvents.cs`, `PricingOutboxRegistration.cs`, `PricingModuleMigration.cs`, `PricingSchemaMigrator.cs`, `PricingDevelopmentSeedGateway.cs` | `["Migrations","Repositories","Directories","Messaging"]` |
| `Tooba.Pricing.Endpoints` | `["PricingEndpointModule.cs"]` | `PricingEndpoints.cs`, `PricingErrorCatalogContributor.cs`, `PricingErrorResources.cs`, `PricingEndpointLocalizer.cs` | `["Admin","Seller","Storefront","Errors","Resources"]` |

`structureCertified` was **not** flipped and `Pricing` was **not** added to
`structureLock.certifiedModules` — the W3 Certify verdict is deliberately left open. Pricing owns zero
HTTP routes, so it is correctly absent from `uncertifiedHttpOwningModules` (`Returns`, `Support`,
`Wallet`, `Promotion`).

## 7. Focused validation

| Validation | Result |
| --- | --- |
| `Tooba.Host.Tests` build | succeeded, **0 errors** |
| `PricingModuleAmsc001W1MigrateGuardTests` + `PricingModuleAmsc001W2StructureGuardTests` | **18 / 18 passed** (W1 9, W2 9) |
| `Tooba.Pricing.Tests` | **14 / 14 passed** |
| `Tooba.Promotion.Tests` | **7 passed / 1 failed** |
| `Tooba.Host.Tests` full suite | **2026 passed / 130 skipped / 79 failed** |
| `Tooba.Host.Tests` full suite at the `069f77d2` baseline (isolated `git worktree`) | **2007 passed / 130 skipped / 80 failed** |
| **New failures introduced by W2** | **ZERO** (set-difference of failing test ids: baseline 80 ⊃ current 79) |
| Failure fixed by W2 | `PricingFoundationTests.Pricing_projects_do_not_reference_masstransit_authzed_or_foreign_infrastructure` (the W1 `using Tooba.Pricing.Domain.Aggregates;` repoint) |
| `.slnx` | `/Modules/Pricing/` group unchanged; all 6 projects present and matching disk |

The single `Tooba.Promotion.Tests` failure and the 79 `Tooba.Host.Tests` failures are all present at the
`069f77d2` baseline and are unrelated to Pricing (Catalog `Results.Json` WIP, missing
`Modules/Wishlist` file, Host/Admin count drift, missing `TaxDomain.cs`, missing
`Host/Promotion/PromotionEndpoints.cs`, missing `PromotionContracts.cs`, and the pre-existing
`Promotion.Infrastructure/Development/` root-allowlist gap in `PromotionArchitectureGuardTests`).
The repository-global `TmarCompleteReferenceStructureGateTests.Certified_modules_satisfy_root_allowlists_and_namespace_alignment`
failure is likewise a baseline failure (the pre-existing `Tooba.Catalog.Contracts/Cart` project-level
namespace deviation, explicitly documented in the Inventory W3 certification as out of scope for a
module-local certification) and remains unchanged by W2.

## 8. Handoff

- `Structure-State = READY_FOR_CERTIFY` — every structure §27 gate met on disk and enforced by the new
  durable guard.
- Residual for W3 Certify: promote the manifest entry into the certified `modules` array
  (`structureCertified: true`), add `Pricing` to `structureLock.certifiedModules`, add
  `PricingModuleAmsc001W3CertGuardTests`, and record the SoT certification block.
- Host final closure preserved: zero Host production files added; schema/migrations unchanged; guards
  strengthened; zero guards weakened; zero unrelated files touched.
