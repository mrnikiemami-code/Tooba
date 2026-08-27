# 08 — Live data runtime proof (TB-P06-T011-R2)

## Verified live (no fake objects)

| Surface | Source | Proof |
| --- | --- | --- |
| Customer orders list | Host `GET /v1/customer/orders` | PNG `06-tooba-customer-orders-desktop.png` — real references/amounts |
| Customer order detail | Host `GET /v1/customer/orders/{checkoutId}` | PNG `05-tooba-customer-order-detail-desktop.png` — line title «پازل ۱۰۰۰ تکه», Paid |
| Seller returns grid | Host `GET /v1/seller/returns` | API returns `[]` — UI shows empty honest state (not seeded demo rows) |
| Admin returns grid | Host `GET /v1/admin/returns` | empty live grid |

## Shopeiva reference runtime

Uses local mock `ordersData` / `orders.json` — **reference UI only** (protocol §A). Side-by-side compares structure; Tooba binds live APIs.

## Not faked

- No hardcoded refund amounts in Tooba UI components
- No static return status badges with demo IDs in Tooba grids
- No mock successful approve/reject actions in Tooba pages

## Eligibility gate (honest empty states)

- Customer return modal not force-opened without **Delivered** fulfillment (would violate live-data rule)
- Seller review modal not force-opened without **Requested** return row
