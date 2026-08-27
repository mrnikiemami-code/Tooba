# 09 — Reviews capability (TB-P06-T020)

Date: 2026-08-27  
Owner: existing `Tooba.Reviews` (EXTENDED — no new module)

## Domain / contracts audited

| Artifact | Path | Finding |
|---|---|---|
| Entity | `ProductReview` | Pending / Published / Rejected; verified purchase snapshot; **no seller reply fields** |
| Application | `IReviewDirectory` | Submit, published page/summaries, home featured, admin pending, publish/reject |
| Host | `ReviewEndpoints.cs` | Storefront GET, customer POST, admin list/publish/reject |
| Pre-wave seller | — | **No** `/v1/seller/reviews*` |

## Seller response / reply

**NOT SUPPORTED.** Domain has no seller response body, reply timestamp, or moderation-by-seller. Wave 2 intentionally **skips** POST reply/response. Host response flags `SellerResponseSupported: false`. Seller UI is read-only list (no fake approve/reject/delete).

## Seller-scoped list (implemented)

| Piece | Behavior |
|---|---|
| HTTP | `GET /v1/seller/reviews?status=&page=&pageSize=` |
| Auth | `SellerPanelAccess.RequireAuthorizedAsync` (SpiceDB party#view) |
| Ownership | Host `SellerPanelComposer.ListOwnedProductIdsAsync` — Offer rows for seller → Catalog variant → ProductId (separate lookups; **no cross-module SQL JOIN**) |
| Directory | `IReviewDirectory.ListForProductsAsync(productIds, statusFilter, page, pageSize)` |
| Isolation | Foreign actor→seller header → 403; reviews filtered to owned ProductIds only |
| Admin | Unchanged: `/v1/admin/reviews`, publish, reject remain live |

## Tests

- Contract: seller list route present; no seller moderation/response routes; DTO privacy
- Postgres: own productIds include; foreign ProductId excluded; empty product set → empty page
- Auth: foreign seller party header denied (`seller.authorization.denied`)

## Verdict

| Capability | Status |
|---|---|
| Seller list own product reviews | **LIVE** |
| Seller reply | **DEFERRED_WITH_REASON** (domain gap) |
| Admin moderation | **LIVE** (pre-existing) |
| Customer submit / storefront published | **LIVE** (unchanged) |
