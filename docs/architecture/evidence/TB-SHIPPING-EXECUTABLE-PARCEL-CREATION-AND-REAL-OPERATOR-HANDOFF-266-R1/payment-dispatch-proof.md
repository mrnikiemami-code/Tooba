# Payment-authority dispatch proof for an EXISTING parcel

Task: `TB-SHIPPING-EXECUTABLE-PARCEL-CREATION-AND-REAL-OPERATOR-HANDOFF-266-R1`

The 266 negative cases stopped at "an unpaid order has no fulfillment at all", which is a command-level refusal
only. This round closes the narrower gap the task named: a parcel that **already exists** (`Created` / tracked)
whose source handoff has since lost payment authority.

File: `src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests/Behavior/PaymentAuthorizedSupplyGate266Tests.cs`
Result: **7/7 passed** (`dotnet test … --filter FullyQualifiedName~PaymentAuthorizedSupplyGate266Tests`).

## What the gate is

`FulfillmentDirectory.EnsureSupplyPaymentAuthorizedAsync` reads the **owning** Order truth
`IOrderFulfillmentReader.GetHandoffAsync(sellerOrderId).IsPaid` and raises
`fulfillment.order.payment_not_authorized` (or `fulfillment.order.not_found`) **before** inventory consume, state
change, event or parcel write. It runs on mark-processing, selections, packing, shipment creation, single and
consolidated dispatch, resend and replacement parcels.

## New cases

### A. `Dispatch_after_payment_authority_lost_is_refused_without_dispatch_or_consumption`

Setup follows the real lifecycle while the order is paid: create-from-paid-seller-order → mark processing →
mark packed → `CreateShipmentAsync` (one `Created` parcel, `ConsumeCalls == 0` because creation does not consume).
Then the recorded handoff flips to `IsPaid = false` and dispatch is attempted.

Asserted:

```
error.Code == "fulfillment.order.payment_not_authorized"
db.Shipments.Single().Status == ShipmentStatus.Created
db.Shipments.Single().DispatchedAt == null
inventory.ConsumeCalls == 0            // the reservation was NOT consumed
```

So the refusal happens before any physical effect, and the existing parcel is left exactly as it was.

### B. `Bulk_group_selections_after_payment_authority_lost_are_refused_without_writing`

Same paid setup, then the handoff flips **before** any selection work. Both bulk grouped entries are attempted:

```
ProcessSelectionsAsync(...) -> "fulfillment.order.payment_not_authorized"
PackSelectionsAsync(...)    -> "fulfillment.order.payment_not_authorized"
db.Items.Single().QuantityProcessing == 0
db.Items.Single().QuantityPacked == 0
db.Fulfillments.Single().Status == ReadyToFulfill
```

No partial write survives a refused bulk call.

### C. `Consolidated_package_with_unpaid_member_refuses_dispatch_and_writes_nothing`

Two paid seller orders of the same checkout are materialised into two fulfillment units, each marked
processing → packed → one `Created` parcel (`shippingMethodCode: "store_courier"`, required by the package
aggregate), then combined with `CreateConsolidatedPackageAsync` (package `Created`, `ConsumeCalls == 0`).

Only **member B** loses payment authority, then the package dispatch is attempted:

```
error.Code == "fulfillment.order.payment_not_authorized"
db.Shipments.Count() == 2 and both remain ShipmentStatus.Created with DispatchedAt == null
inventory.ConsumeCalls == 0
db.ConsolidatedPackages.Single().Status == ConsolidatedPackageStatus.Created
```

One unauthorized member refuses the **whole** package before any member effect, and the paid member is not
silently dispatched either.

## Support changes (test-only, no production change)

- `NoopInventory` gained a `ConsumeCalls` counter so "refused before consume" is assertable rather than assumed.
- `MultiOrderReader` (a test double of `IOrderFulfillmentReader`) maps handoffs by `SellerOrderId` and can flip a
  single member's `IsPaid`, which is what makes case C a real per-member authorization test.
- `Handoff(...)` accepts optional seller order / party / line ids so the second seller is not a copy of the first.

The disclosed limit: `IOrderFulfillmentReader` is the **controlled source reader** in these tests. That proves the
owning gate's behaviour on a recorded payment fact; it is **not** a real provider payment, no money moved, and no
payment/package/quantity/retained-occurrence guard was weakened or bypassed.

## Authorized positive path is the live trial, not a unit test

The positive half of this task is the actual coherent Development trial described in
`browser-control-handoff.md`: the order was paid through the sanctioned Development payment confirmation, then the
real UI created, tracked and dispatched one parcel. Exact recorded dispatch replay stays historical and safe.
