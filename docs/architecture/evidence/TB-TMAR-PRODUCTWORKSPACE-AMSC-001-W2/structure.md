# TB-TMAR-PRODUCTWORKSPACE-AMSC-001-W2 — Structure (tooba-architecture-structure)

## Structure-State

`READY_FOR_CERTIFY`

Baseline: `branch = main`, `HEAD == origin/main == e0fe2885` (W1 Migrate). The wave consumes the W1
handoff `Structure-Handoff-State = REQUIRED` and closes the two structural obligations W1 explicitly
deferred: **F4** (`Tooba.ProductWorkspace.Domain` empty boundary project) and **F5**
(`Application/Composition/{Ports,Validators}` `.gitkeep` ceremony + capability-first normalization).

No route, verb, status code, error code, localization key, DTO, Contracts type, schema or migration was
touched. The restructure is a **physical move + namespace rename** only.

---

## Module applicability

`HTTP_OWNING` — 17 module-owned routes on `/v1/admin/products` mapped exactly once by
`ProductWorkspaceEndpointModule.MapProductWorkspaceModuleEndpoints`, over 4 module-local CQRS queries and
14 module-local CQRS commands (W1). The Endpoints project is not ceremonial.

---

## Physical tree (after W2)

```text
Modules/ProductWorkspace/
  Tooba.ProductWorkspace.Contracts/
    Errors/               ProductWorkspaceErrorCodes.cs
                          ProductWorkspaceErrorResourceSet.cs
    Resources/            ProductWorkspaceErrors.resx
                          ProductWorkspaceErrors.fa.resx
    ProductWorkspaceContractsMarker.cs            (root allowlist)
  Tooba.ProductWorkspace.Domain/                  (declared empty boundary assembly — F4)
    Tooba.ProductWorkspace.Domain.csproj          (zero ProjectReference, zero .cs)
  Tooba.ProductWorkspace.Application/
    Composition/
      ProductWorkspaceOperation.cs                (shared typed-fault -> Result seam)
      ProductManagement/
        Commands/         14 x <Verb>WorkspaceProduct…Command.cs
        Queries/          GetProductWorkspaceQuery.cs + Handler
                          ListProductWorkspaceQuery.cs + Handler
                          QueryProductWorkspaceGridQuery.cs + Handler
                          ProductWorkspaceListComposer.cs
        Models/           ProductWorkspaceModels.cs
        Grid/             AdminProductGridQueryPolicy.cs
  Tooba.ProductWorkspace.Infrastructure/
    ProductWorkspaceModule.cs                     (root allowlist)
  Tooba.ProductWorkspace.Endpoints/
    ProductWorkspaceEndpointModule.cs             (root allowlist, 17 routes)
    Admin/              IProductWorkspaceAdminAuthorizer.cs
```

Removed in W2: `Application/Composition/{Commands,Queries,Models,Grid}` (files moved into the
capability) and `Application/Composition/{Ports,Validators}` plus all four `.gitkeep` placeholders
(`Models/`, `Ports/`, `Queries/`, `Validators/`).

---

## Classification states

| State | Value | Evidence |
|---|---|---|
| Module-Applicability | `HTTP_OWNING` | 17 module-owned routes + 18 module-local CQRS requests with real handlers |
| Folder-Granularity-State | `PROFESSIONAL_SHALLOW` | one real capability (`ProductManagement/{Commands,Queries,Models,Grid}`) under shared `Composition/`; zero per-use-case leaf folders; zero technical-axis-first roots |
| Solution-Explorer-State | `CANONICAL` | `<Folder Name="/Modules/ProductWorkspace/">` in `Tooba.slnx` with exactly 5 projects matching disk |
| Path-Namespace-State | `EXACT` | guard-enforced for all 5 projects (0 mismatches, no exemption needed) |
| Physical-Copy-State | `CLEAN` | one authoritative home per responsibility; no stale flat-layout copy |
| Root-Allowlist-State | `ENFORCED` | Contracts `[ProductWorkspaceContractsMarker.cs]`, Infrastructure `[ProductWorkspaceModule.cs]`, Endpoints `[ProductWorkspaceEndpointModule.cs]`, Application/Domain empty — each equal to disk |
| File-Cohesion-State | `COHESIVE` | largest production file 435 LOC (`ProductWorkspaceEndpointModule.cs`, the 17-route composition entry); no god-file, no over-split |
| Empty-Domain-State | `DECLARED_BOUNDARY_JUSTIFIED` | zero `.cs`, zero `ProjectReference`, manifest justification recorded (F4 closed) |
| Gitkeep-Ceremony-State | `NONE` | zero `.gitkeep` anywhere in the module (F5 closed) |
| Host-Final-Closure-State | `PRESERVED` | no new Host production file/folder; Host keeps only the composition root (`Program.cs` DI + route map, `ToobaModuleComposition` module-list entry) |
| Foreign-Coupling-State | `ZERO` | only foreign edges are `Tooba.Catalog.Contracts` (write gateway) and `Tooba.OperatorProfile.Contracts` (`IActorDisplayLookup`) |

---

## Structural decisions

1. **`ProductManagement` is the honest capability name.** W0 named the surface "Admin composed product
   aggregate" with capabilities C1 (grid list/query), C2 (aggregate view) and C3 (writes). W1 then
   migrated all three to module-local CQRS under `Composition/`. They are one cohesive capability —
   the admin product management workspace over a single product aggregate — not three independent
   capabilities, so they belong under **one** capability folder rather than being fragmented into
   `Grid/`, `Workspace/` and `Writes/` axes.
2. **`Application/Composition/` keeps exactly one file: the typed-fault seam.** `ProductWorkspaceOperation`
   is a genuine cross-capability technical axis (it serves all 14 commands), so it stays in `Composition/`
   at the Application level. The W1 `Composition/{Commands,Queries,Models,Grid}` layout was a
   **technical-axis-under-Composition** shape: `Composition` was standing in for the capability axis while
   the real capability (product management) was invisible in Solution Explorer. W2 makes the capability
   the visible primary axis, exactly as `OperatorProfile` (`Admin/{Commands,Queries,Validators}` +
   `Composition/Models/Ports`) and `Party` (`Seller/{…}` + `Composition/Models/Ports`) do.
3. **`Grid/` is retained as a sub-axis of the capability, not promoted or deleted.**
   `AdminProductGridQueryPolicy` is a real policy type (118 LOC, derived from `GridQueryPolicyBase`) with a
   dedicated 4-test behaviour suite; folding it into `Models/` would blur a policy with a DTO. Keeping it
   as a capability-level sub-folder matches `Order/Admin/OrdersGrid` and `Seller/{Policies}` precedent.
4. **No `Ports/` or `Validators/` folder was invented.** The module consumes its foreign read ports from
   the owning modules' Contracts and owns no port of its own; and W1 deliberately declared
   `EXHAUSTIVE_0_OF_14_NO_VALIDATOR_REQUIRED` because transport shape is validated by the Catalog write
   capability behind the Contracts boundary. Empty ceremonial folders are exactly what the standard
   forbids, so both were deleted rather than filled.
5. **`Domain` stays an empty boundary assembly and is now justified in the manifest (F4).** A
   composition/Admin BFF owns no domain types; the project carries zero `.cs` and zero `ProjectReference`.
   The manifest records the justification explicitly so no later wave silently fills it with a fake domain
   or deletes it.

---

## Namespace realignment (exact path ↔ namespace)

23 production files were moved and re-namespaced; 13 external consumer references were repointed.

| Before | After |
|---|---|
| `…Application.Composition.Commands` | `…Application.Composition.ProductManagement.Commands` |
| `…Application.Composition.Queries` | `…Application.Composition.ProductManagement.Queries` |
| `…Application.Composition.Models` | `…Application.Composition.ProductManagement.Models` |
| `…Application.Composition.Grid` | `…Application.Composition.ProductManagement.Grid` |

Repointed consumers (imports and/or physical-path assertions, **strengthened — never weakened**):

```text
src/backend/Modules/ProductWorkspace/Tooba.ProductWorkspace.Endpoints/ProductWorkspaceEndpointModule.cs
src/backend/Host/Tooba.Host/Program.cs
src/backend/Host/Tooba.Host.Tests/ProductWorkspaceCompositionTests.cs
src/backend/Host/Tooba.Host.Tests/ProductWorkspaceAggregateGetW19Tests.cs
src/backend/Host/Tooba.Host.Tests/ProductCatalogAdminFoundationTests.cs
src/backend/Host/Tooba.Host.Tests/AdminProductGridQueryPolicyPageSizeTests.cs
src/backend/Host/Tooba.Host.Tests/AdminProductGridOfferAmountFilterPolicyTests.cs
src/backend/Host/Tooba.Host.Tests/AdminProductGridAdvancedFilterTests.cs
src/backend/Host/Tooba.Host.Tests/AdminProductGridAdditionalCategoryFilterPolicyTests.cs
src/backend/Host/Tooba.Host.Tests/Architecture/HostAdminAmcW16GuardTests.cs
src/backend/Host/Tooba.Host.Tests/Architecture/HostAdminAmcW19GuardTests.cs
src/backend/Host/Tooba.Host.Tests/Architecture/HostAdminAmcW31PwListGridGuardTests.cs
src/backend/Host/Tooba.Host.Tests/Architecture/ProductWorkspaceModuleAmsc001W1MigrateGuardTests.cs
```

No alias/`GlobalUsings` workaround was used — every namespace matches its physical path.

---

## Manifest reconciliation

`docs/architecture/tmar-module-structure-manifests.json` → `preCertModules[module=ProductWorkspace]`.
Honesty reconciliation only — no allowlist widened, no forbidden entry removed, and the whole-module
`structureCertified` promotion is deferred to W3.

| Project | Change |
|---|---|
| Contracts | `rootAllowlistJustification` refreshed (Errors/ + Resources/ described; the stale "Errors deferred" note removed). `rootAllowlist` unchanged (`[ProductWorkspaceContractsMarker.cs]`). |
| Domain | `rootAllowlistJustification` upgraded to the explicit F4 declaration (empty-but-valid, zero `ProjectReference`). |
| Application | `rootAllowlistJustification` refreshed; `forbiddenTopLevelFolders` extended `[Commands,Queries,Models,Ports]` → `[Commands,Grid,Models,Ports,Queries,Validators]` (the retired technical-axis set). |
| Endpoints / Infrastructure | unchanged (`rootAllowlist` equal to disk). |
| `certificationNote` | replaced the stale W19 note with the AMSC-001 W0→W3 lineage, the Contracts-only write seam, the W2 normalization and the explicit "not certified until W3" lock. |

---

## Durable structure guard

`src/backend/Host/Tooba.Host.Tests/Architecture/ProductWorkspaceModuleAmsc001W2StructureGuardTests.cs`
— **6 passed / 0 failed**.

- `ProductWorkspace_path_namespace_alignment_is_exact`
- `ProductWorkspace_is_capability_first_shallow_without_technical_axis_roots_or_use_case_leaf_folders`
- `ProductWorkspace_root_allowlists_are_enforced_and_no_gitkeep_ceremony_remains`
- `ProductWorkspace_solution_explorer_grouping_is_canonical`
- `ProductWorkspace_domain_stays_a_declared_empty_boundary_project`
- `ProductWorkspace_manifest_structure_allowlists_match_disk`

The W1 migrate guard (`ProductWorkspaceModuleAmsc001W1MigrateGuardTests`) was repointed to the new
capability path and still passes (6/6); the legacy Host/Admin ProductWorkspace guards
(`HostAdminAmcW16/W19/W31`) were repointed to the new physical paths without weakening any assertion.

---

## Validation

```text
dotnet build src/backend/Tooba.slnx                                    -> Build succeeded, 0 errors
ProductWorkspaceModuleAmsc001W2StructureGuardTests                     -> 6 passed / 0 failed
ProductWorkspace* | ErrorCatalogUniqueCodeGuardTests (focused family)  -> 43 passed / 0 failed
full Tooba.Host.Tests suite                                            -> 2066 passed / 79 failed / 130 skipped (2275)
```

The 79 failures are **byte-identical to the pre-W2 baseline** (captured before any file was moved and
compared name-by-name): no test was broken by this wave and none was silently "fixed" by it. They are the
known, out-of-scope repository debt (Host/Admin evacuation count drift, legacy Catalog domain guards,
repository-global recovery pins).

`Foreign App/Infra/Domain coupling = ZERO` before and after; `schemaMigrationState = UNCHANGED`;
`behaviorPreservationRisk = NONE` (physical move + namespace rename only, proven by the identical
before/after full-suite failure set).

---

## Structure-Handoff

`Structure-State = READY_FOR_CERTIFY` → hand off to `tooba-architecture-certify` (W3).

W3 obligations: ARCH-COMPLETE-002 checklist, manifest promotion `preCertModules → modules`
(`structureCertified: true`), `structureLock.certifiedModules` + `tmar-current-state.json` promotion,
Master Recovery checkpoint, durable W3 certification guard.

Stop gate `USER_REVIEW_PRODUCTWORKSPACE_AMSC_001_W2`; `automaticNextImplementationTask = NONE`; the
repository-global Host-root recovery lock is untouched.
