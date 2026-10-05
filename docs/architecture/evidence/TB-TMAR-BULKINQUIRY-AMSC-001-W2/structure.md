# TB-TMAR-BULKINQUIRY-AMSC-001-W2 — Structure

## Structure-State

`READY_FOR_CERTIFY`

## Physical tree (after W2)

```text
Modules/BulkInquiry/
  Tooba.BulkInquiry.Contracts/
    Errors/            BulkInquiryErrorCodes.cs
                       BulkInquiryErrorCatalogContributor.cs
                       BulkInquiryErrorResourceSet.cs
    Resources/         BulkInquiryErrors.resx
                       BulkInquiryErrors.fa.resx
  Tooba.BulkInquiry.Domain/
    Aggregates/        BulkPurchaseInquiry.cs
    Enums/             BulkInquiryStatus.cs
  Tooba.BulkInquiry.Application/
    Composition/       BulkInquiryOperation.cs
    Models/            BulkInquiryModels.cs
    Ports/             IBulkInquiryDirectory.cs
    Storefront/Commands/  SubmitBulkInquiryCommand.cs
    Validation/        BulkInquiryValidationCodes.cs
                       SubmitBulkInquiryCommandValidator.cs
  Tooba.BulkInquiry.Infrastructure/
    BulkInquiryModule.cs
    Directories/       BulkInquiryDirectory.cs
    Persistence/       BulkInquiryDbContext.cs
                       BulkInquiryOutboxRegistration.cs
      Migrations/      20260826120000_InitialBulkInquiry.cs (+ Designer)
                       20260909130700_DecimalBulkInquiryQuantity.cs
                       BulkInquiryDbContextModelSnapshot.cs
  Tooba.BulkInquiry.Endpoints/
    BulkInquiryEndpointModule.cs
    Storefront/        BulkInquiryStorefrontEndpoints.cs
```

## Classification states

| State | Value | Evidence |
|---|---|---|
| Folder-Granularity-State | `PROFESSIONAL_SHALLOW` | Capability `Storefront/Commands` + shared technical axes `Validation`/`Composition`/`Models`/`Ports`; no use-case leaf folders |
| Solution-Explorer-State | `CANONICAL` | `/Modules/BulkInquiry/` in `Tooba.slnx` with all 5 projects incl. Contracts + Endpoints |
| Path-Namespace-State | `EXACT` | All 17 production `.cs` path-derived namespaces equal declared namespaces |
| Physical-Copy-State | `CLEAN` | Single authoritative home per responsibility; no stale copy |
| Root-Allowlist-State | `ENFORCED` | App/Domain/Contracts root = empty; Infra root = `BulkInquiryModule.cs`; Endpoints root = `BulkInquiryEndpointModule.cs` |
| File-Cohesion-State | `COHESIVE` | Largest production file 128 LOC; no god-file, no over-split |

## Decisions

1. **`Application/Storefront/Validators/` retired in W1** (technical-axis placement + validation-codes
   ownership precedent). W2 verifies `Storefront/Validators` does not return and that the shared
   `Application/Validation/` technical axis is the single home for `BulkInquiryValidationCodes` +
   `SubmitBulkInquiryCommandValidator` (AccessControl / Cart / Fulfillment precedent).
2. **Domain foldering unchanged.** `Aggregates/` + `Enums/` is the canonical single-aggregate Domain
   precedent across certified modules (ProductQnA, Wishlist, Party, Media, AddressBook,
   OperatorProfile, UserPreference). A module-specific `Domain/BulkInquiry/` folder was evaluated and
   rejected: it would be non-canonical, and a per-use-case/capability Domain folder holding one file
   is exactly the over-foldering this skill forbids. Analyze's W0 path proposal is superseded by this
   evidence-backed structure decision.
3. **No physical moves in W2.** The tree already satisfied every gate; W2 adds the durable guard that
   machine-detects future drift.

## Root allowlist / forbidden lists

| Project | rootAllowlist | forbiddenRootFiles | forbiddenTopLevelFolders |
|---|---|---|---|
| Contracts | [] | `BulkInquiryErrorCodes.cs` | — |
| Domain | [] | `BulkPurchaseInquiry.cs` | — |
| Application | [] | `BulkInquiryContracts.cs` | `Commands`, `Queries`, `Validators` |
| Infrastructure | [`BulkInquiryModule.cs`] | `BulkInquiryDirectory.cs`, `BulkInquiryDbContext.cs`, `BulkInquiryOutboxRegistration.cs` | `Migrations` |
| Endpoints | [`BulkInquiryEndpointModule.cs`] | `BulkInquiryStorefrontEndpoints.cs` | `Errors`, `Resources` |

## Durable structure guard

`src/backend/Host/Tooba.Host.Tests/Architecture/BulkInquiryModuleAmsc001W2StructureGuardTests.cs`

- `BulkInquiry_path_namespace_alignment_is_exact`
- `BulkInquiry_is_capability_first_shallow_without_technical_axis_roots_or_use_case_leaf_folders`
- `BulkInquiry_root_allowlists_are_enforced_and_no_stale_copy_remains`
- `BulkInquiry_solution_explorer_grouping_is_canonical`
- `BulkInquiry_domain_references_only_buildingblocks_and_own_contracts`

## Host final closure

Preserved. No new Host production file/folder. Host holds only `ALLOWED_COMPOSITION_ROOT`
(`Program.cs` DI + route map, module list entry, migration registry descriptor).

## Validation

`dotnet test --filter FullyQualifiedName~BulkInquiryModuleAmsc001W2StructureGuard` → see W2 commit validation.

## Structure-Handoff

Hand off to `tooba-architecture-certify` (W3).
