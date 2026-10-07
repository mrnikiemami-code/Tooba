# TB-TMAR-PRODUCTQNA-AMSC-001-W2 — Structure

## Structure-State

`READY_FOR_CERTIFY`

Baseline: `branch = main`, `HEAD == origin/main == b29340de` (W1 Migrate). Zero production `.cs`
moved in W2 — the tree already satisfied every gate after W1's `Application/Validation/`
consolidation; W2 independently verifies it, reconciles the manifest allowlists honestly and adds the
durable machine guard that prevents future drift.

## Physical tree (after W2)

```text
Modules/ProductQnA/
  Tooba.ProductQnA.Contracts/
    Errors/                ProductQnAErrorCodes.cs
                           ProductQnAErrorCatalogContributor.cs
                           ProductQnAErrorResourceSet.cs
    Resources/             ProductQnAErrors.resx
                           ProductQnAErrors.fa.resx
  Tooba.ProductQnA.Domain/
    Aggregates/            ProductQuestion.cs
                           ProductAnswer.cs
    Enums/                 ProductQuestionStatus.cs
                           ProductAnswerStatus.cs
  Tooba.ProductQnA.Application/
    Composition/           ProductQnAOperation.cs
    Customer/Commands/     SubmitProductQuestionCommand.cs
    Storefront/Queries/    GetPublishedQuestionsQuery.cs
    Models/                ProductQaModels.cs
    Ports/                 IProductQaDirectory.cs
    Validation/            ProductQnAValidationCodes.cs
                           SubmitProductQuestionCommandValidator.cs
                           GetPublishedQuestionsQueryValidator.cs
  Tooba.ProductQnA.Infrastructure/
    ProductQnAModule.cs
    Directories/           ProductQaDirectory.cs
    Development/           ProductQnADevelopmentSeed.cs
    Persistence/           ProductQnADbContext.cs
                           ProductQnAOutboxRegistration.cs
      Migrations/          20260826120000_InitialProductQnA.cs (+ Designer)
                           ProductQnADbContextModelSnapshot.cs
  Tooba.ProductQnA.Endpoints/
    ProductQnAEndpointModule.cs
    Customer/              ProductQnACustomerEndpoints.cs
                           ProductQnACustomerActorResolver.cs
    Storefront/            ProductQnAStorefrontEndpoints.cs
```

## Classification states

| State | Value | Evidence |
|---|---|---|
| Module-Applicability | `HTTP_OWNING` | 2 real module-owned routes (`GET /v1/storefront/products/{slug}/questions`, `POST /v1/customer/product-questions`) with real CQRS use cases — Endpoints project is not ceremonial |
| Folder-Granularity-State | `PROFESSIONAL_SHALLOW` | Two capabilities (`Customer/Commands`, `Storefront/Queries`) + shared technical axes (`Validation`/`Composition`/`Models`/`Ports`); no use-case leaf folders |
| Solution-Explorer-State | `CANONICAL` | `/Modules/ProductQnA/` in `Tooba.slnx` with exactly 5 projects incl. Contracts + Endpoints |
| Path-Namespace-State | `EXACT` | All production `.cs` path-derived namespaces equal declared namespaces (only EF `Persistence/Migrations` carries the repository-locked `...Infrastructure.Persistence` exemption) |
| Physical-Copy-State | `CLEAN` | One authoritative home per responsibility; no stale copy; no duplicate copy |
| Root-Allowlist-State | `ENFORCED` | App/Domain/Contracts root = empty; Infra root = `ProductQnAModule.cs`; Endpoints root = `ProductQnAEndpointModule.cs` |
| File-Cohesion-State | `COHESIVE` | Largest production file 108 LOC (`Infrastructure/Directories/ProductQaDirectory.cs`); no god-file, no over-split |
| Host-Final-Closure-State | `PRESERVED` | No new Host production file/folder; Host holds only `ALLOWED_COMPOSITION_ROOT` |

## Capability map

| Capability | Application | Endpoints | Route |
|---|---|---|---|
| Customer question submission | `Application/Customer/Commands/SubmitProductQuestionCommand.cs` | `Endpoints/Customer/ProductQnACustomerEndpoints.cs` (+ `ProductQnACustomerActorResolver.cs` session seam) | `POST /v1/customer/product-questions` |
| Storefront published questions | `Application/Storefront/Queries/GetPublishedQuestionsQuery.cs` | `Endpoints/Storefront/ProductQnAStorefrontEndpoints.cs` | `GET /v1/storefront/products/{slug}/questions` |

Shared, non-capability concerns live once at the Application root: `Validation/` (transport
validators + `ProductQnAValidationCodes`), `Composition/` (typed-fault → `Result` seam),
`Models/` (request/result/page models), `Ports/` (`IProductQaDirectory`).

## Decisions

1. **`Application/Validation/` is the single technical axis for transport validation.** W1 retired
   `Customer/Validators` + `Storefront/Validators` (validation-codes ownership precedent:
   AccessControl, Cart, BulkInquiry, Fulfillment, Offer, Order, Payment). W2 verifies neither retired
   folder returns and that the shared `Validation/` folder holds all three files. This is the
   repository-consistent choice for a module whose two validators are cross-capability shared
   transport guards; duplicating them per capability would be folder explosion, not cohesion.
2. **`Models/` + `Ports/` stay shared at the Application root.** Both are single-file shared concerns
   (`ProductQaModels.cs`, `IProductQaDirectory.cs`); they are not capability-specific, so duplicating
   them under each capability would be non-canonical.
3. **Domain foldering unchanged.** `Aggregates/` + `Enums/` is the canonical single-capability Domain
   precedent across certified modules (BulkInquiry, Wishlist, Party, Media, AddressBook,
   OperatorProfile, UserPreference). A module-specific `Domain/ProductQnA/` folder was evaluated and
   rejected: it would be non-canonical, and a capability folder holding the whole Domain is exactly
   the over-foldering this skill forbids. W0's illustrative path proposal is superseded by this
   evidence-backed structure decision.
4. **No physical moves in W2.** Every gate already held; W2 adds the durable guard that
   machine-detects future drift and reconciles the manifest honestly.

## Root allowlist / forbidden lists

| Project | rootAllowlist | forbiddenRootFiles | forbiddenTopLevelFolders |
|---|---|---|---|
| Contracts | `[]` | `ProductQnAErrorCodes.cs` | — |
| Domain | `[]` | `ProductQuestion.cs` | — |
| Application | `[]` | `ProductQaContracts.cs` | `Commands`, `Queries`, `Validators` |
| Infrastructure | `[ProductQnAModule.cs]` | `ProductQaDirectory.cs`, `ProductQnADevelopmentSeed.cs`, `ProductQnADbContext.cs`, `ProductQnAOutboxRegistration.cs` | `Migrations` |
| Endpoints | `[ProductQnAEndpointModule.cs]` | `ProductQnACustomerEndpoints.cs`, `ProductQnAStorefrontEndpoints.cs` | `Errors`, `Resources` |

The only manifest change is honesty reconciliation: `Application.forbiddenTopLevelFolders` gains
`Validators` (it was the missing member of the `Commands`/`Queries` technical-axis triplet) and the
certification note is refreshed. No allowlist was widened and no forbidden entry was removed.

## Durable structure guard

`src/backend/Host/Tooba.Host.Tests/Architecture/ProductQnAModuleAmsc001W2StructureGuardTests.cs`

- `ProductQnA_path_namespace_alignment_is_exact`
- `ProductQnA_is_capability_first_shallow_without_technical_axis_roots_or_use_case_leaf_folders`
- `ProductQnA_root_allowlists_are_enforced_and_no_stale_copy_remains`
- `ProductQnA_solution_explorer_grouping_is_canonical`
- `ProductQnA_domain_references_only_buildingblocks_and_own_contracts`

The pre-existing `ProductQnAModuleAmcW2StructureGuardTests` / `...AmcW3CqrsGuardTests` /
`...AmcW4CertGuardTests` were repointed (never weakened) in W1 and remain part of the gate.

## Host final closure

Preserved. No new Host production file/folder. Host holds only `ALLOWED_COMPOSITION_ROOT`:
`Program.cs` DI + route map, `Composition/ToobaModuleComposition.cs` module-list entry,
`Tooba.MigrationRunner/ModuleMigrationRegistry.cs` schema descriptor.

## Stale / duplicate copy

None. `Application/Customer/Validators`, `Application/Storefront/Validators` and
`Infrastructure/Migrations` do not exist; no responsibility has two live homes; `Tooba.slnx` points
only at existing project paths.

## Validation

```text
dotnet test Tooba.Host.Tests --filter FullyQualifiedName~ProductQnAModuleAmsc001W2StructureGuardTests
  Passed: 5, Failed: 0, Skipped: 0
```

Known pre-existing repository failures unrelated to ProductQnA and untouched by this wave:
`TmarCompleteReferenceStructureGateTests.Certified_modules_satisfy_root_allowlists_and_namespace_alignment`
(`Tooba.Catalog.Contracts/Cart` project-level namespace boundary-Contracts aggregation),
`TmarDurableGuardTests.Recovery_current_state_is_fresh_and_machine_readable` and
`TmarDurableGuardTests.Recovery_sot_sync_001_current_checkpoint_is_unique_and_stop_is_authoritative`.
All three were reproduced identically at the W1 baseline `b29340de` with ProductQnA changes stashed,
so they are pre-existing and out of scope.

## Structure-Handoff

`Structure-State = READY_FOR_CERTIFY` → hand off to `tooba-architecture-certify` (W3).
