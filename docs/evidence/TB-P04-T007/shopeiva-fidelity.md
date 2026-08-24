# TB-P04-T007 — Shopeiva fidelity

## Home

- Shopeiva source: `/` header + search + category chips + hero + product card grid + footer/trust
- Tooba target: `/`
- Preserved: RTL header/search/category strip, hero block, 4-column cards, footer columns, sticky header
- Minimal changes: blue token instead of template red; live Catalog/Offer cards; search submits to `/products`
- Business data: product title, category, seller, amount, availability from `/v1/storefront/home`
- Deviation: no Fuse client catalog; no demo cart drawer; no CMS magazine/blog modules

## Listing

- Shopeiva source: category/search listing + product cards
- Tooba target: `/products`
- Preserved: card family, category chips, search field
- Minimal changes: Host query `q` / `categoryId`; no facet rebuild
- Business data: `/v1/storefront/products`
- Deviation: filters beyond category/search deferred

## PDP

- Shopeiva source: `/product/[id]/[name]` gallery + buy box + seller seam
- Tooba target: `/products/{slug}`
- Preserved: gallery + identity + price block + availability + add-to-cart chrome + other sellers
- Minimal changes: quantity/cart mutation not live; review stars not live; variant UI uses the resolved Catalog variant of the primary Offer
- Business data: `/v1/storefront/products/{slug}`
- Deviation: presentation SVG instead of binary media pipeline; JSON-LD uses tax-exclusive authored amount

## Header/footer

- Preserved Shopeiva composition (logo, search, utility, category nav, dark footer)
- Search is a real listing seam, not Fuse demo JSON
