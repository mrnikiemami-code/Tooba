# 20 — Integration tests (TB-P06-T020)

Date: 2026-08-27  
Map of Task minimum cases → evidence / test coverage.

## Promotions (Task T cases 1–10)

| # | Requirement | Coverage |
|---|---|---|
| 1 | Create own | `PromotionPanelTests` create for seller A |
| 2 | Foreign scope reject | Foreign list empty / get null / activate+update throw |
| 3 | Code validation | Coupon normalizer + evaluate with `SUMMER15` / `summer15` |
| 4 | Date validation | Domain effectiveFrom/To on create (directory) |
| 5 | Discount safety | Percentage eval 15% of 100000 → 15000; expired → 0 |
| 6 | Checkout apply | Order evaluator path + Host composer passes `couponCode` (source assert) |
| 7 | Duplicate / idempotent | Existing Order quote-lock / foundation (pre-wave); panel test re-eval after expire |
| 8 | Order snapshot | Pre-existing Order promotion snapshots (evidence `06`) |
| 9 | Seller UI API | Host `/v1/seller/promotions*` registered + FE `seller-api` |
| 10 | Admin oversight | Admin list/filter/deactivate in panel test + Host admin routes |

## Reviews (Task T cases 11–16)

| # | Requirement | Coverage |
|---|---|---|
| 11 | Seller own list | Postgres `Seller_scoped_list_includes_own_products_and_excludes_foreign` |
| 12 | Foreign seller denied | `Foreign_seller_party_header_is_denied_by_seller_panel_access` |
| 13 | Moderation rules | Seller cannot moderate; admin publish/reject only |
| 14 | Seller response | Explicitly **not supported** — contract asserts `SellerResponseSupported: false` |
| 15 | Admin moderation | Existing admin endpoints + UI |
| 16 | UI APIs | `GET /v1/seller/reviews`; FE `loadSellerReviews` |

## Notifications (Task T cases 17–21)

**Skipped** — DEFERRED_WITH_REASON (`12-notification-decision.md`). No inbox APIs.

## Frontend integrity

| Suite | Role |
|---|---|
| `vendor-panel/panel-nav-integrity.test.ts` | coupons + reviews LIVE; tickets deferred |
| `customer-panel/panel-nav-integrity.test.ts` | notifications remain deferred |
| `vendor-panel/seller-api.test.ts` | seller API client mapping (promotions/reviews as updated) |

## Worker fill

Exact `dotnet test` / `npm test` totals → `21-final-validation.md` (placeholders).
