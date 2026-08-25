# 13 — No Merchandising Proof

Task: `TB-P05-T016`

Mega Menu payload remains navigation-only:

- DTO fields: `categoryId`, `parentCategoryId`, `name` (3 properties)
- Test: `Mega_menu_category_payload_is_navigation_only`
- Serialized JSON excludes price/stock/offer/product keys
- UI renders category links only (no product cards, prices, inventory, seller, ratings)

Live probe `_api-probe.json`: `containsPrice=false`, `containsStock=false`.
