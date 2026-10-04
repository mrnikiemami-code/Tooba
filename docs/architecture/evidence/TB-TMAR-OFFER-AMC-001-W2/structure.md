# TB-TMAR-OFFER-AMC-001 — W2 STRUCTURE

- Task: `TB-TMAR-OFFER-AMC-001` (ARCHITECT_DIRECT_AMSC)
- Module: `Modules/Offer`
- Skill: `Structure` (third of four)
- Starting base: `77f10ed2` (`HEAD == origin/main` after W1)
- Production code changes in this wave: `ZERO` (manifest + guards + evidence only)

## 1. Physical structure verified

### `Tooba.Offer.Application` — capability-first (PROFESSIONAL_SHALLOW)

```text
Tooba.Offer.Application/
  Offers/
    Commands/{ActivateOffer, ArchiveOffer, CreateOffer, SetOfferInventory,
              SetOfferPrice, SetOrderQuantityLimits, SetReturnPolicy, SuspendOffer, UpdateOffer}/
    Queries/{GetOffer, ListSellerOffers}/
    Mappings/   Policies/   Ports/   ReadModels/
  Validation/
```

- root `.cs` files: **none**
- technical-axis-first roots: **none** (`Commands`/`Queries`/`Mappings`/`Ports`/`Policies`/`ReadModels`/`Validators`/`Dtos`/`UseCases`/`ReturnPolicy` all absent)
- single-file leaf folders are **request capability folders** (`CreateOffer/`, `UpdateOffer/`, `SetOfferPrice/`, …), each holding a real request type — not bare leaf sinks. Validators are co-located with their request.

### `Tooba.Offer.Contracts`

```text
Tooba.Offer.Contracts/
  Dtos/   Errors/   Ports/   ReturnPolicy/
```

- root `.cs` files: **none**
- `ReturnPolicy/` holds the boundary vocabulary (`ReturnPolicyOptions`, `OfferReturnPolicyChoices`, `ResolvedReturnPolicy`, `IReturnPolicyResolver`) plus the single Contracts-only `ReturnPolicyResolver` default. Rationale in W1 §2.

### `Tooba.Offer.Domain`

```text
Tooba.Offer.Domain/
  Aggregates/   Events/   ValueObjects/   Errors/
```

- root `.cs` files: **none**; no empty ceremonial folder.

### `Tooba.Offer.Infrastructure`

```text
Tooba.Offer.Infrastructure/
  Persistence/   Adapters/   Adapters/Tracing/   Events/   Outbox/   DependencyInjection/
```

- root `.cs` files: **none**; `Repositories/` is absent (adapters live under `Adapters/`).

### `Tooba.Offer.Endpoints`

```text
Tooba.Offer.Endpoints/
  Seller/   Errors/   Resources/   OfferEndpointModule.cs
```

- root allowlist: exactly `OfferEndpointModule.cs` (the composition seam).
- `Admin/` and `Storefront/` are absent: Offer is a seller-owned surface only.

## 2. Visual Studio solution grouping

`src/backend/Tooba.slnx` → `<Folder Name="/Modules/Offer/">` contains all six Offer projects exactly once:

```text
Modules/Offer/Tooba.Offer.Application/Tooba.Offer.Application.csproj
Modules/Offer/Tooba.Offer.Contracts/Tooba.Offer.Contracts.csproj
Modules/Offer/Tooba.Offer.Domain/Tooba.Offer.Domain.csproj
Modules/Offer/Tooba.Offer.Endpoints/Tooba.Offer.Endpoints.csproj
Modules/Offer/Tooba.Offer.Infrastructure/Tooba.Offer.Infrastructure.csproj
Modules/Offer/Tooba.Offer.Tests/Tooba.Offer.Tests.csproj
```

No Offer project appears anywhere else in the solution.

## 3. Path ↔ namespace alignment

Every production `.cs` file's namespace equals its project-relative folder path exactly
(`Tooba.Offer.<Project>[.<Folder>...]`). Enforced by
`OfferPhysicalStructureGuardTests.ARCH_MODULE_PHYSICAL_001_namespaces_equal_path_derived_namespaces_exactly`
and by the new reconciliation guard. Migration files (`Persistence/Migrations/*`) keep their generated
block-scoped namespaces and are excluded from the single-line namespace check.

## 4. Manifest promotion

`docs/architecture/tmar-module-structure-manifests.json` → Offer entry:

| Field | Before (W1) | After (W2) |
| --- | --- | --- |
| `structureCertified` | `true` | `true` (unchanged) |
| `lockVersion` | `ARCH-COMPLETE-002` | `ARCH-COMPLETE-002` (unchanged) |
| project entries | **3** | **5** |
| `Tooba.Offer.Domain` | absent | present, `rootAllowlist: []` |
| `Tooba.Offer.Contracts` | absent | present, `rootAllowlist: []` |
| `Tooba.Offer.Application.forbiddenTopLevelFolders` | `[]` | 10 technical-axis-first folders |
| `Tooba.Offer.Endpoints.forbiddenTopLevelFolders` | `[]` | `Admin`, `Storefront` |
| `Tooba.Offer.Infrastructure.forbiddenTopLevelFolders` | `[]` | `Repositories` |
| every pre-existing `forbiddenRootFiles` entry | present | preserved |

No existing restriction was weakened or removed.

## 5. Durable guards added

| Guard | Fails when |
| --- | --- |
| `OfferPhysicalStructureGuardTests.ARCH_MODULE_PHYSICAL_001_no_technical_axis_first_application_folders` | any of `Commands/Queries/Mappings/Ports/Policies/ReadModels/Validators/Dtos/UseCases/ReturnPolicy` reappears at the `Offer.Application` root |
| `OfferManifestDiskReconciliationGuardTests.Offer_manifest_has_exactly_one_entry_certified_under_arch_complete_002` | Offer entry count ≠ 1, `structureCertified != true`, or `lockVersion != ARCH-COMPLETE-002` |
| `...Offer_manifest_represents_exactly_the_five_production_projects` | a production project is missing from the manifest, an extra one is represented, or the declared set ≠ the real on-disk `Tooba.Offer.*` directories (excluding `*.Tests`) |
| `...Offer_manifest_root_allowlists_equal_real_disk_root_cs_files` | any project's `rootAllowlist` ≠ the actual top-level production `.cs` files in that directory |
| `...Offer_manifest_forbidden_top_level_folders_are_absent_on_disk` | a declared forbidden folder or root file exists on disk |
| `...Offer_projects_are_grouped_exactly_once_under_Modules_Offer_in_slnx` | the `/Modules/Offer/` folder block ≠ the six projects, or any Offer project appears twice |

All six read the real manifest / real directories / real solution file. None hard-codes a PASS that is
independent of disk state.

## 6. Stale / duplicate copy check

`git status` under `Modules/Offer` shows **no** leftover legacy file, no `*.old`/`*.bak`/duplicate copy, and
no empty ceremonial folder. All W1 `git mv` source directories are gone from disk.

## 7. Structure handoff

```text
Folder-Granularity-State = PROFESSIONAL_SHALLOW
Solution-Explorer-State  = CANONICAL
Path-Namespace-State     = EXACT
Manifest-State           = FIVE_PROJECTS_ROOT_ALLOWLISTS_EXACT
Structure-Handoff-State  = READY_FOR_CERTIFY
```
