# TB-265 — RESULT: courier-origin Returns linkage, exclusive quantity, obligation cancellation, condition-safe stock

Status: **PASS within the issued scope**, decision
**COURIER_ORIGIN_CASE_AS_THIRD_LAWFUL_OUTCOME_WITH_ONE_FULFILLMENT_SOURCE_CLAIM_PHASED_APPROVAL_CLOSURE_AND_CONDITION_AWARE_DISPOSITION**.
NOT Architect ACCEPT, NOT user VISUAL/FUNCTIONAL ACCEPT, NOT "all shipping work complete", NOT a live paid/provider
refund, NOT a new finance engine.

Base `7c32cdc2955d9c0b0c1dd4eef8b961b9a4acb904` (= `Expected-Base-SHA`). Single worker, one task, no delegation, no
historical agent resumed.

## 1. What was actually broken (read from source, not assumed)

1. `ReturnEligibilityEvaluator` admits a return only with `IsPaid` + `LastDeliveredAt` + per-line delivered-window
   snapshots. A never-delivered courier receipt is truthfully `return.not_delivered`, so the existing customer RMA
   path cannot express it — and faking `Delivered` would corrupt the customer rule.
2. There was **no source-quantity authority**: nothing durable stopped the same received units from funding a
   resend AND a replacement AND a case at the same time.
3. `ReturnDirectory.ApproveAsync` started the refund immediately, and `ExecuteRefundAsync` unconditionally restocked
   reservation-bearing items on success — refund before delivery-obligation closure, and damaged goods could be
   restocked as saleable.
4. `ReturnItem` had no source receipt/condition, and `InventoryReturnGateway.RestockFromReturnAsync` had no
   condition parameter.
5. `IOrderInventoryLifecyclePort` releases a whole `Held` reservation by id — no quantity-safe partial release, so a
   sibling portion of the same active-order hold could be released accidentally.

## 2. What landed (ownership + seams)

* **Fulfillment** owns the claim: `CourierReturnClaim` aggregate (+ `CourierReturnClaimLine`, `ReturnedCapacityClaim`),
  `fulfillment.courier_return_claims` / `..._claim_lines`, `ActiveKey` filtered unique index, per-line source
  remainder = receipt quantity − (resend + replacement + case claims). New adapter
  `Fulfillment/.../Returns/CourierReturnSourceDirectory` answers `fulfillment.returns.claim_quantity_exceeded`,
  `...replacement_already_dispatched`, `...claim_receipt_required`, `...claim_occurrence_required`.
  `FulfillmentUnit` gained `RedirectableDamagedQuantity` and `DamagedCommittedToDispatchedReplacement` so a
  `Created` replacement can be deliberately redirected while a live dispatched one is refused.
* **Returns** owns the case: additive `20261113162816_AddCourierOriginReturnsLinkage` (+
  `20261113174500_NormalizeCourierOriginColumnNames` correcting PascalCase columns) adds the origin discriminator,
  source fulfillment/parcel/receipt/claim/occurrence linkage and the closure phase to the EXISTING
  `returns.return_requests`; `ReturnApproval` runs the phased settlement. `ClosurePhase`:
  `None → ObligationClosed → StockSettled → RefundReleased`, each transition monotonic and idempotent.
* **Order** owns the customer obligation: `IOrderReturnObligationClosure` (implemented by
  `OrderReturnObligationClosure`) validates the exact line and closes only that obligation — no historical
  ordered/packed/dispatched decrement, no whole-order `already_dispatched` weakening. The public admin command
  `create_courier_return` lives in `AdminOrderOperationsOrchestrator.Fulfillment.cs` with `return.manage`
  authorization decided BEFORE any evidence read, a requirement for the reviewed idempotency key, canonical
  line/condition intent (duplicate line+condition and unknown condition tokens are `order.operation.invalid`), and
  an occurrence key bound to THIS parcel (`courier-return:{shipmentId:N}:{key}`).
* **Inventory** owns the physical facts: `IInventoryReturnObligationRelease` releases ONLY the unconsumed held
  quantity of the cancelled obligation (reserved decremented; on-hand leaves through disposition, not through
  release), and restock is condition-gated so `DamagedNonSaleable` never increases saleable availability.
* **Frontend**: the existing shared post-dispatch dialog gained the third reviewed outcome «ایجاد پروندهٔ مرجوعی»
  beside resend and replacement, with its own reviewed quantity/condition selection, one contextual entry
  («ایجاد پروندهٔ مرجوعی» / «مشاهدهٔ پروندهٔ مرجوعی»), retained durable envelope/fingerprint, one existing
  toast on confirmed creation, existing `MessageBox` for the irreversible approval, and no financial panel on the
  finance-free shipping view. TB-264-R3 mobile header behaviour and the emitted-utility constraint
  (`features/**` is absent from the Tailwind content globs) were respected — no feature-only class was relied on.

## 3. Exactly what happens at each boundary

| Step | Durable effect | Never happens |
| --- | --- | --- |
| Create case (`Requested`) | one claim row holds the quantity; case linked once; phase `None` | refund, stock release, order cancellation, historical line mutation, second receipt |
| Exact replay (same key/occurrence/lines) | answers the recorded case/claim (`AlreadyExisted`) | a second case or claim |
| Changed source/quantity/condition under the same key | typed conflict, zero added rows | silent re-intent or auto-reminted key |
| Reject / cancel `Requested` | releases ONLY that branch claim | releasing the order stock hold |
| Approve | `ObligationClosed` → Order closes the exact line obligation, eligible `Created` replacement cancelled → `StockSettled` → Inventory releases only unconsumed held once + settles disposition → `RefundReleased` → existing refund path | refund before closure, whole-order cancellation, sibling/consumed-stock release, duplicate refund |
| Retry after any boundary | resumes from the recorded phase, answers the same facts | second release, second refund, second case |
| Already dispatched replacement | `fulfillment.returns.replacement_already_dispatched` | recall/refund as unused stock |
| Parent package / unpaid | explicit refusal / exact blocker | child unlock, invented package refund, fake success |

## 4. Proof (each check once; no base parity, no repeated green loop)

* Fulfillment `CourierReturnClaim265Tests` **7/7**.
* Returns `CourierOriginReturns265Tests` **5/5**.
* Inventory `ReturnObligationRelease265Tests` **5/5**.
* Order `AdminOrderPostDispatchReplayTests` **16/16** — 11 retained TB-258-R3 facts plus **5 new TB-265 facts**:
  `return.manage` required before any Returns read; missing key / no lines / duplicate line+condition / unknown
  condition token → `order.operation.invalid` with zero creates; the claim is bound to the exact parcel and
  reviewed quantity (occurrence `courier-return:{shipmentId:N}:{key}`, condition `Damaged`); an exact replay answers
  the recorded case with exactly one create; a recorded conflict is reported verbatim and creates nothing.
* Host relational matrix on REAL `tooba_alpha` `CourierOriginReturns265RelationalTests` **6/6** (A–F):
  * A `fulfillment=3cf3f7f5-3314-4efc-a76b-42725b913f0e shipment=01a1d80d-c17f-7000-8320-4cb2a167e608
    claim=01a1d80d-c3b0-7000-919c-d7ea7bce084a status=Requested lines=2/1 resendableA=0 outstandingDamagedA=0
    resendableB=1 claims=1 onHandDelta=0`
  * B `fulfillment=092cf0ab-b65b-437e-b2dd-c22653c475a0 shipment=01a1d80d-cb0b-7000-a731-89d20b8fa130
    winners=1 refusals=1 claimedTotal=2 free=0` (two independent contexts, no overdraw)
  * C `claim=01a1d80d-ce85-7000-9533-4dd83ae93e66 status=Rejected resendableA=2 outstandingDamagedA=1
    reClaim=01a1d80d-ceab-7000-83ff-7f70a3ac32c4` (branch claim released, hold retained)
  * D `case=01a1d80d-cd16-7000-80e4-ba4e55b6c6e5 status=Completed phase=RefundReleased releasedHeld=0 refunds=1
    restocks=1 disposition=SaleableRestock reservation=Consumed` (obligation closed, partial released once, no
    second release on replay)
  * E `case=01a1d80d-c9a6-7000-860a-649b8b12709c cancelledReplacement=01a1d80d-c8eb-7000-a7cd-ba00193b21c9
    releasedHeld=1 heldReservation=Released damagedDisposition=DamagedNonSaleable restocks=0 onHand=7 refunds=1`
    (audited deliberate switch from a retained replacement; damaged units never saleable)
  * F `replacementParcel=01a1d80d-c6e1-7000-9370-880b6865818c status=Dispatched
    claimRefused=replacement_already_dispatched reservation=Consumed releasedHeld=0`
* Frontend `app/admin/admin-order-operational-delivery.mount.test.ts` **62/62** including
  `TB-265 mounted: the reviewed courier-origin quantity opens ONE linked returns case` and
  `TB-265 static: the courier-return entry exists only when the server projects it`.
* `tsc --noEmit` **0**; `next build` **0** (`BUILD_ID wqPY0iLLazeSDXeSeyTtu`); `dotnet build` Host **0 errors**;
  `dotnet publish` to `artifacts/publish-primary` **0 errors**.
* Runtime: Host **PID (see RECEIPTS)** on `127.0.0.1:5088`, `/health` **200**, `/health/ready` **200** (SingleStore);
  Next **PID (see RECEIPTS)** on `3000`, `/fa` **200**, served chunk `7825-dadcc8101dfd6099.js` carries
  «ایجاد پروندهٔ مرجوعی». Live read-only admin projection
  `GET /v1/admin/orders/01a1d511-adbb-7000-8575-d3b957453d76/operations?fulfillmentId=9fe40e71-23cb-4014-8a35-e4c3231c9dab`
  through the Development admin seam → **200** with server-projected actions (this checkout is PendingPayment, so it
  truthfully offers no courier-return action).

## 5. Deliberate limits (disclosed, not papered over)

* No real provider refund, wallet credit or customer money movement was executed: approval/refund proof runs through
  the owning orchestration with task-owned payment/wallet ports and real durable rows, and every receipt says so. The
  existing Payment/Wallet/Settlement integration and its idempotency were not rewritten.
* No live browser click-through of the create flow was performed by the worker; the UI proof is the mounted real
  component/dialog against the served build plus the live server projection. The user's own trial remains the
  acceptance step. Entry: `/fa/admin/orders/{checkoutId}/operations?fulfillmentId=…&shippingGroupId=…` on a returned
  parcel with a recorded origin receipt → «ایجاد پروندهٔ مرجوعی», then the case in the existing Returns list.
* Parent consolidated-package return/damage stays unsupported with its explicit refusal and locked child (no silent
  unlock).
* An already dispatched / in-transit / delivered replacement is never treated as unused held stock; the operator
  gets a precise refusal and the real receipt/return path.
* Concurrency is proven with two independent real contexts plus database guards, not a stress test.
* Only ONE canonical database `tooba_alpha` @127.0.0.1:5432 (SingleStore, tenant `store-alpha`) was used; user and
  screenshot records (including method `01a17d0d-…`) were read-only; all new rows are task-labelled `TB265-…`.
* `255` remains parked, live Tipax remains deferred, `264/R1/R2/R3` were not reopened.

## 6. Next

`STOP_FOR_ARCHITECT`. `automaticNextImplementationTask: NONE`. No next task is issued by this result.
