# TB-TMAR-INVENTORY-AMSC-001 — Wave 0 (Analyze)

- **Skill:** `tooba-architecture-analyze` (V2)
- **Mode:** `ARCHITECT_DIRECT_AMSC`
- **Target:** `src/backend/Modules/Inventory/Tooba.Inventory.*`
- **Starting HEAD:** `9ec8e3e976a826ce13723d15e130239dc35bd1d8`
- **Branch:** `main` (`HEAD == origin/main`, clean of unrelated work)
- **Production code changed in this wave:** NONE (analysis only)
- **Host touched in this wave:** NONE

---

## 1. Target analyzed

| Project | Role | Files |
| --- | --- | --- |
| `Tooba.Inventory.Contracts` | module-boundary contracts + stable error codes | 9 |
| `Tooba.Inventory.Domain` | aggregates / value objects / domain events | 11 |
| `Tooba.Inventory.Application` | internal use-case seams, ports, order-supply models | 4 |
| `Tooba.Inventory.Infrastructure` | persistence, directory, adapters, outbox, DI | 16 (+4 migrations) |
| `Tooba.Inventory.Tests` | behavior + architecture guards | 2 |

`Tooba.Inventory.Endpoints` does not exist and must not exist — Inventory is `INTERNAL_ONLY`
(`docs/architecture/tmar-current-state.json` → `completeReferenceModules[Inventory].httpApplicability = INTERNAL_ONLY`,
`endpointOwnership = NOT_APPLICABLE`, `cqrs = INTERNAL_USE_CASE_BOUNDARIES`).

HTTP surface for seller stock writes is owned by **Offer**
(`Tooba.Offer.Endpoints/Seller/OfferSellerEndpoints.cs` → `/offers/{offerId:guid}/inventory` →
`SetOfferInventoryCommand` → `ISellerOfferInventoryGateway` from `Tooba.Inventory.Contracts.Seller`).

---

## 2. Structured State Fields

| Field | Value |
| --- | --- |
| **Foundation-State** | `FOUNDATION_READY` (module already `COMPLETE_REFERENCE_PATTERN`; `INTERNAL_ONLY` so no Endpoints project is required) |
| **Ownership-State** | `correct` (Host owns zero Inventory persistence/business authority) |
| **File-Cohesion-State** | `MULTI_RESPONSIBILITY_COHESION_VIOLATION` (1 file) |
| **Oversized/God-File-State** | `Directories/InventoryDirectory.cs` = 791 LOC — not over the 800 LOC ceiling but a genuine multi-responsibility god-file (see §7) |
| **Localization-State** | `HARDCODED_TEXT` — 24 `inventory.*` fault codes emitted as raw string literals; only 1 code catalogued; no error catalog contributor, no `IErrorResourceSet`, no `.resx`, no `.fa.resx` |
| **API-Result-Pattern-State** | `CANONICAL` (no Endpoints; the one `Result`-returning seam is `SetInventoryAsync`) |
| **Stable-Error-Code-State** | `UNREGISTERED_CODES` (23 of 24 codes unregistered) |
| **Logging-State** | `CANONICAL` (no `Console.WriteLine`, no second logger framework, no ad-hoc logging) |
| **Sensitive-Logging-State** | `NONE` |
| **OpenTelemetry-State** | `CANONICAL` (`IModuleCallTracer` used for Inventory→Offer and Inventory→Catalog lookups) |
| **Correlation-Trace-State** | `CANONICAL` (no competing correlation, no raw `StartActivity`, no `traceparent` parsing) |
| **CQRS-State** | `COMPLIANT_FOR_INTERNAL_ONLY` (no HTTP use cases → no MediatR ceremony required; contracts-port seams are the canonical `INTERNAL_USE_CASE_BOUNDARIES` shape) |
| **Validator-Coverage-State** | `NOT_APPLICABLE_INTERNAL_ONLY` (zero endpoint-reachable requests; no validator classification required) |
| **Contracts-Boundary-State** | `CLEAN` (only missing piece: error-catalog contributor + resource set) |
| **Cross-Module-Coupling-State** | `LEGAL_CONTRACTS_ONLY` (see §5 — no foreign Application/Domain/Infrastructure reference) |
| **Cross-Module-Join-State** | `NONE` (only same-schema joins `Positions ⋈ Locations`; foreign lookups are contract calls) |
| **Persistence-Ownership-State** | `CORRECT` (single `InventoryDbContext`, schema `inventory`) |
| **Endpoint-Ownership-State** | `NOT_APPLICABLE` (internal-only; Offer owns the seller HTTP surface) |
| **Host-Residue-State** | `ALLOWED_COMPOSITION_ROOT_ONLY` |
| **Schema-Migration-State** | `UNCHANGED` (4 migrations; no new migration needed for this AMSC wave) |
| **Behavior-Preservation-Risk** | `LOW` |
| **Canonical-Reference-Used** | `BulkInquiry.Contracts/Errors` + `BulkInquiry.Contracts/Resources` (catalog + resource set + resx pair); `CustomerProfile.Application/Composition/CustomerProfileOperation` + `CustomerProfile.Endpoints/Errors|Resources` (operation seam + catalog + resx pair); `Order`/`Payment` for shared-code non-re-registration; `Offer` for `IModuleCallTracer` decoration; `BuildingBlocks` for `ContractOperationException`/`SemanticError`/`IErrorCatalogContributor`/`IErrorResourceSet` |
| **Folder-Granularity-State** | `PROFESSIONAL_SHALLOW_WITH_ONE_DEVIATION` |
| **Structure-Handoff-State** | `REQUIRED` |
| **Final-Disposition** | `READY_TO_MIGRATE` |

---

## 3. Responsibility map

| Responsibility | Current location | Classification |
| --- | --- | --- |
| Stock position aggregate + invariants | `Domain/Aggregates/StockPosition.cs` | `DOMAIN_RULE` |
| Reservation aggregate + lifecycle | `Domain/Aggregates/StockReservation.cs` | `DOMAIN_RULE` |
| Storage-location aggregate | `Domain/Aggregates/InventoryLocation.cs` | `DOMAIN_RULE` |
| Domain events (5) + status/kind enums (3) | `Domain/Events`, `Domain/ValueObjects` | `DOMAIN_RULE` |
| Inventory write/read directory (791 LOC) | `Infrastructure/Directories/InventoryDirectory.cs` | `PERSISTENCE` + `APPLICATION_USE_CASE` + `INTEGRATION_ADAPTER` → **MUST_SPLIT** |
| Availability read port impl | same file | `PERSISTENCE` → split |
| Seller stock write port impl (`SetInventoryAsync`) | same file | `APPLICATION_USE_CASE` → split |
| Order supply engine (`EnsureOrderSupplyAsync`/`EvaluateLinesAsync`/`Resolve*`) | same file | `APPLICATION_USE_CASE` → split |
| Expired-hold reclaimer (`ReleaseExpiredHoldsAsync`, `FOR UPDATE SKIP LOCKED`) | same file | `PERSISTENCE` → split |
| Offer/Catalog cross-module lookups + tracing | same file | `INTEGRATION_ADAPTER` → split |
| Outbox / integration events | `Infrastructure/Messaging`, `Infrastructure/Events` | `INTEGRATION_ADAPTER` |
| Query gateway / return gateway / dev-seed / schema-migrator adapters | `Infrastructure/Adapters` | `PERSISTENCE` / `DEVELOPMENT_SEED` |
| Module composition + DI | `Infrastructure/DependencyInjection/InventoryModule.cs` | `HOST_COMPOSITION_ROOT` (module-owned) |
| EF model + migrations | `Infrastructure/Persistence` | `PERSISTENCE` |
| Internal ports + order-supply models | `Application/Ports`, `Application/Orders` | `APPLICATION_USE_CASE` |
| Checkout reservation seam | `Application/Checkout` | `APPLICATION_USE_CASE` |
| Boundary DTOs/ports + `InventoryErrorCodes` | `Contracts/*` | `CONTRACT` |
| Host module registration | `Host/Composition/ToobaModuleComposition.cs` (line 63 `new InventoryModule()`) | `HOST_COMPOSITION_ROOT` |
| Host migration-runner registration | `Host/Tooba.MigrationRunner/ModuleMigrationRegistry.cs` (line 53) | `HOST_COMPOSITION_ROOT` |
| Host dev-seed comment | `Host/Development/DevelopmentSchemaMigrator.cs` (line 24, comment only) | `HOST_COMPOSITION_ROOT` (no code) |

---

## 4. Ownership map

Every responsibility maps to **Inventory**. No responsibility in the module belongs to Host, Cart,
Order, Offer, Returns or Fulfillment:

- cart hold release → owned by Inventory as `Contracts/Cart/ICartInventoryHoldPort` (consumed by Cart);
- order lifecycle → owned by Inventory as `Contracts/Orders/IOrderInventoryLifecyclePort` (consumed by Order);
- fulfillment consume/commit → owned by Inventory as `Contracts/Fulfillment/IFulfillmentInventoryLifecyclePort`;
- return restock → owned by Inventory as `Contracts/Returns/IInventoryReturnGateway`;
- seller stock write → owned by Inventory as `Contracts/Seller/ISellerOfferInventoryGateway`.

Host references (3 sites) are `ALLOWED_COMPOSITION_ROOT` only. Zero Host residue folder:
`src/backend/Host/Tooba.Host/Inventory` does **not** exist.

---

## 5. Current illegal dependencies

**None.** Verified absence of:

- `Tooba.Inventory.* → foreign .Application / .Infrastructure / .Domain`;
- foreign `DbContext` / `DbSet` reach-through;
- cross-module EF/SQL join;
- `TypeForwardedTo`;
- namespace alias hiding placement;
- Inventory → Host dependency.

Legal cross-module references (contracts only):

| From | To | Kind |
| --- | --- | --- |
| `Inventory.Application` | `Tooba.Offer.Contracts` | `IOfferLookupGateway`, `OfferErrorCodes` |
| `Inventory.Infrastructure` | `Tooba.Offer.Contracts` | `IOfferLookupGateway`, `IOfferLookupGateway`/`OfferErrorCodes` |
| `Inventory.Infrastructure` | `Tooba.Catalog.Contracts` | `ICatalogVariantLookup` |
| `Inventory.*` | `Tooba.BuildingBlocks`, `Tooba.ModuleContracts`, `Tooba.Persistence` | foundation |

Consumers of Inventory contracts (inbound, legal):

| From | To | Kind |
| --- | --- | --- |
| `Offer.Application` | `Inventory.Contracts.Seller` | `ISellerOfferInventoryGateway` |
| `Cart.*` | `Inventory.Contracts.Cart` | `ICartInventoryHoldPort` |
| `Order.*` | `Inventory.Contracts.Orders` / `.Checkout` | `IOrderInventoryLifecyclePort`, `ICheckoutInventoryReservationPort` |
| `Fulfillment.*` | `Inventory.Contracts.Fulfillment` | `IFulfillmentInventoryLifecyclePort` |
| `Returns.*` | `Inventory.Contracts.Returns` | `IInventoryReturnGateway` |
| `Host/Development` | `Inventory.Contracts.Availability` | `IInventoryDevelopmentSeedGateway`, `IInventorySchemaMigrator`, `IInventoryQueryGateway` |

**`Contracts-Boundary-State = CLEAN`.**

---

## 6. Cross-module join inventory

`NONE`.

The only joins in the module are **same-schema, same-DbContext** joins:

- `InventoryDirectory.GetAvailabilityAsync` — `Positions ⋈ Locations` (both `inventory` schema);
- `InventoryDirectory.GetAvailabilityBatchAsync` — same, batched to avoid N+1.

Foreign data (`Offer`, `Catalog`) is fetched through contract ports decorated with
`IModuleCallTracer.Begin("Inventory", "Offer"|"Catalog", …)` — no foreign table is touched.
`StockPosition.OfferId` / `CatalogVariantId` are deliberately FK-free identifiers.

---

## 7. MUST_SPLIT decisions

### `Infrastructure/Directories/InventoryDirectory.cs` — `MULTI_RESPONSIBILITY_COHESION_VIOLATION`

791 LOC, 5 unrelated reasons to change, 6 port interfaces implemented in one class:

1. `IInventoryDirectory` — location/position/adjust/reserve/release/consume/commit/promote;
2. `IInventoryAvailabilityGateway` — availability read projections;
3. `ISellerOfferInventoryGateway` — seller stock write use case;
4. `Tooba.Inventory.Contracts.Cart.ICartInventoryHoldPort` — cart hold release;
5. `IFulfillmentInventoryLifecyclePort` — fulfillment consume/commit;
6. plus the order-supply engine (`EnsureOrderSupplyAsync`, `EvaluateLinesAsync`,
   `ResolveAvailableAsync`, `ResolveStockItemIdAsync`, `MapOutcome`, `IsTimedHold`) and the
   expired-hold batch reclaimer with raw SQL.

It also declares a second unrelated type in the same file (`OpenInventoryUseCaseGuard`), hosts the
Offer/Catalog lookup+tracing helpers, and holds the only PostgreSQL-specific `PostgresException`
constraint sniffing.

Split plan (behavior-preserving, §12): keep `InventoryDirectory` as the persistence seam; move the
order-supply engine, the expired-hold reclaimer, the seller write use case, the availability read
projections and the guard into their own cohesive files in the same project.

No other file is oversized or multi-responsibility. `OrderInventoryLifecycleAdapter.cs` (234 LOC) is
cohesive. `Contracts/Orders/OrderInventoryLifecycleContracts.cs` (137 LOC) is a single-capability
contract file, not a mixed `*Contracts.cs` dump.

---

## 8. CQRS / MediatR gaps

`NONE_APPLICABLE`. Inventory has zero endpoint-reachable requests, therefore:

- no `IRequest`/`IRequestHandler` is required;
- no `ISender` dispatch is required;
- no MediatR registration is required;
- adding CQRS ceremony here would violate the Migrate skill's "do not create CQRS ceremony for
  internal-only code" rule.

The canonical internal shape (contracts port + Infrastructure adapter) is already in place and
matches the accepted `INTERNAL_USE_CASE_BOUNDARIES` SoT value.

---

## 9. Validation classification matrix

| Request | Classification | Reason |
| --- | --- | --- |
| — | — | Inventory exposes no endpoint-reachable request |

`Validator-Coverage-State = NOT_APPLICABLE_INTERNAL_ONLY` (0 required / 0 no-validator-required).
A durable W2 guard must assert that Inventory keeps zero endpoint-reachable requests and keeps
`Tooba.Inventory.Endpoints` absent, so a future HTTP surface cannot bypass classification.

---

## 10. Localization findings

24 distinct `inventory.*` machine codes are emitted; only **1** (`inventory.quantity.invalid`) is
declared in `Contracts/Errors/InventoryErrorCodes.cs`, and **0** are registered in a canonical
`IErrorCatalogContributor`. There is no `IErrorResourceSet`, no `InventoryErrors.resx` and no
`InventoryErrors.fa.resx`.

Uncatalogued codes emitted as raw string literals:

| Code | Emitted at |
| --- | --- |
| `inventory.location.id_required` | `Domain/Aggregates/InventoryLocation.cs:56` |
| `inventory.location.code_invalid` | `InventoryLocation.cs:61` |
| `inventory.location.name_required` | `InventoryLocation.cs:66` |
| `inventory.position.ids_required` | `Domain/Aggregates/StockPosition.cs:77` |
| `inventory.position.quantity_invalid` | `StockPosition.cs:146` |
| `inventory.reservation.id_required` | `Domain/Aggregates/StockReservation.cs:70` |
| `inventory.reservation.quantity_invalid` | `StockReservation.cs:75` |
| `inventory.reservation.expiry_invalid` | `StockReservation.cs:80` |
| `inventory.reservation.not_active` | `StockReservation.cs:105,110,131,136,161` |
| `inventory.reservation.review_expiry_invalid` | `StockReservation.cs:141` |
| `inventory.catalog_variant.missing` | `Directories/InventoryDirectory.cs:203` |
| `inventory.adjustment.quantity_invalid` | `InventoryDirectory.cs:242` |
| `inventory.adjustment.kind_unknown` | `InventoryDirectory.cs:253` |
| `inventory.supply.unavailable` | `InventoryDirectory.cs:307`, `Application/Checkout/CheckoutInventoryReservationAdapter.cs:56,63` |
| `inventory.reservation.conflict` | `InventoryDirectory.cs:321` |
| `inventory.reservation.not_found` | `InventoryDirectory.cs:406,437,474,478,489,493`, `Adapters/InventoryReturnGateway.cs:66`, `Application/Orders/OrderInventoryLifecycleAdapter.cs:77` |
| `inventory.reservation.release_mismatch` | `InventoryDirectory.cs:422` |
| `inventory.manual_review.unavailable` | `InventoryDirectory.cs:624`, `OrderInventoryLifecycleAdapter.cs:97` |
| `inventory.supply.mode_invalid` | `OrderInventoryLifecycleAdapter.cs:219` |
| `inventory.outbox.unmapped_event_type` | `Messaging/InventoryOutboxRegistration.cs:117` |

Additional non-canonical, non-typed faults (raw `InvalidOperationException("domain.invariant")`):

`InventoryDirectory.cs:201,208,247,256,266,449`, `Adapters/InventoryReturnGateway.cs:51,57,71,87,94`.

Mixed exception typing is the second localization/typing defect: `Domain/Aggregates/StockReservation.cs`
already uses typed `ContractOperationException` while `StockPosition.cs` / `InventoryLocation.cs` use raw
`InvalidOperationException` with a string code — two parallel fault mechanisms in one Domain.

**Preserved semantics (must not change):** every existing code string is consumed by another module
or by a Host test — e.g. `Order.Application/.../RetryCustomerUnpaidOrderCommand.cs:70`,
`Order.Application/Admin/InventoryRecovery/Services/OrderInventoryRecoveryService.cs`,
`Payment.Application/Errors/PaymentExceptionMapper.cs:43`,
`Host.Tests/CartLifetimeSeparationTests.cs:50`, `OrderSupplyFoundationTests.cs:84`,
`PaidOrderReservationLifecycleTests.cs:129/140/153/362`. Codes are therefore **added to the
catalogue with identical strings**, never renamed.

**Descriptor-ownership collision check (already resolved in the repository):**

- `inventory.reservation.retry_limit_reached` — declared in `PaymentErrorCodes.ReservationRetryLimit`
  and `ReservationCycleErrors.RetryLimitReached`, but explicitly **Order-owned**
  (`OrderErrorCatalogContributor` line 114; `PaymentErrorCatalogContributor` line 28 documents the
  deliberate non-registration). Inventory must consume, not register.
- `inventory.supply.unavailable` — consumed by Order (`RetryCustomerUnpaidOrderCommand`,
  `OrderInventoryRecoveryService`) and Payment (`PaymentExceptionMapper`). No module registers a
  descriptor today. Under the canonical rule "duplicate usage allowed, duplicate descriptor
  ownership not", Inventory is the natural bounded-context owner (the code is produced by the
  Inventory directory) and becomes the single registering owner; Order/Payment keep consuming it.
- `inventory.*` recovery-policy codes produced by Order itself
  (`inventory.recovery.required`, `inventory.recovery.insufficient`, `inventory.recovery.rollback_failed`,
  `inventory.reservation.stock_mismatch`) are **not** Inventory-produced and stay outside the
  Inventory contributor so the Inventory `Owns()` keyspace cannot shadow Order-owned semantics.

---

## 11. API result / error mapping findings

- `CANONICAL` at the only presentation-adjacent seam: `ISellerOfferInventoryGateway.SetInventoryAsync`
  returns `Result` (`Result.Failure(new SemanticError(InventoryErrorCodes.QuantityInvalid))`,
  `Result.Failure(new SemanticError(OfferErrorCodes.NotFound))`, `Result.Success()`), consumed by
  `Offer.Application/Offers/Commands/SetOfferInventory/SetOfferInventoryCommand.cs`.
- No `Results.Json` / `Results.BadRequest` / `Results.Problem`, no local `ProblemDetails` builder, no
  `ex.Message` parsing in the module.
- Defect: `InventoryDevelopmentSeedGateway` returns `Result.Failure(new SemanticError(...))` for
  **domain** rules (negative quantity), and the raw-exception seams above cannot be surfaced as
  `Result` at all. W1 introduces the single operation seam so typed faults become `Result` failures
  by stable code.

---

## 12. Logging / sensitive-data findings

- `Logging-State = CANONICAL`: no `Console.WriteLine`, no `Debug.WriteLine`, no custom logger, no
  second telemetry pipeline. The module simply does not log (nothing to log; the directory is
  covered by `IModuleCallTracer` and outbox integration events).
- `Sensitive-Logging-State = NONE`. No passwords/tokens/secrets/`Authorization` headers/cookies/
  query strings/payment payloads are logged or passed to telemetry.
- `IModuleCallTracer.Begin(...)` is used with stable operation names `LookupOffer` / `LookupVariant`;
  event names in `InventoryOutboxRegistration` are stable `inventory.<event>.v1` — both preserved.

---

## 13. OpenTelemetry / correlation findings

- `OpenTelemetry-State = CANONICAL`; `Correlation-Trace-State = CANONICAL`.
- No direct `ActivitySource.StartActivity`, no manual `traceparent` parsing, no second correlation
  header, no `AsyncLocal` correlation, no `Guid.NewGuid()`-as-correlation.
- `IClock` / `IIdGenerator` are injected (no `DateTimeOffset.UtcNow` / `Guid.NewGuid()` in production
  sources) — a durable guard already enforces this and it must stay green.
- Cross-module calls keep `IModuleCallTracer` decoration; W1/W2 must not remove it.

---

## 14. File cohesion / splitting plan

| File | LOC | Verdict | Action |
| --- | --- | --- | --- |
| `Infrastructure/Directories/InventoryDirectory.cs` | 791 | `MULTI_RESPONSIBILITY_COHESION_VIOLATION` | split in W1 (see §7) |
| `Application/Orders/OrderInventoryLifecycleAdapter.cs` | 234 | `COHESIVE` | keep |
| `Contracts/Orders/OrderInventoryLifecycleContracts.cs` | 137 | `COHESIVE` | keep |
| all other production files | ≤ 128 | `COHESIVE` | keep |

No file exceeds the 800 LOC `thresholdNewFileLoc`; `tmar-source-size-baseline.json` has **no** Inventory
entry, so no baseline update is required. Splitting is justified by responsibility count, not by LOC.

---

## 15. Exact target paths / namespaces

Additive, capability-first, namespace-equals-path:

```text
Tooba.Inventory.Contracts/Errors/
    InventoryErrorCodes.cs               (extend: all emitted inventory.* codes)
    InventoryErrorCatalogContributor.cs  (new; single descriptor owner)
    InventoryErrorResourceSet.cs         (new; Owns("inventory."))
Tooba.Inventory.Contracts/Resources/
    InventoryErrors.resx                 (new)
    InventoryErrors.fa.resx              (new)

Tooba.Inventory.Application/Composition/
    InventoryOperation.cs                (new; ContractOperationException -> Result seam)
Tooba.Inventory.Application/Orders/
    OrderSupplyEngine.cs                 (new; extracted from InventoryDirectory)
    ExpiredReservationReclaimer.cs       (new; extracted from InventoryDirectory)

Tooba.Inventory.Infrastructure/Directories/
    InventoryDirectory.cs                (reduced to the persistence seam)
    OpenInventoryUseCaseGuard.cs         (new; extracted)
Tooba.Inventory.Infrastructure/Adapters/
    SellerOfferInventoryWriter.cs        (new; extracted seller write use case)
    InventoryAvailabilityReader.cs       (new; extracted availability projections)
```

Existing folders are already canonical: `Contracts/{Availability,Cart,Checkout,Errors,Fulfillment,Orders,Returns,Seller}`,
`Domain/{Aggregates,Events,ValueObjects}`, `Application/{Checkout,Orders,Ports}`,
`Infrastructure/{Adapters,Directories,DependencyInjection,Events,Messaging,Persistence,Persistence/Migrations}`.

### Folder-Granularity-State deviation

`Application/Ports/InventoryDirectoryPorts.cs` is a **mixed Application file** holding
`ReservationReceipt` (a result model), `IInventoryUseCaseGuard` (an authorization port) and
`IInventoryDirectory` (a persistence port). It is not a `*Contracts.cs` dump and not a request
folder, but capability-first shallow grouping requires splitting it into
`Application/Ports/InventoryDirectoryPort.cs` (+ `Application/Models/ReservationReceipt.cs`).
No single-file request leaf folders exist and no technical-axis-first request tree exists.

---

## 16. Behavior-preservation checklist

Must remain byte-for-byte equivalent in observable behavior:

- **Routes:** none in Inventory (Offer owns `/offers/{offerId:guid}/inventory`).
- **Contracts:** every public type, member name, signature and namespace of
  `Tooba.Inventory.Contracts.*` is preserved; no DTO shape change.
- **Stable error codes:** all 24 code strings preserved exactly, including the ones already consumed
  by Order/Payment/Host tests.
- **Business rules:** reservation hold/commit/promote/release/consume semantics; the "never revive
  Released/Consumed" rule; idempotent release; `EnsureOrderSupply` mode matrix
  (`CheckOnly`/`EnsureReviewHold`/`EnsurePaidDurable`/`EnsureFulfillmentSupply`/`EnsureUnpaidRetryHold`);
  reuse-vs-reacquire decisions; `RemainingQuantity <= 0` short-circuits; all-lines-secured short-circuit.
- **Concurrency:** atomic `ExecuteUpdateAsync` guards (`OnHand - Reserved >= quantity && quantity > 0`,
  `Reserved >= quantity`, `OnHand >= quantity`); `FOR UPDATE SKIP LOCKED` batch reclaim with
  transaction commit per batch; `ix_reservations_idempotency_key` unique-violation mapping to
  `inventory.reservation.conflict`.
- **Idempotency:** `ReserveAsync` prior-hold replay by `IdempotencyKey`; `CommitForPaidOrder` /
  `PromoteForManualPaymentReview` idempotence; `ReturnRestockInbox` dedup; the compact
  `os-{mode}-{line}-{checkout}-{ticks}` key format and the `varchar(128)` constraint.
- **Persistence/schema:** 4 migrations unchanged (`InitialInventory`, `ReservationExpiry`,
  `ReturnRestockInbox`, `DecimalInventoryQuantity`); table/column/index/constraint semantics unchanged;
  `numeric(18,6)` preserved; no new migration.
- **Outbox/telemetry:** `inventory.adjusted.v1`, `inventory.reserved.v1`, `inventory.released.v1`,
  `inventory.reservation_consumed.v1`, `inventory.availability_changed.v1` event names and payload
  shapes preserved; `IModuleCallTracer` operation names preserved.
- **DI lifetimes:** all existing registrations preserved (including the
  `IInventoryDirectory`-as-singleton-instance projection for the three gateway interfaces).
- **Tenant/store scoping:** `ToobaNpgsql.ResolveForContext` + `InventoryDbContext.Schema` preserved.
- **Localization keys:** new keys equal the machine codes; no existing key renamed or repurposed.

---

## 17. Migration order

1. **W1 (Migrate)** — error typing + localization + cohesion:
   1. extend `InventoryErrorCodes` with every emitted `inventory.*` code (identical strings);
   2. add `InventoryErrorCatalogContributor` + `InventoryErrorResourceSet` + `InventoryErrors.resx` /
      `.fa.resx`; register contributor + resource set in `InventoryModule`;
   3. add `Application/Composition/InventoryOperation.cs` (typed `ContractOperationException` → `Result`);
   4. replace raw `InvalidOperationException("domain.invariant")` and raw string literals with typed
      faults; remove parallel fault mechanisms;
   5. split `InventoryDirectory.cs` into the files in §15 (behavior-preserving);
   6. split `Application/Ports/InventoryDirectoryPorts.cs`;
   7. clean using directives (no unused/duplicate).
2. **W2 (Structure)** — structure normalization:
   1. confirm every new path ↔ namespace is exact;
   2. update the module architecture guard allowlists (`Application/Composition`, new folders);
   3. add/extend durable structure guards: no `Tooba.Inventory.Endpoints`, no endpoint-reachable
      request, root allowlists, path↔namespace, no stale/duplicate copy;
   4. verify `/Modules/Inventory/` solution grouping in `src/backend/Tooba.slnx` (already present).
3. **W3 (Certify)** — certification:
   1. add `InventoryModuleAmsc001W3CertGuardTests.cs`;
   2. update `tmar-current-state.json` (W0–W3 records) and `tmar-module-structure-manifests.json`
      (first Inventory entry, `structureCertified: true`, `lockVersion: ARCH-COMPLETE-002`);
   3. record the checkpoint in `TOOBA-TMAR-MASTER-RECOVERY.md`;
   4. run focused builds/tests and the durable guards.

---

## 18. Verification plan

| Wave | Focused validation |
| --- | --- |
| W0 | none (analysis only); `git status` clean of production changes |
| W1 | build `Tooba.Inventory.{Contracts,Domain,Application,Infrastructure}`; run `Tooba.Inventory.Tests`; run `ErrorCatalogUniqueCodeGuardTests` (proves no duplicate descriptor ownership); run `Tooba.Host.Tests` Inventory behavior tests (`CartLifetimeSeparationTests`, `OrderSupplyFoundationTests`, `PaidOrderReservationLifecycleTests`, `OrderInventoryRecoveryTests`, `CheckoutImplW4InventoryLifecycleTests`) |
| W2 | run `Tooba.Inventory.Tests` architecture guard; run the new structure guard; confirm `Tooba.slnx` `/Modules/Inventory/` grouping |
| W3 | run `InventoryModuleAmsc001W3CertGuardTests`, `TmarDurableGuardTests`, `ErrorCatalogUniqueCodeGuardTests`, `HostModuleEndpointOwnershipTests`, `TmarSourceSizeAndInfraAppTests` |

Bounded rule: one deterministic repair per failing focused check, then stop and report.

---

## 19. Certification blockers

Blockers that **must** be closed before `ARCH-COMPLETE-002` certification:

1. `UNREGISTERED_CODES` — 23 of 24 emitted `inventory.*` codes have no `ErrorDescriptor`; the composed
   catalog cannot classify them and `SafeErrorMapper` falls back to a generic 400.
2. `HARDCODED_TEXT` — no `IErrorResourceSet`, no `InventoryErrors.resx`, no `InventoryErrors.fa.resx`.
3. `MULTI_RESPONSIBILITY_COHESION_VIOLATION` — `Directories/InventoryDirectory.cs` (791 LOC, 6 ports,
   god-file).
4. Mixed fault typing — raw `InvalidOperationException("domain.invariant")` beside typed
   `ContractOperationException`; no single operation seam converting typed faults to `Result`.
5. SoT/manifest honesty — no AMSC SoT record, no Master Recovery checkpoint, no ARCH-COMPLETE-002
   durable guard, no AMSC evidence tree.
6. `Application/Ports/InventoryDirectoryPorts.cs` mixes a result model, an authorization port and a
   persistence port in one file.

Non-blocking observations (recorded, no action required):

- `Application/Ports/InventoryDirectoryPorts.cs:79-83` carries a duplicated `<summary>` block
  (documentation defect).
- `InventoryDirectory.AdjustAsync` discards `idempotencyKey` via `_ = idempotencyKey;` — intentional
  today; preserved as-is.
- The module has no `ILogger<T>` usage at all — acceptable, not a violation.

---

## 20. Disposition

`READY_TO_MIGRATE`

`Structure-Handoff-State = REQUIRED` (W1/W2 change physical file placement; final structural verdict
belongs to the Structure skill).
