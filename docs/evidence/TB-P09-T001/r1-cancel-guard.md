# R1 — Cancel guard (TB-P09-T001-R1)

## Authoritative path

- Domain: `SellerOrder.Cancel()` only for `PendingPayment|Submitted|ReservationRequested` (idempotent Cancelled); otherwise `order.cancel.forbidden`.
- Paid pre-shipment: `CancelPaidBeforeShipment()` after `SellerOrderCancellationPolicy` + `ISellerOrderCancelFulfillmentGate` (ReadyToFulfill/Processing, shipmentCount=0).
- Application: `CheckoutDirectory.CancelSellerOrderAsync` enforces policy before domain mutation.
- Host: `AdminOrderOperationsComposer.ExecuteAsync` always routes `cancel` through `CancelSellerOrderAsync` (projection may hide action; direct POST cannot bypass). Forbidden → HTTP 400 `errorCode=order.cancel.forbidden`.

## Runtime (Host `:5088`, tenant `alpha.localhost`, DevActor admin)

| Case | Result |
|------|--------|
| Delivered Paid `01a0453b-6831-…` cancel | `order.cancel.forbidden` HTTP 400 |
| Newly delivered `01a0451c-fac1-…` cancel | `order.cancel.forbidden` HTTP 400 |
| ReadyToFulfill (0 shipments) | `cancel` projected when permitted |

## Tests

`SellerOrderCancellationGuardTests` — allow / forbid / idempotent / policy / eligibility ignores settlement.
