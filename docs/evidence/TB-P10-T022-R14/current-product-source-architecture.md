# Current Product Source Architecture

## Registry

`StoreLandingPageSectionRegistry.ProductSources` = `Manual` | `Category` | `Brand` | `Newest`.

`UnsupportedProductSources` (rejected at validate): `BestSelling` | `Featured` | `Discounted`.

Section type for product rails: `ProductCollection` (and composition variants under ProductShowcase map to this host type).

## Persisted config shape (`ConfigurationJson`)

Common fields used by Host + Admin:

| Source | Key fields |
|---|---|
| `Manual` | `source`, `productIds[]`, optional `title`, `take` |
| `Category` | `source`, `categoryId`, `take`, `title` |
| `Brand` | `source`, `brandId`, `take`, `title` |
| `Newest` | `source`, `take`, `title` |

Validation: `StoreLandingPageSectionConfig` (Catalog domain). Max take 48; default 8.

## Admin UI

- `admin-landing-section-forms.tsx` → `ProductSourceForm`
- Labels via `source-capability.ts` / `strategyLabelFa` (انتخاب دستی / از یک دسته / از یک برند / جدیدترین کالاها)
- Capability from `registry.ts` `DS_PRODUCT` strategies
- Resource pickers: `AdminResourceSelector` + AppDataGrid (no raw GUID primary UX)

## Backend resolver

`StoreLandingPageComposer.ResolveProductItemsAsync`:

1. Parse `source` (default `Newest`) + clamped `take`
2. Base query: published `CatalogProduct`
3. Filter by Category / Brand / Manual productIds; Newest = no extra filter
4. `OrderByDescending(UpdatedAt).Take(take)` → `StoreLandingPageResolvedItem(ProductId, SlugSeam)`

**Not a generic pluggable source resolver abstraction** — switch/if on string source inside composer. Preview/public publish both go through composer resolve for ProductCollection.

## Storefront

`shared-composition-renderer.tsx` / landing blocks consume published section config + resolved items; Manual vs dynamic already branched for articles similarly.

## Locale / store / seller

- Page `Locale` on `StoreLandingPage` — section does not store independent locale for product source
- Tenant DB = store boundary (`tooba_alpha` etc.)
- Product resolve is Catalog-product scoped; does **not** join Offer/seller for source membership today
- Cache: page-level storefront caches exist from prior P10 work; product-source invalidation is page/composition scoped, not campaign-aware

## Empty / fallback

Empty Manual / missing category/brand → empty items list (storefront empty rails). Unsupported sources throw/validate-fail at write time.

## Gap for Amazing source

Need new allowed `ProductSources` value (recommend internal `PromotionCampaign`) + resolver branch joining future campaign membership → Offer → Product, without inventing parallel Catalog-only fake lists.
