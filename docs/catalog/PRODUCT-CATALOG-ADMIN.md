# Product Catalog Admin

> **Task:** TB-P07-T014 · **Phase:** P07 Advanced Catalog
> **Depends on:** [CATEGORY-ARCHITECTURE.md](./CATEGORY-ARCHITECTURE.md), Category Attribute / Facet / PLP foundations, [PRODUCT-MEDIA.md](./PRODUCT-MEDIA.md)

## Product Master ownership

| Role | Owns |
|------|------|
| **Admin** | Canonical Product Master — identity, category, attributes, variants, publish/archive |
| **Seller** | Offer on Product/Variant — price, inventory (later) |

**Product ≠ Offer ≠ Price ≠ Inventory.**

Forbidden on Product:

- `Product.Price`
- `Product.Stock`
- Seller ownership as Product field

## List

- Route: `/admin/products` (locale-prefixed via middleware as `/fa/admin/products`)
- Grid: canonical `AppDataGrid` only — **no** raw `AgGridReact`
- Preserved: Saved Views, filters, Advanced Filter, Jalali, Column Manager, exports, pinned row actions
- Row actions: مشاهده (`?scope=view`) · ویرایش (`?scope=edit`) · بایگانی/حذف امن

## Create flow (progressive)

CTA: **+ محصول جدید**

First step only:

1. Category (searchable human path)
2. Product name (active locale)
3. Slug suggestion (human; no ProductId suffix)
4. Status = **Draft**

Then open Product Workspace. Do not collect attributes/variants/SEO/media/price in the create modal.

## Product Workspace

Route: `/admin/products/{productId}` with `scope=view|edit`

### VIEW / EDIT

Locked Admin form pattern via `useAdminFormMode`:

- VIEW: readable summary, no Save
- EDIT: explicit ویرایش · Save / Cancel · dirty protection · return to VIEW after save

### Tabs

| Tab | Status in T011 |
|-----|----------------|
| عمومی | Implemented foundation |
| ترجمه‌ها | Locale-based foundation (LocalizedText + SlugSeam) |
| ویژگی‌ها | **Implemented** — category-driven Product Attribute Values editor (T012); variant axes informational → tab تنوع‌ها |
| تنوع‌ها | **Implemented** — axes + combination preview + matrix apply (T013); Persian **تنوع**, not گونه |
| رسانه | **Implemented** — gallery + primary + readiness (T014); Media library / binary upload deferred |
| SEO | Shell |
| انتشار | Lifecycle + read-only commercial readiness |
| تاریخچه | Shell |

## Translation model

Locale-based fields (no `NameFa` / `NameEn` columns):

- Name, ShortDescription, Description, SeoTitle, SeoDescription → `CatalogLocalizedText` (`OwnerKind=Product`)
- Human slug → `CatalogProduct.SlugSeam` (primary storefront slug; uniqueness enforced)

Public route strategy remains clean / locale-aware; do not append ProductId to visible slug.

## Category

- Searchable picker with path labels (`کالای دیجیتال > موبایل > …`)
- No raw CategoryId in UI labels
- Category change with attribute/variant data requires explicit confirmation after enriched impact preview (`MessageFa`)
- Attributes tab: [PRODUCT-ATTRIBUTES.md](./PRODUCT-ATTRIBUTES.md) — effective schema → typed values → readiness

## Media

See [PRODUCT-MEDIA.md](./PRODUCT-MEDIA.md). Catalog stores `MediaAssetId` assignments only; unassign does not delete shared assets; Media library deferred.
## Lifecycle

| Status | UI |
|--------|-----|
| Draft | پیش‌نویس |
| Published | منتشر شده |
| Archived | بایگانی |

Prefer **archive** over hard delete when offers/history reference the product.

## Variants

Source: category effective schema → `IsVariantAxis` attributes (shown read-only on ویژگی‌ها) → Product Variants matrix on **تنوع‌ها**.

See [PRODUCT-VARIANTS.md](./PRODUCT-VARIANTS.md). UI term: **تنوع**. No `ProductVariant.Price` / `ProductVariant.Stock`.

## Authorization

Host-authoritative Admin panel access + workspace scope header (`X-Tooba-Workspace-Scope: view`). Fine-grained SpiceDB product.* permissions remain the target model.

## AppDataGrid

Canonical path: `src/frontend/design-system/app-data-grid/AppDataGrid.tsx`

## USER_VISUAL_ACCEPTED

```text
NO
```
