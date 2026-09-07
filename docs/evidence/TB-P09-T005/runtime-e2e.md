# Runtime E2E

## Host / FE

- Host `http://127.0.0.1:5088` listening (`Tooba.Host`)
- FE `http://127.0.0.1:3000/admin/orders` → HTTP 200
- `GET /v1/admin/dashboard` (Host: alpha.localhost + DevActor) → 200
- `GET /v1/admin/orders` → 200 (seeded Paid orders present)

## Migrations

Applied via MigrationRunner `--tenant store-alpha`:

- `fulfillment.items.quantity_packed`
- `order.order_lines` return snapshot columns (`is_returnable_snapshot`, `return_window_days_snapshot`, …)

## Live ops smoke (multi-seller checkout `01a07a63-adea-7000-b184-277160114a48`)

1. `GET .../operations` → `mark_processing`, `mark_packed`, `cancel` projected for ReadyToFulfill sellers
2. Whole-group `POST .../operations` `mark_packed` with empty `selections` → HTTP 200; seller fulfillmentStatus `Packed`; all lines `quantityPacked == quantity`
3. Selected-line `mark_packed` with `selections[{orderLineId,quantity}]` on second seller → HTTP 200
4. Order line DTO exposes `isReturnable=true`, `returnWindowDays=7` (deadline/remaining labels empty until delivered — expected)

No fake final-state SQL insertion. Domain unit tests cover unpack/cancel/split/post-dispatch rejects.
