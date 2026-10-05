# TB-TMAR-INVENTORY-AMSC-001 — Wave 1 (Migrate)

- **Skill:** `tooba-architecture-migrate`
- **Mode:** `ARCHITECT_DIRECT_AMSC`
- **Target:** `src/backend/Modules/Inventory/Tooba.Inventory.*`
- **Parent wave:** `TB-TMAR-INVENTORY-AMSC-001-W0` (`docs/architecture/evidence/TB-TMAR-INVENTORY-AMSC-001-W0/analyze.md`)
- **Starting HEAD:** `c6917553` (`main`, `HEAD == origin/main`)
- **Wave outcome:** `MIGRATED` — every W0 blocker 1–4 and 6 closed
- **Host touched:** NO (only `Tooba.Host.Tests` guard expectations were aligned to the new cohesive file layout; zero Host production change)
- **Behavior preservation:** byte-for-byte observable behavior preserved (see §6)

---

## 1. Scope of this wave

W0 declared `Final-Disposition = READY_TO_MIGRATE` and `Structure-Handoff-State = REQUIRED`.
W1 closes the four production blockers and the cohesion/typing deviation:

| # | W0 blocker | W1 action | Closed |
| --- | --- | --- | --- |
| 1 | `UNREGISTERED_CODES` (23 of 24 codes had no `ErrorDescriptor`) | `InventoryErrorCodes` extended to all emitted codes; `InventoryErrorCatalogContributor` registers every Inventory-owned descriptor | YES |
| 2 | `HARDCODED_TEXT` (no `IErrorResourceSet`, no resx pair) | `InventoryErrorResourceSet` + `InventoryErrors.resx` + `InventoryErrors.fa.resx` | YES |
| 3 | `MULTI_RESPONSIBILITY_COHESION_VIOLATION` (`Directories/InventoryDirectory.cs`, 791 LOC, 6 ports) | split into cohesive partials + the guard moved to its own file | YES |
| 4 | Mixed fault typing (`InvalidOperationException("domain.invariant")` beside typed `ContractOperationException`) | one typed fault mechanism + the `InventoryOperation` composition seam | YES |
| 6 | `Application/Ports/InventoryDirectoryPorts.cs` mixed a result model, an authorization port and a persistence port | split into `IInventoryDirectory.cs`, `IInventoryUseCaseGuard.cs`, `ReservationReceipt.cs` | YES |

---

## 2. Error-code ownership (single owner per code)

`Tooba.Inventory.Contracts/Errors/InventoryErrorCodes.cs` is now the single string owner for every
Inventory-produced machine code (29 declared constants, including the pre-existing
`inventory.quantity.invalid`), with `IsKnown(string?)` used by the composition seam.

`Tooba.Inventory.Contracts/Errors/InventoryErrorCatalogContributor.cs` registers **28** descriptors
with explicit classification, HTTP status and title. Every descriptor is Inventory-owned:

- `inventory.quantity.invalid`, `inventory.location.*`, `inventory.position.*`,
  `inventory.reservation.*` (except the Order-owned retry code), `inventory.catalog_variant.missing`,
  `inventory.adjustment.*`, `inventory.supply.*`, `inventory.manual_review.unavailable`,
  `inventory.return_restock.invalid`, `inventory.outbox.unmapped_event_type`.

Deliberately **not** registered (documented in the contributor header):

- `inventory.reservation.retry_limit_reached` — Order-owned (`OrderErrorCatalogContributor`);
  Payment documents its deliberate non-registration. Inventory consumes, never re-registers.
- `inventory.recovery.*` / `inventory.reservation.stock_mismatch` — produced by Order's recovery
  policy, not by Inventory, so they stay outside the Inventory `Owns()` keyspace.

**Duplicate-descriptor-ownership check:** `ErrorCatalogUniqueCodeGuardTests` stays green
(3/3 passed) — no code is registered twice.

---

## 3. Localization (resource set + bilingual resx)

- `Tooba.Inventory.Contracts/Errors/InventoryErrorResourceSet.cs` implements `IErrorResourceSet`,
  claims `localizationKey.StartsWith("inventory.", StringComparison.OrdinalIgnoreCase)` and delegates
  to `InventoryErrorResources.Manager` (same shape as the certified `BulkInquiry`/`CustomerProfile`
  resource sets).
- `Tooba.Inventory.Contracts/Resources/InventoryErrors.resx` and `InventoryErrors.fa.resx` carry a
  localized entry per machine code; the key **is** the machine code (no key is renamed/repurposed).
- Both singletons are registered in
  `Tooba.Inventory.Infrastructure/DependencyInjection/InventoryModule.cs`:
  `IErrorCatalogContributor → InventoryErrorCatalogContributor` and
  `IErrorResourceSet → InventoryErrorResourceSet`.

---

## 4. One typed fault mechanism + composition seam

- `Tooba.Inventory.Domain/Tooba.Inventory.Domain.csproj` now references
  `Tooba.Inventory.Contracts` so the aggregates raise typed `ContractOperationException` faults
  carrying module-owned codes (the same legal Domain → own-Contracts edge used by CustomerProfile).
- Raw `InvalidOperationException("domain.invariant")` / raw `"inventory.*"` string literals are gone
  from `StockPosition`, `InventoryLocation`, `InventoryDirectory`, `InventoryReturnGateway`,
  `CheckoutInventoryReservationAdapter`, `OrderInventoryLifecycleAdapter` and
  `InventoryOutboxRegistration`.
- `Tooba.Inventory.Application/Composition/InventoryOperation.cs` is the single seam that maps a
  typed `ContractOperationException` (when `InventoryErrorCodes.IsKnown`) and `SemanticException` to
  `Result` / `Result<T>`; unknown codes and unknown exceptions propagate untouched to the canonical
  global boundary.
- The `OrderInventoryLifecycleAdapter` catch filter changed from the fragile
  `ex.Code.StartsWith("inventory.")` prefix sniff to the canonical `InventoryErrorCodes.IsKnown(ex.Code)`.

---

## 5. Cohesion: `Directories/InventoryDirectory.cs` split

`InventoryDirectory` stays one class (all six ports are still implemented by the same scoped
instance, so DI lifetimes and the `IInventoryDirectory`-as-instance projections are unchanged) but is
now physically decomposed into cohesive partials, one reason to change each:

| File | LOC | Responsibility |
| --- | --- | --- |
| `Directories/InventoryDirectory.cs` | 324 | persistence seam: location/position create, adjust, reserve, release, consume, commit, promote + idempotency-conflict mapping |
| `Directories/InventoryDirectory.OrderSupply.cs` | 302 | order-supply engine (`EnsureOrderSupplyAsync`, `EvaluateLinesAsync`, `Resolve*`, outcome mapping) |
| `Directories/InventoryDirectory.Availability.cs` | 84 | availability read projections (single + batch) and the Offer summary projection |
| `Directories/InventoryDirectory.Reclaimer.cs` | 57 | expired-hold batch reclaimer (`FOR UPDATE SKIP LOCKED`) |
| `Directories/InventoryDirectory.SellerWrite.cs` | 47 | seller stock-write use case (`SetInventoryAsync`) returning `Result` |
| `Directories/InventoryDirectory.Lookups.cs` | 43 | Offer/Catalog contract lookups with `IModuleCallTracer` |
| `Directories/OpenInventoryUseCaseGuard.cs` | 11 | the open use-case guard (was a second type inside `InventoryDirectory.cs`) |

791 LOC single-responsibility-violating file → 324 LOC seam + six cohesive collaborators, all under
the 800 LOC `thresholdNewFileLoc`; no baseline entry was required (there was none for Inventory).

`Application/Ports/InventoryDirectoryPorts.cs` (114 LOC, mixed) was replaced by
`Application/Ports/IInventoryDirectory.cs` (74), `Application/Ports/IInventoryUseCaseGuard.cs` (11)
and `Application/Ports/ReservationReceipt.cs` (12).

---

## 6. Behavior preservation

Unchanged, verified by the focused suite:

- **Error codes:** all 24 emitted strings preserved exactly (no rename, no repurpose); codes already
  consumed by Order/Payment/Host tests still match.
- **Business rules:** hold/commit/promote/release/consume semantics, "never revive Released/Consumed",
  idempotent release, the full `EnsureOrderSupply` mode matrix, reuse-vs-reacquire decisions,
  `RemainingQuantity <= 0` and all-lines-secured short-circuits.
- **Concurrency:** atomic `ExecuteUpdateAsync` guards, `FOR UPDATE SKIP LOCKED` per-batch transaction,
  `ix_reservations_idempotency_key` unique-violation → `inventory.reservation.conflict`.
- **Idempotency:** prior-hold replay by `IdempotencyKey`; `os-{mode}-{line}-{checkout}-{ticks}` key
  format and `varchar(128)` constraint.
- **Persistence/schema:** the 4 migrations are untouched; no new migration.
- **Outbox/telemetry:** `inventory.*.v1` event names and `IModuleCallTracer` operation names preserved.
- **DI lifetimes:** all pre-existing registrations preserved; two additive singletons added.
- **Routes:** none in Inventory; Offer still owns `/offers/{offerId:guid}/inventory`.
- **Contracts:** every public type/member/namespace of `Tooba.Inventory.Contracts.*` preserved
  (additive only).

---

## 7. Durable guards added

`Tooba.Inventory.Tests/Architecture/InventoryAmcW1MigrateGuardTests.cs` (7 facts):

1. no raw `InvalidOperationException(` fault anywhere in Inventory production;
2. no raw `"inventory.*"` machine-code literal outside `InventoryErrorCodes.cs`;
3. every declared code is present in the contributor **and** in both resx files;
4. the resource set owns the `inventory.` keyspace and both singletons are registered;
5. the single typed-fault-to-`Result` composition seam exists with the canonical catch filters;
6. `IsKnown` ownership does not claim the foreign `inventory.reservation.retry_limit_reached`
   and does not claim the Order-produced `inventory.recovery.*` keyspace.

---

## 8. Verification performed

| Check | Result |
| --- | --- |
| `Tooba.Inventory.Infrastructure` build (warnings-as-errors on public XML docs) | **succeeded**, 0 warnings |
| `Tooba.Host.Tests` build | **succeeded** |
| `Tooba.Inventory.Tests` | **13/13 passed** |
| `Tooba.Host.Tests` Inventory-behavior subset (`CartLifetimeSeparationTests`, `OrderSupplyFoundationTests`, `UnpaidOrderExpiryTests`, `PaidOrderReservationLifecycleTests`, `OrderInventoryRecoveryTests`, `CheckoutImplW4InventoryLifecycleTests`, `InventoryFoundationTests`) | **37 passed / 5 skipped / 0 failed** |
| `ErrorCatalogUniqueCodeGuardTests` (duplicate descriptor ownership) | **3/3 passed** |
| `Tooba.Host.Tests` full run vs the W0 baseline failure set | **identical 82 pre-existing failures, zero new failures** |

The 82 pre-existing `Tooba.Host.Tests` failures are the repository-wide baseline at the W0 starting
HEAD (unrelated to Inventory: Host Admin/Grid AMC guards, Fulfillment/Cart/Payment behavior and the
global recovery/SoT guards). They were captured before and after this wave and are byte-identical.

Guard expectations that referenced the pre-split physical layout were aligned to the new cohesive
files (aggregate/partial-aware reads) with no behavioral assertion weakened:

- `Host.Tests/CartLifetimeSeparationTests.cs` — now asserts the adapter uses
  `InventoryErrorCodes.SupplyUnavailable` (and no raw `"inventory.` literal) plus the code owner's
  constant, instead of grepping a literal that the migration deliberately removed.
- `Host.Tests/OrderSupplyFoundationTests.cs` — `InvInfra()` aggregates `InventoryDirectory*.cs`.
- `Host.Tests/UnpaidOrderExpiryTests.cs` — reads `InventoryDirectory.OrderSupply.cs` where
  `EnsureOrderSupplyAsync` now lives.

---

## 9. Files changed

Added (production):

```text
Tooba.Inventory.Contracts/Errors/InventoryErrorCatalogContributor.cs
Tooba.Inventory.Contracts/Errors/InventoryErrorResourceSet.cs
Tooba.Inventory.Contracts/Resources/InventoryErrors.resx
Tooba.Inventory.Contracts/Resources/InventoryErrors.fa.resx
Tooba.Inventory.Application/Composition/InventoryOperation.cs
Tooba.Inventory.Application/Ports/IInventoryDirectory.cs
Tooba.Inventory.Application/Ports/IInventoryUseCaseGuard.cs
Tooba.Inventory.Application/Ports/ReservationReceipt.cs
Tooba.Inventory.Infrastructure/Directories/InventoryDirectory.OrderSupply.cs
Tooba.Inventory.Infrastructure/Directories/InventoryDirectory.Availability.cs
Tooba.Inventory.Infrastructure/Directories/InventoryDirectory.SellerWrite.cs
Tooba.Inventory.Infrastructure/Directories/InventoryDirectory.Reclaimer.cs
Tooba.Inventory.Infrastructure/Directories/InventoryDirectory.Lookups.cs
Tooba.Inventory.Infrastructure/Directories/OpenInventoryUseCaseGuard.cs
Tooba.Inventory.Tests/Architecture/InventoryAmcW1MigrateGuardTests.cs
```

Removed / rewritten:

```text
Tooba.Inventory.Application/Ports/InventoryDirectoryPorts.cs   (split)
Tooba.Inventory.Infrastructure/Directories/InventoryDirectory.cs (reduced to the persistence seam)
```

Modified: `InventoryErrorCodes.cs`, `InventoryModule.cs`, `Tooba.Inventory.Domain.csproj`, the three
Domain aggregates, `InventoryReturnGateway.cs`, `CheckoutInventoryReservationAdapter.cs`,
`OrderInventoryLifecycleAdapter.cs`, `InventoryOutboxRegistration.cs`,
`InventoryArchitectureGuardTests.cs` and the three Host.Tests guards above.

---

## 10. Handoff to W2 (Structure)

`Structure-Handoff-State = REQUIRED` (unchanged from W0, now against the new physical layout):

1. confirm every new path ↔ namespace is exact (`Directories` partials, `Application/Composition`,
   `Application/Ports`, `Contracts/Errors`, `Contracts/Resources`);
2. extend the module guard allowlists for the new folders (`Composition` already allow-listed);
3. add durable structure guards: no `Tooba.Inventory.Endpoints`, no endpoint-reachable request,
   root allowlists, no stale/duplicate copy;
4. verify the `/Modules/Inventory/` solution grouping in `src/backend/Tooba.slnx`.

W3 certification blockers remaining after W1: **none of the W0 production blockers**; only the
SoT/manifest/durable-cert-guard honesty items (W0 blocker 5), which W3 owns.
