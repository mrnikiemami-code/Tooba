# Slice selection — TB-TMAR-FE-ADMIN-W3

## Selected: admin-sellers

| Criterion | Evidence |
| --- | --- |
| Bounded ownership | `/admin/sellers` list + sellers query/load/map only |
| admin-api debt | `AdminSellerRow`, `mapAdminSellers`, `loadAdminSellers`, `queryAdminSellersGrid` |
| Flat/god touch | `AdminSellersScreen` + `sellerColumns` in admin-screens |
| Risk | Low — read-only directory grid; no checkout/payment |
| Left in admin-api | `AdminSellerOrder` / `AdminSellerFinancial` (order-detail owned) |

## Rejected this wave

- orders — checkout/payment/fulfillment coupling
- customers/receipts — deferred; similar low-risk candidates for W4
