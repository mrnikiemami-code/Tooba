# TB-262 — ownership and quantity design (recorded BEFORE implementation)

## 1. Which module owns what

| Concern | Owning module | Why |
| --- | --- | --- |
| Final physical receipt per source parcel / line / condition | **Fulfillment** (`ShipmentReturnReceipt`, `ReturnReceiptCondition`) | TB-258 already owns the physical return facts; TB-262 adds no second receipt. |
| Damaged capacity, "how much damage still owes a decision" | **Fulfillment** (`FulfillmentUnit.ReceiptDamagedQuantity` / `DamagedCommittedQuantity` / `OutstandingDamagedQuantity`) | The complement of usable resend capacity must live beside it, or the two readings can disagree. |
| The durable decision itself | **Fulfillment** (`DamagedGoodsResolution` aggregate) | It is a physical-goods decision about a parcel line that only Fulfillment can prove. |
| Saleable stock, healthy availability, reservations, consumes | **Inventory** | Unchanged owner. TB-262 adds ONE narrow Contracts capability instead of touching stock tables from another module. |
| Paid-order obligation, actor/permission authority, orchestration | **Order** (`AdminOrderOperationsOrchestrator` + `resolve_damaged_goods` command) | Existing pattern: Order owns the operator authority and the public command surface. |
| Refund / settlement / customer-service outcome | **Returns / Settlement / Payment** | Untouched. TB-262 never performs a financial write from a shipping button. |

## 2. The minimal Inventory capability

New Contracts-only seam: `Tooba.Inventory.Contracts.Replacement.IDamagedReplacementInventoryPort`
(`ReserveForDamagedReplacementAsync`, `FindDamagedReplacementReservationAsync`,
`ConsumeReplacementReservationAsync`, `ReleaseReplacementReservationAsync`).

* The requested **offer** is never client-supplied: Inventory resolves it from the **original committed sale
  reservation** id handed over by Fulfillment (`ReserveForDamagedReplacementAsync(originalReservationId, …)`), so a
  caller cannot substitute a different SKU/seller/location.
* The hold uses Inventory's **existing** stock/reservation concurrency primitives and a deterministic
  `ReservationKey` (`damage-replacement:{resolutionId:N}`, **stable across attempts**). An exact retry therefore
  reconciles the same hold instead of reserving twice, and a retry after a cancelled parcel **rebinds the retained
  allocation** instead of reserving a second unit.
* RETENTION RULE (2026-11-11 user override «تا سفارش لغو نشده نباید آزاد بشه رزرو»): the hold belongs to the ACTIVE
  order. As soon as it is acquired Fulfillment commits it for the paid order (`ExpiresAt = null`), exactly like the
  other paid-order reservations, so the expired-hold reclaimer (`ReleaseExpiredHoldsAsync`, which only touches
  `expires_at IS NOT NULL`) can never return this stock to saleable availability while the order lives.
* Unavailable healthy stock is a typed `inventory.replacement.unavailable` blocker **with no hold left behind**.
* `ConsumeReplacementReservationAsync` is called only by the existing dispatch path, exactly once, for a replacement
  parcel (`Shipment.IsReplacement`). The original first-sale reservation is never consumed or released again.
* `ReleaseReplacementReservationAsync` is reachable **only** from an authorized ORDER cancellation
  (`AbortForCheckoutCancelAsync`), never from a parcel cancellation.

## 3. Quantity conservation

```
ReceiptDamagedQuantity(sourceParcel, line)   = Σ receipt items with condition Damaged
DamagedCommittedQuantity(sourceParcel, line) = Σ decisions with status != Cancelled
OutstandingDamagedQuantity(sourceParcel, line) = max(0, ReceiptDamaged − DamagedCommitted)

ResendableQuantity(line)  = ReturnedUsableQuantity(line) − ResendCommittedQuantity(line)   [TB-258, unchanged]
```

* Damaged units are **never** part of `ResendableQuantity`; a damaged receipt alone still yields resendable 0.
* A decision is bounded by **its own source parcel's** outstanding damaged quantity **and** by the units the paid
  order still owes (`QuantityOrdered − DeliveredQuantity`). A sibling parcel can neither lend nor borrow.
* One damaged unit funds **exactly one** decision. A second decision for the same unit is refused with
  `fulfillment.damage.exceeds_outstanding`.
* The original sale already consumed stock, so damage **must not** decrement stock again and **must not** raise
  saleable stock. `RestockFromReturnAsync` (refund-time, condition-less) is deliberately **not** invoked.
* A cancelled pre-dispatch replacement detaches **only its parcel allocation**; the healthy-stock hold stays attached
  to the active order (`status Reserved`, `Attempt + 1`) and the next legitimate attempt rebinds the same
  reservation. The damaged units stay non-saleable and bound. A cancelled occurrence is never silently recreated.
* Only an authorized ORDER cancellation releases the unconsumed replacement holds of that checkout, exactly once
  (`fulfillment.damage` decisions become `Cancelled`, the reservation is released, stock returns to saleable
  availability). Parcel cancellation is never treated as order cancellation.
* Dispatch consumes the replacement hold exactly once; consume is not release, and an already-consumed original sale
  reservation is never resurrected.
* Identified active-order expiry path: `InventoryDirectory.ReleaseExpiredHoldsAsync` (PostgreSQL `FOR UPDATE SKIP
  LOCKED` reclaimer). It was defeated for this hold by clearing the TTL on attach — no policy rewrite of the
  abandoned-cart path was needed.

## 4. Identity / idempotency

`(FulfillmentId, OccurrenceKey)` is the durable identity (unique index
`ux_damaged_goods_resolutions_fulfillment_occurrence`). Same occurrence + same
`(sourceShipmentId, orderLineId, quantity, outcome)` = exact replay of the recorded decision and its exact
reservation/parcel. Any changed field is `fulfillment.damage.resolution_intent_conflict` with zero writes.
Read-only `ReconcileDamagedResolutionAsync` answers the same identity rules and never writes.

## 5. Explicitly NOT in scope

No quarantine/bin-transfer UI, no full warehouse system, no automatic refund/replacement, no product substitution,
no extra charge, no parent consolidated-package return orchestration, no receipt correction/backfill.
