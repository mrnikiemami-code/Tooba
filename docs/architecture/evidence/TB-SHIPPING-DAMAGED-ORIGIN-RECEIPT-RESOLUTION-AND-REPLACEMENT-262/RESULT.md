# TB-262 — worker result: owned damaged-receipt decision and healthy-stock replacement

Status: **PASS** within the issued scope.
Decision: **OWNED_DAMAGED_RECEIPT_DECISION_AND_HEALTHY_STOCK_REPLACEMENT_WITH_QUANTITY_CONSERVATION**.
This is **NOT** Architect ACCEPT, **NOT** user VISUAL/FUNCTIONAL ACCEPT, and it does not declare the earlier
five-defect closure or the 261/262 correction series finished from the user's side.

Base `4cf6bfbe084f7c4834205f6033064202bae2cf17` (== `Expected-Base-SHA`). Work/merge base is that commit;
HEAD == `origin/main` after the task commits. Single worker, one task, no parallel worker, no historical agent resumed.

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
  `attempt{N}:{resolutionId:N}` so a retry after cancellation never collides with the cancelled parcel's key.

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
* The hold is purpose-bound and idempotent per `damage-replacement:{resolutionId:N}:{attempt}`; unavailable healthy
  stock throws the typed `inventory.replacement.unavailable` with **no hold left behind**.
* `RestockFromReturnAsync` (refund-time, condition-less) is deliberately **never** invoked for damaged units; the
  original first-sale reservation is never consumed or released a second time. Consuming happens exactly once, on
  the existing dispatch path, only for `IsReplacement` parcels
  (`IFulfillmentInventoryGateway.ConsumeReplacementReservationAsync`); a pre-dispatch cancel calls
  `ReleaseReplacementReservationAsync` once.

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

### 2.4 Same operational UI, one entry, one toast

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
* **D** cancelling a pre-dispatch replacement releases only its OWN hold (`released=1`), creates no parcel, and the
  retry acquires a genuinely NEW hold + parcel; with no healthy stock the decision records without allocating
  (`starvedParcels=1`, `blocker=fulfillment.damage.replacement_unavailable`, `starvedHolds=0` — no leaked hold).
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
  `D:\temp\tb262-backup\tooba_alpha-pre-tb262.dump` (3,448,171 bytes, SHA256 `23B41AA8…`). `plan --tenant
  store-alpha` now reads `module=Fulfillment current=20261111123000_AddDamagedGoodsResolution pendingCount=0`, and
  every module reports `pendingCount=0`.
* Host republished to `artifacts/publish-primary` and restarted (PID **45212**, `/health` 200, `/health/ready` 200
  with `edition=SingleStore`, `postgresql=configured`).
* FE rebuilt and restarted (PID **13608**, BUILD_ID `2t3xzbGoosGy7zATxS-Xh`, `/fa` 200); served chunk
  `7825-33c5ed94cd44fb57.js` returns 200 with the new damaged copy, and the previously served
  `992-24accfc967f39328.js` now 404s.
* ONE database `tooba_alpha` @ 127.0.0.1:5432; Edition `SingleStore`, tenant `store-alpha`; no disposable /
  test / parallel database, no 15433, no edition/connection/port/appsettings change; DataProtection and
  CapturedPreview unchanged (local ON / checked-in OFF); no manual DDL, no business SQL, no reset/reseed/backfill;
  user order/cart/parcel/payment rows and the 261 screenshot fixture read-only; no provider call, no fake waybill;
  255 parked; live Tipax deferred.
* Ledger read-only after the work: `fulfillment.damaged_goods_resolutions` 37 rows whose `occurrence_key` all start
  `tb262-` (fixture-labelled), `29 Fulfilled`, `2 Reserved`, `6 Recorded`, `0 Cancelled`, and 35 shipments carry
  `damaged_resolution_id`; `inventory.reservations` `Consumed 57 / Held 38 / Released 44`.

## 5. Honest limits

* No refund/replacement financial automation: a `Referral` stays a pending customer-service/financial item, exactly
  as issued; Returns/Settlement/Payment were not modified and no money moved.
* No live user trial on this feature was performed by the worker; the real entry point for the user trial is
  `/fa/admin/orders/{checkoutId}/operations` on a parcel whose origin receipt holds a damaged line, and
  `/fa/admin/fulfillments` for the queue.
* A parent consolidated PACKAGE still has no damage/return orchestration of its own (TB-258 child lock preserved,
  no silent unlock); this is inherited, not expanded.
* Concurrency is proven with two real contexts plus unique/index guards, not a stress test.
* The screenshot fixture stays untouched: TB-262 adds the decision surface, it does not rewrite that receipt.
