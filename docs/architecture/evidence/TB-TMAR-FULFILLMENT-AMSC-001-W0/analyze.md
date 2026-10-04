# TB-TMAR-FULFILLMENT-AMSC-001 — W0 Analyze

- Module: `Fulfillment`
- Skill: `tooba-architecture-analyze`
- Mode: ANALYSIS_ONLY
- Starting HEAD: (verified == origin/main before any change)
- Target: `src/backend/Modules/Fulfillment/Tooba.Fulfillment.*`

## 1. Target analyzed

Five production projects + one test project, grouped under `/Modules/Fulfillment/` in `Tooba.slnx`:

| Project | Production files |
| --- | --- |
| `Tooba.Fulfillment.Contracts` | 7 |
| `Tooba.Fulfillment.Domain` | 20 |
| `Tooba.Fulfillment.Application` | 41 |
| `Tooba.Fulfillment.Infrastructure` | 26 |
| `Tooba.Fulfillment.Endpoints` | 12 |

## 2. Structured state fields

1. **Foundation-State** — `FOUNDATION_READY` (module is `structureCertified: true`, ARCH-COMPLETE-002, all five projects + Solution Folder present).
2. **Ownership-State** — correct (single business capability: fulfillment lifecycle + shipping catalog; no mixed-module ownership).
3. **File-Cohesion-State** — `MULTI_RESPONSIBILITY_COHESION_VIOLATION` (`FulfillmentDirectory.cs` 1103 LOC, above the 800 threshold, with distinct write responsibilities).
4. **Oversized/God-File-State** —
   - `Infrastructure/Directories/FulfillmentDirectory.cs` — 1103 LOC — `MULTI_RESPONSIBILITY_COHESION_VIOLATION` (no size-baseline entry; violates ARCH-SIZE-001 800 ceiling).
   - `Infrastructure/Queries/AdminFulfillmentWorkQueueQueryEngine.cs` — 495 LOC — `OVERSIZED_ONLY` (cohesive single query engine; WATCH).
   - `Domain/Aggregates/FulfillmentUnit.cs` — 489 LOC — `OVERSIZED_ONLY` (cohesive aggregate; WATCH).
5. **Localization-State** — `MISSING_INFRASTRUCTURE_USE`. No `IErrorResourceSet`, no `.resx` at all; every user-facing error falls back to the English `SafeTitleFallback`.
6. **API-Result-Pattern-State** — `AD_HOC` (`FulfillmentExceptionMapper` string switch + `catch (InvalidOperationException ex) when (TryMapExact(ex.Message...))`), plus `RAW_RESULTS` for `Results.Json(new { ok = true })` in `ShippingServiceEndpoints` and an anonymous success envelope in `FulfillmentCustomerEndpoints`.
7. **Stable-Error-Code-State** — `UNREGISTERED_CODES` + `STRING_HEURISTIC`. `FulfillmentErrorCodes` declares 19 constants; `FulfillmentErrorCatalogContributor` registers 17 descriptors. `FulfillmentExceptionMapper` recognises ~90 raw string codes, of which only a small subset is catalogued.
8. **Logging-State** — `CANONICAL` (`ILogger<T>` / structured; no `Console.WriteLine`, no second logger framework).
9. **Sensitive-Logging-State** — `NONE`.
10. **OpenTelemetry-State** — `CANONICAL` (`ToobaTelemetry.Meter` via `FulfillmentInstrumentation`; no second `ActivitySource`/`Meter`).
11. **Correlation-Trace-State** — `CANONICAL` (no custom header, no raw `AsyncLocal`, no manual `traceparent`, no direct `StartActivity`).
12. **CQRS-State** — `COMPLIANT` (14 endpoint-reachable requests, all `IRequest<T>` + real `IRequestHandler<,>`, dispatched through `ISender`; endpoints transport-only).
13. **Validator-Coverage-State** — `EXHAUSTIVE` (14 requests: 11 `VALIDATOR_REQUIRED` with concrete validators + 3 `NO_VALIDATOR_REQUIRED`; guarded by `FulfillmentValidatorCoverageGuardTests`).
14. **Contracts-Boundary-State** — `CLEAN` for cross-module references; **but** Application hosts generic mixed `ShippingServiceReadContracts.cs` / `ShippingServiceWriteContracts.cs` bundles (Contracts-named types inside Application).
15. **Cross-Module-Coupling-State** — `LEGAL_CONTRACTS_ONLY` (Order.Contracts, Inventory.Contracts, Payment.Contracts, Party.Contracts, Localization.Contracts, Cart.Contracts, ModuleContracts). Zero foreign Application/Infrastructure/Domain dependency.
16. **Cross-Module-Join-State** — `NONE`.
17. **Persistence-Ownership-State** — `CORRECT` (one `FulfillmentDbContext`, own `fulfillment` schema, own migrations; no foreign DbSet/DbContext).
18. **Endpoint-Ownership-State** — `MODULE_OWNED` (7 route groups, Host HTTP ownership ZERO).
19. **Host-Residue-State** — ZERO (guard proves no `Fulfillment`-named Host file, no Host `FulfillmentDbContext`, no Host shipping endpoints).
20. **Schema-Migration-State** — `UNCHANGED` (8 migrations; no structural work planned that touches them).
21. **Behavior-Preservation-Risk** — `MEDIUM` (error-code plumbing, catalog coverage and localization change the *failure envelope* for previously-unmapped paths; routes, DTO success shapes, schema and state transitions must stay identical).
22. **Canonical-Reference-Used** — `Cart` (freshly certified: capability-first `Carts/{Commands,Queries,Validators}`, typed `SemanticException(new SemanticError(code))` with zero exception-mapper, `Endpoints/Errors/CartErrorCatalogContributor.cs`, `Endpoints/Resources/CartErrors.{resx,fa.resx}` + `CartErrorResources.cs` `IErrorResourceSet`, `AddSingleton<IErrorCatalogContributor/IErrorResourceSet>` in `CartEndpointModule.cs`) and `Offer` (reference module). BuildingBlocks for `ApiResponseFactory`, `IErrorResourceSet`, `ErrorDescriptor`.
23. **Final-Disposition** — `READY_TO_MIGRATE` (bounded 4-wave AMSC: Analyze → Migrate → Structure → Certify).

## 3. Folder-Granularity-State

**`TECHNICAL_AXIS_FIRST` + `OVER_FOLDERED`.**

Current `Tooba.Fulfillment.Application`:

```text
Application/
  Commands/            <- technical axis
    CreateShippingService/CreateShippingServiceCommand.cs        (1 file)
    DeactivateShippingService/DeactivateShippingServiceCommand.cs (1 file)
    EnsureShippingCatalogSeed/EnsureShippingCatalogSeedCommand.cs (1 file)
    ExecuteAdminFulfillmentBulk/ExecuteAdminFulfillmentBulkCommand.cs (1 file)
    SellerMutateFulfillment/SellerMutateFulfillmentCommand.cs     (1 file)
    UpdateShippingService/UpdateShippingServiceCommand.cs         (1 file)
  Queries/             <- technical axis
    GetAdminFulfillment/GetAdminFulfillmentQuery.cs               (1 file)
    GetSellerFulfillment/GetSellerFulfillmentQuery.cs             (1 file)
    GetShippingService/GetShippingServiceQuery.cs                 (1 file)
    ListAdminFulfillments/ListAdminFulfillmentsQuery.cs           (1 file)
    ListCustomerCheckoutFulfillments/ListCustomerCheckoutFulfillmentsQuery.cs (1 file)
    ListEnabledShippingMethodsTree/ListEnabledShippingMethodsTreeQuery.cs (1 file)
    ListSellerFulfillments/ListSellerFulfillmentsQuery.cs         (1 file)
    ListShippingServices/ListShippingServicesQuery.cs             (1 file)
    QueryAdminFulfillmentWorkQueue/QueryAdminFulfillmentWorkQueueQuery.cs (1 file)
  Models/              <- technical axis
  Ports/               <- technical axis
  Validators/          <- technical axis (Admin/Seller/Customer/Shipping audience split)
  Errors/
  Shipping/            <- ad-hoc capability folder
```

Defects:

- 6 single-file Command leaf folders (`Commands/<UseCase>/<UseCase>Command.cs`) → `OVER_FOLDERED` (source-file count = 1, request+handler co-located does not justify the folder).
- 9 single-file Query leaf folders → `OVER_FOLDERED`.
- `Commands`/`Queries`/`Validators` are the primary axis while the module has multiple real capabilities (Shipping, Fulfillment lifecycle, Work queue, Checkout) → `TECHNICAL_AXIS_FIRST`.
- `Validators/{Admin,Seller,Customer,Shipping}` mirrors the audience axis rather than the capability axis.
- `Infrastructure/Queries/` is a technical-axis folder at the Infrastructure root.

Target capability-first tree:

```text
Application/
  Shipping/    Commands/ Queries/ Validators/ Models/ Ports/
  Fulfillments/ Commands/ Queries/ Validators/ Models/
  WorkQueue/   Commands/ Queries/ Validators/
  Checkout/    Queries/ Validators/
  Errors/
```

## 4. Capability map

| Capability | Owned requests | Endpoints audience |
| --- | --- | --- |
| Shipping catalog | `CreateShippingService`, `UpdateShippingService`, `DeactivateShippingService`, `EnsureShippingCatalogSeed`, `GetShippingService`, `ListShippingServices`, `ListEnabledShippingMethodsTree` | Admin (`/v1/admin/shipping-services`), public (`/v1/admin/shipping-methods` + storefront tree) |
| Fulfillment lifecycle | `SellerMutateFulfillment`, `GetSellerFulfillment`, `ListSellerFulfillments`, `GetAdminFulfillment`, `ListAdminFulfillments` | Seller, Admin |
| Work queue | `ExecuteAdminFulfillmentBulk`, `QueryAdminFulfillmentWorkQueue` | Admin |
| Checkout | `ListCustomerCheckoutFulfillments` | Customer |

## 5. Current illegal / non-canonical dependencies

- **No** foreign `Application`/`Infrastructure`/`Domain` reference (project refs are Contracts-only).
- **No** foreign DbContext/DbSet, no cross-module join.
- **Non-canonical (quality, not ownership):**
  1. `FulfillmentExceptionMapper` classifies expected failures by exact string switch and `catch (… ex) when (TryMapExact(ex.Message, …))`.
  2. `ShippingServiceSemantic.Normalize` throws `InvalidOperationException(code)` on unknown codes.
  3. `Results.Json(new { ok = true })` (ShippingServiceEndpoints ×2) and `Results.Json(new { fulfillments, preferredCustomerTrackingReference })` (CustomerEndpoints).
  4. No `IErrorResourceSet`/`.resx` → unlocalized errors.
  5. `shipping_service_option.code.required` / `.name.required` thrown from Domain and mapped but absent from `FulfillmentErrorCodes` and from every contributor.
  6. `seller.order.missing` / `customer.order.missing` declared and mapped but registered by **no** contributor (Fulfillment comment claims Order owns them; Order does not register them).
  7. `Infrastructure/Queries/` technical-axis folder.
  8. Generic mixed `*Contracts.cs` bundles in `Application/Shipping/`.

## 6. Error-catalog truth (before)

| Metric | Value |
| --- | --- |
| `FulfillmentErrorCodes` declared constants | 19 |
| `FulfillmentErrorCatalogContributor` registered descriptors | 17 |
| Codes recognised by `FulfillmentExceptionMapper` | ~90 |
| Reachable codes with a catalogued descriptor | 17 + shared |
| Reachable codes with **no** descriptor (raw 500 `platform.unexpected`) | the remainder |

## 7. Behavior-preservation checklist

Must remain unchanged: routes (7 groups), HTTP methods, success DTO shapes (including the intentional raw/anonymous envelopes), authorization semantics, business rules, state transitions, ordering, cancellation, idempotency, transaction behavior, persistence semantics, schema, migration IDs/Up-Down, telemetry metric names (`tooba.fulfillment.*`), correlation/trace behavior, tenant/store scoping, public Contracts.

Expected, bounded to change (failure envelope only): previously-unmapped expected failures move from `platform.unexpected` 500 to typed, catalogued, localized statuses.

## 8. Wave plan

- **W0 Analyze** (this wave) — evidence + commit.
- **W1 Migrate** — typed `SemanticException(new SemanticError(code))` instead of the string mapper; complete the error catalog (incl. orphaned/`shipping_service_option.*` codes); add `IErrorResourceSet` + `FulfillmentErrors.{resx,fa.resx}`; replace raw `Results.Json` with the canonical factory where the envelope is not a shipped contract; decompose `FulfillmentDirectory.cs`; split Application `*Contracts.cs` bundles by responsibility.
- **W2 Structure** — capability-first Application tree, remove single-file leaf folders and the technical-axis Validators split, `Infrastructure/Queries` → capability folder, Solution Explorer + manifest + root allowlists, durable structure guards.
- **W3 Certify** — ARCH-COMPLETE-002 certification, SoT + manifest, durable certification guard.

## 9. Certification blockers (to be closed by W1/W2)

- `AD_HOC` / `STRING_HEURISTIC` error mapping.
- `RAW_RESULTS` where the canonical factory applies.
- `UNREGISTERED_CODES` (orphaned + `shipping_service_option.*` + Domain code set).
- `MISSING_INFRASTRUCTURE_USE` localization.
- `TECHNICAL_AXIS_FIRST` / `OVER_FOLDERED` Application tree.
- `MULTI_RESPONSIBILITY_COHESION_VIOLATION` in `FulfillmentDirectory.cs`.
