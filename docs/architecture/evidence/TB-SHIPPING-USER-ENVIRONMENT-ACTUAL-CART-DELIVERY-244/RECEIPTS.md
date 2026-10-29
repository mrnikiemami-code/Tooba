# TB-SHIPPING-USER-ENVIRONMENT-ACTUAL-CART-DELIVERY-244 — RECEIPTS

## Runtime

- Host PID 30328: `...\Tooba.Host\bin\Release\net8.0\publish-primary\Tooba.Host.exe`, `ASPNETCORE_URLS=http://127.0.0.1:5088`, DeploymentId `store-alpha-canonical-tb244`, Edition SingleStore, `/health` 200
- FE PID 28396: `D:\Users\User\source\repos\SarvNewVer\src\frontend` Next `:3000`, `TOOBA_HOST_ORIGIN=http://127.0.0.1:5088`, `/fa` 200
- Private launch notes: `D:\temp\tb244-launch.md`, `D:\temp\tb244-start-host.ps1`

## Identity / cart

- OTP login → userId `01a0996c-b8d9-7000-9854-80b3f59e8d7c`; `/v1/auth/me` 200 Edition=SingleStore TenantId=store-alpha
- Cart `01a17b2d-e219-7000-b3b2-5bcdbe6810c5` Active v48; lines 4; quantity sum 18
- Line offers: `0b589982…` qty11, `3bf70c9e…` qty3, `0c4ae7f9…` qty3, `6fce25fb…` qty1

## Merchant configuration writes (admin APIs)

- PUT fulfillment Platform + type `01a16f43-6b4a…` ×4 offers → DB responsibility Platform / type set
- PATCH attribute `01a05387-d504…` (`demo_attr_mobile_weight`) unit → `kg`
- POST category attribute bindings on `01a05387-daf5…` and `01a05387-dba9…` → 201
- PUT product attributes rawValue `1.5` on three cart products → 200
- PUT rate-rows draft `01a16eeb…` group `01a16f4e-1c41…`: keep 1000–2000 + add 0–1000 + 2000–1e9 (same 120000/5000/1–3d) → rev8; publish → snapshot `01a185c1-458a…` selectionVersion 9

## Projection / selection / payment-draft

- POST `/v1/storefront/shipping/projection` 200 → 2 groups, each 1 method post/NULL, 8 slots, freight `120000.00`
- PUT selection concurrent: 200 rev `"1"` + 400 `storefront.captured_selection.stale`
- GET payment-draft 200: `shippingAmount` 250000, `payableAmount` 81661464.16, groups 2

## Database readback (`tooba_alpha`)

- `order.cart_shipping_drafts`: cart_id above, `selection_revision=1`, cart_version 48, destination set
- `order.cart_shipping_groups`: 2 rows; accepted_snapshot `01a185c1-458a…`; receiving 2026-11-02; open/close ticks 504000000000 / 540000000000; freight 120000 packaging 5000 each; confirmed goods 64504221 + 16907243
- checkouts for cart: **0**; payments for cart: **0**; cart status **Active** v48

## Artifacts (private temp; not committed secrets)

- `D:\temp\tb244-admin-assign.json`, `tb244-weight-rate.json`, `tb244-final-flow.json`, `tb244-claim-meta.json`
