# 04 — Order demo data

Task: TB-P06-T024-R2

## Flow

Orders created through legitimate Cart → Checkout submit (`ICartDirectory` / checkout services), **not** direct Order table inserts.

## Orders

| Kind | Idempotency key | Order number (runtime) | SellerOrderId |
|------|-----------------|------------------------|---------------|
| Mobile | `acc-demo-seed-mobile-v1` | `TB-20260827162426-01-d8a7f2` | `01a04409-7a06-7000-b34f-d5418b7bd2f0` |
| Books | `acc-demo-seed-books-v1` | `TB-20260827162525-01-66cecb` | `01a0440a-5da0-7000-aa6e-806493955070` |
| Mixed | `acc-demo-seed-mixed-v1` | `TB-20260827162525-01-1bb893` | `01a0440a-5e2f-7000-9d29-cd869acbdce7` |

## Category projection

Order lines carry Catalog category snapshots from T024-R1; Mobile Order Operator filtering uses those snapshots.

## Browser

Owner list shows all three (`captures/05-seller-orders-owner.png`).
Employee list shows Mobile + Mixed only (`captures/06-seller-orders-employee.png`).
