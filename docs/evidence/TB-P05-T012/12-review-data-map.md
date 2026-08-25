# Reviews and ratings data map

## Write path

```text
PDP review form
→ POST /v1/customer/reviews
→ ReviewEndpoints.ResolveActor
→ IReviewDirectory.SubmitAsync
→ Catalog ICatalogLookupGateway validates the published product
→ Order IOrderPurchaseVerificationGateway checks paid actor-owned matching lines
→ ProductReview.Create (Pending)
→ reviews.product_reviews
```

The actor comes from `CurrentAuthenticatedSession.UserId`, with the explicitly
Development-only actor header seam as fallback. No request field can choose the
author. `ProductReview` validates integer rating 1–5, content, transitions, and
one-review ownership; the database unique index is the concurrency backstop.

## Public read and aggregate path

```text
reviews.product_reviews (Status = Published only)
→ ReviewDirectory.GetPublishedAsync / rating projection query
→ IReviewDirectory public contracts
→ ReviewEndpoints safe DTO
→ StorefrontComposer batched aggregate enrichment
→ PDP summary/list + product cards + Product JSON-LD
```

Average, count, and the 1–5 distribution are calculated in Reviews
Infrastructure from Published rows. Storefront presentation calculates only
bar percentages from those server counts. Pending/Rejected text and internal
author IDs never enter the public DTO.

## Moderation path

```text
Admin Reviews route
→ Development actor or authenticated session
→ AdminPanelAccess.RequireAuthorizedAsync
→ tenant#view authorization decision
→ GET /v1/admin/reviews (Pending)
→ POST /publish or /reject
→ ProductReview.Publish / Reject with moderator audit
→ public aggregate changes only after Published
```

`08-admin-review-denied.png` uses a real non-member actor ID and demonstrates
the server-authorized fail-closed state; the browser does not grant permission.

## Module boundaries

- Reviews owns `ProductReview`, review persistence, moderation, and aggregates.
- Catalog supplies product/variant truth through `ICatalogLookupGateway`.
- Order owns purchase proof through `IOrderPurchaseVerificationGateway`.
- Host composes HTTP and storefront DTOs; it does not perform cross-module SQL.
- Catalog has no `Reviews` ORM collection and no review/rating authority column.
- Reviews does not reference Catalog or Order Infrastructure or their
  DbContexts.

Primary implementation files:

- `src/backend/Modules/Reviews/Tooba.Reviews.Domain/ProductReview.cs`
- `src/backend/Modules/Reviews/Tooba.Reviews.Application/ReviewContracts.cs`
- `src/backend/Modules/Reviews/Tooba.Reviews.Infrastructure/ReviewDirectory.cs`
- `src/backend/Modules/Reviews/Tooba.Reviews.Infrastructure/Persistence/ReviewsDbContext.cs`
- `src/backend/Modules/Order/Tooba.Order.Infrastructure/OrderPurchaseVerificationGateway.cs`
- `src/backend/Host/Tooba.Host/Reviews/ReviewEndpoints.cs`
- `src/backend/Host/Tooba.Host/Storefront/StorefrontComposer.cs`
- `src/frontend/app/storefront/storefront-pdp-reviews.tsx`
- `src/frontend/app/admin/admin-screens.tsx`
