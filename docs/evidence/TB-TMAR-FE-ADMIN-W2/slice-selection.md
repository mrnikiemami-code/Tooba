# Slice selection — TB-TMAR-FE-ADMIN-W2

## Selected: admin-reviews

| Criterion | Evidence |
| --- | --- |
| Bounded ownership | `/admin/reviews` + review moderation API/grid only |
| admin-api debt | `AdminReviewRow`, `AdminReviewsPage`, `mapAdminReviews`, `loadAdminReviews`, `moderateAdminReview`, `queryAdminReviewsGrid` |
| Flat/god touch | `AdminReviewsScreen` + `reviewColumns` lived in `admin-screens.tsx` |
| Visual/workflow risk | Low — list + publish/reject; no builder/editor |
| Storefront/SEO | None |
| Characterization | Cheap — route/API/map/denied/export removal |

## Rejected this wave

- admin-sellers / admin-customers / admin-receipts — similar candidates; deferred to keep one-slice rule
- catalog-units — already separate API file; weaker admin-api shrink
- order-detail / category-admin — giants; need characterization-first GODFILE wave

Selection rationale: clearest remaining low-risk capability still dumping into admin-api + admin-screens, matching ADMIN-W1 pattern.
