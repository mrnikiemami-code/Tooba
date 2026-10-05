# TB-TMAR-CUSTOMERPROFILE-AMSC-001-W2 — Structure

## Mode

`ARCHITECT_DIRECT_AMSC` — Structure. Physical/folder normalization only; no business-behavior,
route, DTO-shape, status-code or schema change.

Baseline: `HEAD == origin/main == 4599c97f` (W1).

## Folder-Granularity-State

`TECHNICAL_AXIS_FIRST` → **`PROFESSIONAL_SHALLOW`**

The Application project was organized by technical axis with one folder per use case
(`Commands/<UseCase>/`, `Queries/<UseCase>/`, `Validators/<UseCase>/`, root `Models/`), producing
single-file request leaf folders. It is now capability-first with secondary technical axes.

## Structure changes

### Application — capability-first

| Before | After |
|---|---|
| `Commands/UpsertCustomerProfile/UpsertCustomerProfileCommand.cs` | `Profile/Commands/UpsertCustomerProfileCommand.cs` |
| `Queries/GetCustomerProfilePage/GetCustomerProfilePageQuery.cs` | `Profile/Queries/GetCustomerProfilePageQuery.cs` |
| `Validators/UpsertCustomerProfile/UpsertCustomerProfileCommandValidator.cs` | `Profile/Validators/UpsertCustomerProfileCommandValidator.cs` |
| `Queries/GetCustomerAccountDashboard/GetCustomerAccountDashboardQuery.cs` | `Account/Queries/GetCustomerAccountDashboardQuery.cs` |
| `Models/CustomerAccountPages.cs` | `Account/Models/CustomerAccountPages.cs` |
| `Ports/ICustomerAccountDisplayTexts.cs` | `Ports/` (shared, unchanged) |
| `Composition/CustomerProfileOperation.cs` | `Composition/` (shared, unchanged — W1) |
| `CustomerProfileContracts.cs` (comment-only no-op) | **deleted** |

Namespaces follow exactly: `Tooba.CustomerProfile.Application.Profile.{Commands,Queries,Validators}`,
`Tooba.CustomerProfile.Application.Account.{Queries,Models}`.

### Contracts — split by boundary semantics

`CustomerProfileContracts.cs` (mixed file) → `Ports/ICustomerProfileDirectory.cs`,
`Dtos/CustomerProfileSnapshot.cs`, `Dtos/CustomerProfileWrite.cs`, alongside the W1
`Errors/CustomerProfileErrorCodes.cs`. The public port stays in Contracts because
`Tooba.Identity.Application` consumes it — it is a real module boundary, not an Application-internal type.

### Domain

`CustomerProfile.cs` → `Aggregates/CustomerProfile.cs` (Order/AddressBook/Content precedent).

### Infrastructure

| Before | After |
|---|---|
| `CustomerProfileModule.cs` (root) | `DependencyInjection/CustomerProfileModule.cs` |
| `CustomerProfileDirectory.cs` (root) | `Directories/CustomerProfileDirectory.cs` |
| `CustomerProfileOutboxRegistration` (co-declared in the module file) | `Messaging/CustomerProfileOutboxRegistration.cs` (own file) |
| `Migrations/` (top-level) | `Persistence/Migrations/` |

The module composition file was split so each type has one reason to change; the EF configuration
(`ConfigureModuleContext`) is passed `typeof(CustomerProfileDbContext)` as the migrations-assembly marker,
so moving the `Migrations/` folder required no EF wiring change.

### EF migration identity preserved

EF persists CLR type names inside `Migration` designer metadata. The model entity string
`"Tooba.CustomerProfile.Domain.CustomerProfile"` in `20260825225000_InitialCustomerProfile.Designer.cs`
and `CustomerProfileDbContextModelSnapshot.cs` was therefore **left byte-identical** even though the
aggregate now lives in the `Domain.Aggregates` namespace. Migration IDs, `Up`/`Down` bodies and the
snapshot's entity identity are unchanged, so existing databases remain compatible.

Namespace declarations in the three migration files did move to
`Tooba.CustomerProfile.Infrastructure.Persistence.Migrations` (AddressBook precedent) — the migrations
assembly marker is the DbContext type, and nothing references the old namespace.

## Solution Explorer

`/Modules/CustomerProfile/` already grouped all five projects in `src/backend/Tooba.slnx`; no change was
needed. Verified by the durable guard.

## Stale guards repaired (pre-existing RED at clean HEAD)

Two inherited guards asserted a flat `/Modules/` solution folder that does not exist anywhere in
`Tooba.slnx` (the file has no `/Modules/` folder at all), so they failed with
`Sequence contains no matching element` before this wave. Both were repaired to their actual intent —
"CustomerProfile is not ALSO placed in a flat `/Modules/` folder":

| Guard | Repair |
|---|---|
| `CustomerProfileSolutionGroupingGuardTests.CustomerProfile_projects_are_grouped_exactly_once_under_Modules_CustomerProfile` | `Single` → `FirstOrDefault`, empty when the flat folder is absent |
| `HostCustomerProfileEvacuationGuardTests.Parent_R1_solution_grouping_remains_exact` | same |

Both now pass and still fail if a flat `/Modules/` CustomerProfile entry is ever introduced.

`HostDevelopmentMigrationSeamGuardTests.All_twenty_eight_active_modules_register_their_own_schema_migrator`
pins each module's composition-root path; CustomerProfile's entry was updated to
`DependencyInjection/CustomerProfileModule.cs`.

## Manifest

`docs/architecture/tmar-module-structure-manifests.json` — CustomerProfile did not exist in the manifest.
It was added to **`preCertModules`** (not the certified `modules` array), mirroring how ProductWorkspace
records an uncertified-but-structured module, with `structureCertified: false` and a per-project
`rootAllowlist` / `rootAllowlistJustification` / `forbiddenRootFiles` / `forbiddenTopLevelFolders`
record for all five projects.

The manifest's existing formatting and key layout were preserved by a targeted text insertion; a
round-trip re-serialization was reverted because it changed quoting style and broke two guards that
match the exact `"module": "..."` / `"structureCertified": true` literals.

## Durable structure guard (new)

`src/backend/Host/Tooba.Host.Tests/Architecture/CustomerProfileModuleAmsc001W2StructureGuardTests.cs`
— 10 assertions:

1. Application is capability-first (`Account`, `Profile`, shared `Composition`/`Ports` present;
   `Commands`/`Queries`/`Models`/`Validators` absent as top-level axes).
2. No single-file request leaf folder.
3. The stale `CustomerProfileContracts.cs` placeholder is absent from Application and Contracts.
4. Contracts holds boundary semantics only (no `IRequest`, `IRequestHandler`, `DbContext`, Domain or
   Application reference) and keeps `Ports/ICustomerProfileDirectory.cs`, `Dtos/*`.
5. Single stable-code owner: Domain declares no `public const string` code of its own; the owner is
   `Contracts/Errors/CustomerProfileErrorCodes.cs`.
6. Endpoints do not reference Domain (csproj or source).
7. Infrastructure uses `DependencyInjection/`, `Directories/`, `Messaging/`, `Persistence/Migrations/`
   with no root `.cs` and no top-level `Migrations/`.
8. Manifest root allowlists match disk exactly and forbidden roots are absent, read from
   `preCertModules` (and asserting CustomerProfile is NOT in the certified `modules` array).
9. Path ↔ namespace exact equality across all five projects.
10. `/Modules/CustomerProfile/` solution folder groups exactly the five projects.

## Behavior preservation

| Surface | State |
|---|---|
| Routes / methods / response DTO shape | `UNCHANGED` |
| Session + actor resolution semantics | `UNCHANGED` |
| Domain invariants and derived names | `UNCHANGED` |
| Schema, table, columns, keys | `UNCHANGED` |
| Migration IDs, `Up`/`Down`, EF model identity | `UNCHANGED` |
| DI lifetimes / outbox registration | `UNCHANGED` |
| Seed values / idempotency | `UNCHANGED` |
| Host composition seams | `UNCHANGED` (composition-root import path only) |

## Validation

Full Host suite, W2 tree vs the clean W0 baseline worktree at `39a5de09`:

```
W0 baseline: Failed 85 / Passed 1816 / Skipped 130 / Total 2031
W2 tree:     Failed 83 / Passed 1828 / Skipped 130 / Total 2041
```

Compare-Object by test name: **zero new failures**; two pre-existing stale guards now pass. The
`+10` tests are this wave's new structure guard; `+2` passed are the repaired grouping guards.

## Handoff to W3

`Structure-State = READY_FOR_CERTIFY`, `Folder-Granularity-State = PROFESSIONAL_SHALLOW`,
`SolutionExplorer-State = CANONICAL`, `PathNamespace-State = EXACT`, `PhysicalCopy-State = CLEAN`,
`RootAllowlist-State = ENFORCED`. Certification (`structureCertified: true`, SoT record, Master
Recovery checkpoint, durable cert guard) is owned by W3.
