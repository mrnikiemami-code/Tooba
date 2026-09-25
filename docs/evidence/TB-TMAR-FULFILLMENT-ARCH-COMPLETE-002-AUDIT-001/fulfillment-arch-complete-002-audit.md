# TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-AUDIT-001 — Fulfillment ARCH-COMPLETE-002 readiness audit

Audit-only. Zero Fulfillment / Host production code change. No guard strengthened. Fulfillment is **not** certified here.

Parent: `TB-TMAR-SETTLEMENT-ARCH-COMPLETE-002-STRUCTURE-001` — ARCHITECT-ACCEPTED
(certification commit `54b1c8ff1f6e9214a5b5c16b6103f0285bd2a37e`, SoT stamp `01d15f3cb1ad38f0e91ed65e990e32c4f9d19876`).
Certified set at audit time: Order, Cart, StoreContext, Offer, Payment, Settlement.

## 1. Endpoint inventory (exhaustive)

Endpoint files audited: `Admin/FulfillmentAdminEndpoints.cs`, `Seller/FulfillmentSellerEndpoints.cs`,
`Customer/FulfillmentCustomerEndpoints.cs`, `Shipping/ShippingServiceEndpoints.cs`,
`Shipping/ShippingMethodsEndpoints.cs`.

| # | Request | Kind | Route(s) | IRequest | Handler | ISender | Direct App/Directory/DbContext call |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | `ListAdminFulfillmentsQuery` | Query | GET /v1/admin/fulfillments | yes | yes | yes | none |
| 2 | `QueryAdminFulfillmentWorkQueueQuery` | Query | POST /v1/admin/fulfillments/work-queue/query + /work-queue/bulk | yes | yes | yes | none |
| 3 | `ExecuteAdminFulfillmentBulkCommand` | Command | POST /v1/admin/fulfillments/work-queue/bulk | yes | yes | yes | none |
| 4 | `GetAdminFulfillmentQuery` | Query | GET /v1/admin/fulfillments/{id} | yes | yes | yes | none |
| 5 | `ListShippingServicesQuery` | Query | GET /v1/admin/shipping-services | yes | yes | yes | none |
| 6 | `GetShippingServiceQuery` | Query | GET /v1/admin/shipping-services/{id} | yes | yes | yes | none |
| 7 | `CreateShippingServiceCommand` | Command | POST /v1/admin/shipping-services | yes | yes | yes | none |
| 8 | `UpdateShippingServiceCommand` | Command | PUT /v1/admin/shipping-services/{id} | yes | yes | yes | none |
| 9 | `DeactivateShippingServiceCommand` | Command | POST /v1/admin/shipping-services/{id}/deactivate | yes | yes | yes | none |
| 10 | `EnsureShippingCatalogSeedCommand` | Command | POST /v1/admin/shipping-services/ensure-seed | yes | yes | yes | none |
| 11 | `ListEnabledShippingMethodsTreeQuery` | Query | GET /v1/admin/shipping-methods | yes | yes | yes | none |
| 12 | `ListSellerFulfillmentsQuery` | Query | GET /v1/seller/fulfillments | yes | yes | yes | none |
| 13 | `GetSellerFulfillmentQuery` | Query | GET /v1/seller/fulfillments/{id} | yes | yes | yes | none |
| 14 | `SellerMutateFulfillmentCommand` | Command | POST /v1/seller/fulfillments/{id}/{processing,packed,shipments,shipments/{sid}/tracking,dispatch,deliver} | yes | yes | yes | none |
| 15 | `ListCustomerCheckoutFulfillmentsQuery` | Query | GET /v1/customer/orders/{checkoutId}/fulfillments | yes | yes | yes | none |

- Endpoint-reachable requests = **15**
- Total MediatR `IRequest` types in Fulfillment = **15** (identical set; each has exactly one real `IRequestHandler` in `Tooba.Fulfillment.Application`)
- Worker/internal-only requests = **0** (no `BackgroundService`/`IHostedService`/worker in the module)
- Endpoint bodies are thin: authorizer + `ISender` + `ApiResponseFactory` only; no `DbContext`, no Directory, no Application service call.
- `ShippingMethodsEndpoints.ListShippingMethodsAsync` is registered by `ShippingMethodsEndpoints.Map` and reached from the module group; note it takes **no authorizer** (public shipping-methods tree) — recorded as an observation, not a defect in this audit.

## 2. Validator matrix

| Request | Classification | Reason |
| --- | --- | --- |
| `SellerMutateFulfillmentCommand` | `VALIDATOR_REQUIRED` | untrusted body (`FulfillmentCreateShipmentRequest` carrier/lines/quantity/method code/metadata) + route id + `SellerFulfillmentMutationKind` |
| `CreateShippingServiceCommand` | `VALIDATOR_REQUIRED` | untrusted `ShippingServiceWriteModel` body |
| `UpdateShippingServiceCommand` | `VALIDATOR_REQUIRED` | untrusted route id + `ShippingServiceWriteModel` body |
| `ExecuteAdminFulfillmentBulkCommand` | `VALIDATOR_REQUIRED` | untrusted `AdminFulfillmentWorkQueueBulkRequest` body |
| `QueryAdminFulfillmentWorkQueueQuery` | `VALIDATOR_REQUIRED` | untrusted `GridQueryRequest` envelope (grid policy stays owned by `AdminFulfillmentGridQueryPolicy`) |
| `GetShippingServiceQuery` | `VALIDATOR_REQUIRED` | untrusted route `serviceId` (Guid from route) |
| `DeactivateShippingServiceCommand` | `VALIDATOR_REQUIRED` | untrusted route `serviceId` |
| `GetAdminFulfillmentQuery` | `VALIDATOR_REQUIRED` | untrusted route `fulfillmentId` |
| `GetSellerFulfillmentQuery` | `VALIDATOR_REQUIRED` | untrusted route `fulfillmentId` (SellerPartyId comes from the authorizer and is deliberately not a validator reason) |
| `ListShippingServicesQuery` | `NO_VALIDATOR_REQUIRED_NO_INPUT` | only an optional presentation `Language`; no untrusted payload |
| `ListEnabledShippingMethodsTreeQuery` | `NO_VALIDATOR_REQUIRED_NO_INPUT` | only an optional presentation `Language`; no untrusted payload |
| `ListAdminFulfillmentsQuery` | `NO_VALIDATOR_REQUIRED_NO_INPUT` | parameterless |
| `ListSellerFulfillmentsQuery` | `NO_VALIDATOR_REQUIRED_AUTH_SCOPED_QUERY` | `SellerPartyId` comes only from `IFulfillmentSellerAuthorizer`; no untrusted payload |
| `ListCustomerCheckoutFulfillmentsQuery` | `NO_VALIDATOR_REQUIRED_AUTH_SCOPED_QUERY` | `CheckoutId` route value is only meaningful after `IFulfillmentCustomerAuthorizer` scopes it to the actor; no untrusted body |
| `EnsureShippingCatalogSeedCommand` | `NO_VALIDATOR_REQUIRED_NO_INPUT` | parameterless admin operation |

Totals: `VALIDATOR_REQUIRED = 9`, `present = 0`, `missing = 9`, `NO_VALIDATOR_REQUIRED = 6`
(4 `NO_VALIDATOR_REQUIRED_NO_INPUT` + 2 `NO_VALIDATOR_REQUIRED_AUTH_SCOPED_QUERY`).

Evidence of absence: no `Validators/` folder anywhere in the module, zero `FluentValidation` / `AbstractValidator`
reference, zero `AddValidatorsFromAssembly` usage. Discovery would be the standard
`AddToobaCqrsFoundation`/`AddValidatorsFromAssembly` path already used by certified modules.

Deferred classification note: `QueryAdminFulfillmentWorkQueueQuery` grid envelope validator must shape only the
primitive envelope and must not duplicate `AdminFulfillmentGridQueryPolicy`. Trusted-authorizer values
(`ActorUserId`, `SellerPartyId`) must never be the reason for a validator.

## 3. Physical production structure

| Project | Top-level folders (live) | Root `.cs` (live) |
| --- | --- | --- |
| `Tooba.Fulfillment.Domain` | `Aggregates`, `Events`, `ValueObjects` | none |
| `Tooba.Fulfillment.Contracts` | `Errors`, `Events`, `History`, `Operations`, `Returns`, `Shipping` | none |
| `Tooba.Fulfillment.Application` | `Commands`, `Errors`, `Models`, `Ports`, `Queries`, `Shipping` | none |
| `Tooba.Fulfillment.Infrastructure` | `Adapters`, `Bridges`, `DependencyInjection`, `Directories`, `Errors`, `Gateways`, `Handlers`, `Messaging`, `Observability`, `Persistence`, `Queries`, `Shipping` | none |
| `Tooba.Fulfillment.Endpoints` | `Admin`, `Customer`, `Seller`, `Shipping` | `FulfillmentEndpointModule.cs` |

Likely root allowlist for certification:
- Application → (empty)
- Endpoints → `FulfillmentEndpointModule.cs`
- Infrastructure → (empty)

Note: Fulfillment has **no** `GlobalUsings*.cs` at all (unlike Settlement/Payment), so the GlobalUsings rule is
satisfied empty rather than by an allowlist.

Structural gap vs ARCH-COMPLETE-002: `Application` has **no capability `Validators` folder**, and the audit found
no ceremonial folder should be created until the 9 required validators are added (see §2).
`Shipping` folders exist in Application/Infrastructure/Endpoints/Contracts and are capability folders for the
shipping-catalog bounded slice; they are legitimate and must be part of the certification allowlist.

## 4. Path ↔ namespace

Exact path-derived scan across all five production projects (Domain, Contracts, Application, Infrastructure,
Endpoints), excluding `bin`/`obj` and the legitimate EF `Persistence/Migrations/*` + `*ModelSnapshot.cs`
exemptions: **zero mismatches**. `PATH_NAMESPACE = EXACT`.

The existing guard uses a loose `StartsWith(nsPrefix)` + first-folder prefix check
(`FulfillmentArchitectureGuardTests.AssertNamespacesAlign`) — it is prefix-tolerance, not exact equality, so the
structure task must replace it.

## 5. GlobalUsings / aliases / shims

- Fulfillment projects: zero `GlobalUsings*.cs`. No foreign module global alias. No `TypeForwardedTo`.
- Host-level alias debt found: `src/backend/Host/Tooba.Host/FulfillmentReturnsGridAliases.cs` declares 7
  `global using` aliases (`AdminFulfillmentWorkQueueRow`, `AdminFulfillmentQueueFilters`,
  `AdminFulfillmentWorkQueueBulkRequest`, `AdminFulfillmentWorkQueueBulkItem`,
  `AdminFulfillmentWorkQueueBulkResult`, plus 2 Returns aliases). Verified consumers in Host: **only the alias
  file itself** — i.e. the aliases are dead residue that Host folder/architecture guards currently whitelist.
  Classification: Host-side `REMOVE_DEAD_RESIDUE` (move/delete during the structure task only; not repaired here).
- No compatibility shim re-declaring a flattened Fulfillment namespace was found.

## 6. Host Fulfillment residue

| Host artifact | Classification |
| --- | --- |
| `Seller/HostFulfillmentSellerAuthorizer.cs` | KEEP_AS_EXPLICIT_THIN_HOST_SECURITY_ADAPTER |
| `Admin/HostFulfillmentAdminAuthorizer.cs` | KEEP_AS_EXPLICIT_THIN_HOST_SECURITY_ADAPTER |
| `Customer/HostFulfillmentCustomerAuthorizer.cs` | KEEP_AS_EXPLICIT_THIN_HOST_SECURITY_ADAPTER |
| `Program.cs` (module assembly registration, 3 authorizer registrations, `MapFulfillmentEndpoints()`) | NOT_FULFILLMENT_AUTHORITY (composition root) |
| `Composition/ToobaModuleComposition.cs` (`FulfillmentModule`) | NOT_FULFILLMENT_AUTHORITY (composition root) |
| `Admin/ProductWorkspaceDevelopmentBootstrap.cs` (`FulfillmentDbContext` migrate) | NOT_FULFILLMENT_AUTHORITY (dev bootstrap allowlist) |
| `FulfillmentReturnsGridAliases.cs` | REMOVE_DEAD_RESIDUE (Host alias file, see §5) |

- No Host Fulfillment endpoints, no Host Fulfillment grid engine, no Host Fulfillment panel composer,
  no Fulfillment business service/runtime owner in Host (`hostRoot/Fulfillment` does not exist;
  `AdminListGridPolicies` contains no `Fulfillments`).
- `FulfillmentDbContext` appears in Host only in `ProductWorkspaceDevelopmentBootstrap.cs` (allowed dev/migration surface).
- `Fulfillment -> Host dependency = ZERO` (no `Tooba.Host` project reference from any Fulfillment project; no
  production source reference).

Host residue count is **three** thin security adapters (one more than Settlement's two), which the structure lock
must state explicitly.

## 7. Cross-module boundaries

- `Domain`: BuildingBlocks only.
- `Contracts`: BuildingBlocks only.
- `Application`: BuildingBlocks + `Fulfillment.Contracts` + `Fulfillment.Domain` + `Order.Contracts` +
  `Localization.Contracts`.
- `Infrastructure`: `Fulfillment.Application` + `Fulfillment.Contracts` + `Tooba.ModuleContracts` + `Tooba.Persistence`
  + `Order.Contracts` + `Party.Contracts` + `Inventory.Contracts` + `Payment.Contracts` + `Localization.Contracts`.
- `Endpoints`: `Fulfillment.Application` + BuildingBlocks (no Infrastructure, no Host, no DbContext).

No foreign Application/Infrastructure/Domain reference, no foreign DbContext, no `Tooba.Host`. Contracts-only
where foreign. `CROSS_MODULE_BOUNDARY = CONTRACTS_ONLY_WHERE_APPROVED` (already guarded by the existing
`Infrastructure_references_foreign_Contracts_only` test for Order/Inventory/Payment; Party/Localization are
Contracts too and should be asserted explicitly by the structure guard).

## 8. Existing guard gaps (`FulfillmentArchitectureGuardTests` + `FulfillmentEndpointOwnershipTests`)

Present today: folder-name dumping-ground check, loose namespace prefix check, foreign-Contracts-only checks,
Host `FulfillmentDbContext` allowlist, Host endpoint/panel/composer absence, ISender/ApiResponseFactory presence,
no `DbContext` in endpoints, Host authorizer existence, physical-layout assertions.

Missing vs ARCH-COMPLETE-002:
1. exact path-derived namespace equality (only loose `StartsWith` today),
2. explicit project root allowlists (`rootAllowlist` assertions per project, including Endpoints
   `FulfillmentEndpointModule.cs` and empty Application/Infrastructure),
3. explicit forbidden flattened root file list,
4. alias-workaround / `TypeForwardedTo` / flattened-namespace shim rejection (Host alias file is currently
   whitelisted instead),
5. exhaustive endpoint-reachable request inventory (15) as a checked manifest,
6. validator coverage manifest (9 required / 6 no-validator-required) with DI resolution and
   "no validator for the six" assertions,
7. MediatR `12.5.0` and ISender-only invariants,
8. explicit Host residue manifest (exactly three thin adapters + named non-authority composition/dev files),
9. cross-module boundary assertions for Party/Localization Contracts.

No guard was strengthened in this audit.

## 9. Deterministic decision

```text
NEEDS_PRECERT_REPAIR_THEN_STRUCTURE
```

Repair scope (exact, one bounded wave, to be authorized as a separate task):

```text
ADD_EXACTLY_9_FULFILLMENT_TRANSPORT_VALIDATORS
  SellerMutateFulfillmentCommand        -> Tooba.Fulfillment.Application/Validators/Seller
  CreateShippingServiceCommand          -> Tooba.Fulfillment.Application/Validators/Shipping
  UpdateShippingServiceCommand          -> .../Validators/Shipping
  DeactivateShippingServiceCommand      -> .../Validators/Shipping
  GetShippingServiceQuery               -> .../Validators/Shipping
  EnsureShippingCatalogSeedCommand      -> (no validator; NO_VALIDATOR_REQUIRED_NO_INPUT)
  ExecuteAdminFulfillmentBulkCommand    -> .../Validators/Admin
  QueryAdminFulfillmentWorkQueueQuery   -> .../Validators/Admin   (primitive grid envelope only; no duplication of AdminFulfillmentGridQueryPolicy)
  GetAdminFulfillmentQuery              -> .../Validators/Admin
  (GetSellerFulfillmentQuery / ListSellerFulfillmentsQuery -> Seller folders as applicable)
PRIMITIVE_TRANSPORT_SHAPE_ONLY
NO_BUSINESS_DOMAIN_RULES_IN_VALIDATORS
NEVER_VALIDATE_TRUSTED_AUTHORIZER_VALUES
NO_CEREMONIAL_VALIDATORS_FOR_THE_6_NO_VALIDATOR_REQUIRED_REQUESTS
DISCOVERY_VIA_EXISTING_CQRS_FOUNDATION
THEN
TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-STRUCTURE-001
  exact namespace equality, root allowlists, forbidden root files, alias rejection,
  exhaustive 15-request inventory, 9/6 validator coverage, MediatR 12.5.0, ISender-only,
  Host residue manifest (three thin adapters), cross-module boundary assertions,
  manifest certification + removal from uncertifiedHttpOwningModules
```

## 10. Focused validation

| Check | Result |
| --- | --- |
| `dotnet build Tooba.Fulfillment.Tests.csproj --no-restore` | PASS, 0 errors |
| `FulfillmentArchitectureGuardTests` + `FulfillmentEndpointOwnershipTests` | PASS (4/4) |

No full Fulfillment suite, Host suite, broad TMAR suite, solution test/build, Testcontainers or DB integration
test was run.

## 11. Preserved state

`ARCH-COMPLETE-002`, `HOST-MODULE-ENDPOINT-001`, `ARCH-CQRS-001/002` unchanged; certified modules
(Order, Cart, StoreContext, Offer, Payment, Settlement) untouched; Fulfillment **not** certified and **not**
removed from `uncertifiedHttpOwningModules`; Checkout `PAUSED_AT_SAFE_W5_CHECKPOINT`; `frontendFrozen = true`;
frontend untouched; no schema/migration change.
