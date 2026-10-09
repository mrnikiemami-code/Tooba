# TB-TMAR-STORY-AMSC-001 — Wave 2 (Structure)

- **Skill:** `tooba-architecture-structure` (V2)
- **Mode:** `ARCHITECT_DIRECT_AMSC`
- **Target:** `src/backend/Modules/Story/Tooba.Story.*`
- **Starting HEAD:** `2a09e7bb7f1ab687435007951160b4bcfefb4c18` (Wave 1)
- **Branch:** `main`
- **Files moved:** 3 — **Files split:** 1 — **Files deleted:** 0
- **Observable behavior / API shape / schema changed:** NONE
- **Wave-local verdict:** `Structure-State = READY_FOR_CERTIFY`

---

## 1. Scope of this wave

`tooba-architecture-structure` owns **physical layout only**: Solution Explorer grouping,
path↔namespace exactness, project-root hygiene, folder granularity, single-file request leaves,
technical-axis-first trees, file cohesion, god-file balance, stale/duplicate copies and manifest
structure. It does **not** own business semantics, CQRS meaning, module cert verdicts, error codes or
localization.

Wave 1 closed B1/B2/B3 (localization, validator matrix, Persian docs) and handed four structural
divergences to this wave:

| Handoff (from W0/W1) | Decision |
| --- | --- |
| `Infrastructure/StoryModule.cs` at the project root | **Moved** to `Infrastructure/DependencyInjection/` |
| `StoryOutboxRegistration` sharing the composition file | **Split** into `Infrastructure/Messaging/` |
| `Infrastructure/Directory/` (singular) | **Renamed** to `Infrastructure/Directories/` |
| `Stories/Composition/StoryOperation.cs` (nested technical axis) | **Moved** to `Application/Composition/` |
| `Stories/StoryFailureMapper.cs` | **Kept** — it is capability-owned failure translation, not a technical-axis leaf |
| The single-capability `Stories/` wrapper | **Kept** — it is the capability name, and shallow |

## 2. Precedent used

Every decision above is a *precedent match*, not an invention:

- `Infrastructure/DependencyInjection/<Module>Module.cs` is the home of the composition entry in 21
  certified modules (Cart, Payment, Returns, Settlement, Fulfillment, Notification, Promotion, …).
- `<Module>OutboxRegistration` lives in `Infrastructure/Messaging/` (or `Outbox/`) in
  Cart, Payment, Returns, Settlement, Fulfillment and Notification.
- Plural `Directories/` matches the 20-module precedent for a persistence-directory capability.
- A shallow `Application/Composition/` root matches 21 modules for a cross-capability shared seam.

## 3. Files moved / split / deleted

| Action | From | To | Namespace |
| --- | --- | --- | --- |
| moved | `Tooba.Story.Infrastructure/StoryModule.cs` | `Tooba.Story.Infrastructure/DependencyInjection/StoryModule.cs` | `Tooba.Story.Infrastructure` → `Tooba.Story.Infrastructure.DependencyInjection` |
| split | `StoryOutboxRegistration` (second class inside `StoryModule.cs`) | `Tooba.Story.Infrastructure/Messaging/StoryOutboxRegistration.cs` | new `Tooba.Story.Infrastructure.Messaging` |
| moved | `Tooba.Story.Infrastructure/Directory/StoryDirectory.cs` | `Tooba.Story.Infrastructure/Directories/StoryDirectory.cs` | `Tooba.Story.Infrastructure.Directory` → `Tooba.Story.Infrastructure.Directories` |
| moved | `Tooba.Story.Application/Stories/Composition/StoryOperation.cs` | `Tooba.Story.Application/Composition/StoryOperation.cs` | `Tooba.Story.Application.Stories.Composition` → `Tooba.Story.Application.Composition` |

Deleted: **0**. `Stories/Composition/` and `Infrastructure/Directory/` no longer exist.

### 3.1 The outbox split

`StoryModule.cs` had carried two types: the composition root and the `IOutboxModuleRegistration`
implementation. The registration is a distinct integration seam with its own fail-fast contract over
unsupported integration events, so it becomes its own cohesive file. `StoryModule.cs` now only
*references* `StoryOutboxRegistration`; the class body is gone from it.

`Messaging/` (not `Outbox/`) was chosen because that is the dominant sibling spelling and because the
folder holds the module's integration-event seam, not an outbox table owner.

## 4. Composition-root consequence (Host)

Moving the composition entry changed exactly **one** Host line:

| Action | File | Change |
| --- | --- | --- |
| modified | `src/backend/Host/Tooba.Host/Composition/ToobaModuleComposition.cs` | `using Tooba.Story.Infrastructure;` → `using Tooba.Story.Infrastructure.DependencyInjection;` |

This is composition plumbing, not Host authority: no Host business rule, endpoint, persistence entity,
join or Story file was added or touched. `HOST_ROOT_FINAL_CERTIFIED` is preserved and
`src/backend/Host/Tooba.Host/Story` remains **ABSENT**.

## 5. Path ↔ namespace exactness

Re-derived over all five production projects after the moves:

```text
Tooba.Story.Contracts        path == namespace : 1/1
Tooba.Story.Domain           path == namespace : 7/7
Tooba.Story.Application      path == namespace : 12/12   (was 11 + 1 mismatch pre-move)
Tooba.Story.Endpoints        path == namespace : 10/10
Tooba.Story.Infrastructure   path == namespace : 10/10   (was 8 + 2 mismatches pre-move)
                             ---------------------------------
                             mismatches: 0
```

The guard re-computes this from disk by walking `namespace` declarations against the physical
directory chain, so a future rename cannot silently desynchronize them.

## 6. Project-root hygiene

| Project | Root `.cs` before | Root `.cs` after |
| --- | --- | --- |
| `Tooba.Story.Contracts` | 0 | 0 |
| `Tooba.Story.Domain` | 0 | 0 |
| `Tooba.Story.Application` | 0 | 0 |
| `Tooba.Story.Infrastructure` | **1** (`StoryModule.cs`) | **0** |
| `Tooba.Story.Endpoints` | **1** (`StoryEndpointModule.cs`) | **1** (allowlisted composition entry) |

All five manifest `rootAllowlist` entries are now **empty** except the deliberate Endpoints composition
entry, which the manifest keeps as an explicit allowlist. No loose `.cs` file floats at a project root.

## 7. Folder granularity

- `Application/Stories/` remains the single capability, **shallow**: `Commands/{Admin,Seller}`,
  `Queries/{Admin,Seller,Storefront}`, `Models`, `Ports`, `Presentation`, `Validators`.
- **No single-file request leaf** exists anywhere under `Stories/` — every `Commands`/`Queries`
  directory carries ≥2 cohesive source files. (Guard-enforced.)
- **No technical-axis-first root tree** (`Application/Commands`, `Application/Queries`,
  `Application/Validators`, `Application/Models`, `Application/Ports`) exists. Capability is the primary
  axis; technical folders only appear *inside* a capability. (Guard-enforced.)
- `Application/Composition/` holds exactly one shared seam file.

## 8. God-file / over-foldering balance

- No file exceeds the 800-LOC ceiling.
- `Infrastructure/Directories/StoryDirectory.cs` (672 LOC) remains a **single-responsibility**
  persistence directory for one aggregate. It stays `WATCH` (monitor, do not split), because splitting
  it now would create an artificial over-foldered tree with no cohesion gain.
- No over-foldering (no folder holding one leaf file) and no over-nesting (no deep technical chains).

## 9. Stale / duplicate copies

- Zero stale copies: `Directory/`, `Stories/Composition/` and the root `StoryModule.cs` are gone from
  disk, and the guard asserts their non-existence so a merge cannot resurrect them.
- Zero duplicate type definitions across the module.
- Migration files were **not** touched: `Persistence/Migrations/` still holds the two accepted
  migrations plus the model snapshot, and no `.Designer.cs`/snapshot was regenerated.

## 10. Solution Explorer grouping

`src/backend/Tooba.slnx`:

```xml
<Folder Name="/Modules/Story/">
  <Project Path="Modules/Story/Tooba.Story.Domain/Tooba.Story.Domain.csproj" />
  <Project Path="Modules/Story/Tooba.Story.Application/Tooba.Story.Application.csproj" />
  <Project Path="Modules/Story/Tooba.Story.Contracts/Tooba.Story.Contracts.csproj" />
  <Project Path="Modules/Story/Tooba.Story.Infrastructure/Tooba.Story.Infrastructure.csproj" />
  <Project Path="Modules/Story/Tooba.Story.Endpoints/Tooba.Story.Endpoints.csproj" />
</Folder>
```

Exactly **five** projects, each listed **once**, all under the single canonical `/Modules/Story/` group —
a professional, standard Visual Studio grouping. (Guard-enforced.)

## 11. Manifest state

`docs/architecture/tmar-module-structure-manifests.json` → `Story` entry:

- `structureCertified: true` and `lockVersion: "ARCH-COMPLETE-002"` are **unchanged**.
- `Tooba.Story.Infrastructure.rootAllowlist` → `[]` with an updated justification naming
  `DependencyInjection/`, `Messaging/` and `Directories/`.
- `Tooba.Story.Application.rootAllowlistJustification` → updated to record the shallow
  `Composition/` root.
- `certificationNote` → extended to name the AMSC-001 wave line and the new physical homes. The
  AMC-001-W6 certification history is **retained inside the same note**, not overwritten.

## 12. Guards added / updated

**Added** `StoryModuleAmsc001W2StructureGuardTests` (7 facts):

1. `Infrastructure_composition_entry_outbox_and_directories_are_canonically_foldered`
2. `Application_fault_seam_is_shallow_and_no_technical_axis_first_tree_exists`
3. `No_single_file_request_leaf_folder_exists_under_the_Stories_capability`
4. `Every_production_namespace_matches_its_physical_path`
5. `All_project_roots_are_empty_and_match_the_manifest`
6. `Solution_group_contains_exactly_the_five_Story_projects_once_each`
7. `SoT_records_the_AMSC_001_W2_structure_wave`

**Updated** (path/namespace only — no assertion semantics relaxed):

| Guard | Change |
| --- | --- |
| `HostDevelopmentMigrationSeamGuardTests` | Story composition-root path repointed to `DependencyInjection/` |
| `HostGridAmcR2GuardTests` | `StoryModule.cs`, `IAdminStoryGridPort.cs` and `Stories/Presentation/StoryPresentationComposer.cs` paths repointed |
| `AdminDbNativeGridQueryTests` | `Stories/Presentation/StoryPresentationComposer.cs` path repointed |
| `StoryModuleAmcW1GuardTests` | `Directories/StoryDirectory.cs` path repointed |
| `StoryModuleAmcW3StructureGuardTests` | new paths; Infrastructure root file set is now **empty**; exact namespaces |
| `StoryModuleAmcW4ResultGuardTests` | `StoryOperation.cs` path repointed |
| `StoryModuleAmcW6CertGuardTests` | new paths; empty Infrastructure root |
| `StoryModuleAmsc001W1MigrateGuardTests` | `Composition/StoryOperation.cs` path repointed (W1 wave guard follows the moved seam) |
| `StoryFoundationTests` | `using` directives repointed to `…Directories` |

Guards weakened: **NONE**. Baselines widened: **NONE**. No assertion was deleted; every change is a
path repoint.

## 13. Pre-existing, unrelated red guards (declared, not repaired)

Three sibling `HostGridAmc` guards are **already failing at the pristine Wave-1 HEAD** and are not
Story debt, so this wave deliberately leaves them untouched (no unrelated-file edits):

| Guard | Stale pin | Real cause |
| --- | --- | --- |
| `HostGridAmcR3GuardTests.Catalog_owns_product_title_id_lookup_contract` | `Catalog/Tooba.Catalog.Infrastructure/CatalogModule.cs` | file has since moved; the guard was not repointed |
| `HostGridAmcR4GuardTests.Party_owns_sellers_grid_policy_engine_port_and_row_model` | `Party/Tooba.Party.Contracts/AdminSellersGridContracts.cs` | moved to `Ports/` |
| `HostGridAmcR5R1GuardTests.Catalog_owns_review_product_and_title_batch_contracts` + `Host_Grid_remains_absent_and_R5_R1_evidence_present` | Catalog contracts path; a **repository-global** `lastAcceptedTask` pin that has legitimately advanced to `TB-TMAR-HOST-ROOT-FINAL-CERT-001` | historical AMC-001 R5-R1 pins |

Proof: these four facts were run against a **clean `git worktree` at the untouched Wave-1 HEAD**
(`2a09e7bb`, no W2 changes applied) and failed there too — 6 red at baseline, of which the 2
`HostGridAmcR2GuardTests` Story-path failures **were** real W2 debt and are now corrected by this wave.
The residual 4 are the pre-existing Catalog/Party/global-pointer drift above.

## 14. Behavior preservation

- Zero endpoint, route, status code, success payload or error code changed.
- Zero DI service lifetime, registration key or schema changed.
- `StoryDbContext` still declares schema `story`; `AddStoryReviewOwnership` is still the latest
  migration; no migration file was added, regenerated or edited.
- CQRS surface unchanged: 25 real `IRequest<T>` / `IRequestHandler<,>`, dispatched through `ISender`.
- Validator surface unchanged: 16 `VALIDATOR_REQUIRED` + 9 `NO_VALIDATOR_REQUIRED` (17 validator
  classes) exactly as closed in Wave 1 — the moves moved files, not validation.
- Localization unchanged: 10 validation codes + 5 error codes still bilingually resourced in
  `StoryErrors.resx` / `StoryErrors.fa.resx`.

## 15. Coupling / microservice extractability (re-proved)

Unchanged and re-verified after the moves:

- The module references **no foreign module at all** — only `Tooba.BuildingBlocks`,
  `Tooba.ModuleContracts` and `Tooba.Persistence`, at both `using` and `csproj` `ProjectReference` level.
- Zero cross-module `DbContext`, `DbSet`, join, navigation or shared table.
- Contracts are the only boundary; `Tooba.Story.Contracts` holds error codes only.
- Host owns only composition plumbing and the legitimate security adapter.
- Therefore Story remains cleanly extractable as an independent microservice: drop
  `/Modules/Story/` into its own host, keep the three platform references, and it compiles and runs
  with no foreign module.

## 16. Focused build / test evidence

```text
dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj                              -> 0 errors

dotnet test  --filter "StoryModuleAmsc001W2StructureGuardTests|StoryModuleAmsc001W1MigrateGuardTests|
                      StoryModuleAmcW1|StoryModuleAmcW3|StoryModuleAmcW4|StoryModuleAmcW5|
                      StoryModuleAmcW6|HostStoryAmc|StoryFoundation|HostGridAmc|
                      HostDevelopmentMigrationSeam|AdminDbNativeGridQuery"
  -> Failed: 4, Passed: 65, Skipped: 2, Total: 71
```

All Story-owned facts pass, including every W2 fact. The 4 remaining failures are exactly the
pre-existing Catalog/Party/global-pointer drift declared in §13 (red at the untouched W1 HEAD too);
zero Story fact fails.

## 17. Residual debt / handoff to Wave 3

- **B5 (AMSC lineage)** — W2 added `storyAmsc001W2` to the SoT; W3 adds `storyAmsc001W3`, the
  certification checkpoint and the Master Recovery note.
- **B6 (module-scoped AMSC guard)** — W2 delivered the structure guard; W3 adds the certification guard.
- Non-blocking `WATCH`: `StoryDirectory.cs` at 672 LOC (single responsibility, below the 800 ceiling).
- Pre-existing, unrelated: `TmarSourceSizeAndInfraAppTests` fails for an environment reason (stale
  git-ignored `.tmp-baseline` clone) — not Story debt, untouched by this wave.

## 18. Wave outcome

```text
Structure-State  = READY_FOR_CERTIFY
Folder granularity, root hygiene, path↔namespace, solution grouping,
god-file balance, stale-copy cleanliness and manifest structure are canonical.
Behavior unchanged. Coupling unchanged (zero). Host authority preserved.
```

Wave 3 (`tooba-architecture-certify`) may now certify Story against `ARCH-COMPLETE-002`.
