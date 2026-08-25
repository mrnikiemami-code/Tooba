# 12 — Mega Menu Data Map

Task: `TB-P05-T016`

```text
CatalogCategory (published)
  ParentCategoryId self-FK (arbitrary depth)
    ↓
StorefrontComposer.ListCategoriesAsync()
    ↓
GET /v1/storefront/categories  (+ /home payload)
  StorefrontCategoryItem { categoryId, parentCategoryId, name }
    ↓
storefront-api mapCategory()
    ↓
StorefrontShopeivaHeader client projection
  L1 = parentCategoryId == null
  L2 = parentCategoryId == selectedRoot
  L3 = parentCategoryId == selectedL2
    ↓
Links → /products?categoryId={id}
```

Development seed path:

```text
StorefrontDemoCatalogMatrix (8×24×72)
  → StorefrontDemoCatalogBootstrap / EnsureThirdLevelCategoriesAsync
  → Catalog DB
  → same live API
  → Mega Menu UI
```

No frontend-only fake hierarchy.
