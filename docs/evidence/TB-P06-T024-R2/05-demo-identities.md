# 05 — Demo identities

Task: TB-P06-T024-R2

**Do not publish passwords/secrets in evidence.** Development passwords follow existing seller-dev patterns and are not listed here.

## Live IDs (from Host demo-preview)

| Field | Value |
|-------|-------|
| Platform Admin actor | `01a036c2-970e-7000-8eb7-94bf5cc2d8db` |
| Seller Party | `01a030d1-40cb-7000-8abe-6d31739956c5` (فروشگاه آرمان) |
| Seller Owner actor | `01a03628-3f68-7000-844d-99f1cadb54b0` (اپراتور آرمان) |
| Employee actor | `01a04407-47ae-7000-a6b1-43f69a17cd1a` (اپراتور سفارش موبایل) |
| Mobile category | `01a043f3-30c5-7000-9c2d-2e96d8da1439` (موبایل) |
| Books category | `01a03826-b010-7000-99e8-e7540e21e31a` (کتاب) |
| Mobile Order Operator role | `a90b3e41-c459-46de-82dc-2248de37bda0` (`mobile-order-op`) |

## Safe local preview procedure

1. Host `:5088` with `ASPNETCORE_ENVIRONMENT=Development`.
2. Frontend `:3000` with `TOOBA_HOST_ORIGIN=http://127.0.0.1:5088`.
3. Open panels; use vendor context switcher (`GET /v1/seller/dev-contexts` includes `scoped-employee`).
4. Or set FE localStorage: `tooba.sellerActorUserId` + `tooba.sellerPartyId` (same pattern as capture script).
5. Admin: `tooba.adminActorUserId` from `/v1/admin/dev-context`.

Employee email (identifier only): `seller-employee-mobile@tooba.local`.
