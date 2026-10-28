# RECEIPTS — 240-R2

## Identity / environment
Host: `D:\temp\tb240r2-build\out` PID per restart, `http://127.0.0.1:5190` Development Marketplace Tenant NULL
`tb239-marketplace`, DB `tooba_marketplace_tb239`.
Opt-in: `Tooba__Development__MarketplaceSellerAccess__Enabled=true` (task-local env only).
Real identities: SellerA `01a181b0-5c87-7000-976e-ae5ce19864f6` (user `01a18178-67bc-7000-9662-fb5cd519fd58`),
SellerB `01a181b0-5c9a-7000-bf9b-8c8e1c47da72` (user `01a18178-6820-7000-a04e-01f81bcca3a4`),
buyer `01a18178-66d4-7000-8b5b-76b15de03a98`, platform admin user `01a1818b-5257-7000-bada-84233a2b21fb`.

## Role commissioning (AccessControl prelude)
`access_control.user_role_assignments`: exactly 2 `seller-owner` rows, one per real seller scope; `roles=3`.
`access_control.user_capability_tuples` republished for the two real actor pairs (`SyncUserCapabilityTuplesAsync`).
Prelude invocation is idempotent: re-running the bootstrap produced no new assignment rows.

## Seller capture authoring (normal seller APIs, own token + own header)
SellerA draft `01a181dd-6bcb-7000-81eb-576c1f587eef` (owner Seller/SellerA), rev6, Selected snapshot
`01a181de-0c80-7000-9c01-ca51a0da05f4`, area `01a181dd-6bcc-7000-823b-ee56a67b5736`, row
`01a181de-0c26-7000-891b-a803cf897db6`, binding `post`/`standard`, type `01a181c7-7863-7000-925f-b6f64ed4694b`,
manual/prepaid IRR, freight 62000 + packaging 3000, delivery 2..3, week Sat..Thu 09:00-17:00 Asia/Tehran.
SellerB draft `01a181de-0d04-7000-a79f-1bf723510020` rev6, snapshot `01a181de-0e59-7000-9127-f5142617340e`, area
`01a181de-0d04-7000-9d2b-f3ffede2fde9`, row `01a181de-0e2d-7000-83ef-06d8623b713a`, same binding/type, freight
47000 + 2500.
Denials proven: SellerB token + SellerA header -> 403 `seller.authorization.denied`; seller token on admin authoring
-> 403 `admin.authorization.denied`.

## Mixed cart + projection
Cart `01a181e2-0d5e-7000-a94c-87e9b826f94d`, 3 offers (Platform-owned, SellerA-owned, SellerB-owned).
`POST /v1/storefront/shipping/projection` -> 200, cartVersion 7, three priced groups (values in RESULT §A).

## Selection defects (measured)
- pre-repair: `shipping.group.duplicate_seller_method` from `CartShippingGroupPlanner` (seller|method collapse).
- after repair 1: `storefront.captured_selection.unavailable` (row resolved by seller+method, payable mismatch).
- after repair 2 (obligation-keyed row resolution):

```
PUT /v1/storefront/shipping/selection -> 500 platform.unexpected
Npgsql.PostgresException 23505: duplicate key value violates unique constraint
"ux_cart_shipping_groups_cart_seller_method"
```

## Payment concurrency
Pre-repair: two simultaneous same-key start-payment -> `200` + `500 platform.unexpected (DbUpdateException)`.
Post-repair receipts in RESULT §C (identical PaymentId and AttemptId on both branches, one payment row, one attempt,
sequential replays identical).
JSON: camelCase browser body bound (payment key persisted as `start-payment:payment:<key>`, provider `manual`,
`ExpectedSelectionRevision="1"` preserved as text). No serializer change needed.

## Not run
No order/payment for the mixed cart (blocked), no coupon, no FE build/screenshot, no Order-only mixed read.
