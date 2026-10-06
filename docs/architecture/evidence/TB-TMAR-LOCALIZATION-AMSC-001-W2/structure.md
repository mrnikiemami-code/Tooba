# TB-TMAR-LOCALIZATION-AMSC-001-W2 — Structure (tooba-architecture-structure)

- Task: `TB-TMAR-LOCALIZATION-AMSC-001-W2`
- Parent: `TB-TMAR-LOCALIZATION-AMSC-001-W1` (commit `074fc3a4`)
- Skill: `tooba-architecture-structure`
- Target: `src/backend/Modules/Localization/Tooba.Localization.*`
- Lock: `ARCH-COMPLETE-002`
- Starting HEAD: `074fc3a4`

---

## 1. Physical tree before / after

**Unchanged.** The W0/W1 surface already satisfied every structural gate. No file was moved,
renamed, created or deleted in this wave. The only physical-tree question raised by W0 was three
empty `artifacts/` folders, resolved in §5 below.

```text
Tooba.Localization.Contracts
  Errors/  LanguageErrorCodes.cs  LocalizationErrorCatalogContributor.cs  LocalizationErrorResourceSet.cs
  Ports/   ILanguageActivationPort.cs  ILanguageReferenceGuard.cs  LanguageLookupContracts.cs
  Resources/ LocalizationErrors.resx  LocalizationErrors.fa.resx

Tooba.Localization.Domain
  Aggregates/ Language.cs
  Enums/      LanguageCalendarPolicy.cs  LanguageDirection.cs

Tooba.Localization.Application
  Composition/ LanguageMappings.cs  LocalizationOperation.cs
  Languages/
    Commands/   CreateLanguageCommand.cs  UpdateLanguageCommand.cs  PatchLanguageCommand.cs
    Queries/    ListLanguagesAdminQuery.cs
    Validators/ CreateLanguageCommandValidator.cs  UpdateLanguageCommandValidator.cs
                PatchLanguageCommandValidator.cs  ListLanguagesAdminQueryValidator.cs
                LocalizationValidationCodes.cs
  Models/ LanguageSnapshot.cs
  Ports/  ILanguageDirectory.cs

Tooba.Localization.Infrastructure
  LocalizationModule.cs
  Adapters/    LanguageActivationBridge.cs  LanguageLookupBridge.cs
  Bootstrap/   LanguageBootstrapHostedService.cs
  Languages/   LanguageDirectory.cs
  Persistence/ LocalizationDbContext.cs  LocalizationOutboxRegistration.cs
    Migrations/ 20260901220000_InitialLocalization.cs  .Designer.cs  LocalizationDbContextModelSnapshot.cs

Tooba.Localization.Endpoints
  LocalizationEndpointModule.cs
  Admin/ ILocalizationAdminAuthorizer.cs  LocaleAdminEndpoints.cs
```

## 2. Classification states

| State | Value |
| --- | --- |
| Folder-Granularity-State | `PROFESSIONAL_SHALLOW` |
| Solution-Explorer-State | `CANONICAL` |
| Path-Namespace-State | `EXACT` |
| Physical-Copy-State | `CLEAN` |
| Root-Allowlist-State | `ENFORCED` |
| File-Cohesion-State | `COHESIVE` |
| Structure-State | `READY_FOR_CERTIFY` |

## 3. Capability map

One real business capability: **Languages** (the language/locale registry).

| Layer | Capability placement |
| --- | --- |
| Application | `Languages/{Commands,Queries,Validators}` — capability first, technical axes secondary |
| Endpoints | `Admin/` audience folder — the registry is an admin-only surface |
| Infrastructure | `Languages/` directory, `Adapters/` bridges, `Bootstrap/` seed, `Persistence/` |
| Contracts | `Ports/` cross-module ports, `Errors/` + `Resources/` error boundary |
| Domain | `Aggregates/Language`, `Enums/` |

Genuinely cross-capability Application concerns live in shared `Composition/`, `Models/`, `Ports/` —
the same shape Inventory, CustomerProfile, Cart, Fulfillment, BulkInquiry and AccessControl use.
No capability was invented mechanically.

## 4. Folder-granularity audit

| Check | Result |
| --- | --- |
| Technical-axis-first request tree (`Application/Commands/<UseCase>`, …) | **NONE** |
| Single-file request/use-case leaf folder | **NONE** — `Languages/Commands` (3 files), `Languages/Queries` (1 file), `Languages/Validators` (5 files) are capability-scoped technical folders, not per-use-case leaves |
| Root `.cs` dump | **NONE** |
| Empty ceremonial folder | **NONE** |
| Over-nesting beyond capability → technical axis → files | **NONE** |

`Application/Languages/Queries/` holds one file, but it is a **capability-scoped technical folder**
that is the canonical `<Capability>/Queries` shape used by every certified module, not a folder
created to wrap a single use case. It is not over-foldering.

## 5. Empty `artifacts/` folders — finding withdrawn

W0 flagged three empty `artifacts/` folders under `Application/`, `Domain/` and `Infrastructure/`
as candidate structure debt. W2 withdraws that finding after repository-wide verification:

- `git ls-files | grep '/artifacts/'` returns **zero** tracked files — the folders are untracked and
  git-ignored scratch space, not product source.
- The same `artifacts/` folder exists in **~70 projects** across the repository, including certified
  modules (Offer, Order, Payment, AccessControl, Cart, Content, Inventory, Media, …).

They are therefore a repository-wide convention, not Localization-specific structure debt. Removing
only Localization's copies would be an inconsistent local mutation outside the structure mandate, and
they cannot affect `rootAllowlist` (which enumerates `*.cs` files) or the Solution Explorer (which
enumerates projects). **No change made.**

## 6. Root allowlist verification (manifest ↔ disk)

| Project | Manifest `rootAllowlist` | Disk root `*.cs` | Match |
| --- | --- | --- | --- |
| `Tooba.Localization.Contracts` | `[]` | none | ✓ |
| `Tooba.Localization.Domain` | `[]` | none | ✓ |
| `Tooba.Localization.Application` | `[]` | none | ✓ |
| `Tooba.Localization.Endpoints` | `["LocalizationEndpointModule.cs"]` | `LocalizationEndpointModule.cs` | ✓ |
| `Tooba.Localization.Infrastructure` | `["LocalizationModule.cs"]` | `LocalizationModule.cs` | ✓ |

Forbidden root files / forbidden top-level folders from the existing manifest entry are all absent:

- `Application`: `LanguageContracts.cs` absent; no `Commands`/`Queries` top-level folders.
- `Domain`: `Language.cs` absent at root.
- `Endpoints`: `LocaleAdminEndpoints.cs` absent at root; no `Errors` top-level folder.
- `Infrastructure`: `LanguageDirectory.cs`, `LanguageLookupBridge.cs`, `LanguageActivationBridge.cs`,
  `LanguageBootstrapHostedService.cs`, `LocalizationDbContext.cs` all absent at root; no
  `Migrations` top-level folder (migrations live under `Persistence/Migrations/`).
- `Contracts`: `LanguageErrorCodes.cs`, `LanguageLookupContracts.cs`, `ILanguageReferenceGuard.cs`,
  `ILanguageActivationPort.cs` all absent at root.

## 7. Path ↔ namespace exactness

Verified for all 33 production `.cs` files across the five projects by deriving the expected
namespace from the physical path and comparing against the declared namespace.

**Result: NONE mismatched.** (Note: two `Domain/Enums/*.cs` files begin with a UTF-8 BOM; any
verifier must strip it before matching `^namespace`.)

No namespace alias workaround, no `TypeForwardedTo`, no duplicate physical copy, no stale root copy.

## 8. Solution Explorer

`src/backend/Tooba.slnx` contains:

```xml
<Folder Name="/Modules/Localization/">
  <Project Path="Modules/Localization/Tooba.Localization.Domain/Tooba.Localization.Domain.csproj" />
  <Project Path="Modules/Localization/Tooba.Localization.Contracts/Tooba.Localization.Contracts.csproj" />
  <Project Path="Modules/Localization/Tooba.Localization.Application/Tooba.Localization.Application.csproj" />
  <Project Path="Modules/Localization/Tooba.Localization.Infrastructure/Tooba.Localization.Infrastructure.csproj" />
  <Project Path="Modules/Localization/Tooba.Localization.Endpoints/Tooba.Localization.Endpoints.csproj" />
</Folder>
```

All five projects grouped under `/Modules/Localization/`; Endpoints present; entries resolve to real
on-disk `.csproj` paths; no decorative or stale entry. `Solution-Explorer-State = CANONICAL`.

## 9. Manifest interaction

The existing `modules[]` entry for Localization already declares `structureCertified: true`,
`lockVersion: ARCH-COMPLETE-002`, and accurate `rootAllowlist` / `forbiddenRootFiles` /
`forbiddenTopLevelFolders` for all five projects. It was **re-verified against disk** in §6 and
matches exactly. No manifest edit was required or made in this wave — the wave's job is structural
truth, and the structure is already true. Promotion/annotation is Certify's mandate.

No `preCertModules` entry was created: Localization is already a `modules[]` member, so adding a
duplicate pre-cert entry would create the exact duplicate-entry defect the standard forbids.

## 10. Cohesion balance

| File | LOC | Assessment |
| --- | --- | --- |
| `Infrastructure/Languages/LanguageDirectory.cs` | 231 | cohesive single responsibility |
| `Domain/Aggregates/Language.cs` | 120 | cohesive |
| `Endpoints/Admin/LocaleAdminEndpoints.cs` | 107 | cohesive transport |
| `Application/Composition/LanguageMappings.cs` | 76 | cohesive |
| `Application/Models/LanguageSnapshot.cs` | 69 | 7 related record shapes in one capability file — acceptable |
| `Persistence/Migrations/LocalizationDbContextModelSnapshot.cs` | 155 | EF-generated |

No file above the 800 LOC `ARCH-SIZE-001` ceiling; no Localization entry in
`Baselines/tmar-source-size-baseline.json`; no cosmetic over-split. `COHESIVE`.

## 11. Durable structure guard

`src/backend/Host/Tooba.Host.Tests/Architecture/LocalizationModuleAmsc001W2StructureGuardTests.cs`
— 9 facts locking: capability-first Application, no single-file request leaf folder, root allowlists
and forbidden roots, Contracts boundary-only semantics, Endpoints not referencing Domain or
Infrastructure, path↔namespace exactness across all five projects, `/Modules/Localization/` solution
grouping with all five projects, migration placement under `Persistence/Migrations/`, and the absence
of alias / `TypeForwardedTo` shims.

Shared cross-capability Application buckets (`Composition/`, `Models/`, `Ports/`) are explicitly
allowed by the guard, matching the certified Inventory/CustomerProfile/AddressBook/BulkInquiry/
AccessControl precedent; only request trees (`Commands/`, `Queries/`, `Validators/`) are forbidden as
a top-level Application axis.

## 12. Focused validation

`dotnet test --filter FullyQualifiedName~LocalizationModuleAmsc001` —
**16 passed / 0 failed** (7 W1 migrate facts + 9 W2 structure facts).

## 13. Host final closure

Preserved. No Host production file was added, moved or changed in this wave.
`HOST_FINAL_CLOSURE_REGRESSION = NONE`.

## 14. Verdict

`Structure-State = READY_FOR_CERTIFY`
`Structure-Handoff-State = READY_FOR_CERTIFY`
