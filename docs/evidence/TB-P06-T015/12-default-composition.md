# 12 — Default composition (TB-P06-T015)

Canonical order (seed + restore-default + frontend default):

1. `hero`
2. `stories`
3. `category_grid`
4. `product_rail_flash`
5. `best_sellers`
6. `product_rail_most_viewed`
7. `middle_banners`
8. `brands`
9. `newest_products`
10. `customer_reviews`
11. `latest_articles`

Matches pre-Task `storefront-home.tsx` visual order so restore-default returns Shopeiva-faithful layout.

E2E: `afterRestoreSectionTypes` in `_composition-e2e-api-proof.json` equals this list.
