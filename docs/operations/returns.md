# Returns & Refunds operations

Task: `TB-P06-T011`

## Domain boundaries

- **Order** owns checkout and paid state.
- **Fulfillment** owns delivery quantities.
- **Return** owns merchandise return workflow (`returns` schema).
- **Refund** money movement goes through **Payment** `IPaymentRefundGateway` only.

No cross-module SQL JOIN. Eligibility reads use contract bridges only.

## Customer flow

1. Order must be **Paid** and fulfillment **Delivered** for eligible lines.
2. Customer POST `/v1/customer/returns` with `sellerOrderId`, line quantities, idempotency key.
3. Return status starts at **Requested**.

## Seller flow

- GET `/v1/seller/returns` — list for seller party
- POST `/v1/seller/returns/{id}/approve` — triggers refund via Payment gateway
- POST `/v1/seller/returns/{id}/reject` — terminal reject

## Admin flow

- GET `/v1/admin/returns` — operational grid
- POST `/v1/admin/returns/{id}/retry-refund` — when status is **RefundFailed**

## Refund gateway

| Environment | Provider |
| --- | --- |
| Development/Testing | `FakePaymentRefundGateway` (success; `-FAIL-REFUND` suffix fails) |
| Production unconfigured | `FailClosedPaymentRefundGateway` |

## Return window

Default **30 days** from last delivery timestamp (foundation constant in Returns module).

## Events (Outbox)

- `return.requested.v1`
- `return.approved.v1`
- `refund.succeeded.v1`

## Inventory restock

Only via `IReturnInventoryGateway` contract after successful refund. Current implementation logs/no-op for consumed reservations (restock deferred).
