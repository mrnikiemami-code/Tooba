# TB-TMAR-USERPREFERENCE-AMSC-001-W2 — Structure (`tooba-architecture-structure`)

```text
TASK:          TB-TMAR-USERPREFERENCE-AMSC-001-W2
MODE:          ARCHITECT_DIRECT_AMSC
SKILL:         tooba-architecture-structure
TARGET:        src/backend/Modules/UserPreference/Tooba.UserPreference.*
PARENT:        TB-TMAR-USERPREFERENCE-AMSC-001-W1
PARENT COMMIT: a0edab10
STARTING HEAD: a0edab10
STATE:         STRUCTURE_COMPLETE
VERDICT:       READY_FOR_CERTIFY
LOCK VERSION:  ARCH-COMPLETE-002
```

W2 is a **structure wave**: it locks the physical shape and moves the module's structure authority
from the historical `TB-TMAR-USERPREFERENCE-AMC-001-W4` to `TB-TMAR-USERPREFERENCE-AMSC-001-W2`.
No business, CQRS, validator or module-cert verdict is owned here. **No project was added, removed,
renamed or re-pathed, and no production file was moved.**

---

## 1. Physical tree (verified, locked)

Machine scan over all five projects (`inspect.cjs`):

```text
projects        = Application, Contracts, Domain, Endpoints, Infrastructure
per-project .cs = {Application:11, Contracts:3, Domain:2, Endpoints:6, Infrastructure:11}
total .cs       = 33
mismatches      = 0
```

```text
src/backend/Modules/UserPreference/
  Tooba.UserPreference.Contracts/          (3 .cs + 2 .resx)
    Errors/    UserPreferenceErrorCodes.cs, UserPreferenceErrorCatalogContributor.cs,
               UserPreferenceErrorResourceSet.cs
    Resources/ UserPreferenceErrors.resx, UserPreferenceErrors.fa.resx
  Tooba.UserPreference.Domain/             (2 .cs)
    Aggregates/ UserPreference.cs, UiPreference.cs
  Tooba.UserPreference.Application/        (11 .cs)
    Composition/ UserPreferenceOperation.cs
    LocalePreferences/{Commands,Queries,Validators}
    UiPreferences/{Commands,Queries,Validators}
    Models/ UserPreferenceModels.cs
    Ports/ IUserPreferenceDirectory.cs, IUiPreferenceDirectory.cs
  Tooba.UserPreference.Endpoints/          (6 .cs)
    UserPreferenceEndpointModule.cs
    Admin/    IUserPreferenceAdminAuthorizer.cs, UserPreferenceAdminEndpoints.cs,
              UiPreferenceAdminEndpoints.cs
    Customer/ UserPreferenceCustomerActorResolver.cs, UserPreferenceCustomerEndpoints.cs
  Tooba.UserPreference.Infrastructure/     (11 .cs + 5 migration artifacts)
    UserPreferenceModule.cs
    Development/ UserPreferenceDevelopmentSeed.cs
    Directories/ UserPreferenceDirectory.cs, UiPreferenceDirectory.cs
    Persistence/ UserPreferenceDbContext.cs, UserPreferenceOutboxRegistration.cs
    Persistence/Migrations/ 2 migrations + 2 designers + snapshot
```

## 2. Structure verdicts

| Check | Result |
| --- | --- |
| `PATH_NAMESPACE_ALIGNMENT` | **EXACT** — 0 mismatches / 33 production files (path-derived namespace per project) |
| `ROOT_ALLOWLIST` | **ENFORCED** — Contracts/Domain/Application have empty root allowlists (0 root `.cs`); Endpoints allowlist `UserPreferenceEndpointModule.cs`; Infrastructure allowlist `UserPreferenceModule.cs` |
| `FOLDER_GRANULARITY` | **PROFESSIONAL_SHALLOW** — capability-first (`LocalePreferences`, `UiPreferences`, `Admin`, `Customer`, `Directories`, `Persistence`, `Development`); no single-file request leaf |
| `TECHNICAL_AXIS_FIRST` | **NONE** — no root `Commands`/`Queries`/`Validators`/`Presentation` in Application; no root `Migrations` in Infrastructure (canonical `Persistence/Migrations`) |
| `GOD_FILE` | **NONE** — largest production file 77 LOC vs the 800 LOC `ARCH-SIZE-001` ceiling |
| `STALE_DUPLICATE_COPY` | **NONE** — exactly one `UserPreferenceErrorCodes.cs`, one `UserPreferenceErrorCatalogContributor.cs`, one EN `.resx` and one FA `.resx` in the module |
| `PHYSICAL_COPY` | **CLEAN** |
| `SOLUTION_EXPLORER` | **CANONICAL** — `/Modules/UserPreference/` holds exactly the five projects; disk == solution |
| `ALIAS_WORKAROUND` | **NONE** (no `TypeForwardedTo`, no namespace alias) |
| `SCHEMA` | **UNCHANGED** — no migration/designer/snapshot touched |

### 2.1 `GlobalUsings.cs` is deliberately absent (justified)

The certified `Pricing`/`Catalog` Domain precedent allows a single root `GlobalUsings.cs`
namespace-bridge. `Tooba.UserPreference.Domain` has **no** such file and does not need one: both
aggregates declare `namespace Tooba.UserPreference.Domain.Aggregates` and import
`Tooba.UserPreference.Contracts.Errors` explicitly. Adding a bridge would be ceremony, so the W2
guard pins the Domain root as **empty** (0 root `.cs`), which is the stronger form of the same
invariant.

## 3. Solution grouping

`src/backend/Tooba.slnx` lines 14–20:

```xml
<Folder Name="/Modules/UserPreference/">
  <Project Path="Modules/UserPreference/Tooba.UserPreference.Domain/Tooba.UserPreference.Domain.csproj" />
  <Project Path="Modules/UserPreference/Tooba.UserPreference.Contracts/Tooba.UserPreference.Contracts.csproj" />
  <Project Path="Modules/UserPreference/Tooba.UserPreference.Application/Tooba.UserPreference.Application.csproj" />
  <Project Path="Modules/UserPreference/Tooba.UserPreference.Infrastructure/Tooba.UserPreference.Infrastructure.csproj" />
  <Project Path="Modules/UserPreference/Tooba.UserPreference.Endpoints/Tooba.UserPreference.Endpoints.csproj" />
</Folder>
```

Five projects, no missing and no extra project directory on disk. UNCHANGED by W2.

## 4. Manifest structure authority (updated)

`docs/architecture/tmar-module-structure-manifests.json` → `modules[]` → `UserPreference`:

```text
structureAuthorityTask   : TB-TMAR-USERPREFERENCE-AMSC-001-W2   (was: implicit AMC-001 W4)
structureAuthorityCommit : <W2 commit>
structureHandoffState    : READY_FOR_CERTIFY_CONSUMED_BY_W3
structureCertified       : true        (unchanged)
lockVersion              : ARCH-COMPLETE-002   (unchanged)
```

The five `projects[]` allowlist/forbidden-list records are unchanged — W2 only moved the authority
and handoff state. No assertion in any pre-existing guard was relaxed or repointed.

## 5. Durable guard

`src/backend/Host/Tooba.Host.Tests/Architecture/UserPreferenceModuleAmsc001W2StructureGuardTests.cs`
— 9 facts:

1. `Contracts_is_capability_first_with_no_root_dump`
2. `Domain_is_capability_first_with_no_root_dump`
3. `Application_is_capability_first_and_never_technical_axis_first`
4. `Endpoints_root_holds_only_the_composition_entry_and_owns_no_localization_surface`
5. `Infrastructure_root_holds_only_the_composition_entry_with_migrations_under_persistence`
6. `Path_and_namespace_are_exact_across_every_production_file`
7. `Solution_has_no_extra_or_missing_userpreference_project`
8. `Manifest_records_the_userpreference_structure_authority_and_allowlists`
9. `Module_has_no_stale_or_duplicate_physical_copies_of_the_localization_surface`

**Result: 9 of 9 PASSED.**

## 6. Verification

```text
dotnet build Tooba.Host.Tests.csproj                        -> SUCCEEDED, 0 errors
dotnet test --filter UserPreferenceModuleAmsc001W2StructureGuardTests
             | UserPreferenceModuleAmsc001W1MigrateGuardTests
             | UserPreferenceModuleAmc* | HostPreferencesAmcGuardTests
             | ErrorCatalogUniqueCodeGuardTests              -> Failed: 0, Passed: 32, Skipped: 0
```

W1 semantics preserved and re-verified (not weakened): 8/8 still green.

```text
Path-Namespace-State        : EXACT
Root-Allowlist-State        : ENFORCED
Folder-Granularity-State    : PROFESSIONAL_SHALLOW
Technical-Axis-First-State  : NONE
God-File-State              : NONE
Stale-Duplicate-Copy-State  : NONE
Physical-Copy-State         : CLEAN
Solution-Explorer-State     : CANONICAL
Alias-Workaround-State      : NONE
Schema-State                : UNCHANGED
Structure-Authority         : TB-TMAR-USERPREFERENCE-AMSC-001-W2
Structure-Handoff-State     : READY_FOR_CERTIFY_CONSUMED_BY_W3
Guards-Weakened             : NONE
```

## 7. Next wave

W3 (`tooba-architecture-certify`) authors the durable cert lock, refreshes the manifest
`certificationNote` with the AMSC-001 W0→W3 lineage, records the SoT lineage and the Master
Recovery checkpoint, and proves zero new test failures.
