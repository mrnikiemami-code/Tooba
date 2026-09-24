# TB-TMAR-OFFER-ARCH-COMPLETE-002-STRUCTURE-001 — Offer structure certification

Task: `TB-TMAR-OFFER-ARCH-COMPLETE-002-STRUCTURE-001`
Parent: `TB-TMAR-OFFER-HOST-RESIDUE-REPAIR-001` (ARCHITECT-ACCEPTED at `b9008efdfcdb0057b45357c002861aff90e03df4`)
Channel: `tooba-main` · Mode: `BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE` · Track: `OFFER_ARCH_COMPLETE_002_STRUCTURE`

## Parent acceptance

The Architect accepted `TB-TMAR-OFFER-HOST-RESIDUE-REPAIR-001` on `b9008efd`:

- `StorefrontPrimaryOfferResolver.cs` is gone from Host.
- Offer selection is Offer-owned: `Tooba.Offer.Contracts/Ports/IPrimaryOfferSelectionPolicy.cs` + `Tooba.Offer.Contracts/Dtos/OfferSelectionCandidate.cs`,
  implemented by `Tooba.Offer.Application/Policies/PrimaryOfferSelectionPolicy.cs` and registered Singleton by `OfferModule`.
- `StorefrontComposer` consumes only `Tooba.Offer.Contracts.*` for that policy.
- `OfferGlobalUsings.cs` is gone and the Host temp residue `.tmp-t014-test-out/` is gone.
- `HostOfferSellerAuthorizer` remains a thin Host security adapter.
- Offer was **not** yet ARCH-COMPLETE-002 structure-certified. This task closes that.

## A. Exact path ↔ namespace repair

The defective file `src/backend/Modules/Offer/Tooba.Offer.Contracts/Errors/OfferErrorCodes.cs` declared
`namespace Tooba.Offer.Contracts;` while living under `Errors/`.

- Changed to `namespace Tooba.Offer.Contracts.Errors;` (type name `OfferErrorCodes` unchanged).
- The two layers stay separate: `Tooba.Offer.Contracts.Errors.OfferErrorCodes` = stable cross-boundary semantic codes;
  `Tooba.Offer.Domain.Errors.OfferErrorCodes` = domain-internal codes. They were **not** merged.
- All Offer production/test consumers were repointed to the exact `Tooba.Offer.Contracts.Errors` namespace, and the
  now-loose `using Tooba.Offer.Contracts;` was removed from every file where nothing else from that namespace was needed:
  `ReturnPolicyContracts`, `OfferErrorCatalogContributor`, `OfferSellerEndpoints`, `OfferReadModelComposer`,
  `CreateOfferCommand` / `UpdateOfferCommand`, `OfferStore`, `OfferDirectoryTestHelper`,
  `OfferReferenceShapeTests`, `OfferHandlerTests`, `OfferErrorCatalogAndLocalizationTests`, `SellerOfferInvariantTests`.
- The one file that needs **both** layers is `src/backend/Modules/Offer/Tooba.Offer.Domain/Aggregates/SellerOffer.cs`:
  it keeps a single explicit `using Tooba.Offer.Domain.Errors;` and names the Domain type unqualified, so no
  fully-qualified site and no alias were required. **No project-wide/global alias and no namespace workaround exists.**
- Because type names remain identical per layer, no file-scoped alias was added anywhere; the namespace split alone removes the ambiguity.

Two further exact path↔namespace defects were repaired so the tightened guard can demand equality, not `StartsWith`:

| File | Before | After |
| --- | --- | --- |
| `Tooba.Offer.Application/Mappings/OfferContractMapping.cs` | `Tooba.Offer.Application` | `Tooba.Offer.Application.Mappings` |
| `Tooba.Offer.Infrastructure/Adapters/Tracing/OfferModuleCallGateways.cs` | `Tooba.Offer.Infrastructure.Adapters` | `Tooba.Offer.Infrastructure.Adapters.Tracing` |
| `Tooba.Offer.Infrastructure/Adapters/Tracing/OfferModuleCallTracingRegistration.cs` | `Tooba.Offer.Infrastructure.Adapters` | `Tooba.Offer.Infrastructure.Adapters.Tracing` |

Consumers of the moved `Mappings` namespace (`Activate/Suspend/Archive/SetOrderQuantityLimits/SetReturnPolicy`, `OfferStore`)
and of the moved `Tracing` namespace (`Program.cs`, `OfferModule`, `OfferTraceTopologyTests`) received the explicit using.

## B. Validators capability

New folder `src/backend/Modules/Offer/Tooba.Offer.Application/Validators`, namespace `Tooba.Offer.Application.Validators`,
using FluentValidation (already referenced transitively through `Tooba.BuildingBlocks`, FluentValidation 11.11.0):

- `OfferValidationCodes.cs` — stable machine codes (`offer.validation.*`), never localized identity.
- `OfferFluentRules.cs` — reusable primitive-shape fragments.
- `GetOfferQueryValidator.cs`
- `CreateOfferCommandValidator.cs`
- `UpdateOfferCommandValidator.cs`
- `SetOfferPriceCommandValidator.cs`
- `SetOfferInventoryCommandValidator.cs`

Exactly **five** validators. **No validator for `ListSellerOffersQuery`.**

### Endpoint request inventory (exactly six)

| Endpoint-reachable request | Classification | Validator |
| --- | --- | --- |
| `ListSellerOffersQuery` | `NO_VALIDATOR_REQUIRED_AUTH_SCOPED_QUERY` | none (by design) |
| `GetOfferQuery` | `VALIDATOR_REQUIRED` | `GetOfferQueryValidator` |
| `CreateOfferCommand` | `VALIDATOR_REQUIRED` | `CreateOfferCommandValidator` |
| `UpdateOfferCommand` | `VALIDATOR_REQUIRED` | `UpdateOfferCommandValidator` |
| `SetOfferPriceCommand` | `VALIDATOR_REQUIRED` | `SetOfferPriceCommandValidator` |
| `SetOfferInventoryCommand` | `VALIDATOR_REQUIRED` | `SetOfferInventoryCommandValidator` |

### NO_VALIDATOR_REQUIRED rationale

`SellerPartyId` for `ListSellerOffersQuery` is supplied by the trusted seller authorization boundary
(`IOfferSellerAuthorizer.RequireAuthorizedAsync`) and not from an arbitrary request payload, so there is no
untrusted transport input to shape. The endpoint passes `sellerId` from the authorizer:
`new ListSellerOffersQuery(sellerId)`. The guard also asserts the query record itself carries no payload-supplied seller value.

### Rule scope

Only transport shape is validated: required ids, defined contract enum, optional-status membership in the create/patch sets,
supported return-policy choice, `>= 1` custom return window, positive min/max and min ≤ max, amount `>= 0`, 3-character currency shape,
non-blank market/reason, non-blank seller SKU. Nothing here duplicates DB uniqueness, seller existence, catalog existence,
return-policy governance, or domain business rules. No numeric reason limit was invented: Inventory's contract/domain expose no
canonical adjustment-reason length, so `Reason` only gets a non-blank shape rule. For `SellerSku`, the persistence limit is not duplicated
(shape only).

## C. Validator discovery

No bespoke manual validation call was added to any endpoint or handler, and no endpoint/handler invokes a validator directly.
Discovery is the existing `Tooba.BuildingBlocks` `ValidationBehavior<,>` registered by `AddToobaCqrsFoundation(...)`, which calls
`AddValidatorsFromAssembly(assembly)` for each module assembly. `Tooba.Offer.Application` is registered in Host `Program.cs`
(`typeof(Tooba.Offer.Application.Commands.CreateOffer.CreateOfferCommand).Assembly`). MediatR remains **12.5.0**.
Endpoints remain `ISender`-only with `ApiResponseFactory api` and no Application service/Directory invocation.

## D. Physical structure guard tightening

`src/backend/Modules/Offer/Tooba.Offer.Tests/Architecture/OfferPhysicalStructureGuardTests.cs`:

- The former loose `StartsWith` namespace test was replaced by `ARCH_MODULE_PHYSICAL_001_namespaces_equal_path_derived_namespaces_exactly`:
  exact path-derived namespace equality for every production `.cs` of Domain/Application/Contracts/Infrastructure/Endpoints.
- Added `ARCH_MODULE_PHYSICAL_001_contracts_and_validator_folders_are_namespace_exact` covering
  `Contracts/Dtos → Tooba.Offer.Contracts.Dtos`, `Contracts/Ports → Tooba.Offer.Contracts.Ports`,
  `Contracts/Errors → Tooba.Offer.Contracts.Errors`, `Application/Validators → Tooba.Offer.Application.Validators`.
- EF migrations and `OfferDbContextModelSnapshot.cs` remain the only exception (generated block-scoped namespaces).
- Existing root-dump, approved-folder and empty-ceremonial-folder checks were preserved (not weakened).
- Added `ARCH_MODULE_PHYSICAL_001_no_alias_workaround_in_production_sources` failing on any `global using` in Offer production and on
  any Host Offer alias (`global using Tooba.Offer…` / `global using Offer…`).

The guard therefore fails on a wrong namespace for a physical folder, flat-root dumping, an unapproved top-level folder, and an alias workaround.
Contracts and Validators are included in the checks.

## E. Validator coverage guard

New `src/backend/Modules/Offer/Tooba.Offer.Tests/Architecture/OfferEndpointValidatorCoverageGuardTests.cs` proves:

- exactly **6** endpoint-reachable requests, manifested exactly once, and matching the `new *Command/*Query` set actually constructed in `Tooba.Offer.Endpoints`;
- **5** `VALIDATOR_REQUIRED` requests resolve a concrete `IValidator<T>` through `AddToobaCqrsFoundation`;
- the single `NO_VALIDATOR_REQUIRED_AUTH_SCOPED_QUERY` request has **no** validator registered;
- all six are real `MediatR.IRequest<>` and endpoints use `ISender`;
- no endpoint direct Application service/Directory invocation;
- MediatR package/version remains `12.5.0`;
- the auth-scoped rationale and `RequireAuthorizedAsync` origin are asserted.

No representative substitution: the inventory is exhaustive against the endpoint sources.

## F. Structure manifest

`docs/architecture/tmar-module-structure-manifests.json` adds Offer as `structureCertified: true`, `lockVersion: ARCH-COMPLETE-002`:

- `Tooba.Offer.Application` — `rootAllowlist: []`; forbidden root files `OfferContractMapping.cs`, `IOfferStore.cs`,
  `OfferReadModelComposer.cs`, `OfferRequests.cs`, `OfferHandlers.cs`, `OfferQueries.cs`, `OfferQueryHandlers.cs`; no forbidden folders.
  Approved capability folders (already used): `Commands`, `Queries`, `Mappings`, `Ports`, `ReadModels`, `Policies`, `Validators`.
- `Tooba.Offer.Endpoints` — `rootAllowlist: ["OfferEndpointModule.cs"]`; forbidden root files `OfferSellerEndpoints.cs`,
  `IOfferSellerAuthorizer.cs`, `OfferErrorCatalogContributor.cs`, `OfferErrorResources.cs`, `OfferEndpointLocalizer.cs`.
- `Tooba.Offer.Infrastructure` — `rootAllowlist: []`; forbidden root files `OfferModule.cs`, `OfferDbContext.cs`, `OfferStore.cs`,
  `OfferEvents.cs`, `OfferModuleMigration.cs`, `OfferSchemaMigrator.cs`, `OfferDevelopmentSeedGateway.cs`, `OfferOutboxRegistration.cs`.

Offer was removed from `uncertifiedHttpOwningModules` (now: Settlement, Fulfillment, Returns, Notification, Support, Wallet, Payment, Promotion).
No other module manifest entry was altered.

## G. SoT / high-level closure

- `docs/architecture/tmar-current-state.json`: `lastAcceptedTask = TB-TMAR-OFFER-HOST-RESIDUE-REPAIR-001`,
  `lastAcceptedCommit = b9008efdfcdb0057b45357c002861aff90e03df4`, `nextTask = USER_REVIEW_OFFER_ARCH_COMPLETE_002_STRUCTURE_001`,
  `nextTaskGate = NEXT_TMAR_WAVE_AFTER_OFFER_STRUCTURE_CERTIFICATION`; new `offerArchComplete002Structure` block records
  state `COMPLETE_REFERENCE_PATTERN`, `httpApplicability HTTP_OWNING`, `endpointOwnership MODULE_ENDPOINTS`, `cqrs MEDIATR_12_5`,
  `structureCertifiedUnderArchComplete002 true`, `hostResidue BUSINESS_RESIDUE_REMOVED_THIN_SECURITY_ADAPTER_ONLY`,
  `validatorCoverage 5_REQUIRED_PRESENT_1_NO_VALIDATOR_REQUIRED`, `pathNamespace EXACT`,
  `selectionOwner OFFER_CONTRACT_PORT_APPLICATION_POLICY`; `completeReferenceModules.Offer` carries the structure certification fields;
  `structureLock.certifiedModules = Order, Cart, StoreContext, Offer`.
- `TOOBA-TMAR-MASTER-RECOVERY.md` and `TOOBA-ARCHITECT-BOOTSTRAP.md` record the certification and the new next task/gate.
- `TmarDurableGuardTests` and `TmarCompleteReferenceStructureGateTests` assert the same certified set, the Offer SoT coherence and the manifest agreement.
- Checkout remains `PAUSED_AT_SAFE_W5_CHECKPOINT`; `frontendFrozen = true`. Payment was not touched; Checkout was not resumed; no frontend change.

## Host residue remains closed

- `StorefrontPrimaryOfferResolver.cs` absent; `OfferGlobalUsings.cs` absent; `.tmp-t014-test-out/` absent (already asserted by the residue guard).
- No Host source contains the old ordering predicate or a global Offer alias.
- `StorefrontComposer` uses the `Tooba.Offer.Contracts.Ports` port, never the Application implementation.
- `HostOfferSellerAuthorizer` remains the thin Host security adapter.
- No Offer production project references `Tooba.Host`.

## Certification state

Offer = `COMPLETE_REFERENCE_PATTERN` / `HTTP_OWNING` / `MODULE_ENDPOINTS` / `MEDIATR_12_5` + `ARCH-COMPLETE-002 STRUCTURE_CERTIFIED`.
Cart / Order / StoreContext certifications unchanged. Offer not advanced to Payment.

## Validation

- `dotnet build src/backend/Tooba.slnx` — **Build succeeded, 0 errors**.
- `dotnet test src/backend/Modules/Offer/Tooba.Offer.Tests` — **79 passed, 0 failed** (includes `OfferArchitectureGuardTests`,
  `OfferPhysicalStructureGuardTests`, the new `OfferEndpointValidatorCoverageGuardTests`, `PrimaryOfferSelectionPolicyTests`).
- `dotnet test Tooba.Host.Tests --filter TmarCompleteReferenceStructureGateTests|TmarDurableGuardTests|OfferFoundationTests|StorefrontCompositionTests|HostModuleEndpointOwnershipTests|HostFolderStructureTests|HostCartResidualGuardTests` — **45 passed, 1 skipped, 0 failed**.
- `TmarSourceSizeAndInfraAppTests` has **pre-existing failures unrelated to this task** (source-size baseline drift on `CatalogDirectory`,
  `CartDirectory`, `InventoryDirectory`, `AdminOrderOperationsOrchestrator`, `Order.Infrastructure -> AccessControl.Application` /
  `Fulfillment.Application` edges). Verified identical on a pristine `HEAD` worktree at `b9008efd` (3 failed / 3 passed both before and after);
  this task neither created nor expanded them, and this task added no file above the 800-LOC threshold.

## Residual defects

- Pre-existing `TmarSourceSizeAndInfraAppTests` drift (baseline drift + two Order.Infrastructure → foreign Application edges) exists at
  `b9008efd` and is out of this task's scope; it was measured on a pristine HEAD worktree and left untouched.
- `ListSellerOffersQuery` intentionally remains validator-free (auth-scoped).
