# Runtime Smoke

Date: 2026-09-08
Host `:5088` `Host: alpha.localhost` health 200 `{"status":"ok"}`
DevActor: `X-Tooba-Dev-Actor-User-Id: 01a036c2-970e-7000-8eb7-94bf5cc2d8db`

## Multi-seller whole-order cancel

Checkout `01a07c4a-2344-7000-ad84-a20f4d31d392`

GET operations: codes `restore_deposit,cancel` — **CANCEL_COUNT=1**

POST `cancel` 200 cancelled 3 seller orders.

After cancel: `restore_deposit,restore_cancelled_order`

POST `restore_cancelled_order` 200. After: `restore_deposit,cancel`

## Manual payment restore

Same checkout, payment `d8e2c3ff-6fd6-42d4-bcf0-ce28bf2f36d9`

POST `restore_deposit` 200 `{ status: Pending, newlySucceeded: false }`

After: `confirm_deposit,reject_deposit,cancel`

Repeat restore while Pending → 400 `payment.restore.invalid_state` (not projected; no success event)

## Forbidden restore

Active (non-cancelled) order: POST `restore_cancelled_order` → 400 `order.operation.invalid`

Delivered checkout `01a07bd2-5ef4-7000-b770-a2173d8ae25c`: actions `request_return` only; restore 400.

## Shipment rejection + tracking

Packed checkout `01a07bd1-03f4-7000-8b35-5306591ec94a` / fulfillment `3678fbb9-…`

create_shipment 200 → assign_tracking `TRK-T009-OLD` 200 → `correct_tracking` visible → correct to `TRK-T009-NEW` 200 → cancel_shipment 200 → create_shipment replacement 200.

## Focused validation

- Host.Tests AdminOrderCorrectiveActions + AdminOrderOperations + FulfillmentLineQuantityOps + FulfillmentFoundation: 27 passed
- FE admin-order-operations.test.ts: 19 passed
- recovery-staleness.guard.test.mjs: 3 passed
- git diff --check: clean (CRLF warnings only)
