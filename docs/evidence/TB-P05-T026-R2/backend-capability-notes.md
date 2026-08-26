# Backend Capability Decisions — TB-P05-T026-R2

## Brand logo

- Added optional `CatalogBrand.LogoMediaAssetId` and `StorefrontBrandItem.LogoMediaAssetId`.
- Development seed assigns deterministic placeholder media IDs to published brands when missing.
- Frontend uses `storefrontMediaUrl(logoMediaAssetId)` with Host media fallback.

## Featured reviews (home)

- Extended `IReviewDirectory` with `GetRecentPublishedForHomeAsync`.
- Extended `ICatalogLookupGateway` with `GetReviewableProductsByIdsAsync` for product title/slug binding.
- `StorefrontHomePage.FeaturedReviews` exposes live published reviews only; `VerifiedPurchase` from Reviews module truth.

## Content (home articles)

- New minimal `Content` module (`content.articles`) with published-only read path via `IContentDirectory.ListPublishedForHomeAsync`.
- Fields: slug, title, excerpt, cover media, publish date, author display, tags, featured flag.
- `ContentDevelopmentSeed` inserts deterministic Persian articles in Development only.
- No CMS/editor workflow in this task.

## Fake data policy

- No fabricated ratings, discounts, reviews, or articles in production composition paths.
- Helpful/like/view counts omitted where backend has no capability (documented as minor deviation).
