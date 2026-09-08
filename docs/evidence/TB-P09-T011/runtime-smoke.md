# Runtime smoke — TB-P09-T011

Host `:5088` `Host: alpha.localhost` health 200 `{"status":"ok"}`
DevActor: `X-Tooba-Dev-Actor-User-Id: 01a036c2-970e-7000-8eb7-94bf5cc2d8db`

## Orders

- Multi (A/B): `01a07ec5-81ad-7000-b77d-4f60962d6d50`
- Single (C–F): `01a07ec5-7b94-7000-8e14-9b81ae2261d1`

## A — Cancel + grid refresh

POST `cancel` on multi → 200. GET operations → `[restore_cancelled_order]` only (confirm/reject gone).
`POST /v1/admin/orders/query` → that row `status=Cancelled` / `paymentState=Cancelled` without a browser reload. FE grid remounts via `reloadToken` after kebab `onCompleted`.

## B — Cancelled blocks forward/payment

POST `confirm_deposit` / `reject_deposit` / `mark_processing` → 400 `order.cancelled.blocks_action` FA «سفارش لغوشده است؛ این عملیات مجاز نیست.»

## C — Single selection after StartProcessing

Pack before StartProcessing → 400 `order.operation.invalid` (T010 sequence preserved).
POST `mark_processing` → 200 `Processing`.
GET operations: seller `pack_selected` + per-line `pack_selected` for L1/L2/L3. FE single-select is never mixed; Pack Selected uses intersection.

POST `pack_selected` L1 qty 1 → 200; L1 packed=1; seller stays `Processing`.

## D — Mixed Processing + Packed

After L1 fully packed and L2 still Processing: POST `pack_selected` L1+L2 → 400 `fulfillment.bulk.incompatible` FA «ردیف‌های انتخاب‌شده برای این عملیات سازگار نیستند.» Same for mixed unpack.

## E — Row kebab

After StartProcessing: line-level `pack_selected` projected (kebab visible). After partial/full pack of L1: `unpack` for L1 only. Lines without actions stay hidden (`lineActions.length === 0`).

## F — Return display once

`returnRemainingDisplay=""`, `returnDeadlineDisplay="7 روز پس از تحویل"` — FE `canonicalReturnDisplay` shows the policy once.
