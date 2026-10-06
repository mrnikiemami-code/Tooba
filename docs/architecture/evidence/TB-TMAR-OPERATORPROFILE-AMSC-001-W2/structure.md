# TB-TMAR-OPERATORPROFILE-AMSC-001-W2 — Structure (tooba-architecture-structure)

## Structure-State (overall)

`READY_FOR_CERTIFY`

## Folder-Granularity-State

`PROFESSIONAL_SHALLOW`

- Application: capability-first `Admin/{Commands,Queries,Validators}` with request/validator files **directly on the axes** (zero per-use-case child folders — verified by enumerating children of all three axes) + shared `Composition/`, `Models/`, `Ports/`. Single-capability module (Admin operator self-profile); no empty ceremony folders.
- Domain: `Aggregates/` (1 aggregate file, 136 LOC).
- Infrastructure: integration folders `Adapters/`, `Development/`, `Profiles/`, `Persistence/` (+ `Persistence/Migrations/` for EF artifacts); root = `OperatorProfileModule.cs` only.
- Endpoints: audience folder `Admin/` + root composition `OperatorProfileEndpointModule.cs` only; no `Errors/` folder (descriptors intentionally live in Contracts — the module has no Endpoints-local error surface).
- Contracts: boundary folders `Errors/`, `Ports/`, `Resources/` only.

Single-file leaf audit: every leaf folder under `Application/Admin/*` carries its cohesive file set directly; no `Commands/<UseCase>/`-style wrapper exists; no `TECHNICAL_AXIS_FIRST` root; no `ROOT_DUMP`.

## Solution-Explorer-State

`CANONICAL` — `src/backend/Tooba.slnx` groups exactly the 5 production projects under `<Folder Name="/Modules/OperatorProfile/">` (Domain, Contracts, Application, Infrastructure, Endpoints), matching disk paths; assembly names unchanged.

## Path-Namespace-State

`EXACT` — path-derived namespace equality holds for every production `.cs` across all 5 projects (EF `Persistence/Migrations` artifacts follow the repository-wide EF exemption). Guard-enforced by `OperatorProfileModuleAmsc001W2StructureGuardTests.Path_derived_namespaces_are_exact_across_all_production_projects`. No `TypeForwardedTo`, no namespace alias workaround anywhere in the module.

## Physical-Copy-State

`CLEAN` — one authoritative physical home per responsibility; no stale or duplicate copies. (Untracked local `artifacts/` build-output directories are excluded from git via the repository's local exclude and are not part of the tracked tree.)

## Root-Allowlist-State

`ENFORCED` — all 5 projects match their manifest `rootAllowlist` exactly (Application/Domain/Contracts root empty; Infrastructure root `OperatorProfileModule.cs`; Endpoints root `OperatorProfileEndpointModule.cs`); zero `forbiddenRootFiles`/`forbiddenTopLevelFolders` resurrections (including the retired `OperatorProfileContracts.cs` mixed dump). Guard-enforced.

## Endpoints boundary hygiene

`Endpoints` references only `Application` + `Contracts` + BuildingBlocks; zero `Tooba.OperatorProfile.Infrastructure|Domain`, zero `Tooba.Host.`, zero direct `DbContext` usage. Guard-enforced.

## Host final closure

`PRESERVED` — no `src/backend/Host/Tooba.Host/OperatorProfile` folder; Host residue remains composition root + thin `HostOperatorProfileAdminAuthorizer` + dev-seed invocation + migration descriptor (`HostOperatorProfileAmcGuardTests` continues to pin this).

## Durable guard added

`src/backend/Host/Tooba.Host.Tests/Architecture/OperatorProfileModuleAmsc001W2StructureGuardTests.cs` (5 facts): capability-first tree + zero per-use-case leaves, manifest root allowlists/forbidden lists, exact path↔namespace, Endpoints import hygiene + project-reference hygiene, canonical solution grouping + Host closure + no alias workaround.

## Validation

`dotnet test Host.Tests --filter OperatorProfileModuleAmsc001W2` → 5/5 PASS. No production file moved; zero production change in this wave.
