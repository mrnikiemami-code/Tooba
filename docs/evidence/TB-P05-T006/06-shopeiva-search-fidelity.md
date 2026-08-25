# TB-P05-T006 — Shopeiva search fidelity

- Header, mobile drawer, category navigation, filter sidebar, listing grid, cards, and public `/products` route remain the Shopeiva-derived storefront structure.
- Header search submits `q` to the public listing route; product matching is performed by the Host over composed live cards, not by browser-side catalog filtering.
- Search covers the live localized product title, category name, and seller display name.
- Category discovery uses only published Catalog hierarchy. Category landing includes descendants.
- Availability is derived from Inventory positions on the selected Offer. Seller facets are emitted only from composed active Offers. Unsupported brand and price-range facets remain hidden.
- Sorting and pagination are backend-owned. Price ordering uses the backend-composed promotional amount when applicable, otherwise the active Pricing amount.
- Cards preserve `Product != Offer != Price != Inventory`: identity comes from Catalog, seller and listing identity from Offer/Party, amount from Pricing/Promotion, and availability from Inventory.
- Empty search/filter results and Host failures are distinct Persian states and do not fall back to fixture data.
- `/products` and category/page listings remain indexable with self-referencing canonical URLs. Search, seller, availability, and non-default sort combinations are `noindex,follow` and canonicalize to the stable public listing/category boundary.
