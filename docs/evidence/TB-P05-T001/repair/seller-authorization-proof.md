# TB-P05-T001 REPAIR — Seller authorization proof

## Matrix (Host + unit)

| Case | Result | Evidence |
| --- | --- | --- |
| Actor A + Seller A | Allow 200 | live dashboard + `SellerPanelAuthorizationTests` |
| Actor A + Seller B | Deny 403 | live curl + UI `08-seller-auth-denied.png` |
| Actor B + Seller B | Allow 200 | live dashboard after context switch |
| Actor B + Seller A | Deny 403 | unit matrix + same guard path |
| missing actor | Deny 401 | live curl `missing: 401` |
| Offer mutation cross-seller | Deny 403 before composer | endpoints call `RequireAuthorizedAsync` first |
| Order detail cross-seller | Deny 403 / missing | same guard + seller filter |

Live Actor IDs (Development seed at proof time):

- Actor A: `01a03628-3f68-7000-844d-99f1cadb54b0` → Seller A `01a030d1-40cb-7000-8abe-6d31739956c5`
- Actor B: `01a03628-3ff0-7000-a04e-6420c2e76f72` → Seller B `01a030d1-40db-7000-b90c-a0705133f0eb`

## Changing only SellerPartyId

Preserving Actor A while setting SellerPartyId=B yields **403** and UI “دسترسی مجاز نیست”. Screenshot: `08-seller-auth-denied.png`.

## Tests

- `SellerPanelAuthorizationTests` — allow/deny matrix, missing actor, unavailable fail-closed, header spoof
- `SellerPanelCompositionTests` — endpoints require `IAuthorizationGuard` / `RequireAuthorizedAsync`
- Frontend `seller-api.test.ts` — actor header distinct; Persian status mapping
