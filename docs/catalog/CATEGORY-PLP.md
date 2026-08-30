# Category PLP

> **Task:** TB-P07-T010 · **Phase:** P07 Advanced Catalog  
> **Depends on:** [CATEGORY-FACETS.md](./CATEGORY-FACETS.md), [CATEGORY-MEGA-MENU.md](./CATEGORY-MEGA-MENU.md)

## Route

Canonical storefront Category PLP:

```text
/{locale}/category/{localizedSlug}
```

- Resolve via Catalog `ResolveCategoryRouteAsync` (published + visible for storefront)
- Stale slug → redirect to current slug path (`IsRedirect` / `RedirectToPath`)
- Locale segments: `fa` / `en` (UI); Catalog locale `fa-IR` / `en`

## Composition (Host)

`GET /v1/storefront/category-plp/{slug}?locale=&sort=&page=&pageSize=`

Typed filter query params:

| Prefix | Meaning | Example |
|--------|---------|---------|
| `f_` | enum / multi-select | `f_color=blue,red` |
| `f_brand` | global Brand facet (`Product.BrandId`) | `f_brand=aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa` |
| `r_` | numeric range | `r_ram=8:16` |
| `b_` | boolean | `b_waterproof=true` |

Semantics:

- Product set = masters assigned to category **or taxonomy descendants** (`ParentCategoryId`)
- Cross-attribute filters: **AND**
- Same multi-select attribute values: **OR**
- Facets from effective visible category facet config (T008) **plus** global Brand facet (`code=brand`)
- Brand options come only from brands among discoverable products; no fake «بدون برند» entity; brandless products remain when no Brand filter is selected and are excluded when a Brand is selected
- Facets are **never** unioned from included products' Primary Category facet configs
- Commercial card fields from Offer / Pricing / Inventory contracts — **no** `Product.Price`, **no** cross-module SQL JOIN

Response highlights: breadcrumb, subcategories, facets + counts, applied filter chips, paged product cards, canonical path, supported sorts.

## Frontend

- Public prefix: `/category` in `PUBLIC_STOREFRONT_PREFIXES`
- Page: `app/category/[slug]/page.tsx`
- UI: `storefront-category-plp.tsx` (sidebar / mobile drawer, chips, sort, grid via existing product card)
- SEO: clean category path is canonical; filtered / non-default sort → `robots: noindex,follow`
- Mega Menu destinations (`/{locale}/category/{slug}`) resolve to this PLP; `LocalizedLink` accepts already-prefixed hrefs

## Hard locks

- Do not redesign `AppCategoryTree`
- Storefront PLP is not Admin AgGrid
- Product ≠ Offer

## USER_VISUAL_ACCEPTED

```text
NO
```
