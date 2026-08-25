# TB-P05-T013 — Wishlist domain decision

## Ownership

Wishlist is a dedicated module because it owns a customer's private product-saving intent. It is not part of Product or Cart. Its only entity is `WishlistItem`, identified by `WishlistItemId`, and carrying server-authoritative `OwnerUserId`, opaque `ProductId`, and `CreatedAt`.

## Invariants and identity

`(OwnerUserId, ProductId)` is unique. Add is idempotent and remove of an absent row is safe. The Host supplies owner identity from `CurrentAuthenticatedSession`; Development/Testing may use the existing controlled customer actor seam. Requests contain no owner ID and production without an authenticated actor returns 401.

## Boundaries

Product validity is checked through `ICatalogLookupGateway`; Wishlist has no Catalog foreign key, navigation, SQL join, or DbContext dependency. Price, stock, Offer and rating are never snapshotted. Host composes current presentation from Storefront boundaries. An unpublished or currently uncomposable product remains a saved intent and is returned with `product-unavailable` and no purchasable card.

The module owns schema `wishlist`, its migration and Outbox table. This supports later service extraction with User and Product remaining opaque references.

## Deferred

Public sharing, collaboration, named lists, notifications, gift registries, recommendations, analytics and comparison are deferred.

## Live verification

Development startup applies the Wishlist migration and deterministic seed through `ProductWorkspaceDevelopmentBootstrap`, after the tenant commerce context is assigned. On 2026-08-25 the normal Host returned three saved product references for the deterministic customer actor. A separate actor began with zero items, could add one item without changing the seeded actor's count, and returned to zero after removal.

The browser evidence in `03` through `09` confirms that the private references are composed into current Storefront cards only at read time. The API proof in `10` confirms unique owner/product behavior, idempotent add, safe remove, membership batching and actor isolation.
