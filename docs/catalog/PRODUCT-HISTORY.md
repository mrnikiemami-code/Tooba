# Product History

> **Task:** TB-P07-T019 · **Phase:** P07 Advanced Catalog  
> **Depends on:** [PRODUCT-CATALOG-ADMIN.md](./PRODUCT-CATALOG-ADMIN.md), [PRODUCT-PUBLISHING.md](./PRODUCT-PUBLISHING.md)

## Ownership

Catalog owns **Product Master history** (append-only audit for Admin Workspace تب تاریخچه).

| Owns | Does not own |
|------|----------------|
| Product identity / lifecycle / category / attributes / variants / media / SEO edits | Offer history |
| Actor display name at write time | Seller commercial audit |
| Compact before/after summaries | Pricing / Inventory joins |

**Product ≠ Offer.** History queries never JOIN Offer, Pricing, Inventory, or Seller tables.

## Persistence

- Table: `catalog.product_history_entries`
- Entity: `CatalogProductHistoryEntry`
- Application: `ICatalogDirectory.ListProductHistoryAsync` / `AppendProductHistoryAsync`
- Host API: `GET /v1/admin/products/{id}/history?skip=&take=&section=`
- **Append-only** from normal Admin flows — no edit/delete history endpoints

## Actor policy

- `ICatalogActorContext` is bound per request via Host endpoint filter (`CatalogActorHttpBinding`)
- Prefer OperatorProfile `DisplayName`
- Fallback: `اپراتور` (authenticated) or `سیستم` (no actor)
- UI must not show SpiceDB subject syntax or opaque user IDs as primary labels

## Event coverage

| Event | Section | SummaryFa (example) |
|-------|---------|---------------------|
| product.created | general | محصول ایجاد شد |
| product.general.changed | general | اطلاعات اصلی محصول ویرایش شد |
| product.localized.changed | general | محتوای محلی محصول ویرایش شد |
| product.category.changed | category | دسته‌بندی محصول تغییر کرد |
| product.attributes.changed | attributes | ویژگی‌های محصول ویرایش شد |
| product.variants.changed | variants | تنوع‌های محصول به‌روزرسانی شد |
| product.media.changed | media | رسانهٔ محصول به‌روزرسانی شد / تصویر اصلی تغییر کرد |
| product.seo.changed | seo | اطلاعات سئو ویرایش شد |
| product.published | lifecycle | محصول منتشر شد |
| product.unpublished | lifecycle | محصول از انتشار خارج شد |
| product.archived | lifecycle | محصول بایگانی شد |
| product.restored | lifecycle | محصول از بایگانی خارج شد |

Publish and Restore remain **distinct** events. Archived → Published direct remains forbidden (see publishing docs).

## Before / after

Optional compact strings (≤512 chars), e.g. lifecycle labels, slug/title diffs. No full aggregate snapshots or request payloads.

## UX

- Product Workspace tab **تاریخچه**: timeline/list, newest first
- Shows Persian summary, actor, timestamp, section label, optional before→after
- Loading / empty / error; section filter; server paging
- VIEW-only — no mutation of audit rows
- Prefer timeline over grid; if a grid is ever needed, only canonical `AppDataGrid`

## Retention

No automatic pruning in application code. Retention / archival of old history rows is an **operational** concern (DB maintenance), not Admin UX.

## Related

- [PRODUCT-CATALOG-ADMIN.md](./PRODUCT-CATALOG-ADMIN.md)
- [PRODUCT-PUBLISHING.md](./PRODUCT-PUBLISHING.md)
