# TB-TMAR-PARTY-AMSC-001-W2 — Structure (tooba-architecture-structure)

## Scope

Re-verification of the touched Party surface under the current `tooba-architecture-structure` skill, starting head `ee9ba997` (post-W1). Zero production change, zero files moved, zero manifest mutation — W1 introduced no new folder (the new `IPartyDevelopmentDirectory.cs` lives in the existing `Contracts/Ports/` axis; `PartyDevelopmentDirectoryAdapter.cs` in the existing `Infrastructure/Development/` folder).

## Physical Tree Audit (before = after; no moves this wave)

```text
Tooba.Party.Contracts/            root .cs: NONE (allowlist empty ✓)
  Errors/   PartyErrorCodes.cs, PartyErrorCatalogContributor.cs, PartyErrorResourceSet.cs
  Ports/    IPartyLookup.cs, IPartySellerSettings.cs, AdminSellersGridContracts.cs,
            IPartyAdminSellerReadGateway.cs, IPartyDevelopmentSeedGateway.cs, IPartyDevelopmentDirectory.cs
  Resources/ PartyErrors.resx, PartyErrors.fa.resx
Tooba.Party.Domain/               root .cs: NONE ✓
  Aggregates/ (5), Enums/ (6), Events/ (1)
Tooba.Party.Application/          root .cs: NONE ✓
  Composition/ PartyOperation.cs
  Models/      PartyReferences.cs
  Ports/       IPartyDirectory.cs
  Admin/Sellers/{Queries ×2, Validators ×1}      ← capability-first, files directly on axis
  Seller/{Commands ×1, Queries ×1, Models ×1, Validators ×1}
Tooba.Party.Infrastructure/       root .cs: PartyModule.cs only ✓
  Admin/ Grid/ Adapters/ Seller/ Development/ Directories/ Events/ Projections/
  Persistence/ (+ Persistence/Migrations EF-exempt)
Tooba.Party.Endpoints/            root .cs: PartyEndpointModule.cs only ✓
  Admin/Sellers/ PartyAdminSellersEndpoints.cs
  Seller/       PartySellerSettingsEndpoints.cs, IPartySellerAuthorizer.cs
```

## Classification States

- **Folder-Granularity-State: `PROFESSIONAL_SHALLOW`** — capability is the first axis (`Admin/Sellers`, `Seller`), technical axes (Commands/Queries/Validators/Models/Ports) are secondary and hold request files directly; zero per-use-case request leaf folders (machine scan over Application+Endpoints: every single-file directory is a shared technical axis — Composition, Models, Ports, Seller/Commands, Seller/Queries, Seller/Validators, Admin/Sellers/Queries, Admin/Sellers/Validators — or a cohesive integration folder: Admin, Grid, Adapters, Seller, Development, Directories, Events, Projections, Persistence; none is use-case-named).
- **Solution-Explorer-State: `CANONICAL`** — `/Modules/Party/` folder in `src/backend/Tooba.slnx` lines 49–54 contains all 5 projects matching disk exactly.
- **Path-Namespace-State: `EXACT`** — path-derived namespace equality verified for all 5 projects (guard-enforced); EF `Persistence/Migrations` exemption follows the repository lock; no alias workaround, no TypeForwardedTo.
- **Physical-Copy-State: `CLEAN`** — one authoritative home per responsibility; no stale/duplicate copies after W1 (no files moved).
- **Root-Allowlist-State: `ENFORCED`** — roots match the manifest allowlists/forbidden lists exactly (`Tooba.Party.Endpoints` root = `PartyEndpointModule.cs`; `Tooba.Party.Infrastructure` root = `PartyModule.cs`; Contracts/Domain/Application roots empty).
- **Structure-State: `READY_FOR_CERTIFY`** — all completion gates hold; Host final closure preserved (no Host Party growth; W1 touched Host-zero files only in module + Promotion consumer).

## Endpoints Import Hygiene

`Tooba.Party.Endpoints.csproj` references Application + Contracts + BuildingBlocks only — no Infrastructure, no Domain reference.

## Durable Guard

New `PartyModuleAmsc001W2StructureGuardTests.cs` (5 facts): flat capability axes with zero child dirs, no per-use-case request leaf (folder-name-based, counting source files not types), manifest root allowlists/forbidden files/forbidden folders, exact path↔namespace for all 5 projects with the EF exemption, canonical `/Modules/Party/` solution grouping, Endpoints import hygiene.

## Focused Validation

Party guard family (W1 migrate + W2 structure + legacy Amc W1/W2/W3/W4 + Host guards) — 20/20 PASS; Host.Tests build 0 errors.
