# Product Media

> **Task:** TB-P07-T014 · **Phase:** P07 Advanced Catalog
> **Depends on:** Catalog product media references, storefront media placeholder URL

## Assignment model

Catalog owns **product ↔ media assignment** only:

| Field | Meaning |
|-------|---------|
| `ProductId` | Product owning the assignment |
| `MediaAssetId` | Opaque reference to a Media asset (no binary in Catalog) |
| `DisplayOrder` | Deterministic gallery order |
| `IsPrimary` | Exactly one primary when media count &gt; 0 |
| `AltText` | Product-contextual alt on the assignment |

Table: `catalog.product_media_references` (`CatalogProductMediaReference`).

Catalog does **not** store image bytes, paths, or upload blobs.

## Primary rules

- First attached media becomes primary automatically.
- Admin may set another assigned asset as primary (`تصویر اصلی`).
- Backend enforces uniqueness: at most / exactly one `IsPrimary` when count &gt; 0.
- Detaching the primary promotes the next row by `DisplayOrder` (then `ReferenceId`).

## Unassign ≠ delete

Removing media from a Product deletes the **assignment row only**.

Shared MediaAsset lifecycle remains a future Media-module concern. There is no Catalog hard-delete of binary assets (none are stored here).

## Readiness

`ProductMediaReadiness`:

- `HasPrimaryImage`
- `MediaCount`
- `IsReady` = count &gt; 0 and has primary
- `MessageFa` — e.g. «تصویر اصلی تعیین نشده» / «رسانه کامل است»

UI also shows «N رسانه». Publishing (T016) may consume readiness; this task does not invent publish rules.

## Admin UX

Product Workspace tab **رسانه**:

- **VIEW:** main image, ordered thumbs, count, readiness — no mutation controls
- **EDIT:** «افزودن تصویر نمایشی» (server mints opaque `MediaAssetId` + attach), reorder up/down, set primary, patch alt, unassign
- Guid paste is **not** primary UX; optional advanced disclosure only, with deferred Media-library warning
- No raw `AgGridReact`; mobile-friendly stacked cards

## Shopeiva / PLP–PDP handoff

Ordered gallery + primary support future storefront:

- PLP / list thumbnails → primary `MediaAssetId`
- PDP gallery → ordered assignments + alt
- Preview today: `GET /v1/storefront/media/{assetId}` SVG placeholder via `storefrontMediaUrl()`

Do not redesign PDP in T014.

## Variant media

**Deferred.** Current model is Product-level only. Extension point: optional `VariantId` on assignment when a later task adds a safe schema; do not force fragile Variant FK here.

## Media library / upload

**Deferred architectural concern.** There is no Media DAM module in-repo yet. Placeholder mint exists so Admin can exercise gallery UX without inventing a second upload/storage subsystem. Real upload/select («انتخاب از رسانه‌ها» / «بارگذاری رسانه») lands with the Media pipeline (`docs/architecture/15-media-image-pipeline.md`).

## APIs

| Method | Path |
|--------|------|
| GET | `/v1/admin/products/{id}/media` |
| GET | `/v1/admin/products/{id}/media/readiness` |
| POST | `/v1/admin/products/{id}/media` |
| POST | `/v1/admin/products/{id}/media/placeholder` |
| PUT | `/v1/admin/products/{id}/media/order` |
| PUT | `/v1/admin/products/{id}/media/{assetId}/primary` |
| PATCH | `/v1/admin/products/{id}/media/{assetId}` |
| DELETE | `/v1/admin/products/{id}/media/{assetId}` |

CatalogDirectory owns mutations; Host thin-wraps.

## Boundaries

- Product ≠ Offer; no `Product.Price` / `Product.Stock`
- No Pricing/Inventory joins for media
- No second binary storage subsystem
