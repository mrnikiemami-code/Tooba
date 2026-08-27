# 09 — Wallet / gift E2E (TB-P06-T029)

Host APIs only · `directDbMutation: false` · artifact: `commercial-demo.json` · at: `2026-08-27T21:58:36.065Z`

## Identity

| Role | Id |
| --- | --- |
| Customer | `aaaaaaaa-aaaa-4aaa-8aaa-000000000009` |
| Seller user | `01a03628-3f68-7000-844d-99f1cadb54b0` |
| Seller party | `01a030d1-40cb-7000-8abe-6d31739956c5` |
| Admin actor | `01a036c2-970e-7000-8eb7-94bf5cc2d8db` |

## Proven this run

| Step | Result |
| --- | --- |
| Admin wallet adjust (top-up) | **200** · `POST /v1/admin/wallets/{customer}/adjustments` Credit |
| Wallet read | **200** · balance before pay **1316500** IRR |
| Full wallet checkout | **200** · payable 381500 · `canPayFullyWithWallet: true` · `requiresPspRedirect: false` · Succeeded |
| Return refund → Wallet credit | approve **200**; post-journey balance **1285000** |

Payment id `aeb80a42-139f-425e-a7b5-204a9a410b80` · checkout `01a0453b-6829-7000-8c77-32cfb5f5d409`.

## Gift redeem (session note)

Gift Card redeem was exercised **earlier in this Worker session** (prior Host/UI path under customer gift-cards). This commercial-demo run did **not** re-redeem; funding for the sale used **admin adjust** top-up above. FE surface remains: http://localhost:3000/customer-panel/gift-cards

## FE URLs

| Surface | URL |
| --- | --- |
| Wallet | http://localhost:3000/customer-panel/wallet |
| Checkout | http://localhost:3000/fa/checkout |
| Order | http://localhost:3000/customer-panel/orders/01a0453b-6829-7000-8c77-32cfb5f5d409 |
