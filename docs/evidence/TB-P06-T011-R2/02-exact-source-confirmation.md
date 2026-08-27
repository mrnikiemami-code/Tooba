# 02 — Exact source confirmation (TB-P06-T011-R2)

Reference root: `D:\Users\User\source\repos\SarvNewVerRequirment\reference\shopeiva`

## Customer

| Tooba | Shopeiva source | Notes |
| --- | --- | --- |
| `returns/return-ui.tsx` → `ReturnFormModal` | `components/dashboard/orders/returnFormModal.jsx` | reason select, amber banner, min-10 description, success step, sticky header |
| `customer-panel/orders/[checkoutId]/page.tsx` | `components/dashboard/orders/orders.jsx` + `returnFormModal` trigger | Delivered-gated CTA; live Host order detail |

## Seller

| Tooba | Shopeiva source | Notes |
| --- | --- | --- |
| `returns/return-ui.tsx` → `ReturnReviewModal` | `components/vendor/panel/orders/returnDetailModal.jsx` | two-step approve/reject, reject reason required |
| `returns/return-ui.tsx` → `ReturnDetailCard` | `returnDetailModal.jsx` read sections | status, date grid, reason/description blocks |
| `vendor-panel/returns/page.tsx` | `vendor/panel/orders/ordersList.jsx` density | accepted Tooba DataGrid shell (T023) |
| `vendor-panel/returns/[returnRequestId]/page.tsx` | `orderDetail.jsx` + `returnDetailModal` | card + review modal |

## Admin

| Tooba | Basis | Notes |
| --- | --- | --- |
| `admin/admin-screens.tsx` → `AdminReturnsScreen` | T024 Admin ops shell + Shopeiva vendor order detail card patterns | no dedicated Shopeiva Admin returns |

Accepted accent deviation: `#2563EB` vs `#E53935`.
