# TB-P05-T013 — Shopeiva wishlist/backend convergence

## Initial backend portion

- Shopeiva feature: customer wishlist list, remove action, product navigation, live card presentation, empty state, and existing heart controls.
- Backend before: customer panel exposed an honest `WishlistAvailable: false` shell and had no owning persistence capability.
- Backend after: dedicated Wishlist module provides private idempotent add, safe remove, own list, batched membership, count, schema/migration/Outbox, and Development seed.
- Binding path: `/v1/customer/wishlist` derives the actor server-side, reads Wishlist references, then `WishlistComposer` asks `StorefrontComposer.ComposeProductCardsAsync` only for requested Product IDs.
- Live truth: Catalog publication, active Offer, active Pricing, Inventory availability, Promotion and published Reviews remain in their owning boundaries.
- Unavailable behavior: saved unpublished or uncomposable references are retained and returned with no card and `product-unavailable`; purchasability is never invented.
- Dashboard: Wishlist capability is enabled and its count comes from the owning module.
- Minimal UI additions: the existing Shopeiva hearts on PDP and product cards now consume one shared Wishlist provider; the customer Wishlist page reads the private live list, displays the empty state, and removes through the same backend contract.
- Deferred: public/shareable lists, collaboration, named lists, alerts, registry, recommendations, analytics, comparison, and move-to-cart UI.

No cross-module SQL or foreign-module DbContext navigation is used by Wishlist. Host composition is the explicit in-process convergence boundary.

## Completed live convergence

- `POST /v1/customer/wishlist/{productId}` drives idempotent add; repeated live calls returned `201` then `200`.
- `DELETE /v1/customer/wishlist/{productId}` is safe; repeated live calls both returned `204`.
- `POST /v1/customer/wishlist/membership` accepts `{ productIds }` and supplies batched heart state to listing and PDP UI.
- A seeded actor retained three rows while a separate actor moved independently from zero to one row.
- Normal browser captures use the real Next rewrite to the normal Development Host; no response interception, static replacement, disabled web security or screenshot editing was used.
- `03`–`09` show desktop list, empty list, PDP toggle, listing-card state, live price/availability/rating, remove result and the exact `390x844` mobile viewport.
