# TB-TMAR-PAGECOMPOSITION-AMSC-001-W2 — Structure (tooba-architecture-structure)

## Scope

`src/backend/Modules/PageComposition/Tooba.PageComposition.*` — structure re-verification under the current structure skill, zero production change, zero files moved, starting head `11e22747` (W1 reconciliation), parent `TB-TMAR-PAGECOMPOSITION-AMSC-001-W1`.

## Structure Classification Model (section 4 states)

- **Folder-Granularity-State**: `PROFESSIONAL_SHALLOW` — capability-first shallow trees; zero per-use-case request leaf folders (scan below); zero technical-axis-first roots; zero root dumps.
- **Solution-Explorer-State**: `CANONICAL` — `Tooba.slnx` lines 63–68: `<Folder Name="/Modules/PageComposition/">` with all 5 projects (Domain, Contracts, Application, Infrastructure, Endpoints) matching disk paths exactly.
- **Path-Namespace-State**: `EXACT` — every production `.cs` namespace equals the path-derived namespace (spot-verified for all W1-touched files and each project's deepest capability folders; EF `Persistence/Migrations` exemption per repository lock; the repository-global gate's single red is the disclosed pre-existing Catalog Contracts namespace debt, Catalog-owned, out of scope).
- **Physical-Copy-State**: `CLEAN` — no stale path, no duplicate copy; single authoritative home per responsibility.
- **Root-Allowlist-State**: `ENFORCED` — root `.cs` files: Application none, Domain none, Contracts none, Infrastructure only `PageCompositionModule.cs`, Endpoints only `PageCompositionEndpointModule.cs` — matching the manifest `rootAllowlist` arrays exactly; zero forbidden root files; zero forbidden top-level folders (`Commands/`, `Queries/`, `Validators/`, `Presentation/` absent from Application root; `Migrations/` absent from Infrastructure root).
- **Structure-State (overall)**: `READY_FOR_CERTIFY`.

## Physical tree (after — unchanged this wave)

```text
Application/   (no root .cs)
  Admin/{Commands,Queries,Validators}   (request/validator files directly on the axes)
  Storefront/{Queries,Validators}       (request/validator files directly on the axes)
  Composition/  (PageCompositionOperation.cs, PageCompositionPresentationComposer.cs)
  Models/       (PageCompositionModels.cs)
  Ports/        (IPageCompositionDirectory.cs)
Domain/        (no root .cs)
  Aggregates/   (PageDefinition.cs, PageSection.cs)
  Catalog/      (SectionCatalog.cs)
  Constants/    (PageCompositionTenantIds.cs, PageKeys.cs)
Infrastructure/ (PageCompositionModule.cs only)
  Directories/  Development/  Persistence/ (+ Persistence/Migrations/)
Endpoints/     (PageCompositionEndpointModule.cs only)
  Admin/        (PageCompositionAdminEndpoints.cs, IPageCompositionAdminAuthorizer.cs)
  Storefront/   (PageCompositionStorefrontEndpoints.cs)
  Models/       (PageCompositionHttpModels.cs)
Contracts/     (no root .cs)
  Errors/       (codes + catalog contributor + resource set)
  Resources/    (PageCompositionErrors.resx + .fa.resx)
```

## Single-file leaf scan (raw evidence)

The machine scan (`*.cs`-only, `obj`/`bin`/`Migrations` excluded) reports the following single-file
directories. **None is a per-use-case request leaf** — every one is a shared technical axis or a
cohesive non-request folder holding multi-type files, which the structure skill explicitly excludes
from the leaf rule:

| Folder | File | Classification |
|---|---|---|
| `Application/Models` | `PageCompositionModels.cs` (8 record types: snapshots + 2 nested command inputs) | shared Models axis — multi-type, not use-case-named |
| `Application/Ports` | `IPageCompositionDirectory.cs` (1 port interface) | shared Ports axis |
| `Application/Admin/Commands` | `AdminHomeCompositionCommands.cs` (5 request+handler pairs) | shared Commands axis — capability-first grouping |
| `Application/Admin/Queries` | `AdminGetHomeCompositionQuery.cs` (1 request+handler) | shared Queries axis |
| `Application/Admin/Validators` | `PageCompositionValidators.cs` (6 validators) | shared Validators axis |
| `Application/Storefront/Queries` | `HomeCompositionQueries.cs` (2 request+handler pairs) | shared Queries axis |
| `Application/Storefront/Validators` | `StorefrontCompositionValidators.cs` (2 validators) | shared Validators axis |
| `Domain/Catalog` | `SectionCatalog.cs` (approved section catalog + config schema) | domain capability folder (not a request tree) |
| `Infrastructure/Directories` | `PageCompositionDirectory.cs` | integration folder (INFRASTRUCTURE_CAPABILITY_INTEGRATION_FOLDERS) |
| `Infrastructure/Development` | `PageCompositionDevelopmentSeed.cs` | development-seed folder |
| `Endpoints/Models` | `PageCompositionHttpModels.cs` | shared HTTP DTO axis |
| `Endpoints/Storefront` | `PageCompositionStorefrontEndpoints.cs` | audience folder (ENDPOINTS_CAPABILITY_FOLDERS) |

Per-use-case request leaf folders: **ZERO**. Technical-axis-first roots (top-level `Commands/` etc.
under Application): **ZERO** (manifest `forbiddenTopLevelFolders` enforces and the durable guard
re-checks).

## Import hygiene (Endpoints)

`Tooba.PageComposition.Endpoints.csproj` references only `Application` + `Contracts` +
`BuildingBlocks` — zero Infrastructure/Domain/Host/DbContext references; endpoints
import `Application.{Admin,Storefront}.{Commands,Queries,Models,Composition}` + BuildingBlocks
presentation only.

## Durable guard

`PageCompositionModuleAmsc001W2StructureGuardTests` (new, scoped, 5 facts): flat capability axes
(zero child dirs under `Admin/{Commands,Queries,Validators}` and `Storefront/{Queries,Validators}`),
zero per-use-case request leaves by machine scan, exact root allowlists + forbidden files/folders
per manifest, exact path↔namespace for the four non-Endpoints projects (Endpoints covered by the
repo-global gate), canonical `.slnx` solution grouping `/Modules/PageComposition/` with all 5
projects. No pre-existing guard weakened.

## Focused validation

`PageCompositionModuleAmsc001W2StructureGuardTests` + `PageCompositionModuleAmcW2StructureGuardTests`
(legacy layer guard, still green) + `HostPageCompositionAmcGuardTests` +
`PageCompositionModuleAmsc001W1MigrateGuardTests` + `PageCompositionModuleAmcW4CertGuardTests` — all
PASS at this head.

## Completion states

All READY gates hold → **`READY_FOR_CERTIFY`**. Handoff to `tooba-architecture-certify` (W3).
Host final closure preserved; global Host root checkpoint untouched.
