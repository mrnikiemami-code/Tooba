# TB-P07-T024-R1 — Deferred / canonical ownership

## Canonical decisions

| Concern | Decision |
|---------|----------|
| Product name / short / full text | Locale-based LocalizedText via **Translations** tab only — not fixed NameFa/NameEn on General |
| Brand | Catalog `CatalogProduct.BrandId` — **assignable** in General (`PUT /brand`) |
| Catalog code | **Variant** `CatalogCodeSeam` only — General links to Variants tab |
| Model / Series | **Not invented** as Product scalar; use Category Attribute schema when a category needs it |
| GTIN / EAN / UPC / barcode | **Architectural Concern** — no Catalog identifier table yet; no fake UI |

## Implemented in R1

- General is language-neutral: category, brand assign, global slug, status/timestamps, media preview
- Brand list: `GET /v1/admin/products/brand-options`
- Brand assign/clear: `PUT /v1/admin/products/{id}/brand`
- Translations remain the editable source for fa/en content with locale isolation
