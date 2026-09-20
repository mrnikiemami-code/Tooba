# admin-api shrink — TB-TMAR-FE-ADMIN-W2

| Metric | Before (ADMIN-W1 tip) | After |
| --- | --- | --- |
| `admin-api.ts` physical LOC | 1234 | 1145 |
| Named capability exports (FE-FOLDER-002 set) | 55 | 49 |
| Reviews capability methods in admin-api | present | removed |

Removed exports: `AdminReviewRow`, `AdminReviewsPage`, `loadAdminReviews`, `mapAdminReviews`, `moderateAdminReview`, `queryAdminReviewsGrid`.

Remaining admin-api: CAPABILITY_SPECIFIC_DEBT (orders/sellers/customers/receipts/…) + SHARED_TECHNICAL (formatters, AdminResult re-exports, headers).
