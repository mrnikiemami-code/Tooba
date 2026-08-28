# TB-P07-T001-R3 — Order grid

## Columns
Reference (readable order id), customer, seller count, lines, payment, status, payable amount, date, مشاهده action.

## Status metadata
- Payment + status columns use multi-select `enumOptions` with Persian labels from `formatAdminStatus`
- Expanded labels cover common payment/fulfillment/order codes (Paid, PendingPayment, Delivered, Shipped, …)

## English leftovers removed
| Before | After |
| --- | --- |
| description «پیگیری checkout…» | «پیگیری تسویه و سفارش‌های فروشندگان» |
| Fulfillment column «Checkout» | «شناسه تسویه» |
| Detail «Checkout:» | «تسویه:» |
| Detail «Seller order:» | «سفارش فروشنده:» |

## Saved views
`savedViewStore={createHostSavedViewStore("grid.admin.orders")}` via `GridPage`.

## Files
- `src/frontend/app/admin/admin-screens.tsx`
- `src/frontend/app/admin/admin-api.ts` (`formatAdminStatus`)
