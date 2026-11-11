# TB-262 — worker result: owned damaged-receipt decision and healthy-stock replacement

Status: **PASS** within the issued scope (including the 2026-11-11 USER OVERRIDE on reservation retention).
Decision: **OWNED_DAMAGED_RECEIPT_DECISION_AND_HEALTHY_STOCK_REPLACEMENT_WITH_QUANTITY_CONSERVATION** and
**ORDER_BOUND_REPLACEMENT_RESERVATION_RETAINED_UNTIL_AUTHORIZED_ORDER_CANCELLATION**.
This is **NOT** Architect ACCEPT, **NOT** user VISUAL/FUNCTIONAL ACCEPT, and it does not declare the earlier
five-defect closure or the 261/262 correction series finished from the user's side.

Base `4cf6bfbe084f7c4834205f6033064202bae2cf17` (== `Expected-Base-SHA`). After the first result, `origin/main`
published the USER OVERRIDE «تا سفارش لغو نشده نباید آزاد بشه رزرو», which supersedes the original
release-on-parcel-cancellation wording; this revision implements it on top of the same task and re-proves the
affected cases. HEAD == `origin/main` after the merge commit. Single worker, one task, no parallel worker, no
historical agent resumed.

## 1. The dead end this closes

TB-261 proved the screenshot's missing resend was lawful: the source parcel's origin receipt is
`[{line 8e0a897b-…, quantity 1.000000, condition "Damaged"}]`, so `ReturnedUsableQuantity = 0` and
`ResendableQuantity = 0`. TB-262 adds the missing **operational outcome for that damaged unit** instead of
converting it into usable capacity or leaving a silent dead end.

## 2. Landed

### 2.1 Fulfillment owns the physical decision (`Tooba.Fulfillment.Domain`)

New aggregate `DamagedGoodsResolution` (table `fulfillment.damaged_goods_resolutions`) records, for one returned
parcel line: source `shipment_id`, `shipment_return_receipt_id`, `fulfillment_id`, `order_line_id`, the ACTUAL
damaged quantity, `outcome` (`Replacement` / `Referral`), `status` (`Recorded` / `Reserved` / `Fulfilled` /
`Cancelled`), `decided_by_user_id`, `occurrence_key`, `attempt`, `note`, `created_at` / `updated_at`, plus the two
linked identities `replacement_reservation_id` and `replacement_shipment_id`. Unique
`ux_damaged_goods_resolutions_fulfillment_occurrence (fulfillment_id, occurrence_key)` is the durable identity, so a
retried reviewed intent can only answer the recorded decision.

Quantity conservation (asserted, not inferred):

```
ReceiptDamagedQuantity(sourceParcel, line)     = Σ receipt items with condition Damaged
DamagedCommittedQuantity(sourceParcel, line)   = Σ decisions with status != Cancelled
OutstandingDamagedQuantity                      = max(0, ReceiptDamaged − DamagedCommitted)
ResendableQuantity(line)                        = ReturnedUsableQuantity − ResendCommittedQuantity   [TB-258 unchanged]
```

* Damaged units are never usable/resend capacity; a damaged-only receipt still yields resendable 0.
* A decision is bounded by **its own source parcel's** outstanding damaged quantity **and** by the paid-order
  obligation that is still undelivered. A sibling parcel can neither lend nor borrow.
* One damaged unit funds exactly one decision: a second decision for the same unit is
  `fulfillment.damage.exceeds_outstanding`.
* `Shipment` gained `DamagedResolutionId`, `ReplacementReservationId`, `IsReplacement`; `IsReplacement` is
  distinct from `IsResend`, and replacement parcels link BOTH the damaged source (`ResendOfShipmentId` = immediate
  returned source) and the exact resolution/reservation. The cancelled-attempt dispatch key uses
  `attempt{N}:{resolutionId:N}` so a retry after cancellation never collides with the cancelled parcel's key, while
  the Inventory reservation key stays stable across attempts (retention rule below).

New typed, localized error codes: `fulfillment.damage.{resolution_ids_required, resolution_qty_positive,
resolution_outcome_invalid, resolution_actor_required, resolution_occurrence_required, resolution_note_invalid,
resolution_intent_conflict, source_not_returned, receipt_missing, line_foreign, no_damaged_quantity,
exceeds_outstanding, obligation_already_settled, replacement_unavailable, original_reservation_missing,
resolution_cancelled, resolution_not_replacement, resolution_not_open, resolution_not_found,
reservation_required, reservation_conflict, replacement_conflict, replacement_missing,
replacement_source_required, replacement_occurrence_required, concurrency_conflict}` — all added to both resx
files with Persian copy, so no raw code reaches the operator.

### 2.2 Inventory owns healthy stock through ONE narrow Contracts seam

New `Tooba.Inventory.Contracts.Replacement.IDamagedReplacementInventoryPort`:

* `ReserveForDamagedReplacementAsync(originalReservationId, quantity, reservationKey, externalReference, expiresAt)`
  — Inventory resolves the OFFER from the **original committed sale reservation**, so a client can never supply a
  product/seller/location identity; it reuses Inventory's existing atomic availability/reservation primitives.
* `FindDamagedReplacementReservationAsync(reservationId)` — recovery of an uncertain reserve.
* The hold is purpose-bound and idempotent per `damage-replacement:{resolutionId:N}` — **stable across attempts** —
  so an exact retry answers the same hold and a retry after a cancelled parcel rebinds the retained one instead of
  reserving a second unit; unavailable healthy stock throws the typed `inventory.replacement.unavailable` with **no
  hold left behind**.
* RETENTION RULE (USER OVERRIDE, 2026-11-11): the hold belongs to the ACTIVE order. It is committed for the paid
  order immediately (`CommitReservationForPaidOrderAsync` → `ExpiresAt = null`), so the Inventory expired-hold
  reclaimer (`ReleaseExpiredHoldsAsync`, `expires_at IS NOT NULL` only) can never return this stock to saleable
  availability while the order lives.
* `RestockFromReturnAsync` (refund-time, condition-less) is deliberately **never** invoked for damaged units; the
  original first-sale reservation is never consumed or released a second time. Consuming happens exactly once, on
  the existing dispatch path, only for `IsReplacement` parcels
  (`IFulfillmentInventoryGateway.ConsumeReplacementReservationAsync`); `ReleaseReplacementReservationAsync` is
  reachable **only** from an authorized ORDER cancellation, never from a parcel cancellation.

### 2.3 Order owns the operator authority and the public command

* New command `resolve_damaged_goods` on the existing admin operations endpoint → `ResolveDamagedGoodsCommand` →
  `AdminOrderOperationsOrchestrator.ResolveDamagedGoodsAsync` → `IFulfillmentAdminOperations`.
* Real request fields: `orderLineId`, `quantity`, `damagedOutcome` (`replacement` | `referral`) — nothing else can
  identify a product or a seller.
* Projection exposes the action `resolve_damaged_goods` («تعیین تکلیف کالای آسیب‌دیده») and the parcel capability
  `canResolveDamagedGoods` ONLY for a `Returned` parcel with its own receipt and `OutstandingDamagedQuantity > 0`
  under `order.handle` / `fulfillment.manage`; the per-line facts `damagedQuantity` /
  `outstandingDamagedQuantity` travel on the fulfillment items and on the parcel lines, so the card, the orders
  line summary and the queue all read the same authority.
* Public replay: `ReconcileDamagedIntentAsync` answers `NotRecorded` / `Recorded` / `ResolvedCancelled` /
  `Conflict` from the recorded decision **before** fresh readiness, re-checking current actor, checkout,
  fulfillment, parcel and the exact built intent every time. An exact recorded decision replays with **zero
  writes** after the action has disappeared from the projection; any changed quantity/outcome/source is
  `fulfillment.damage.resolution_intent_conflict`. A referral is never reported as resolved money or as a refund.
* `FulfillmentAdminOperationsAdapter` maps all of it, including the new contract fields
  `DamagedQuantity`, `OutstandingDamagedQuantity` (item + parcel line), `DamagedResolutionId`,
  `ReplacementReservationId`, `IsReplacement`, `DamagedResolutions[]`.

### 2.5 RETENTION RULE — the hold stays with the active order (USER OVERRIDE, 2026-11-11)

Latest explicit user rule: «تا سفارش لغو نشده نباید آزاد بشه رزرو». Implemented at the owning seams:

* `DamagedGoodsResolution.ReservationKey` is stable (`damage-replacement:{resolutionId:N}`), so a retry reproduces
  the same Inventory idempotency identity.
* `DamagedGoodsResolution.RetainStockBeforeDispatch` (replacing the old `ReleaseBeforeDispatch`) detaches **only**
  the parcel allocation: `ReplacementShipmentId = null`, `Attempt + 1`, the reservation id and a `Reserved` status
  are kept. `FulfillmentUnit.RetainReplacementStockForCancelledParcel` is what `CancelShipmentAsync` now calls — it
  performs no Inventory release at all.
* `FulfillmentDirectory.ResolveDamagedGoodsAsync` commits the newly acquired hold for the paid order
  (`ExpiresAt = null`) and, on a retry where a reservation is already attached, re-reads it and requires a live
  `Held` status. A genuinely gone hold is the typed `fulfillment.damage.reservation_conflict` — never a silent
  second reservation for the same damaged units.
* `FulfillmentUnit.ReleaseReplacementStockForOrderCancel` + `DamagedGoodsResolution.MarkOrderCancelled` are called
  only from `AbortForCheckoutCancelAsync`, which releases each unconsumed replacement hold **once** and marks the
  decision `Cancelled`. Parcel cancellation is never treated as order cancellation.
* Identified active-order expiry path: `InventoryDirectory.ReleaseExpiredHoldsAsync`. It is defeated for this hold
  by clearing the TTL on attach; the abandoned-cart policy was **not** rewritten as a shortcut.
* Dispatch still consumes the replacement hold exactly once (consume ≠ release); an already-consumed original sale
  reservation is never resurrected, and physically damaged stock is never presented as healthy availability.

### 2.6 Same operational UI, one entry, one toast

* `admin-order-operational-delivery-section.tsx` shows exactly ONE 44px «تعیین تکلیف کالای آسیب‌دیده» for a parcel
  whose own outstanding damaged quantity is positive (`admin-order-post-dispatch-damaged-action-{shipmentId}`), and
  the existing unresolved-damage sentence (`admin-order-post-dispatch-damaged-{shipmentId}`) stays contextual. The
  healthy «ارسال مجدد» entry keeps its own TB-258 authority; the two never share capacity.
* The shared reviewed dialog (`post-dispatch-exception-dialog.tsx`) gained the `resolve_damaged_goods` mode: damaged
  line, its own remaining damaged quantity, the two server-projected outcomes with one concise Persian sentence
  each, an explicit quantity bounded by that remainder, and one inline refusal when the quantity is invalid or
  above the remainder (no `Math.min` clamp, zero POST).
* Confirmed actions use the ONE existing `react-toastify` `toast.success` in the panel; blockers, field validation
  and the retained reconcile row stay inline — no persistent success banner, no duplicate same-outcome toast.
* The complete reviewed envelope (viewer scope, checkout, fulfillment, source parcel, mode `resolve_damaged_goods`,
  line, quantity, outcome) is retained durably BEFORE the first POST via the existing
  `post-dispatch-attempt-store` boundary; a storage that cannot persist refuses the NEW send with zero fetch, a
  same-tick double submit sends one request, and remount/reload reconciles the EXACT reviewed payload.
* No new page, no second operational component, no raw enum, no tenant/source id, no architecture prose.

## 3. Proof (focused, one canonical database)

| Proof | Result |
| --- | --- |
| Fulfillment `DamagedResolution262Tests` (pure domain) | **7/7** |
| Fulfillment `PostDispatchException` (TB-258 retained, incl. R1/R2/R3 authority) | **36/36** |
| Host `DamagedGoodsResolution262RelationalTests` on REAL `tooba_alpha` | **6/6** (A, B, C, D, D2, E) |
| Order `AdminOrderPostDispatchReplayTests` (incl. public-command damaged replay) | **10/10** |
| FE mounted real section/panel/dialog `admin-order-operational-delivery.mount.test.ts` | **52/52** (3 TB-262 cases) |
| FE `tsc --noEmit` | 0 |
| Backend `dotnet build Tooba.slnx -c Release` | 0 errors |
| FE `next build` | 0 |

Relational matrix (fixture ids in `tb262-relational.txt`, all rows created through production domain/application
paths and labelled `TB262-*`):

* **A** damaged1 ⇒ `resendableA=0`, `outstandingA=1`, `onHand` unchanged and `reserved=0`: damage is never usable
  capacity, never a second stock deduction, never a saleable increase. The unit still owes one decision.
* **B** usable2 + damaged1 ⇒ healthy `resendable=2`, exactly ONE replacement `reserved=1` against NEW healthy stock;
  a changed outcome on the same occurrence, a foreign line and an over-quantity decision are typed refusals with
  `rows=1` (no second decision).
* **C** decision → own reservation → explicit preparation → NEW linked replacement parcel → tracking → dispatch →
  delivery: the NEW hold is consumed exactly once (`consumed=1`, `reserved=0`, `onHand 10 → 8` for 1 sale + 1
  replacement), the ORIGINAL sale reservation stays consumed and untouched, `delivered=1` counts the physical
  customer unit once, and the exact occurrence replays the SAME parcel after fresh readiness is gone.
* **D** RETENTION RULE: cancelling a Created replacement parcel detaches only that parcel allocation. The hold stays
  `Held`, `reserved` stays 1 (saleable availability does not increase), its `expires_at` is `NULL` so the reclaimer
  cannot free it, and the retry REBINDS the exact same reservation (`sameHold=1`, one live replacement hold row)
  while creating a genuinely new parcel (`retriedParcelOnly != first`). A second cancellation of the retried parcel
  keeps the same allocation. With no healthy stock the decision records without allocating (`starvedParcels=1`,
  `blocker=fulfillment.damage.replacement_unavailable`, `starvedHolds=0` — no leaked hold).
  The only authorized release path (`AbortForCheckoutCancelAsync` → `ReleaseReplacementStockForOrderCancel`) is
  unreachable for an order that already shipped a parcel (`fulfillment.cancel.already_dispatched`), which is exactly
  the honest limit recorded in §5; the release behaviour itself is proven by the pure case.
* **D2** a fault between the new hold and the parcel recovers the EXACT recorded hold and parcel (`holds=1`,
  `rows=1`), not a scan/guess.
* **E** two independent contexts cannot resolve one damaged unit twice: exactly one winner, one typed
  `fulfillment.damage.concurrency_conflict` loser, `rows=1` decision and `holds=1` reservation.

Mounted UI: the 44px damaged entry, the reviewed quantity/outcome, the durable-before-send envelope, the same-tick
single request, the retained exact replay and the zero-POST refusal for an over-quantity or non-durable envelope are
all driven through the real section/panel/dialog with the real callback contract.

## 4. Deploy

* Migration: `fulfillment 20261111123000_AddDamagedGoodsResolution` (additive: two nullable shipment columns +
  one new table + its indexes). Applied by the repository `MigrationRunner` after the private backup
  `D:\temp\tb262-backup\tooba_alpha-pre-tb262.dump` (3,448,171 bytes, SHA256 `23B41AA8…`). The 2026-11-11 retention
  revision adds **no** schema change: `plan --tenant store-alpha` still reads
  `module=Fulfillment current=20261111123000_AddDamagedGoodsResolution pendingCount=0`, every other module reports
  `pendingCount=0`, and `fulfillment.__ef_migrations_history` head is unchanged.
* Host republished to `artifacts/publish-primary` with the retention revision and restarted (PID **30928**,
  `/health` 200, `/health/ready` 200 with `edition=SingleStore`, `postgresql=configured`; the previous PID 45212 was
  stopped first so no file lock blocked the publish).
* FE unchanged in this revision (no frontend source change): restarted FE process PID **13608** still serves
  BUILD_ID `2t3xzbGoosGy7zATxS-Xh`, `/fa` 200, chunk `7825-33c5ed94cd44fb57.js` 200 with the damaged copy.
* ONE database `tooba_alpha` @ 127.0.0.1:5432; Edition `SingleStore`, tenant `store-alpha`; no disposable /
  test / parallel database, no 15433, no edition/connection/port/appsettings change; DataProtection and
  CapturedPreview unchanged (local ON / checked-in OFF); no manual DDL, no business SQL, no reset/reseed/backfill;
  user order/cart/parcel/payment rows and the 261 screenshot fixture read-only; no provider call, no fake waybill;
  255 parked; live Tipax deferred.
* Ledger read-only after the retention revision: `fulfillment.damaged_goods_resolutions` 47 rows whose
  `occurrence_key` all start `tb262-` (fixture-labelled) — `34 Fulfilled`, `10 Recorded`, `3 Reserved`; the rows with
  a retained hold are exactly the task fixtures. `inventory.reservations` `Consumed 65 / Held 42 / Released 44`;
  the four live no-TTL replacement holds are the retention-run fixtures (`damaged-resolution:01a1cae6dd7e…`,
  `…dc8f…`, `…db82…`, `…df52…`) — retained after their parcel was cancelled, never released by a hold timeout.
  Older `TB262-*` fixture holds from the pre-override run still carry a TTL because that code released on
  cancellation; they are historical fixture rows and were **not** rewritten.

## 5. Honest limits

* No refund/replacement financial automation: a `Referral` stays a pending customer-service/financial item, exactly
  as issued; Returns/Settlement/Payment were not modified and no money moved.
* No live user trial on this feature was performed by the worker; the real entry point for the user trial is
  `/fa/admin/orders/{checkoutId}/operations` on a parcel whose origin receipt holds a damaged line, and
  `/fa/admin/fulfillments` for the queue.
* Concrete ordering limit (reported, not worked around): the authorized ORDER cancellation path
  (`AbortForCheckoutCancelAsync`) refuses an order that already dispatched a parcel
  (`fulfillment.cancel.already_dispatched`), and a damaged origin receipt necessarily comes from a shipped parcel.
  So for the real damaged-return scenario the held replacement stock stays attached to the order — which is exactly
  what the user rule requires — but the "release on order cancellation" branch is only reachable on orders that
  never shipped. No new line-cancellation capability was invented (the task forbids it); closing that gap is a
  separate authorized order-lifecycle decision.
* A parent consolidated PACKAGE still has no damage/return orchestration of its own (TB-258 child lock preserved,
  no silent unlock); this is inherited, not expanded.
* Concurrency is proven with two real contexts plus unique/index guards, not a stress test.
* The screenshot fixture stays untouched: TB-262 adds the decision surface, it does not rewrite that receipt.
