# TB-SHIPPING-USER-ENVIRONMENT-FINAL-CONFIGURED-DELIVERY-244-R2 — RECEIPTS

## Runtime

- Host PID 30328 `publish-primary` → `http://127.0.0.1:5088` /health 200; Edition SingleStore; DeploymentId store-alpha-canonical-tb244; DB `tooba_alpha`
- FE PID 28396 → `http://127.0.0.1:3000/fa` 200; BFF `/api/auth/me` requires session (Host `/v1/auth/me` used for identity)
- Local StorefrontCapturedPreview gates ON in Host process env only; checked-in gates OFF

## Identity resolution

- OTP login `09111111111` → userId `01a0996c-b8d9-7000-9854-80b3f59e8d7c`
- Host `/v1/auth/me` 200: Edition=SingleStore TenantId=store-alpha
- Cart GET 200: version 48, 4 lines, qty 18
- DB `cart.carts.owner_user_id` = same userId (no identity conflict)
- Credentials / tokens not committed

## Apply (owning APIs)

Script: `D:\temp\tb244r2-apply-flow.cjs` → `D:\temp\tb244r2-apply-flow.json`

| Step | Result |
| --- | --- |
| PUT `/v1/admin/offers/{id}/fulfillment` ×4 Platform+type01a16f43 | 200; DB responsibility=Platform, type set |
| POST category bindings daf5/dba9 → weight def | 201×2 |
| PUT product attributes rawValue `500` ×3 | 200; DB canonical_value=500 |
| Weight def unit | remains `g` |
| PUT rate-rows: min 0, max null, freight 120000, pack 5000, days 1–3 | 200 (unbounded upper accepted) |
| POST publication | 200 snapshot `01a186f7-dd7a-7000-95e9-3c8243e01991` selVer 11 srcRev 10 |

## Projection / selection / payment-draft

- Projection 200: 2 priced groups, method post/NULL «پیشتاز», freight 120000 packaging 5000, 8×60-minute slots 09–17
- Explicit choice per group: date 2026-11-02, hour 14:00–15:00
- Concurrent selection PUT: 200 rev `"2"` + 400 stale
- payment-draft 200: shipping 250000 (=2×125000), payable 81661464.16, groups dated 2026-11-02 window 14:00-15:00, canInitiatePayment true
- Readback: `D:\temp\tb244r2-payment-draft.json`

## DB accepted graph (active only)

| shipping_group_id | snapshot | freight | pack | date | ticks | owner | lines |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `01a185c1-ff77-…891b…` | `01a186f7-dd7a…` | 120000 | 5000 | 2026-11-02 | 504e9–540e9 | Store/store-alpha | 3 (Huawei+Samsung+Xiaomi Arman) |
| `01a185c1-ff77-…a965…` | `01a186f7-dd7a…` | 120000 | 5000 | 2026-11-02 | 504e9–540e9 | Store/store-alpha | 1 (Xiaomi Nilva) |

Draft selection_revision=2, shipping_amount=250000. checkouts=0. cart Active v48.

SlotDurationMinutes=60 on `shipping.authoring_weekly_schedules` for draft `01a16eeb…`.

## Constraints

- No screenshots / no StartPayment / no Order / no cart delete / no business SQL / no new ports / no Marketplace DB / no D:\temp runtime
- TB185 paused/default-OFF/PendingAllocation
- User LOCAL preserved
