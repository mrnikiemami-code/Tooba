# TB-TMAR-PRICING-AMSC-001-W3-R3 — structure verification

Re-enumerated independently from disk by
`node docs/architecture/evidence/TB-TMAR-PRICING-AMSC-001-W3-R3/certify-audit.cjs`
(full output in `audit-after.json`). The Structure authority for this surface is
`TB-TMAR-PRICING-AMSC-001-W3-R2` @ `7159c8f7` (`READY_FOR_CERTIFY`).

## 1. Final physical tree (production projects)

```text
src/backend/Modules/Pricing/
  Tooba.Pricing.Contracts/           (root .cs: none)
    Dtos/  Errors/  Ports/  Resources/  Seller/
  Tooba.Pricing.Domain/              (root .cs: GlobalUsings.cs only)
    Aggregates/  Enums/  Events/  ValueObjects/
  Tooba.Pricing.Application/         (root .cs: none)
    Composition/  Ports/
  Tooba.Pricing.Infrastructure/      (root .cs: none)
    Adapters/  DependencyInjection/  Events/  Outbox/  Persistence/
  Tooba.Pricing.Tests/
    Architecture/  Contracts/  Domain/  Infrastructure/  Observability/
```

`Modules/Pricing/` contains exactly five project directories — `Tooba.Pricing.Contracts`,
`Tooba.Pricing.Domain`, `Tooba.Pricing.Application`, `Tooba.Pricing.Infrastructure`,
`Tooba.Pricing.Tests`. No `Tooba.Pricing.Endpoints` directory exists, and no
`Tooba.Pricing.Tests/Endpoints` folder exists.

## 2. Root allowlists

| Project | Root `.cs` on disk | Manifest `rootAllowlist` | State |
| --- | --- | --- | --- |
| `Tooba.Pricing.Contracts` | *(none)* | `[]` | `ENFORCED` |
| `Tooba.Pricing.Domain` | `GlobalUsings.cs` | `["GlobalUsings.cs"]` | `ENFORCED` |
| `Tooba.Pricing.Application` | *(none)* | `[]` | `ENFORCED` |
| `Tooba.Pricing.Infrastructure` | *(none)* | `[]` | `ENFORCED` |

`GlobalUsings.cs` is the single namespace bridge for the capability-first Domain (certified
Catalog/Order Domain precedent); it declares no namespace and holds no type. Every forbidden root file
and forbidden top-level folder in the manifest entry is absent on disk.

## 3. Path ↔ namespace exactness

Every production `.cs` under the four production projects was compared against its path-derived
namespace (`<Project>[.<SubFolder>…]`), excluding `bin`/`obj` and `GlobalUsings*`:

```text
namespaceMismatches = []      (0 mismatches)
Path-Namespace-State = EXACT
```

## 4. Folder granularity

Capability-first shallow grouping only:

- `Application/` = `Composition/` (typed-fault seam) + `Ports/` (module-internal use-case guard).
  No `Commands`/`Queries`/`Validators`/`Models`/`Handlers`/`Requests` folder exists, and no
  technical-axis-first request tree exists.
- `Contracts/` = `Dtos/`, `Errors/`, `Ports/`, `Resources/`, `Seller/`.
- `Domain/` = `Aggregates/`, `Enums/`, `Events/`, `ValueObjects/`.
- `Infrastructure/` = `Adapters/`, `DependencyInjection/`, `Events/`, `Outbox/`, `Persistence/`
  (with `Persistence/Migrations/`).

`Folder-Granularity-State = PROFESSIONAL_SHALLOW`. There is no per-use-case single-file leaf folder and
no over-foldering, because there are no use cases to folder.

## 5. Physical copies / stale duplicates

`Physical-Copy-State = CLEAN`:

- no stale root copy of the stable-code file (`Contracts/PricingErrorCodes.cs` absent);
- no stale root copy of the seller boundary (`Contracts/SellerOfferPricingContracts.cs` absent);
- no duplicate `Domain/Errors/` folder (retired and forbidden);
- no duplicate compatibility type, no `TypeForwardedTo`, no namespace alias workaround;
- exactly one `PricingErrorCatalogContributor` and one `PricingErrorResourceSet` declaration across the
  whole module (machine-checked).

## 6. Solution Explorer grouping

`src/backend/Tooba.slnx`, solution folder `/Modules/Pricing/`:

```text
Modules/Pricing/Tooba.Pricing.Application/Tooba.Pricing.Application.csproj
Modules/Pricing/Tooba.Pricing.Contracts/Tooba.Pricing.Contracts.csproj
Modules/Pricing/Tooba.Pricing.Domain/Tooba.Pricing.Domain.csproj
Modules/Pricing/Tooba.Pricing.Infrastructure/Tooba.Pricing.Infrastructure.csproj
Modules/Pricing/Tooba.Pricing.Tests/Tooba.Pricing.Tests.csproj
```

Exactly **5** project entries, no `Tooba.Pricing.Endpoints` entry. `Solution-Explorer-State = CANONICAL`.

## 7. File cohesion / size

Largest production files (EF migration artefacts excluded):

| File | LOC |
| --- | --- |
| `Tooba.Pricing.Infrastructure/Adapters/PriceDirectory.cs` | 485 |
| `Tooba.Pricing.Domain/Aggregates/AuthoredPrice.cs` | 260 |
| `Tooba.Pricing.Infrastructure/Persistence/Migrations/20260823085546_InitialPricing.Designer.cs` | 189 |

No production file exceeds the 800 LOC threshold; `over800 = []`. No new god-file, no artificial
parallel decomposition.

## 8. Durable structure guards

| Guard | Scope |
| --- | --- |
| `PricingModuleAmsc001W3R3CertGuardTests.Structure_invariants_hold_for_the_five_project_solution_group` | root allowlists, folder granularity, path↔namespace, physical copies, solution grouping |
| `PricingModuleAmsc001W3R3CertGuardTests.Internal_only_applicability_holds_zero_routes_and_zero_requests` | Endpoints absence, zero routes, no CQRS ceremony |
| `PricingModuleAmsc001W2StructureGuardTests` | capability-first shallow tree, manifest allowlists, canonical grouping |
| `PricingArchitectureGuardTests` | Domain/Infrastructure reference rules, no >800 LOC, no Offer leakage |
| `TmarCompleteReferenceStructureGateTests` | certified-module root allowlists + namespace alignment (repository-global) |

All green (see `validation.md`). No structural assertion was relaxed by this wave; the only guard edits
repoint the *certification claim* (pre-cert → certified) while keeping every structural assertion.

## 9. Structure gate integrity

The consumed Structure gate is current and not contradicted:

```text
Structure-State = READY_FOR_CERTIFY  (W3-R2 @ 7159c8f7)
Folder-Granularity-State = PROFESSIONAL_SHALLOW
Solution-Explorer-State = CANONICAL
Path-Namespace-State = EXACT
Physical-Copy-State = CLEAN
Root-Allowlist-State = ENFORCED
Host final closure preserved
```

Every one of those fields was re-derived from disk in this wave and is durably re-asserted, so the
structure gate was verified rather than inherited.
