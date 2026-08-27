# 13 — Seller settings UI

## Route
`/vendor-panel/settings`

## Behavior
- Replaces dashboard-only placeholder with Shopeiva-derived **store** form
- Only `store` tab live; profile/password, notifications, appearance tabs hidden
- Fields: displayName, supportPhone, supportEmail, addressLine (+ optional legalName, description)
- `GET/PUT /v1/seller/settings` via `seller-api.ts` headers:
  - `X-Tooba-Seller-Party-Id`
  - `X-Tooba-Dev-Actor-User-Id`
- `canManage=false` → read-only form + clear message
- `403` → ErrorState denied
- Next proxy added: `app/api/seller/[...path]/route.ts` (forwards seller/actor headers)

## Files
- `app/vendor-panel/settings/page.tsx`
- `app/vendor-panel/seller-api.ts` (`mapSellerSettings`, `loadSellerSettings`, `saveSellerSettings`)
- `app/api/seller/[...path]/route.ts`

## Preview
`http://127.0.0.1:3000/vendor-panel/settings?sellerPartyId=01a030d1-40cb-7000-8abe-6d31739956c5`

Set actor via vendor shell / localStorage `tooba.sellerActorUserId` from `/v1/seller/dev-contexts`.
