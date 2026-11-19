# Remaining-scope closure (TB-281-R2)

## Closed in this worker pass

- Media seller upload no longer uses UploadAsync Ready-then-bind; atomic Ready+binding+occurrence after object store.
- Media seller routes thin ISender CQRS; admin-for-shop upload route added with Active Party gate.
- StableStoryTenantGuid BuildingBlocks seam retained for Story/Media tenant mapping.
- Seller stop → Disabled/Approved still projects `return_to_draft`; stop→fresh read→return exercised.
- Seller create/add occurrence + stop/return occurrence-before-version.
- Cover bind on seller create/update and admin-for-shop create.
- Product CTA ownership via Catalog storefront cards (fail-closed when port unavailable).
- Seller server query with page/count/search/filter for owning shop.
- Admin grid shop human-name filter via complete Party projections of Story shop ids.
- Public cards batch shop DisplayName; do not invent ShopPublicId from Party Guid.
- Host composition-only Development bootstrap opt-out env `TOOBA_SKIP_DEVELOPMENT_BOOTSTRAP=1`.
- FE transport: stop/return occurrenceKey + `querySellerStories`.

## Still open (forces PARTIAL)

1. Dedicated MediaDirectory recording object-store failure/conflict suite (rows/object call assertions).
2. Real PostgreSQL rollback-only G matrix (public boundaries, uniqueness, shop-filter counts, Media constraints).
3. Full all-route occurrence/version matrix for admin composites, submit, item remove/reorder, approve/reject.
4. Canonical public shop id/link when Party/public identity exists (currently name-only, id null).
5. Complete ui-contract-handoff exhaustive samples for every error/blocker.

## Why work stopped where it did

Authorized independent implementation for seller query, shop-name filter, CTA, CQRS, stop cycle, and occurrence on the highest-risk routes was completed and proven with focused InMemory Directory + AMSC pin updates. The remaining gaps are the original G1–G7 relational/recording proof surface and the leftover all-route occurrence matrix — not an external blocker. No live Story/Media business write was performed.
