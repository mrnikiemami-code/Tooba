# TB-265 — ownership design: courier-origin Returns linkage, obligation closure, condition-safe stock

Status: WORKER RESULT (pending Architect review). Base `7c32cdc2955d9c0b0c1dd4eef8b961b9a4acb904`.

## The problem in one paragraph

A returned parcel that physically comes back to the seller could previously fund exactly two outcomes: resend the
same healthy units, or reserve NEW healthy stock for a replacement. There was no lawful third outcome (a real
Returns case), the customer-delivered RMA eligibility rule (`IsPaid + LastDeliveredAt + per-line delivered-window
snapshot`) correctly refuses a never-delivered courier receipt, `ReturnDirectory.ApproveAsync` always began a refund
immediately, `ExecuteRefundAsync` always restocked reservation-bearing items, `Inventory.RestockFromReturnAsync`
had no notion of condition, and `IOrderInventoryLifecyclePort` could only release a WHOLE reservation by id. Blind
reuse of any of those would have invented eligibility, refunded before closing the delivery obligation, released an
active-order hold, or turned physically damaged goods into saleable stock.

## Ownership (Contracts only, no cross-module DbContext/entity access)

| Question | Owner | New narrow seam |
| --- | --- | --- |
| How much of this receipt is still free to fund a branch? | **Fulfillment** (it owns the physical receipt) | `CourierReturnClaim` aggregate + `FulfillmentReturnContracts` `CreateCourierReturnClaimCommand` / `CourierReturnClaimSnapshot` / `CourierReturnClaimResult` |
| Is the case allowed, what does it contain, and what phase of closure is it in? | **Returns** | `ReturnOrigin.CourierOrigin` + `CourierOriginClosurePhase` (`None → ObligationClosed → StockSettled → RefundReleased`) on the EXISTING `ReturnRequest`; `IReturnAdminOperations.CreateCourierOriginAsync` / `ReconcileCourierOriginAsync` |
| Is the customer still owed this line, and may the obligation be closed? | **Order** | `IOrderReturnObligationClosure` (`OrderReturnContracts`): validate + monotonically close the exact line obligation; never decrements historical ordered/packed/dispatched quantities |
| May the unconsumed held quantity be released, and may anything become saleable? | **Inventory** | `IInventoryReturnObligationRelease` (quantity-safe partial held release) and the existing restock gateway used ONLY when the disposition is `SaleableRestock` |

The operator surface (Order endpoints → `AdminOrderOperationsOrchestrator`) stays the single action owner: the
courier case is an ordinary post-dispatch command (`create_courier_return`) with the existing permission model
(`return.manage` for the mutation, re-checked on every replay) — there is no parallel Returns page, no parallel
refund service and no new finance panel on the finance-free shipping view.

## ONE source-quantity authority

`fulfillment.courier_return_claims` + `fulfillment.courier_return_claim_lines` hold the exclusive claim.
`ActiveKey` is non-null only while a claim holds capacity (`Requested`/`Approved`), so a filtered unique index
allows many terminal rows while permitting at most ONE active claim per `(receipt, line, condition)`. Claiming
happens inside the owning Fulfillment transaction BEFORE the Returns case exists; the receipt's own quantities are
the upper bound, so `usable 2 + damaged 1` of one line is three independent pools and `usable 2` claimed twice is a
typed refusal (`fulfillment.returns.claim_quantity_exceeded`). Because the guard is a database constraint plus the
aggregate, two independent contexts cannot overdraw (proven in scenario B: `winners=1 refusals=1 claimedTotal=2 free=0`).

A damaged quantity already committed to an ALREADY DISPATCHED replacement is not free capacity:
`FulfillmentUnit.DamagedCommittedToDispatchedReplacement` excludes it and the claim answers
`fulfillment.returns.replacement_already_dispatched`, so a live parcel can never be refunded as if it were unused
held stock (scenario F).

## Creation vs approval — exactly what releases what

* **Create (`Requested`)**: claims source quantity in Fulfillment, creates the Returns case, links it once. Moves
  no money, releases no stock, does not cancel the order, does not touch historical order lines. A pending
  replacement hold is retained (scenario A: `onHandDelta=0`, healthy replacement hold still `Held`).
* **Reject / cancel a `Requested` case**: releases ONLY that branch claim (`Rejected`/`Cancelled`); the order
  stock hold is untouched and the quantity becomes available to a lawful branch again (scenario C:
  `resendableA=2 outstandingDamagedA=1`).
* **Approve**: the owning Returns approval entry runs a phased, resumable settlement —
  1. validate payment/actor/source/claim and record the durable approval intent;
  2. Order closes ONLY the corresponding line delivery obligation (monotonic, idempotent, no shipped-history
     decrement) and Fulfillment cancels an eligible `Created` replacement allocation;
  3. Inventory releases ONLY the unconsumed held quantity assigned to that cancelled obligation (partial holds and
     sibling portions preserved) and settles the physical disposition once;
  4. only when the owning closure is confirmed does the EXISTING refund approval/execution run.
  Each boundary is idempotent and re-entrant: a retry after any boundary resumes from the recorded
  `ClosurePhase`, never re-releases and never double-refunds (scenario D:
  `status=Completed phase=RefundReleased releasedHeld=0 refunds=1 restocks=1 disposition=SaleableRestock`).
* Consumption is not release: the original consumed sale reservation is never released, recreated or consumed
  twice; a released replacement hold is a separate healthy-stock fact.

## Condition-aware physical disposition

`ReturnItem` now carries source shipment/receipt + condition, so the case detail shows money phase and physical
disposition separately. Healthy received quantity may enter saleable stock ONCE, through the existing Inventory
return policy, when the workflow permits it (`SaleableRestock`). Damaged quantity never calls the unconditional
restock: it is recorded as `DamagedNonSaleable` and produces zero saleable increase (scenario E:
`damagedDisposition=DamagedNonSaleable restocks=0 onHand=7`). Duplicate stock addition is prevented because the
disposition settles once per case/phase and the claim already consumed the source quantity.

## Refusal boundaries (explicit, never silent)

* already dispatched / in-transit / delivered replacement → `fulfillment.returns.replacement_already_dispatched`
  with the next legitimate step, no recall/provider integration invented here;
* parent consolidated-package return/damage → unchanged refusal, child never unlocked, no invented package refund;
* unpaid / no real payment → exact blocker, no fake success;
* a source quantity already funding resend/replacement/another case → typed conflict, zero added rows.
