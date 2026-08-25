# Review development seed proof

Captured on 2026-08-25 from the running Development Host at
`http://127.0.0.1:5088`; this is not a hand-authored UI fixture.

## Deterministic source

`src/backend/Modules/Reviews/Tooba.Reviews.Infrastructure/ReviewsDevelopmentSeed.cs`
targets the real Catalog product `workspace-live-shirt` and inserts four
actor-owned rows only when that product/actor pair does not already exist:

- Published: مریم, 5, `کیفیت خوب`
- Published: علی, 4, `خرید مناسب`
- Published: سارا, 3, no title
- Pending: کاربر تازه, 5, `در انتظار بررسی`

All four use the fixed instant `2026-08-25T12:00:00Z`. The seed passes
`verifiedPurchase: false`; it does not manufacture Order evidence or a verified
badge. Re-running the bootstrap is idempotent because the lookup uses the same
database-backed one-review-per-product/actor key that the unique index guards.

## Live public API proof

Command:

```powershell
curl.exe -sS http://127.0.0.1:5088/v1/storefront/products/workspace-live-shirt/reviews
```

Observed authority:

```json
{
  "averageRating": 4,
  "reviewCount": 3,
  "ratingDistribution": { "1": 0, "2": 0, "3": 1, "4": 1, "5": 1 },
  "page": 1,
  "pageSize": 20,
  "totalCount": 3
}
```

The three returned public rows are exactly مریم/5, علی/4, and سارا/3, all with
`verifiedPurchase: false`. The Pending row is absent from this response.

## Live moderation proof

`07-admin-review-moderation.png` shows the separate Pending row for کاربر تازه,
including the real product title `پیراهن مردانه لینن`, rating 5, body excerpt,
Pending state, and publish/reject controls. The queue was read from
`GET /v1/admin/reviews` after the Development admin actor was obtained from
`GET /v1/admin/dev-context`; it was not inserted into browser state.

## Real zero state

Command:

```powershell
curl.exe -sS http://127.0.0.1:5088/v1/storefront/products/demo-mobile-1/reviews
```

Observed `reviewCount: 0`, all five distribution buckets zero, and an empty
`reviews` array. This real Catalog product is the source of
`06-pdp-zero-review-state.png`.
