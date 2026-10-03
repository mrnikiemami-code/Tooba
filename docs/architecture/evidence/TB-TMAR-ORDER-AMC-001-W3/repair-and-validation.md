# TB-TMAR-ORDER-AMC-001-W3 — Migrate/Repair: canonical mapping, validator matrix, god-file split, cross-module Contracts boundary

## Scope of this wave

W0 analyzed, W1 split Domain + flattened Application, W2 verified Infrastructure/Endpoints structure and
repaired the stale Host-path guards. W3 closes the remaining **migration/certification blockers** that W2 left
explicitly red:

1. residual ad-hoc endpoint result mapping;
2. incomplete endpoint-reachable validator classification;
3. duplicate/ambiguous error-descriptor ownership in the composed catalog;
4. two remaining god files above the cohesion ceiling;
5. a real cross-module boundary violation (`Reviews.Infrastructure` → `Order.Application`).

No business behavior, schema, route, status code, or public Contracts shape was changed.

## 1. Canonical API result/error mapping

| Before | After |
|---|---|
| `StorefrontOrderEndpoints` geography route returned `Results.Json(StorefrontIranGeography.Provinces)` | `(ApiResponseFactory api) => api.From(Result.Success(StorefrontIranGeography.Provinces))` |

Residual `Results.*` scan over `src/backend/Modules/Order/**` (excluding `bin`/`obj`):

- `Results.Json` — **0**
- `Results.BadRequest` / `Results.Problem` — **0**
- `Results.Content` — 2, in `Admin/Completeness/AdminOrderCompletenessEndpoints.cs`, both on the **HTML
  document** success path only (`result.IsFailure ? api.From(result) : Results.Content(html, "text/html")`).
  `ApiResponseFactory` does not model an HTML body; the failure path is still canonical. Not a violation.

`OrderAdminOperationsArchitectureGuardTests` already asserts `DoesNotContain("Results.Json")`.

## 2. Validator coverage — exhaustive matrix

`OrderEndpointValidatorCoverageGuardTests` manifest now classifies **every** endpoint-reachable request exactly
once. Newly added entries (11):

| Request | Classification |
|---|---|
| `GetSellerOrderDashboardSummaryQuery` | `VALIDATOR_REQUIRED` |
| `GetStoreReservationPolicyQuery` | `NO_VALIDATOR_REQUIRED` |
| `SaveStoreReservationPolicyCommand` | `VALIDATOR_REQUIRED` |
| `GetCategoryReservationPolicyQuery` | `VALIDATOR_REQUIRED` |
| `SaveCategoryReservationPolicyCommand` | `VALIDATOR_REQUIRED` |
| `GetOffersReservationPolicyBatchQuery` | `NO_VALIDATOR_REQUIRED` |
| `GetOfferReservationPolicyQuery` | `VALIDATOR_REQUIRED` |
| `SaveOfferReservationPolicyCommand` | `VALIDATOR_REQUIRED` |
| `GetReservationPolicyAuditQuery` | `VALIDATOR_REQUIRED` |
| `GetSellerOfferReservationPolicyQuery` | `VALIDATOR_REQUIRED` |
| `DenySellerOfferReservationPolicyCommand` | `VALIDATOR_REQUIRED` |

Concrete validators authored:

- `Admin/Settings/ReservationPolicy/Validators/ReservationPolicyQueryValidators.cs`
  (`GetReservationPolicyAuditQuery`, `GetOfferReservationPolicyQuery`, `GetCategoryReservationPolicyQuery`,
  `GetSellerOfferReservationPolicyQuery`, `DenySellerOfferReservationPolicyCommand`)
- `Seller/Queries/GetSellerOrderDashboardSummaryQueryValidator.cs`

New stable machine codes added to `OrderValidationCodes` (no localized text):
`order.validation.offer_id_required`, `order.validation.category_id_required`.

`NO_VALIDATOR_REQUIRED` for the two store/batch policy reads is justified: the request carries only an
optional identifier that is resolved by the owning application service, not transport shape.

## 3. Error-descriptor ownership (composed catalog)

`reservation.policy.initial.invalid`, `reservation.policy.retry.invalid`, `reservation.policy.max.invalid` are
emitted by Order admin/seller reservation-policy settings but are **canonically owned by
`CatalogErrorCatalogContributor`** (Catalog hold-policy is the natural bounded context and primary producer).

- Duplicate **usage** is allowed; duplicate **ownership** is not.
- `OrderEndpointPresentationTests` now classifies these as `SharedCatalogCodes` and includes
  `CatalogErrorCatalogContributor` in the composed catalog test.
- Order keeps the stable machine codes and its FA resources; it does **not** re-register the descriptors.
- No suppression / `DistinctBy` / first-or-last-wins was introduced. `ErrorDefinitionCatalog` fail-fast
  duplicate detection is preserved.

`Tooba.Order.Tests.csproj` gained the `Tooba.Catalog.Contracts` project reference required to consume the
contributor in the composition test.

## 4. Cross-module boundary violation repaired (microservice blocker)

`Tooba.Reviews.Infrastructure` referenced `Tooba.Order.Application` directly to obtain
`IOrderPurchaseVerificationGateway` / `OrderPurchaseVerification`. That is a direct foreign Application
dependency and a hard microservice blocker.

| Item | Before | After |
|---|---|---|
| Contract location | `Tooba.Order.Application/PurchaseVerification/OrderPurchaseVerificationContracts.cs` | `Tooba.Order.Contracts/PurchaseVerification/OrderPurchaseVerificationContracts.cs` |
| Namespace | `Tooba.Order.Application.PurchaseVerification` | `Tooba.Order.Contracts.PurchaseVerification` |
| `Reviews.Infrastructure.csproj` | `ProjectReference → Tooba.Order.Application` | `ProjectReference → Tooba.Order.Contracts` |

`Tooba.Order.Application/GlobalUsings.cs` and `Tooba.Order.Infrastructure/GlobalUsings.cs` add
`global using Tooba.Order.Contracts.PurchaseVerification;`. Every Order file that previously imported the
Application namespace had that using removed.

Result: **0** foreign csproj references to `Tooba.Order.Application`, `Tooba.Order.Infrastructure`, or
`Tooba.Order.Domain` anywhere in the solution (verified by csproj scan). Only Order's own projects, `Host`
(composition), and `Tooba.Order.Tests` reference Order projects.

## 5. God-file split (cohesion, ARCH-SIZE-001 / ARCH-MODULE-FILE-001)

### 5a. `AdminOrderOperationsOrchestrator` (2 689 LOC → cohesive partials)

| File | LOC |
|---|---|
| `AdminOrderOperationsOrchestrator.cs` | 337 |
| `AdminOrderOperationsOrchestrator.Operations.cs` | 422 |
| `AdminOrderOperationsOrchestrator.Projection.cs` | 433 |
| `AdminOrderOperationsOrchestrator.Fulfillment.cs` | 330 |
| `AdminOrderOperationsOrchestrator.Returns.cs` | 198 |
| `AdminOrderOperationsOrchestrator.Payment.cs` | 281 |
| `AdminOrderOperationsOrchestrator.Rules.cs` | 560 |
| `AdminOrderWholeOrderActions.cs` (standalone cohesive type) | 74 |

Split axis is **responsibility**, not arbitrary line count: operations dispatch, projection/composition,
fulfillment, returns, payment, and business rules each have a distinct reason to change.

### 5b. `CheckoutDirectory` (930 LOC → cohesive partials)

| File | LOC |
|---|---|
| `CheckoutDirectory.cs` | 449 |
| `CheckoutDirectory.Reservations.cs` | 332 |
| `CheckoutDirectory.Access.cs` | 151 |

Axis: core directory, reservation-cycle persistence, and access/read guards.

All Order production files are now below the size ceiling (largest = 560 LOC, `…Orchestrator.Rules.cs`).

## 6. Storefront geography extraction

`StorefrontIranGeography` (Iran province/city catalogue) was removed from `StorefrontRecipientNames.cs` and
placed in its own cohesive capability file
`Application/Storefront/Geography/StorefrontIranGeography.cs`, namespace
`Tooba.Order.Application.Storefront.Geography`. The endpoint was updated to consume it through the canonical
response factory (section 1).

## 7. Focused validation

```text
dotnet build src/backend/Tooba.slnx
Build succeeded. 0 Error(s)

dotnet test src/backend/Modules/Order/Tooba.Order.Tests
Passed! - Failed: 0, Passed: 137, Skipped: 0, Total: 137
```

The three guards left red by W2 now pass:

- `OrderEndpointPresentationTests.Every_order_owned_error_code_has_an_explicit_descriptor`
- `OrderEndpointPresentationTests.Composed_catalog_resolves_order_and_shared_codes_without_duplicates`
- `OrderEndpointValidatorCoverageGuardTests.Manifest_covers_every_endpoint_reachable_request_exactly_once`

`Tooba.Order.Tests` compiles again (the earlier `Tooba.AccessControl` reference defect recorded as `K3` in SoT
is no longer present on this HEAD).

## 8. Route ownership (recorded)

`Tooba.Order.Endpoints` owns **43** route registrations across 12 endpoint files. Host owns no Order route.
Host `AdminPanelEndpoints` maps only `/v1/admin/dashboard`.

## Residual non-blocking debt

- `CheckoutDirectory.cs` (449 LOC) and `…Orchestrator.Rules.cs` (560 LOC) remain the largest files but are
  single-responsibility and under the ceiling; no further split is justified.
- No schema/migration change in this wave.
