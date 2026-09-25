# TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-PRECERT-REPAIR-001

Fulfillment pre-certification repair: add the ten missing Fulfillment transport/input
validators and remove the dead Host alias residue.

- Parent: `TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-AUDIT-001-R1` (ARCHITECT-ACCEPTED at
  `ac6156bcf796147910e5b5ea8e85c5bc9084027f`)
- Track: `FULFILLMENT_ARCH_COMPLETE_002_PRECERT_REPAIR`
- Mode: `BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE`
- Fulfillment structure certification: **PENDING** (deliberately not performed here)

## 1. Exact endpoint-reachable inventory (15 / 10 / 5)

Exhaustive inventory of every endpoint-reachable Fulfillment MediatR request. The module has
no worker-only or internal-only request, so the endpoint inventory equals the full `IRequest`
set.

| # | Request | Surface | Classification |
|---|---------|---------|----------------|
| 1 | `SellerMutateFulfillmentCommand` | Seller | `VALIDATOR_REQUIRED_PRESENT` |
| 2 | `GetSellerFulfillmentQuery` | Seller | `VALIDATOR_REQUIRED_PRESENT` |
| 3 | `ListSellerFulfillmentsQuery` | Seller | `NO_VALIDATOR_REQUIRED_AUTH_SCOPED_QUERY` |
| 4 | `ListCustomerCheckoutFulfillmentsQuery` | Customer | `VALIDATOR_REQUIRED_PRESENT` |
| 5 | `GetAdminFulfillmentQuery` | Admin | `VALIDATOR_REQUIRED_PRESENT` |
| 6 | `ExecuteAdminFulfillmentBulkCommand` | Admin | `VALIDATOR_REQUIRED_PRESENT` |
| 7 | `QueryAdminFulfillmentWorkQueueQuery` | Admin | `VALIDATOR_REQUIRED_PRESENT` |
| 8 | `ListAdminFulfillmentsQuery` | Admin | `NO_VALIDATOR_REQUIRED_NO_INPUT` |
| 9 | `CreateShippingServiceCommand` | Shipping | `VALIDATOR_REQUIRED_PRESENT` |
| 10 | `UpdateShippingServiceCommand` | Shipping | `VALIDATOR_REQUIRED_PRESENT` |
| 11 | `DeactivateShippingServiceCommand` | Shipping | `VALIDATOR_REQUIRED_PRESENT` |
| 12 | `GetShippingServiceQuery` | Shipping | `VALIDATOR_REQUIRED_PRESENT` |
| 13 | `ListShippingServicesQuery` | Shipping | `NO_VALIDATOR_REQUIRED_OPTIONAL_PRESENTATION_LOCALE` |
| 14 | `EnsureShippingCatalogSeedCommand` | Shipping | `NO_VALIDATOR_REQUIRED_NO_INPUT` |
| 15 | `ListEnabledShippingMethodsTreeQuery` | Shipping | `NO_VALIDATOR_REQUIRED_OPTIONAL_PRESENTATION_LOCALE` |

Totals: `endpointReachableRequests = 15`, `VALIDATOR_REQUIRED = 10`,
`validatorsPresent = 10`, `validatorsMissing = 0`, `NO_VALIDATOR_REQUIRED = 5`
(2 `NO_INPUT` + 1 `AUTH_SCOPED_QUERY` + 2 `OPTIONAL_PRESENTATION_LOCALE`).

## 2. Exactly ten validators added

Folder layout (capability folders only, no `Common`/`Helpers`/`Utils`/`Managers`):

`src/backend/Modules/Fulfillment/Tooba.Fulfillment.Application/Validators/`

- `Seller/SellerMutateFulfillmentCommandValidator.cs`
- `Seller/GetSellerFulfillmentQueryValidator.cs`
- `Customer/ListCustomerCheckoutFulfillmentsQueryValidator.cs`
- `Admin/GetAdminFulfillmentQueryValidator.cs`
- `Admin/ExecuteAdminFulfillmentBulkCommandValidator.cs`
- `Admin/QueryAdminFulfillmentWorkQueueQueryValidator.cs`
- `Shipping/CreateShippingServiceCommandValidator.cs`
- `Shipping/UpdateShippingServiceCommandValidator.cs`
- `Shipping/DeactivateShippingServiceCommandValidator.cs`
- `Shipping/GetShippingServiceQueryValidator.cs`

Supporting shared files stay inside the same capability root:
`Validators/FulfillmentValidationCodes.cs` (stable `fulfillment.validation.*` codes, kept
separate from business `FulfillmentErrorCodes`) and `Validators/FulfillmentFluentRules.cs`
(reusable primitive-shape fragments).

## 3. Exact validator rules (transport/input shape only)

| Validator | Rules |
|-----------|-------|
| `SellerMutateFulfillmentCommandValidator` | `FulfillmentId != Guid.Empty`; `Permission is not null` (command-shape invariant); supplied `ShipmentId != Guid.Empty`; supplied `CarrierDisplayName`, `TrackingReference`, `ShippingMethodCode` not whitespace-only; supplied `ShipmentLines` must not contain null items, each `OrderLineId != Guid.Empty`, each `Quantity > 0` |
| `GetSellerFulfillmentQueryValidator` | `FulfillmentId != Guid.Empty` |
| `ListCustomerCheckoutFulfillmentsQueryValidator` | `CheckoutId != Guid.Empty` |
| `GetAdminFulfillmentQueryValidator` | `FulfillmentId != Guid.Empty` |
| `ExecuteAdminFulfillmentBulkCommandValidator` | `Request is not null` |
| `QueryAdminFulfillmentWorkQueueQueryValidator` | `Request is not null` |
| `CreateShippingServiceCommandValidator` | `Model is not null` |
| `UpdateShippingServiceCommandValidator` | `ServiceId != Guid.Empty`; `Model is not null` |
| `DeactivateShippingServiceCommandValidator` | `ServiceId != Guid.Empty` |
| `GetShippingServiceQueryValidator` | `ServiceId != Guid.Empty` |

Deliberately **not** validated (kept with their real owners):

- `ActorUserId` / `SellerPartyId` — trusted seller/admin authorization boundary
  (`IFulfillmentSellerAuthorizer`, `IFulfillmentAdminAuthorizer`).
- Seller ownership, fulfillment existence/state, mutation-kind-specific field
  requirements, shipping-method existence and tracking/provider business semantics.
- Bulk `action-code` allowlist, empty-bulk semantics, cross-seller rules, row identity
  matching, action compatibility and shipment resolution — Application behavior.
- Grid paging/field/operator/sort/filter/advanced/search semantics — owned by
  `AdminFulfillmentGridQueryPolicy`.
- `ShippingServiceSemantic` and the shipping directory/domain rules.
- No invented max lengths were introduced.

## 4. Explicit NO_VALIDATOR_REQUIRED requests (five)

| Request | Reason |
|---------|--------|
| `ListAdminFulfillmentsQuery` | `NO_VALIDATOR_REQUIRED_NO_INPUT` |
| `EnsureShippingCatalogSeedCommand` | `NO_VALIDATOR_REQUIRED_NO_INPUT` |
| `ListSellerFulfillmentsQuery` | `NO_VALIDATOR_REQUIRED_AUTH_SCOPED_QUERY` — `SellerPartyId` produced only by `IFulfillmentSellerAuthorizer` |
| `ListShippingServicesQuery` | `NO_VALIDATOR_REQUIRED_OPTIONAL_PRESENTATION_LOCALE` |
| `ListEnabledShippingMethodsTreeQuery` | `NO_VALIDATOR_REQUIRED_OPTIONAL_PRESENTATION_LOCALE` |

No ceremonial empty validators were created.

## 5. Central discovery proof

- Validation discovery uses only the existing
  `AddToobaCqrsFoundation` → `AddValidatorsFromAssembly` path in
  `src/backend/BuildingBlocks/Tooba.BuildingBlocks/TmarFoundation.cs`.
- `MediatR` remains `12.5.0` in `Tooba.BuildingBlocks.csproj`.
- No second validation pipeline, no manual validator invocation in endpoints or handlers.
- `FulfillmentValidatorCoverageGuardTests` resolves all ten concrete validators through a
  real foundation DI container and proves that none of the five no-validator-required
  requests has a registered `IValidator<>`.

## 6. Dead Host alias residue deletion proof

- File deleted: `src/backend/Host/Tooba.Host/FulfillmentReturnsGridAliases.cs`
  (7 `global using` aliases).
- Zero-consumer proof re-run before deletion: no production consumer exists in
  `src/backend/Host/Tooba.Host`; the only alias-type references outside the owning modules
  are the explicit `using Tooba.Fulfillment.Application.Models;` /
  `using Tooba.Returns.Application.Models;` directives already present inside test files.
- No compatibility alias, type forwarding, rename or move was introduced.
- Only the two Host guards that explicitly allowlisted the file were repaired:
  `HostFolderStructureTests.RootCsAllowlist` (entry removed) plus a new explicit absence
  assertion, and `HostCartResidualGuardTests.CartNamingAllowlist` (entry removed).
- Runtime behavior is unchanged.

## 7. Focused guards and tests

Added:

- `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests/Architecture/FulfillmentValidatorCoverageGuardTests.cs`
  — exhaustive 15/10/5 inventory, manifest-vs-endpoint-construction equality, foundation DI
  resolution of the ten concrete validators, none registered for the five, all 15 real
  `IRequest` types, `ISender`-only endpoints with no `IValidator`/`ValidateAsync`, handlers
  free of direct validator invocation, exact `Seller`/`Customer`/`Admin`/`Shipping` folder
  layout with no dumping-ground folders, MediatR `12.5.0`, dead Host alias absent and
  Fulfillment still not structure-certified / still inside `uncertifiedHttpOwningModules`.
- `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests/Architecture/FulfillmentValidatorBehaviorTests.cs`
  — direct in-memory reject/accept proofs, `CheckoutId != Guid.Empty` as the only customer
  rule, non-policing of `ActorUserId`/`SellerPartyId`, non-duplication of bulk/grid/shipping
  business rules.

Updated:

- `FulfillmentArchitectureGuardTests.AllowedApplicationFolders` now permits the `Validators`
  capability folder (no other architectural rule was loosened).

Results:

- Fulfillment focused run: `Passed! - Failed: 0, Passed: 19, Skipped: 0`
  (`FulfillmentValidatorCoverageGuardTests`, `FulfillmentValidatorBehaviorTests`,
  `FulfillmentArchitectureGuardTests`).
- Host focused alias run: `Passed! - Failed: 0, Passed: 17, Skipped: 0`
  (`HostFolderStructureTests`, `HostCartResidualGuardTests`).
- `dotnet build .../Tooba.Fulfillment.Tests.csproj --no-restore` → `Build succeeded, 0 Error(s)`.
- No broad suite, structure gate, solution build/test, Testcontainers or DB integration was
  run (FAST-VALIDATION-BUDGET).

## 8. Protected state preserved

- `ARCH-COMPLETE-002`, `HOST-MODULE-ENDPOINT-001`, `ARCH-CQRS-001/002` untouched.
- Certified modules unchanged: Order, Cart, StoreContext, Offer, Payment, Settlement.
- Fulfillment → Host dependency = `ZERO`; exactly three thin Host security adapters remain
  (`HostFulfillmentAdminAuthorizer`, `HostFulfillmentSellerAuthorizer`,
  `HostFulfillmentCustomerAuthorizer`).
- No schema/migration change; existing routes and behavior preserved.
- `Checkout = PAUSED_AT_SAFE_W5_CHECKPOINT`; `frontendFrozen = true`.
- Fulfillment remains **NOT** structure-certified and remains listed in
  `uncertifiedHttpOwningModules`.

## 9. Explicit structure certification = PENDING

This task did not structure-certify Fulfillment, did not edit
`tmar-module-structure-manifests.json` to certify it, did not add it to
`certifiedModules`, did not remove it from `uncertifiedHttpOwningModules`, and did not
strengthen the full exact namespace/root-allowlist guard.

Recovery: `nextTask = TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-STRUCTURE-001`,
`nextTaskGate = NEXT_TMAR_WAVE_AFTER_FULFILLMENT_PRECERT_REPAIR`.
