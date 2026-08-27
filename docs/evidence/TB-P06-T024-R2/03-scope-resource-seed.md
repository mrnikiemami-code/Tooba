# 03 — Scope resource seed

Task: TB-P06-T024-R2

## Categories (Catalog)

| Name (FA) | Role |
|-----------|------|
| دمو کنترل دسترسی | Root |
| موبایل | Leaf under root |
| کتاب | Leaf under root |

Created via `ICatalogDirectory.CreateCategoryAsync` when missing; reused by Persian name lookup.

## Products / offers

| Slug | Title (demo) | Category |
|------|--------------|----------|
| `acc-demo-mobile-phone` | گوشی دمو موبایل | موبایل |
| `acc-demo-books-novel` | کتاب دمو | کتاب |

Each offer has inventory at location codes `WH-ACC-MOB` / `WH-ACC-BOOK`, tax category assigned for checkout, and ≥1 variant attribute axis.

## Real IDs (runtime snapshot)

See demo-preview: `mobileCategoryId`, `booksCategoryId`, `mobileOfferId`, `booksOfferId`.
