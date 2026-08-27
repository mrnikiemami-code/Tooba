# 10 — Seller reviews UI (TB-P06-T020)

Date: 2026-08-27  
Source: `SarvNewVerRequirment/reference/shopeiva/src/components/vendor/panel/reviews/reviewsList.jsx`

## Port

| Tooba | Role |
|---|---|
| `app/vendor-panel/reviews/page.tsx` | Route entry (replaces `VendorCapabilityShell` stub) |
| `app/vendor-panel/vendor-reviews-ui.tsx` | Live Shopeiva-shaped list |
| `app/vendor-panel/seller-api.ts` | `loadSellerReviews` / `mapSellerReviewsPage` → `GET /v1/seller/reviews` |
| `vendor-shell.tsx` | `reviews` nav `live: true`; removed from `VENDOR_DEFERRED_NAV_HREFS` |

## Visual fidelity (locked tokens)

- Accent `#E53935` (icon chip, focus ring, active filter, pagination)
- Header Star chip + **مدیریت نظرات** + emerald “تایید شده” pill
- Stats `grid grid-cols-3` — تایید شده / در انتظار / رد شده
- Search + status filter dropdown; card borders by status (emerald/amber/red)
- Amber star rating; Package placeholder thumb (no fake product image URLs)
- Client pagination 4/page (native controls; Fuse/ReactPaginate not added as deps)

## Honesty

| Concern | Behavior |
|---|---|
| Counts | From Host `PublishedCount` / `PendingCount` / `RejectedCount` only |
| Approve / Reject / Delete | **Omitted** — seller cannot moderate; admin owns publish/reject |
| Seller reply | Honest note when `sellerResponseSupported === false` |
| Empty / loading / denied | Real states; no seeded fake rows |

## Nav

`panel-nav-integrity.test.ts` updated: `/vendor-panel/reviews` in LIVE_HREFS, not deferred.
