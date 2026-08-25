# TB-P05-T007 — Shopeiva PDP fidelity

The purchased Shopeiva PDP structure remains intact: breadcrumb, three-column desktop composition, gallery and thumbnails, product identity, information cards, seller/availability buy box, quantity control, primary cart CTA, other-seller seam, tabs, and responsive single-column mobile flow.

Bindings replaced or completed:

- identity, localized description, category and brand → Catalog;
- gallery assets → Catalog media references through the bounded storefront media seam;
- primary/alternate seller identity → Offer plus Party display lookup;
- displayed amount → active Pricing record selected by backend composition;
- availability → Inventory positions for each Offer;
- add-to-cart → existing guest Cart HTTP flow with `OfferId` line identity and authoritative refresh;
- quantity → bounded by composed availability while backend validation and customer-safe error mapping remain authoritative;
- related-product rail → live composed sellable cards, excluding the current Product and preferring its category;
- metadata and Product JSON-LD → server-rendered PDP SEO boundary.

No layout replacement, new cart model, frontend price/inventory authority, fixture product data, direct database access, or cross-module SQL join was introduced. The visible truncated Offer identifier was removed; internal identity remains transport-only where the existing Cart command requires `OfferId`.
