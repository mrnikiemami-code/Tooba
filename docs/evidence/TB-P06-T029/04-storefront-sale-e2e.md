# 04 — Storefront sale E2E (TB-P06-T029)

Host APIs only · `directDbMutation: false` · artifact: `commercial-demo.json` · at: `2026-08-27T21:58:36.065Z`

## Identity

| Role | Id |
| --- | --- |
| Customer | `aaaaaaaa-aaaa-4aaa-8aaa-000000000009` |
| Seller user | `01a03628-3f68-7000-844d-99f1cadb54b0` |
| Seller party | `01a030d1-40cb-7000-8abe-6d31739956c5` |
| Admin actor | `01a036c2-970e-7000-8eb7-94bf5cc2d8db` |

## Flow (recorded)

| Step | HTTP / result |
| --- | --- |
| Add line | 200 |
| Checkout | 200 · checkoutId `01a0453b-6829-7000-8c77-32cfb5f5d409` · payable **381500** IRR |
| Wallet quote | 200 · balance 1316500 · maxUsable 381500 · remainingPayable **0** · `canPayFullyWithWallet: true` |
| Pay | 200 · `providerCode=wallet` · `requiresPspRedirect=false` · paymentStatus **Succeeded** |
| Payment id | `aeb80a42-139f-425e-a7b5-204a9a410b80` |

No PSP redirect. Full wallet tender. Order Paid path via wallet gateway.

## FE URLs

| Surface | URL |
| --- | --- |
| Home | http://localhost:3000/fa |
| Products | http://localhost:3000/fa/products |
| Cart | http://localhost:3000/fa/cart |
| Checkout | http://localhost:3000/fa/checkout |
| Order | http://localhost:3000/customer-panel/orders/01a0453b-6829-7000-8c77-32cfb5f5d409 |
| Wallet | http://localhost:3000/customer-panel/wallet |
