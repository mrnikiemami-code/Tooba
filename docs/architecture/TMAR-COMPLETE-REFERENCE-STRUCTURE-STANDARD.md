# TMAR COMPLETE_REFERENCE_STRUCTURE_STANDARD — ARCH-COMPLETE-002

Definition marker:
`COMPLETE_REFERENCE_PATTERN_REQUIRES_ENDPOINTS_CQRS_RESULT_CONTRACTS_VALIDATION_CAPABILITY_STRUCTURE_GUARDS_SOT`

A module qualifies as `COMPLETE_REFERENCE_PATTERN` only when build, CQRS, endpoint ownership, Result/Contracts, validation coverage, capability structure, guards and SoT all hold.

## Application standard

- Capability-driven organization; capability-specific `.cs` files must not live at project root.
- Commands/Queries/Models/Ports/Policies/Services grouped under the owning capability.
- No miscellaneous `*Contracts.cs` dumping at root.
- MediatR validators live with the request or under an explicit shared `Validation` rules capability.
- Business validation (entity existence, ownership, state, eligibility, limits) stays out of FluentValidation.

Example (Order):

```text
Tooba.Order.Application
├─ Admin/{Completeness,Customers,Detail,Dashboard,Operations,OrdersGrid,Sellers,Supply,LegacyList,InventoryRecovery}
├─ Checkout/{Abuse,Process,Policies,Contracts}
├─ Customer/
├─ Seller/{Policies,Queries,...}
├─ Storefront/{Checkout,Shipping,PendingPayment,Services,Models,Ports}
├─ ReservationCycle/{Contracts,Services,Policies}
├─ PurchaseVerification/
└─ Validation/
```

## Endpoints standard

- Capability `*Endpoints.cs` files must not live at project root.
- Admin/Customer/Seller/Storefront grouping must be visible in Solution Explorer.
- Root may contain the endpoint composition entry only (plus explicitly shared `Errors/`, `Resources/`).
- The composition entry maps each capability exactly once; Host must not duplicate ownership.

Example (Order):

```text
Tooba.Order.Endpoints
├─ Admin/{Customers,Completeness,Detail,InventoryRecovery,Operations,OrdersGrid}
├─ Customer/
├─ Seller/
├─ Storefront/
├─ Errors/
├─ Resources/
└─ OrderEndpointModule.cs
```

## Infrastructure standard

- Capability/integration-driven; capability implementation/bridge/service files must not live at root.
- Root is the module composition entry only.
- Persistence lives under `Persistence`; migrations stay under `Persistence/Migrations`.
- Foreign adapters grouped under a coherent `Integrations/<Module>` or the owning capability path.
- One integration must not be fragmented across competing top-level folders.

Example (Order):

```text
Tooba.Order.Infrastructure
├─ Admin/ (+ Fulfillment/)
├─ Checkout/{Abuse,Persistence}
├─ Customer/ Seller/ Storefront/ ReservationCycle/ PurchaseVerification/
├─ Integrations/{Fulfillment,Payment,Returns,Notifications,GridEnrichment}
├─ Events/ (+ Payment/)
├─ Guards/ Messaging/ Persistence/
└─ OrderModule.cs
```

## Namespace standard

- Every file namespace matches its physical path.
- Namespace-alias workarounds purely to hide folder debt are forbidden.
  (A type alias for a Domain entity that collides with a namespace segment is allowed and must be justified in evidence.)

## Root allowlist principle

- Each project has an explicit root `.cs` allowlist; any new root file fails the gate unless the manifest is deliberately updated.

## Validation standard

- Transport/input validation (primitive shape, required ids, ranges, lengths) belongs to FluentValidation validators discoverable through DI.
- Business validation stays in Application services/handlers.
- Every endpoint-reachable request is classified `VALIDATOR_REQUIRED` or `NO_VALIDATOR_REQUIRED` in a durable manifest/manifest-equivalent guard.

## Certification process

1. Fill the module manifest entry in `docs/architecture/tmar-module-structure-manifests.json`.
2. Root allowlists match reality for each project.
3. Path↔namespace alignment passes.
4. Forbidden root files / duplicate top-level folders are absent.
5. Validator coverage guard passes for HTTP-owning transport requests.
6. Add `structureCertified: true` with `lockVersion` and record in `tmar-current-state.json.structureLock.certifiedModules`.

## Existing modules

- Order and Cart are certified under ARCH-COMPLETE-002 now.
- All other existing COMPLETE_REFERENCE_PATTERN modules are NOT structure-certified until separately reverified; they must not be claimed as ARCH-COMPLETE-002.
