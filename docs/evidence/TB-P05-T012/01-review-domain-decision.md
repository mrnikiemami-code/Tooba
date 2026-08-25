# Review domain decision

## Ownership

```text
Owning module: Reviews
Capability kind: supporting commerce capability
Schema: reviews
Aggregate: ProductReview
Projection: ProductRatingAggregate
```

Reviews owns review text, integer rating, moderation lifecycle, verified
purchase snapshot, public review projection, and rebuildable Published-only
rating aggregate. Catalog must not gain rating columns or an ORM `Reviews`
collection.

This ownership follows:

- `docs/architecture/01-capability-domain-map.md`;
- `docs/architecture/03-data-ownership-and-module-contracts.md`;
- `docs/architecture/24-reviews-ratings.md`.

## Aggregate

`ProductReview` contains:

- opaque `ReviewId`;
- opaque Catalog `ProductId`;
- internal author `UserId` used only for ownership and duplicate policy;
- customer-safe display name snapshot;
- rating `1..5`;
- optional title and required body;
- `Pending`, `Published`, or `Rejected` status;
- truthful verified-purchase snapshot and optional opaque Order evidence;
- submission/update/moderation timestamps and moderator audit fields.

One actor may own one review per product. Database uniqueness is the concurrency
backstop; the application returns a clear duplicate conflict.

`ProductRatingAggregate` is derived solely from Published reviews and includes
average, count, and 1–5 distribution. Pending and Rejected rows never affect
public UI or SEO.

## External references and boundaries

```text
Reviews → ICatalogLookupGateway
  validates Published product/variant

Reviews → IOrderPurchaseVerificationGateway
  asks Order-owned application contract for paid line evidence

Host → Reviews public/application contracts
  composes storefront and Admin HTTP DTOs
```

Reviews never references Catalog/Order Infrastructure and never reads their
DbContexts or schemas. Order verification receives Catalog variant IDs resolved
by the consumer, so Order does not depend on Catalog.

Authenticated `CurrentAuthenticatedSession.UserId` is submission authority.
Request bodies cannot select an author. Admin moderation remains protected by
the existing server-side tenant authorization boundary.

## Verified purchase decision

Current Order can truthfully prove:

- actor owns the order through `PlacedByUserId`;
- line contains a supplied Catalog variant;
- seller order reached `Paid`.

It cannot prove delivery because Fulfillment is absent. Therefore:

```text
Paid + actor-owned + matching variant = verified purchase
Delivered badge semantics = DEFERRED
Gateway unavailable / guest mismatch / unpaid = false
```

Live checkout currently writes a fixed guest actor, so authenticated production
customers normally remain unverified until checkout identity is connected.
Development/test evidence may only show verified true when an actual paid,
actor-owned Order fixture exists.

## Deliberately deferred

- seller reviews;
- review media;
- helpful votes;
- comments/replies and seller replies;
- abuse scoring, CAPTCHA, ML/NLP moderation;
- advanced moderation workflows and notes;
- notifications and campaigns;
- delivered-purchase proof;
- external syndication and analytics.
