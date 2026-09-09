# Runtime smoke A–E — TB-P09-T016

Host `:5088` `Host: alpha.localhost`. Raw: `runtime-raw.json`. `ok: true`.

## A — paid pre-dispatch cancel (Created + tracking)

Checkout `01a084cc-8be3-7000-bbcf-9fb2cfb1cc2c`. Qty 1.25 kg. Packed 0.50, shipment Created, tracking assigned. One `cancel` projected. Confirm covers shipment/inventory/refund. POST cancel 200. After: only `restore_cancelled_order`. Reserved `1.25 → 0`. Shipment `Cancelled`.

## B — delivered T015 checkout blocks

Checkout `01a084a4-4138-7000-a7d1-676e53356f73`. Cancel not projected. POST cancel 400 `order.cancel.forbidden` / `پس از ارسال کالا، لغو کامل سفارش امکان‌پذیر نیست.`

## C — fulfillment stop after cancel

`pack_selected` and `dispatch_shipment` after A: 400 `order.cancelled.blocks_action`.

## D — unpaid waiting-payment cancel

Checkout `01a084cc-8aef-7000-ae2d-3dfaff8d6344`. Cancel 200. After: restore only.

## E — decimal regression

Cart/order 1.25. Pack 0.50. Remain 0.75. Inventory release exact 1.25.

Grid query after A lists checkout as `Cancelled` without full page reload (reloadToken / operations refresh).
