# R1 — Cancel domain/application guard

## Authoritative path

- Domain: `SellerOrder.Cancel()` only for `PendingPayment|Submitted|ReservationRequested` (idempotent on `Cancelled`); otherwise `order.cancel.forbidden`.
- Paid pre-shipment: `CancelPaidBeforeShipment()` only after application gate.
- Shared policy: `SellerOrderCancellationPolicy` + `ISellerOrderCancelFulfillmentGate` (`FulfillmentSellerOrderCancelGate`).
- Mutation: `CheckoutDirectory.CancelSellerOrderAsync` enforces policy before domain transition.
- Host: `AdminOrderOperationsComposer.ExecuteAsync` always routes `cancel` through `CancelSellerOrderAsync` (projection may hide; API cannot bypass). Maps `order.cancel.forbidden`.

## Runtime (Host `:5088`, Dev Actor admin)

Delivered Paid seller order `01a0453b-6831-7000-b7eb-f5f957802309`:

```http
POST /v1/admin/orders/01a0453b-6829-7000-8c77-32cfb5f5d409/operations
{"code":"cancel","sellerOrderId":"01a0453b-6831-7000-b7eb-f5f957802309"}
→ 400 errorCode=order.cancel.forbidden
```

Same after R1 deliver of `01a0451c-fac1-7000-9371-f9d9ebb05855` → `order.cancel.forbidden`.

## Tests

`SellerOrderCancellationGuardTests` + focused filter with Admin/Settlement/Eligibility: **16 passed**.
