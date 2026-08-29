# Product Publishing

> **Task:** TB-P07-T018 / TB-P07-T018-R1 · **Phase:** P07 Advanced Catalog
> **Depends on:** [PRODUCT-CATALOG-ADMIN.md](./PRODUCT-CATALOG-ADMIN.md), component readiness from Attributes / Variants / Media / SEO

## Ownership

Catalog owns Product Master **publish lifecycle** and **aggregate publish readiness**.

Product publish readiness answers:

> آیا هویت محصول برای نمایش عمومی Catalog آماده است؟

It does **not** answer sellability. Offer / Price / Stock / Inventory / Promotion belong to commercial readiness and stay outside this gate.

**Product ≠ Offer.** No `Product.Price` / `Product.Stock`. No Pricing/Inventory SQL JOIN for publish.

## Lifecycle

Canonical statuses (`CatalogPublicationStatus`):

| Status | Persian | Notes |
|--------|---------|--------|
| Draft | پیش‌نویس | Default for new Product |
| Published | منتشرشده | Eligible for public storefront Product routes |
| Archived | بایگانی‌شده | Soft archive; data preserved; not normal public index |

Transitions:

| Action | From → To | Notes |
|--------|-----------|--------|
| Publish | **Draft → Published only** | Backend enforces aggregate readiness; idempotent if already Published. **Archived → Published is forbidden.** |
| Unpublish | Published → Draft | Explicit; Archive cannot unpublish |
| Archive | Draft/Published → Archived | Soft; no hard delete; preserves translations/attributes/variants/media/SEO |
| Restore | Archived → Draft | **Only** explicit exit from Archive; Offer tables are not mutated |

To republish an archived product: **Archived → Restore → Draft → Publish** (two explicit steps). Hard delete remains the separate safe-delete path (soft-archive when Offer references exist).

## Aggregate readiness

Type: `ProductPublishReadiness`

| Flag | Source (reused, not duplicated) |
|------|----------------------------------|
| `CategoryReady` | Level-3 assignable primary category |
| `TranslationReady` | Localized product identity (name) |
| `AttributeReady` | `ProductAttributeReadiness.IsComplete` |
| `VariantReady` | `ProductVariantReadiness.IsValid` |
| `MediaReady` | `ProductMediaReadiness.IsReady` |
| `SeoReady` | `ProductSeoReadiness.IsReady` (locale, default `fa-IR`) |
| `IsReady` | All of the above |
| `MissingRequirements[]` | Code + `MessageFa` + Workspace tab target |
| `MessageFa` | Ready text or «برای انتشار، N مورد دیگر باید تکمیل شود.» |

**Explicitly excluded:** Seller Offer, Price, Stock, Inventory, Promotion.

## APIs

| Method | Path |
|--------|------|
| GET | `/v1/admin/products/{id}/publish/readiness?locale=` |
| POST | `/v1/admin/products/{id}/publish` |
| POST | `/v1/admin/products/{id}/unpublish` |
| POST | `/v1/admin/products/{id}/archive` |
| POST | `/v1/admin/products/{id}/restore` |

Publish rejection returns human-readable missing requirements; partial publish is forbidden. Frontend button state is not authoritative.

## Admin UX

Product Workspace tab **انتشار**:

- **VIEW:** lifecycle label, readiness checklist, commercial Offer summary (readonly), no mutation controls
- **EDIT/action:** publish (not while Archived) / unpublish / archive / **خروج از بایگانی** (Restore) / safe-delete with confirmations
- While **Archived**, Publish is hidden; Restore is the only exit to Draft
- Missing checklist items navigate to the relevant Workspace tab (`general` / `attributes` / `variants` / `media` / `seo`)
- Product list AppDataGrid keeps existing status column + Persian chips (no raw `AgGridReact`)

## Public storefront

- **Published:** eligible for `/{locale}/products/{slug}` (existing StorefrontComposer filter)
- **Draft / Archived:** not treated as normal indexable public Products (existing Published-only queries)

## Authorization

Host-authoritative via Admin panel access + workspace `CanPublish` / `X-Tooba-Workspace-Scope: view`. SpiceDB-centered permission id `product.publish` remains the catalog target. VIEW-only users see state/readiness but cannot mutate.

## Audit hooks (for T019)

Reuse existing domain events / outbox:

- `CatalogProductPublishedDomainEvent` → `catalog.product_published.v1`
- `CatalogProductUpdatedDomainEvent` on unpublish / archive / restore

Timestamps: `CatalogProduct.UpdatedAt` on transitions. Do not invent a parallel audit subsystem here; History UI remains deferred.

## Architectural notes

- Global `SlugSeam` remains the public slug (no per-locale product slug invented here)
- Offer lifecycle coordination on archive is deferred (no Offer table queries from Catalog publish)
