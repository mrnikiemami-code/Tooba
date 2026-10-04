# TB-TMAR-FULFILLMENT-AMSC-001 — W2 Structure

- Module: `Fulfillment`
- Skill: `tooba-architecture-structure`
- Starting HEAD: `bfd53da4f835016da04db636111b5d2b7bdfe181` (== origin/main, clean tree)
- Structure-Handoff-State: `READY_FOR_CERTIFY`
- Host final closure: preserved (no Host production folder/file added or widened)

## 1. Classification states

| Axis | Before | After |
| --- | --- | --- |
| Folder-Granularity-State | `TECHNICAL_AXIS_FIRST` + `OVER_FOLDERED` | `PROFESSIONAL_SHALLOW` |
| Solution-Explorer-State | `CANONICAL` (`/Modules/Fulfillment/`, 6 projects) | `CANONICAL` (unchanged) |
| Path-Namespace-State | `EXACT` | `EXACT` |
| Physical-Copy-State | `CLEAN` | `CLEAN` |
| Root-Allowlist-State | `ENFORCED` | `ENFORCED` (manifest `forbiddenTopLevelFolders` tightened) |
| File-Cohesion-State | `MULTI_RESPONSIBILITY_COHESION_VIOLATION` (`FulfillmentDirectory.cs` 1214 LOC) | `COHESIVE` (two cohesive partials, both < 800) |

## 2. Folder-Granularity-State — the defect that was repaired

`Tooba.Fulfillment.Application` was organized on the **technical axis** with one
use-case folder per request, each holding exactly one source file:

- `Commands/<UseCase>/<UseCase>Command.cs` × 6 → `OVER_FOLDERED` (source-file count = 1)
- `Queries/<UseCase>/<UseCase>Query.cs` × 9 → `OVER_FOLDERED` (source-file count = 1)
- `Validators/{Admin,Seller,Customer,Shipping}/` → audience axis, not capability axis
- `Models/`, `Ports/` at the Application root → technical axis at the top level
- `Shipping/` existed as an ad-hoc capability folder holding only 3 unrelated mixed bundles

Because Fulfillment has **four** real capabilities (Shipping catalog, Fulfillment
lifecycle, Admin work queue, Customer checkout) the technical axis was the primary
organization → `TECHNICAL_AXIS_FIRST`.

## 3. Repaired tree (capability-first, shallow)

```text
Tooba.Fulfillment.Application/
  Checkout/
    Queries/     ListCustomerCheckoutFulfillmentsQuery.cs
    Validators/  ListCustomerCheckoutFulfillmentsQueryValidator.cs
  Errors/        FulfillmentErrors.cs, FulfillmentExceptionMapper.cs
  Fulfillments/
    Commands/    SellerMutateFulfillmentCommand.cs
    Models/      (9 snapshots/commands)
    Ports/       IFulfillmentDirectory.cs, IFulfillmentInventoryGateway.cs,
                 IFulfillmentUseCaseGuard.cs, ISellerFulfillmentAuthorizer.cs
    Queries/     GetAdmin/GetSeller/ListAdmin/ListSellerFulfillment(s)Query.cs
    Validators/  GetAdminFulfillmentQueryValidator.cs, GetSellerFulfillmentQueryValidator.cs,
                 SellerMutateFulfillmentCommandValidator.cs
  Shipping/
    Commands/    Create/Update/DeactivateShippingServiceCommand.cs, EnsureShippingCatalogSeedCommand.cs
    Ports/       IShippingServiceDirectory.cs
    Queries/     GetShippingServiceQuery.cs, ListShippingServicesQuery.cs,
                 ListEnabledShippingMethodsTreeQuery.cs
    Validators/  4 shipping validators
    ShippingProviderMetadata.cs      (was ShippingMethodRegistry.cs — provider metadata validator)
    ShippingServiceReadModels.cs     (was half of ShippingServiceReadContracts.cs)
    ShippingServiceSemantic.cs       (was half of ShippingServiceReadContracts.cs)
    ShippingServiceWriteModels.cs    (was half of ShippingServiceWriteContracts.cs)
  Validators/    FulfillmentFluentRules.cs, FulfillmentValidationCodes.cs  (cross-capability)
  WorkQueue/
    Commands/    ExecuteAdminFulfillmentBulkCommand.cs
    Models/      AdminFulfillmentWorkQueueModels.cs
    Queries/     QueryAdminFulfillmentWorkQueueQuery.cs
    Validators/  ExecuteAdminFulfillmentBulkCommandValidator.cs,
                 QueryAdminFulfillmentWorkQueueQueryValidator.cs
```

Namespaces are **full path-derived** (Cart/AddressBook canonical style), e.g.
`Tooba.Fulfillment.Application.Shipping.Commands`, not a flattened
`...Application.Shipping`.

`Validators/` at the Application root is retained deliberately and is *not* a technical
axis: it holds the two **cross-capability** helpers (`FulfillmentFluentRules`,
`FulfillmentValidationCodes`) that all four capabilities consume. Flattening them into a
single capability would have created a false owner.

## 4. Mixed `*Contracts.cs` bundles split by responsibility

The structure skill (§12) rejects "Mixed `*Contracts.cs` Application dumps". The three
`Application/Shipping/*.cs` bundles were split **by responsibility only** — no member was
renamed, no signature or body changed:

| Before | After |
| --- | --- |
| `ShippingServiceReadContracts.cs` (DTOs **+** `ShippingServiceSemantic`) | `ShippingServiceReadModels.cs` + `ShippingServiceSemantic.cs` |
| `ShippingServiceWriteContracts.cs` (language gate **+** seed/translation models **+** `IShippingServiceDirectory` port) | `ShippingServiceWriteModels.cs` + `Ports/IShippingServiceDirectory.cs` |
| `ShippingMethodRegistry.cs` (only `ShippingProviderMetadataValidator` + provider metadata records) | `ShippingProviderMetadata.cs` (corrected name; `ShippingMethodRegistry` itself lives in `Tooba.Fulfillment.Contracts.Shipping`) |

`IShippingServiceLanguageGate` deliberately stays in `ShippingServiceWriteModels.cs`:
it is the language seam whose companion `ShippingServiceSeedLanguage` model it returns, so
splitting it would have produced a one-member file.

## 5. File-Cohesion-State — god-file decomposed

`Infrastructure/Directories/FulfillmentDirectory.cs` was **1214 LOC**
(`MULTI_RESPONSIBILITY_COHESION_VIOLATION`, no size-baseline entry, ARCH-SIZE-001 ceiling
800). It was decomposed into two **cohesive partials** with zero behavior change:

| File | LOC | Responsibility |
| --- | --- | --- |
| `FulfillmentDirectory.cs` | 794 | lifecycle: create-from-paid, queries, processing/pack/ship/dispatch/deliver, checkout void/ensure/abort/restore, snapshot mapping |
| `FulfillmentDirectory.Packages.cs` | 440 | consolidated packages: read/membership/lock + create/cancel/tracking/dispatch/deliver/void |

Both are under the 800 ceiling. This mirrors the repository's established cohesive-partial
pattern (`Order.Infrastructure/Checkout/Persistence/CheckoutDirectory{,.Access,.Reservations}.cs`,
`Order.Application/Admin/Operations/Services/AdminOrderOperationsOrchestrator.*.cs`).

## 6. Behavior preservation

Routes, HTTP methods, success DTO shapes, authorization semantics, business rules, state
transitions, ordering, cancellation, idempotency, transactions, persistence semantics,
schema, migration IDs/Up-Down, telemetry metric names, correlation/trace behavior and
public Contracts: **unchanged**. This wave is pure physical reorganization plus a
member-preserving partial split.

## 7. Durable guards added / updated

`Tooba.Fulfillment.Tests/Architecture/FulfillmentArchitectureGuardTests.cs`
- `AllowedApplicationFolders` → `Checkout/Errors/Fulfillments/Shipping/Validators/WorkQueue`.
- **New** `Fulfillment_application_is_capability_first_with_no_technical_axis_root`.
- **New** `Fulfillment_has_no_single_file_use_case_leaf_folders`.
- **New** `Fulfillment_application_has_no_flat_mixed_contracts_bundles`.
- **New** `Fulfillment_directory_stays_under_the_arch_size_ceiling` (< 800 × 2).
- Tree-query path assertion re-pointed at `Shipping/Queries/`.

`Tooba.Fulfillment.Tests/Architecture/FulfillmentValidatorCoverageGuardTests.cs`
- `Validator_folder_layout_is_exactly_seller_customer_admin_shipping` →
  `Validator_folder_layout_is_capability_first_shallow` (rejects the audience axis,
  asserts per-capability validator counts 4/3/2/1 and the 2 cross-capability helpers).

## 8. Focused validation

| Validation | Result |
| --- | --- |
| `dotnet build src/backend/Tooba.slnx` | Build succeeded, 0 errors |
| `Tooba.Fulfillment.Tests` | **69/69 passed** (was 65 + 4 new guards) |
| Host `Fulfillment*`/`Shipping*`/`Tmar*`/`ErrorCatalog*`/`AdminDbNativeGrid*` filter | 22 failed / 124 passed — **byte-identical failure set to the pre-change baseline** (verified by `git stash` re-run; see `validation.md`) |
| Pre-existing Host failures | Not introduced by W2: stale `TmarSourceSizeAndInfraAppTests` baselines, `.tmp-baseline/` cruft, unbuilt `Catalog` WIP, and the Domain `ContractOperationException` parity divergence recorded in W0/W1 |

## 9. Handoff

- `Structure-State = READY_FOR_CERTIFY` — all §27 gates met.
- Residual for W3 Certify: manifest/SoT sync for the W2 physical truth, and a durable
  `FulfillmentModuleAmsc001W3CertGuardTests`.
- Host final closure preserved: `HOST_TMAR_EVACUATION_FINAL_CLOSURE_CERTIFIED` and
  `HOST_ROOT_FINAL_CERTIFIED` untouched; zero Host production files added.
