# Product SEO

> **Task:** TB-P07-T017 · **Phase:** P07 Advanced Catalog
> **Depends on:** Catalog LocalizedText + SlugSeam, [PRODUCT-CATALOG-ADMIN.md](./PRODUCT-CATALOG-ADMIN.md), locale-prefixed storefront routing

## Ownership

Catalog owns Product SEO inputs:

| Field | Storage | Locale scope |
|-------|---------|--------------|
| Public slug | `CatalogProduct.SlugSeam` | **Global** (one canonical public slug) |
| SEO title | `CatalogLocalizedText` field `seo_title` (+ `SeoTitleSeam` mirror for fa) | Per locale |
| SEO description | `CatalogLocalizedText` field `seo_description` | Per locale |

Do **not** invent a fragile per-locale product slug table. Category already has per-locale translation + slug history; Product reuses the existing global `SlugSeam` seam as the storefront identity.

Canonical/robots composition stays with the SEO / storefront layer (`docs/architecture/13-seo-architecture.md`). This task supplies Catalog metadata only.

## Slug rules

- Persian Unicode allowed (no forced transliteration)
- Deterministic normalization via `CatalogCategorySlugNormalizer` (kebab, trim, lowercase letters where applicable)
- Human-readable; **no ProductId/Guid suffix** in the normal public URL
- Uniqueness: **tenant-global** on `SlugSeam`
- Duplicate error (Persian): «این نشانی صفحه قبلاً استفاده شده است.»
- Invalid/empty after normalize: «نشانی صفحه نامعتبر است.»

## Public route

Locked storefront shape (already live):

```text
/{locale}/products/{slug}
```

Examples: `/fa/products/گوشی-سامسونگ-galaxy-s24`, `/en/products/linen-shirt`.

Do **not** invent `/product/` (singular). Resolver: published product by `SlugSeam` (Host storefront composition). Guid-based lookup remains a non-primary fallback only.

## Slug history / redirects

`CatalogCategorySlugHistory` exists for **categories**. There is **no** Product slug-history table in this baseline. Do not invent a parallel redirect subsystem here; old product slugs do not auto-redirect until a later Catalog task adds a safe history model.

## SEO readiness

`ProductSeoReadiness` (separate from Media / Attributes / commercial readiness):

| Flag | Rule |
|------|------|
| `HasValidSlug` | Non-empty normalized `SlugSeam` |
| `HasSeoTitleOrFallback` | Locale `seo_title` **or** product name (documented fallback) |
| `HasSeoDescription` | Locale `seo_description` present |
| `HasLocalizedIdentity` | Product name available for locale (or fa-preferring fallback) |
| `IsReady` | All of the above |

`MessageFa` examples:

- آدرس محصول تکمیل نشده است
- عنوان نتیجه جستجو تکمیل نشده است
- توضیح نتیجه جستجو تکمیل نشده است
- اطلاعات هویتی محصول تکمیل نشده است
- اطلاعات سئو کامل است

No folklore character-count hard locks. **No** Offer / Price / Stock / Pricing / Inventory dependency.

## Admin UX

Product Workspace tab **SEO**:

- **VIEW:** آدرس محصول، عنوان برای موتورهای جستجو، توضیح نتیجه جستجو، مسیر عمومی، readiness، SERP-style preview — no mutation controls
- **EDIT:** same fields + locale switch (fa-IR / en isolation) + Save / Cancel + dirty-state + validation errors from Host
- VIEW-only workspace scope cannot mutate (`CanEditCatalog` / `X-Tooba-Workspace-Scope: view`)

## APIs

| Method | Path |
|--------|------|
| GET | `/v1/admin/products/{id}/seo?locale=` |
| PUT | `/v1/admin/products/{id}/seo` |
| GET | `/v1/admin/products/{id}/seo/readiness?locale=` |

`PATCH /v1/admin/products/{id}/core` still accepts SEO fields for the General tab; dedicated SEO endpoints are the Workspace SEO tab contract.

CatalogDirectory owns mutations; Host thin-wraps + Admin authorization.

## Storefront linkage

- Product cards / PDP already link `/{locale}/products/{slug}` via `SlugSeam`
- PDP metadata prefers LocalizedText `seo_description` when present, then short description, then a minimal composed fallback
- Shopeiva visual structure unchanged in this task

## Boundaries

- Product ≠ Offer; no `Product.Price` / `Product.Stock`
- No Pricing/Inventory joins for SEO
- AppDataGrid unchanged; no raw `AgGridReact`
- Level-3 category assignment and category-schema attributes unchanged
