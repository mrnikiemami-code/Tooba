# admin-api shrink — TB-TMAR-FE-ADMIN-W1

| Metric | Before | After |
| --- | ---: | ---: |
| `admin-api.ts` physical LOC | 1321 | 1234 |
| Named capability exports (FE-FOLDER-002 set) | 59 | 55 |
| Promotions capability methods in admin-api | present | removed |

Remaining admin-api: CAPABILITY_SPECIFIC_DEBT (orders/sellers/customers/reviews/…) + SHARED_TECHNICAL (formatters, AdminResult re-exports, headers).

Classification file note: formatters used by promotions screen remain SHARED_TECHNICAL until moved to `lib/admin`.
