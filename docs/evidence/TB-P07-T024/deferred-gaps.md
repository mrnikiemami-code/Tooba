# TB-P07-T024 — Deferred / not invented Product fields

## Intentionally not invented

| Field | Reason |
|-------|--------|
| Model / series | Not present on `CatalogProduct`; no justified Catalog seam |
| GTIN / EAN / UPC / barcode | Not in Catalog domain |
| Product-level Catalog code | Code lives on `CatalogVariant.CatalogCodeSeam` |
| Brand assign in Workspace | Brand is readable (`BrandName`); assign API not exposed on Product core yet |
| Per-locale slug table | Global `SlugSeam` preserved; non-fa core updates no longer rewrite global slug/SEO seam |
| Media DAM / file upload | Existing placeholder assignment seam only |

## Implemented in T024

- General: full description (LocalizedText `full_description`) + denser grouping
- Translations: real editable locale content via `PATCH /core` with fa-IR-only global seam mutation
- Locale readiness: کامل / ناقص / ایجاد نشده
