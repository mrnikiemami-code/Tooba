# L1/L2 Subtree PLP

Storefront `GET /v1/storefront/category-plp/{slug}` resolves L1/L2 (200).
Demo Products remain Draft (Published=0), so PLP cards may be empty; subtree discovery verified via:

- PLP subcategories present
- Admin products with Primary in descendant L3 of the viewed L1/L2

Evidence: `live-proof.json` sections `l1Plp` / `l2Plp`. Facet semantics unchanged from T036 (viewed category owns facets).
