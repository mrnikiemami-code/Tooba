# Slice selection — TB-TMAR-FE-ADMIN-W1

## Selected: admin-promotions

| Criterion | Evidence |
| --- | --- |
| Ownership | Seller promotion/coupon supervision |
| admin-api methods | `AdminPromotionRow`, `mapAdminPromotions`, `loadAdminPromotions`, `deactivateAdminPromotion` (removed) |
| Screen | Was in `admin-screens.tsx` (CRITICAL); extracted without full god-file decomposition |
| Risk | Low visual; admin-only; bounded client grid |
| Giants | Touched `admin-screens` only to remove promotions slice (LOC shrink 1230→1120) |

## Rejected

- reviews/sellers/customers/receipts — still entangled in admin-screens; promotions was cleanest API+screen pair
- category/product/article — giants
- catalog-units — already separate API; would not shrink admin-api capability methods
