TB-262 HANDOFF — damaged-goods resolution: exact owning seams and gaps
=====================================================================
Status: PLANNED, **NOT ISSUED**, NOT implemented in TB-261. Nothing in this file is a claim that
damage is resolved. This is the honest remaining requirement for the next task.

What exists today after TB-258/258-R1/258-R2/258-R3 + TB-261
------------------------------------------------------------
- A physically received origin receipt is recorded per line **and per condition**:
  `ShipmentReturnReceipt` / `ShipmentReturnReceiptItem`, unique on `(receipt_id, order_line_id, condition)`
  (`ux_shipment_return_receipt_items_line_condition`).
- `Tooba.Fulfillment.Domain.ValueObjects.ReturnReceiptCondition` = `Usable = 0`, `Damaged = 1`.
  Its own doc comment is the governing rule: *only `Usable` goods may become resend capacity; damaged
  goods are received and recorded but never resendable.*
- Capacity authority (one place, `FulfillmentUnit`):
  - `ReturnedUsableQuantity(sourceShipmentId, orderLineId)` counts `Usable` only;
  - `ResendCommittedQuantity(sourceShipmentId, orderLineId)` counts live (non-cancelled) child resend
    parcels of THAT source;
  - `ResendableQuantity(...) = max(0, usable − committed)`, and `HasResendableQuantity(sourceShipmentId)`
    is what the projection turns into `canResend` for exactly that parcel.
  - `CreateResendShipment` refuses anything above that number (`fulfillment.return.receipt_qty_exceeds`
    family) and links `ResendOfShipmentId` to the **immediate** source.
- Consequence, measured on the user's fixture (`tb261-live-fixture.txt`): a receipt of `1 Damaged`
  yields `returnedUsableQuantity = 0` and `resendableQuantity = 0`. No resend is offered — correct.

So the damaged unit is *recorded* and *excluded from capacity*. It is **not** resolved: there is no
quarantine, no replacement stock movement, no customer/financial decision, and no operator exit.

Owning seams a future TB-262 must go through (read-only inspection, no code changed here)
---------------------------------------------------------------------------------------
1. FULFILLMENT (owns the physical fact)
   - `Modules/Fulfillment/Tooba.Fulfillment.Domain/Aggregates/FulfillmentUnit.cs`
     (`ReturnedUsableQuantity` line ~717 / ~751, `ResendCommittedQuantity` ~730 / ~761,
      `ResendableQuantity` ~740 / ~769, `CreateResendShipment` ~1004)
   - `.../ValueObjects/ReturnReceiptCondition.cs` — the condition vocabulary. Any new resolution state
     belongs beside this owning aggregate, never as a UI-only status.
   - `Modules/Fulfillment/Tooba.Fulfillment.Contracts/Operations/FulfillmentAdminOperationsContracts.cs`
     — the ONLY seam the Order/orchestration side may call; capabilities already project
     `canRecordNonDelivery/canStartReturn/canRecordReturnReceipt/canResend`.

2. INVENTORY (owns stock; the damaged unit currently moves NO stock)
   - `Modules/Inventory/Tooba.Inventory.Contracts/Returns/InventoryReturnContracts.cs`
     `IInventoryReturnGateway.RestockFromReturnAsync(reservationId, quantity, idempotencyKey)` — its own
     doc says *after a successful refund*. **It must not be used for damaged goods**: returning damaged
     goods to saleable stock is exactly the forbidden conversion.
   - `Modules/Inventory/Tooba.Inventory.Contracts/Fulfillment/FulfillmentInventoryLifecyclePort.cs`
     `ConsumeAsync(reservationId, consumerKey)` — replay-safe consume with a stable consumer identity.
     A replacement MUST NOT consume the original sale reservation a second time.
   - `Modules/Inventory/Tooba.Inventory.Contracts/Checkout/CheckoutInventoryReservationContracts.cs`
     `ICheckoutInventoryReservationPort.ReserveForCheckoutAsync(...)` — the only existing reservation
     entry point. **Gap:** there is no `reserve replacement for an already-paid order` command.
   - **Gap:** no quarantine / location concept exists in Inventory Contracts at all, so "set the damaged
     unit aside" has no owning seam yet.

3. RETURNS / CUSTOMER SERVICE (owns the customer-facing and financial decision)
   - `Modules/Returns/Tooba.Returns.Contracts/Operations/ReturnAdminOperationsContracts.cs`
     (`CreateAdminInitiatedAsync`, `ApproveAsync`, `RejectAsync`, `RetryRefundAsync`,
      `EvaluateEligibilityAsync`) — the delivered-customer RMA path.
   - `Modules/Returns/Tooba.Returns.Contracts/Settlement/ReturnSettlementContracts.cs`
     (`IReturnSettlementReader`) and `.../Events/RefundSucceededIntegrationEvent.cs` — money.
   - A courier-damaged, never-delivered unit is NOT covered by this path today: it has no return request,
     no refund decision, and no replacement decision.
   - **Gap:** no seam links an origin receipt line of condition `Damaged` to either a refund decision or
     a replacement order line.

Planned acceptance cases (do not implement before the task is issued)
--------------------------------------------------------------------
A. Damaged receipt `1` and NOTHING else: capacity stays 0, no resend action exists, the operator sees the
   damaged quantity as its own unresolved requirement (TB-261 already labels it
   «آسیب‌دیده — نیازمند تعیین تکلیف»), and NO packaged/shipped/ordered counter changes.
B. Mixed receipt `usable 2 + damaged 1`: exactly 2 resendable and 2 resendable after one resend of 2;
   the damaged 1 keeps its own unresolved state and never joins the resend.
C. Replacement path (if the customer-service decision is replacement): a NEW linked parcel with immediate
   lineage to the damaged source, funded by an authorized replacement decision — never by inventing
   `Usable` capacity, never by a second consume of the original reservation, never by raising
   `quantityPacked` or decrementing lifetime `quantityShipped`.
D. Refund path: settlement/refund owns the money; the damaged unit does not silently restock. If any
   stock returns, it is through `IInventoryReturnGateway` with an idempotency key and an explicit
   `Damaged`-vs-`Usable` distinction (today the gateway has no condition parameter — a gap).
E. Concurrency/replay: identical recorded decision replays with the same durable ids; changed intent is a
   typed refusal with zero writes; a locked package member still refuses.
F. No duplicate: one damaged source can never fund two live branches (the same rule already proven for
   usable capacity in TB-258-R1 case C).

Hard constraints inherited from TB-261 (do not violate)
------------------------------------------------------
No silent damaged→usable conversion. No raising packed/ordered. No decrementing lifetime shipped. No
automatic refund. No releasing the original sale reservation twice. No adding saleable damaged stock. No
swapping the customer-selected product. No charging the customer again. No status-only button that
claims resolution without an owning fact.
