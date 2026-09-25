# TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-STRUCTURE-001 — Fulfillment structure certification

## Accepted parent

- Parent task: `TB-TMAR-FULFILLMENT-HOST-EVACUATION-001` — ARCHITECT-ACCEPTED at
  `552c928c9d21211698868df73f498d1ac57c0e58` (all three Fulfillment-specific Host authorizers evacuated to
  module-owned implementations; Host Fulfillment-specific files/types = ZERO).
- Accepted state preserved: Fulfillment → Host = ZERO; AccessControl/Order boundaries Contracts/shared-seam
  only; validator inventory 15 endpoint requests = 10 validator-required + 5 no-validator-required;
  MediatR 12.5.0; ISender-only; Checkout paused; frontend frozen.

## A. Exact live folder set

`Tooba.Fulfillment.Domain`: `Aggregates`, `Events`, `ValueObjects` (root `.cs` = none).

`Tooba.Fulfillment.Contracts`: `Errors`, `Events`, `History`, `Operations`, `Returns`, `Shipping` (root `.cs` = none).

`Tooba.Fulfillment.Application`:
`Commands`, `Queries`, `Errors`, `Models`, `Ports`, `Shipping`, `Validators`
(`Validators/{Seller,Customer,Admin,Shipping}`). Root `.cs` = none.

`Tooba.Fulfillment.Endpoints`:
`Admin`, `Customer`, `Seller`, `Shipping`. Root `.cs` = `FulfillmentEndpointModule.cs`.

`Tooba.Fulfillment.Infrastructure`:
`Adapters`, `Bridges`, `DependencyInjection`, `Directories`, `Errors`, `Gateways`, `Handlers`, `Messaging`,
`Observability`, `Persistence` (`Persistence/Migrations`), `Queries`, `Shipping`. Root `.cs` = none.

No ceremonial folder was created and no already-conforming file was moved. Zero production `.cs` was added,
deleted, or relocated by this task.

## B. Exact path ↔ namespace guard

`FulfillmentArchitectureGuardTests.AssertNamespacesAlign` was replaced: the old loose `StartsWith(nsPrefix)`
acceptance is gone. The guard derives the expected namespace from the physical path
(`nsPrefix` + dot-joined relative directory, or the exact project-root namespace) and asserts **exact equality**
for every Fulfillment production `.cs` file across `Domain`, `Contracts`, `Application`, `Infrastructure`,
`Endpoints`.

Exemptions (unchanged, legitimate): `Infrastructure/Persistence/Migrations/*` and `*ModelSnapshot.cs`
(EF-generated migration namespace).

Result: `PATH_NAMESPACE = EXACT`. No real mismatch was found, so no repair was performed.

## C. Explicit root allowlists / forbidden flattened files

New guard: `Fulfillment_root_allowlists_and_forbidden_flattened_files_are_enforced`.

| Project | rootAllowlist (asserted exactly) | forbiddenRootFiles (absence asserted) |
| --- | --- | --- |
| `Tooba.Fulfillment.Domain` | *(empty)* | `FulfillmentAggregate.cs`, `ShipmentAggregate.cs`, `ConsolidatedPackage.cs` |
| `Tooba.Fulfillment.Contracts` | *(empty)* | `FulfillmentContracts.cs`, `ShippingCatalogContracts.cs`, `FulfillmentErrorCodes.cs` |
| `Tooba.Fulfillment.Application` | *(empty)* | `FulfillmentContracts.cs`, `FulfillmentCommands.cs`, `FulfillmentQueries.cs`, `FulfillmentHandlers.cs`, `FulfillmentErrorCodes.cs`, `FulfillmentModels.cs`, `SellerMutateFulfillmentCommand.cs`, `QueryAdminFulfillmentWorkQueueQuery.cs` |
| `Tooba.Fulfillment.Endpoints` | `FulfillmentEndpointModule.cs` | `FulfillmentSellerEndpoints.cs`, `FulfillmentAdminEndpoints.cs`, `FulfillmentCustomerEndpoints.cs`, `ShippingServiceEndpoints.cs`, `ShippingMethodsEndpoints.cs`, `IFulfillmentSellerAuthorizer.cs`, `IFulfillmentAdminAuthorizer.cs`, `IFulfillmentCustomerAuthorizer.cs` |
| `Tooba.Fulfillment.Infrastructure` | *(empty)* | `FulfillmentModule.cs`, `FulfillmentDbContext.cs`, `FulfillmentDirectory.cs`, `FulfillmentOutboxRegistration.cs`, `AdminFulfillmentWorkQueueQueryEngine.cs`, `FulfillmentErrorCatalogContributor.cs` |

Both allowlist equality and forbidden-file absence are asserted, so future capability files cannot be
flattened into root silently.

## D. Alias-workaround guard

New guard: `Fulfillment_rejects_namespace_alias_workarounds_and_type_forwarding` rejects:
- `TypeForwardedTo` anywhere in Fulfillment production sources,
- foreign-module alias assignments (`using X = Tooba.<Other>.(Application|Infrastructure|Domain)…`),
- `GlobalUsings*.cs` files at project root in any Fulfillment project.

Legitimate self-module import ergonomics are explicitly allowed (e.g.
`using AppModels = Tooba.Fulfillment.Application.Models;` in `FulfillmentAdminOperationsAdapter.cs`): it is a
self-module short alias and hides no flattened namespace or foreign coupling.

Result: `Alias-Workaround = NONE`.

## E. Cross-module boundary lock

- `Endpoints` → `Application` + `BuildingBlocks` + `Order.Contracts` + `Cart.Contracts` only; no
  `Infrastructure`, no `Host`, no `AccessControl`, no `Order.Application`, no `DbContext`.
- No `Tooba.Host`, no `Tooba.AccessControl.*`, no `Tooba.Order.Application` in any Fulfillment production source.
- Effective access arrives only through the neutral `IPlatformEffectiveAccessReader` seam; ordering through
  `Order.Contracts`; guest actor via the single `Order.Contracts.Fulfillment.StorefrontGuestActor.ActorId` constant.
- `Infrastructure` consumes approved foreign `Contracts` only (Order/Inventory/Payment).

## F. Host Fulfillment ownership lock

- Zero Host Fulfillment-specific files (`Directory.EnumerateFiles(hostRoot, "*.cs")` filtered by name
  `Fulfillment` returns empty) and zero Host Fulfillment-specific types.
- The three Host authorizer paths are asserted absent; the three module-owned authorizers are asserted present
  in `Tooba.Fulfillment.Endpoints/{Admin,Customer,Seller}`.
- `Fulfillment -> Host = ZERO`.

## G. Validator coverage (certification prerequisite)

Preserved unchanged via `FulfillmentValidatorCoverageGuardTests` (its final assertion was flipped from
"still not certified" to "structure certified"):

- Endpoint-reachable requests = 15; all real `IRequest`; real handlers; all dispatched via `ISender`;
  no `IValidator`/`ValidateAsync` in endpoints; MediatR = 12.5.0.
- `VALIDATOR_REQUIRED = 10`, all 10 present and resolvable to their exact concrete validators via foundation DI.
- `NO_VALIDATOR_REQUIRED = 5` (2 `NO_INPUT` + 1 `AUTH_SCOPED_QUERY` + 2 `OPTIONAL_PRESENTATION_LOCALE`), none registered.
- Validator folder layout exactly `Admin`/`Customer`/`Seller`/`Shipping` with no `Common/Helpers/Utils/Managers`.
- No ceremonial validator was created by this task.

## H. Manifest change

`docs/architecture/tmar-module-structure-manifests.json`:

- Added Fulfillment module: `structureCertified = true`, `lockVersion = ARCH-COMPLETE-002`, with the five
  projects above, their `rootAllowlist`, `rootAllowlistJustification` (Application + Infrastructure) and
  `forbiddenRootFiles`, `forbiddenTopLevelFolders = []`.
- Removed `Fulfillment` from `uncertifiedHttpOwningModules`.
- No other module entry was altered.

## I. SoT certification closure

Updated: `tmar-current-state.json`, `TOOBA-TMAR-MASTER-RECOVERY.md`, `TOOBA-ARCHITECT-BOOTSTRAP.md`,
`TOOBA-RECOVERY-CONTEXT.md`, `TmarDurableGuardTests.cs`, `TmarCompleteReferenceStructureGateTests.cs`,
`FulfillmentArchitectureGuardTests.cs`, `FulfillmentValidatorCoverageGuardTests.cs`.

Recorded Fulfillment state = `COMPLETE_REFERENCE_PATTERN`, `httpApplicability = HTTP_OWNING`,
`endpointOwnership = MODULE_ENDPOINTS`, `cqrs = MEDIATR_12_5`,
`structureCertifiedUnderArchComplete002 = true`,
`validatorCoverage = COMPLETE_10_OF_10_REQUIRED_PRESENT_5_NO_VALIDATOR_REQUIRED`,
`endpointReachableRequests = 15`, `workerInternalRequests = 0`, `pathNamespace = EXACT`,
`rootAllowlist = ENFORCED`, `aliasWorkaround = NONE`, `hostFulfillmentResidue = ZERO`,
`fulfillmentHostSpecificFileCount = 0`, `fulfillmentToHostDependency = ZERO`, `manifestCertified = true`.

Certified set (`structureLock.certifiedModules`): `Order, Cart, StoreContext, Offer, Payment, Settlement, Fulfillment`.

`workflow = HOST_FIRST_FOLDER_BY_FOLDER`, `nextHostFolder = AccessControl`,
`nextTask = TB-TMAR-HOST-ACCESSCONTROL-INVENTORY-001`,
`nextTaskGate = HOST_FIRST_FOLDER_BY_FOLDER_AFTER_FULFILLMENT_STRUCTURE_CERTIFICATION`.
No further module was auto-selected or started.

Preserved: `Checkout = PAUSED_AT_SAFE_W5_CHECKPOINT`, `frontendFrozen = true`.

## J. Focused validation results

| Check | Result |
| --- | --- |
| `FulfillmentArchitectureGuardTests` | PASS |
| `FulfillmentValidatorCoverageGuardTests` | PASS |
| `TmarCompleteReferenceStructureGateTests` | PASS |
| `TmarDurableGuardTests` | PASS |
| `dotnet build Tooba.Fulfillment.Tests.csproj --no-restore` | PASS |

No full Fulfillment/Host suite, broad TMAR suite, solution test, solution build, Testcontainers, or DB
integration test was run.

## K. Checkout / frontend preservation

Checkout remains `PAUSED_AT_SAFE_W5_CHECKPOINT`; frontend production changes = none; `frontendFrozen = true`.
