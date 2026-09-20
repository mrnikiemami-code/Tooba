# Client / server boundary audit — TB-TMAR-FE-BASELINE

Automated scan: **253** files with `"use client"`.
By ownership: app-other=38, admin=98, content=6, customer=16, ops=3, storefront=34, seller=26, design-system=25, lib=7.

Classification guide (applied to critical SEO routes below):

| Class | Meaning |
| --- | --- |
| REQUIRED_INTERACTIVE | Must be client (forms, carousels, cart qty, auth widgets) |
| CLIENT_BY_DEPENDENCY | Client because dependency is browser-only (Swiper, AG Grid, editors) |
| CLIENT_BY_PARENT_CONTAGION | Became client because a parent marked use client |
| LIKELY_SERVER_CANDIDATE | Mostly static/presentational; candidate to push server-ward later |
| NEEDS_REVIEW | Ambiguous; needs FE-F5 pass |

## Critical storefront routes

### Home — `app/page.tsx`
- **Server entry**: async Server Component; `generateMetadata` server-side; composition loaded via `composition-api` / store-page resolver.
- **Main content**: `StorefrontShopeivaHome` / landing sections composed from server-fetched data.
- **Client boundary**: interactive rails/stories/sliders as nested clients (CLIENT_BY_DEPENDENCY / REQUIRED_INTERACTIVE).
- **Indexable content**: primary home composition is server-rendered — GOOD.
- Contagion risk: NEEDS_REVIEW if large presentational trees sit under client parents.

### Category PLP — `app/category/[slug]/page.tsx`
- Server route composes storefront PLP modules; listing guards exist (`test:category-plp-guard`).
- Facets/filters: REQUIRED_INTERACTIVE.
- Product cards/text: must remain SSR-capable (FE-SEO-001).

### PDP — `app/products/...` / slug product routes under `app/[slug]` / products tree
- PDP structure guards (`test:pdp-guard`).
- Variant/ATC widgets: REQUIRED_INTERACTIVE.
- Title/description/offers for indexability: must not be client-only fetch.

### Search / listing rails (`best-seller`, `sale`, `trending`, …)
- Mixed; treat textual listing payloads as server-preferred.

### Landing — `app/landing/[slug]/page.tsx`
- Composition-driven; server metadata helpers present in storefront SEO module.

### Campaign / amazing
- Admin campaigns under `app/admin/campaigns` (operator UI — client OK).
- Storefront campaign merchandising must keep product identity SSR-safe.

### Articles — `app/blogs/[slug]/page.tsx`
- Content pages; rich HTML must remain SSR/indexable; editors are admin-only clients.

### Cart / shipping
- Cart page: server shell + client cart (index:false). Checkout redirects to shipping.

## Machine-readable

`client-server-boundaries.json` — file list only; human classes in this document.

**No conversions in this task.**
