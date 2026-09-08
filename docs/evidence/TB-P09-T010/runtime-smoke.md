# Runtime smoke — TB-P09-T010

Host `:5088` `Host: alpha.localhost` health 200 `{"status":"ok"}`
DevActor: `X-Tooba-Dev-Actor-User-Id: 01a036c2-970e-7000-8eb7-94bf5cc2d8db`
Checkout: `01a07a63-ab32-7000-a62a-e67df880a8ce` (single seller, 3 order lines, fulfillment `ea2d1bd7-6829-41c5-8962-fe313110e57f`)

## A — Pack unavailable until StartProcessing

GET operations: `mark_processing,cancel` — no `mark_packed` / `pack_selected`.
POST `mark_packed` → 400 `order.operation.invalid` (not projected).

## B — StartProcessing then pack

POST `mark_processing` → 200 `Processing`.
Line statuses: all `Processing` (not seller-wide Packed).

## C — Exact / partial selection

POST `pack_selected` L1 qty 1 of 2 → 200.
After: L1 packed=1 `Processing`; L2 packed=0 `Processing`; L3 packed=0 `Processing`.
Seller fulfillment stays `Processing` (did not jump to Packed).

## D — Mixed / incompatible bulk

POST `pack_selected` L1+L2 → 400 `fulfillment.bulk.incompatible`
FA: ردیف‌های انتخاب‌شده برای این عملیات سازگار نیستند.

## E — Row actions

Projected line kebab codes after partial pack of L1: `pack_selected` + `unpack` for L1 only (remaining packable + unpackable qty). Hidden for lines not on the fulfillment unit.

Note: cloned extra order lines L2/L3 are on the checkout but not on the fulfillment item set, so pack_selected for those line IDs is rejected (no silent sibling pack). Sibling isolation on a 3-item unit is covered by `AdminFulfillmentScopeSequenceTests`.
