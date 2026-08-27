# Evidence — TB-P06-T020

**Commercial Panel Completion Wave 2 — Seller Coupons/Reviews + Honest Notifications Decision**

| Field | Value |
|---|---|
| Task-ID | `TB-P06-T020` |
| Phase | P06 |
| Channel | `tooba-main` |
| Predecessor | `4c839098d3896c76feefecb878cbace5a2d336dd` |
| Commit message target | `feat close commercial panel gaps wave 2 [TB-P06-T020]` |
| Architect status (SoT) | `AWAITING_ARCHITECT_ACCEPT` |
| May report readiness | `COMMERCIAL_PANEL_WAVE2_LIVE` (NOT `PRODUCT_FULLY_READY`) |

## Prior Architect ledger (corrected)

| Task | Status |
|---|---|
| TB-P06-T018 | ACCEPTED |
| TB-P06-T019 | SUPERSEDED_BY_ARCHITECT_RESCOPE |
| TB-P06-T019-R1 | ACCEPTED |

## Capability flags

```text
SELLER_PROMOTIONS = LIVE
SELLER_REVIEWS = LIVE
ADMIN_PROMOTIONS = LIVE
ADMIN_REVIEWS = LIVE
NOTIFICATIONS = DEFERRED_WITH_REASON
PANEL_NAVIGATION = HONEST_LIVE_ONLY
VISUAL_CONTRACT = SHOPEIVA_LOCKED
NEW_UI_RULE = SOURCE_DERIVED_NATIVE_FIT
```

## Files

| # | File | Topic |
|---|---|---|
| 01 | `01-runtime-before-wave2.md` | Pre-work triad health |
| 02 | `02-shopeiva-wave2-source-map.md` | Exact Shopeiva coupons/reviews/notifications sources |
| 03 | `03-current-backend-capability-audit.md` | Promotion / Reviews / Notifications audit |
| 04 | `04-promotion-domain-boundary.md` | EXTEND `Tooba.Promotion` |
| 05 | `05-seller-promotion-api.md` | Seller/Admin promotion HTTP |
| 06 | `06-promotion-checkout-integration.md` | Coupon → Order evaluator |
| 07 | `07-seller-promotion-ui.md` | Vendor coupons LIVE |
| 08 | `08-admin-promotion-ui.md` | Admin promotions LIVE |
| 09 | `09-reviews-capability.md` | Seller list LIVE; reply deferred |
| 10 | `10-seller-reviews-ui.md` | Vendor reviews LIVE |
| 11 | `11-admin-reviews-ui.md` | Admin reviews LIVE |
| 12 | `12-notification-decision.md` | Option C DEFER |
| 13 | `13-notification-ui.md` | Nav hidden / shells only |
| 14 | `14-navigation-integrity.md` | Live coupons+reviews; notifications hidden |
| 15 | `15-wave2-i18n-proof.md` | fa RTL; locale≠market |
| 16 | `16-native-fit-map.md` | Shopeiva source mapping |
| 17 | `17-browser-side-by-side.md` | Capture placeholders / CDP pending |
| 18 | `18-fake-stub-audit.md` | No fake coupon/review/notification data |
| 19 | `19-authorization-proof.md` | PromotionPanelTests + ReviewsFoundationTests |
| 20 | `20-integration-tests.md` | Case map → tests |
| 21 | `21-final-validation.md` | Command result placeholders |
| 22 | `22-final-runtime.md` | Preview URLs + probe placeholders |
| 23 | `23-readiness-after-wave2.md` | Honest % + deferred domains |

## Live entry points

- Seller coupons: `/vendor-panel/coupons`
- Seller reviews: `/vendor-panel/reviews`
- Admin promotions: `/admin/promotions`
- Admin reviews: `/admin/reviews`
