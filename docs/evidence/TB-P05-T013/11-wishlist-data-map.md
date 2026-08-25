# TB-P05-T013 — Wishlist data map

## Owned persistence

Wishlist owns only schema `wishlist`:

- `wishlist_items`: `WishlistItemId`, server-derived `OwnerUserId`, opaque `ProductId`, `CreatedAt`.
- Unique index: `(OwnerUserId, ProductId)`.
- Read index: `(OwnerUserId, CreatedAt)`.
- Module-local Outbox table.

There is no stored title, slug, media, category, seller, Offer, price, promotion, stock, rating, Catalog foreign key or foreign-module navigation.

## Write path

1. Host resolves the current actor from `CurrentAuthenticatedSession`; only Development/Testing permits the controlled actor header.
2. Add receives only a product ID in the route.
3. `IWishlistDirectory` verifies Published product validity through the Catalog application gateway.
4. Wishlist inserts the private reference if `(OwnerUserId, ProductId)` does not already exist.
5. Remove scopes by the resolved actor and product; an absent row is a successful no-op.

## Read and composition path

1. `IWishlistDirectory.ListAsync(actorUserId)` returns only the actor's owned references.
2. `WishlistComposer` batches those product IDs into `StorefrontComposer.ComposeProductCardsAsync`.
3. Host obtains current Catalog publication and descriptive projection, active Offer, Pricing, Promotion, Inventory and Published Reviews through their established boundaries.
4. Host joins results in memory by opaque product ID.
5. A reference that cannot currently compose remains saved and is returned with `product-unavailable`; purchasability is not invented.

## Live response example

The seeded linen shirt reference composed at runtime to:

- title `پیراهن مردانه لینن`;
- seller `دیجی‌استایل نمونه`;
- current amount `1,790,000 IRR`;
- `availableUnits: 4`, `inStock: true`;
- `averageRating: 4`, `reviewCount: 3`.

These fields were returned by live Storefront composition and rendered in `03`, `05`, `06`, and `07`; none are columns in Wishlist persistence.
