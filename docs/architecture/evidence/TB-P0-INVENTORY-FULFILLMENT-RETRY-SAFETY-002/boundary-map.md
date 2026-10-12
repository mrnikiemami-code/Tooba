# TB-P0-INVENTORY-FULFILLMENT-RETRY-SAFETY-002 — Gate A: boundary map

Base SHA: `04e3aa79f1fcf26c2c4c17f10ea1d3fce99dac91` (parent task TB-P0-FULFILLMENT-PACKAGE-DISPATCH-ATOMICITY-001)
Mode: design/decision gate. Read-only analysis. **No production code changed.**

## 1. Actual call chain (dispatch → consumption)

| # | Step | File / line | Persistence owner |
|---|------|-------------|-------------------|
| 1 | `DispatchConsolidatedPackageAsync` validates all members, then in one write phase loops `ApplyShipmentDispatched` + `ConsumeInventoryForShipmentAsync` + `MarkDispatched`, then a single `_db.SaveChangesAsync()` | `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Infrastructure/Directories/FulfillmentDirectory.Packages.cs:250,274,284-290` | `fulfillment` schema |
| 2 | `ConsumeInventoryForShipmentAsync(FulfillmentUnit, Shipment, ct)` — per shipment line: skip when `ReservationId is null \|\| ReservationConsumed`; skip unless `QuantityShipped >= QuantityOrdered`; otherwise call the inventory gateway and set the flag in memory | `.../Directories/FulfillmentDirectory.cs:509-527` | Fulfillment read-model in memory |
| 3 | `FulfillmentInventoryGateway.ConsumeReservationAsync` → `IFulfillmentInventoryLifecyclePort.ConsumeAsync` (pass-through, no idempotency key) | `.../Fulfillment.Infrastructure/Gateways/FulfillmentInventoryGateway.cs` | — |
| 4 | Port definition: **only** `ConsumeAsync(Guid reservationId, ct)` and `CommitReservationForPaidOrderAsync(Guid reservationId, ct)` | `src/backend/Modules/Inventory/Tooba.Inventory.Contracts/Fulfillment/FulfillmentInventoryLifecyclePort.cs` | Contracts (public seam) |
| 5 | `InventoryDirectory.ConsumeAsync` — load reservation → immediate guarded `ExecuteUpdateAsync` on `stock_positions` → `MoveTo(Consumed)` → `RecordConsumed` → `SaveChangesAsync` | `.../Inventory.Infrastructure/Directories/InventoryDirectory.cs:270-294` | `inventory` schema |
| 6 | `StockReservation.MoveTo` rejects any transition when `Status != Held` (`inventory.reservation.not_active`) and **clears `IdempotencyKey`** | `.../Inventory.Domain/Aggregates/StockReservation.cs:157-166` | `inventory.reservations` |
| 7 | `StockPosition.RecordConsumed` raises `StockReservationConsumedDomainEvent` only (no persisted consumption row) | `.../Inventory.Domain/Aggregates/StockPosition.cs` | domain event → outbox |
| 8 | Outbox interceptor appends `inventory.reservation_consumed.v1` in the **same** `SaveChangesAsync` as the reservation status change | `src/backend/BuildingBlocks/Tooba.Persistence/OutboxSaveChangesInterceptor.cs:33-46,66-148` | `inventory.outbox_messages` |

## 2. Reservation identity and ownership (what actually binds a reservation)

* `reservations` PK: `ReservationId` (`ValueGeneratedNever`), columns `StockItemId`, `Quantity numeric(18,6)`, `Status varchar(32)`, `ExternalReference varchar(128)`, `IdempotencyKey varchar(128)` with a **filtered unique index** (`idempotency_key IS NOT NULL`), `CreatedAt/UpdatedAt/ExpiresAt` — `InventoryDbContext.OnModelCreating`.
* **There is no owner/tenant/consumer column.** Tenant isolation is physical: one database per tenant (`tooba_alpha` for tenant-alpha). Nothing inside the row proves which Fulfillment/shipment/order process is entitled to consume it.
* `IFulfillmentInventoryLifecyclePort.ConsumeAsync` accepts **only** a `ReservationId`. There is no consumer key, no expected quantity, no expected `StockItemId`, no correlation id.
* `FulfillmentItem` (`src/backend/Modules/Fulfillment/Tooba.Fulfillment.Domain/Aggregates/FulfillmentItem.cs`) holds `ReservationId` (nullable) + `ReservationConsumed` (bool) and `MarkReservationConsumed()`. This is the only Fulfillment-side record that a reservation was consumed; it lives in a **different database transaction** than the inventory deduction.
* Order-side read exists but is not on the Fulfillment seam: `IOrderInventoryLifecyclePort.FindReservationAsync` → `OrderInventoryReservationView(ReservationId, StockItemId, Quantity, Status, ExpiresAt)` (`Inventory.Contracts/Orders/OrderInventoryLifecycleContracts.cs`). Fulfillment does not reference `IOrderInventoryLifecyclePort`.
* `FulfillmentItem.RebindActiveReservation` can replace the reservation id while not yet shipped — reservation→item binding is “current”, not immutable history.

## 3. Existing idempotency facilities (as found)

| Facility | Where | Reusable as consumption-replay proof? |
|---|---|---|
| Hold idempotency key + partial unique index | `StockReservation.IdempotencyKey`, `reservations` unique index | **No.** `MoveTo` sets `IdempotencyKey = null` on consume/release — the key is destroyed by the very operation being replayed. |
| `return_restock_inbox` (Inventory-owned dedup journal) | `ReturnRestockInboxRecord` + `InventoryReturnGateway.RestockFromReturnAsync:43-106` | **Precedent, not a reuse target.** Reserved for return restock; keyed by an external string key; the inbox row is committed in a *second* `SaveChangesAsync` after the mutation already committed (`AdjustAsync`/`ReleaseAsync` commit first) → its own narrow window. |
| Outbox messages | `inventory.outbox_messages` (`inventory.reservation_consumed.v1`) | **No (rejected).** It is an integration-event bus table: payload is opaque JSON, no indexed key on `reservationId`, no uniqueness/invariant semantics, retention is an ops concern, and it still carries **no consumer identity**. Live `tooba_alpha` count: 5 consumed events = 5 consumed reservations, i.e. it is only a *de-facto* side-effect log. |
| Release replay tolerance | `InventoryDirectory.ReleaseAsync:238-265` (silent no-op when `Released or Consumed`) | Pattern reference only; it is safe solely because a repeated release must not credit stock twice. |
| Domain/EF concurrency guard on consume | — | **None.** `ConsumeAsync` performs a raw `ExecuteUpdateAsync` guarded only by `Reserved >= q && OnHand >= q`, then calls `MoveTo` (which throws after the update already ran). No compare-and-set on `reservations.Status`, no row version, no unique constraint on a consumption journal. |

## 4. Atomicity boundary conclusion

* One `FulfillmentDbContext.SaveChangesAsync` per package dispatch (parent repair) — **Fulfillment-local atomicity: PASS**.
* Inventory commits consumption on a separate `InventoryDbContext`, in a separate transaction, *inside* the Fulfillment write loop (before the Fulfillment commit) — **no distributed atomicity; no saga/outbox *command* is used for consumption** (the outbox here is publish-only).
* Worse than “separate transaction”: inside `ConsumeAsync` the stock deduction is an **immediate autocommitted statement** (`ExecuteUpdateAsync`) while the reservation status/journal/outbox are written by the later `SaveChangesAsync`. The deduction is therefore **not even atomic with the reservation row it consumes**.
* Fulfillment is a *consumer* of `Tooba.Inventory.Contracts` only; no cross-module DbContext, no shared transaction, no direct `inventory.*` table access (verified: no `InventoryDbContext` reference under `src/backend/Modules/Fulfillment`).
