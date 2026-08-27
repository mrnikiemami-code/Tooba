# 26 — Browser + live API proof

## Live E2E (no direct DB mutation)

Script: `15-wallet-checkout-e2e.mjs` → `15-wallet-checkout-e2e.json` (**ALL_OK**)

| Step | Proof |
| --- | --- |
| wallet-quote | `canPayFullyWithWallet=true`, `remainingPayable=0`, `mixedTenderDeferred=true` |
| wallet pay | `providerCode=wallet`, `requiresPspRedirect=false`, `paymentStatus=Succeeded` |
| order | checkout seller order → **Paid** |
| ledger debit | `OrderPaymentDebit` sourced by `paymentId` |
| fulfillment | Processing→Packed→Ship→Track→Dispatch→Deliver |
| return | create+approve with `destination=Wallet` (enum `1`) |
| refund credit | exactly **1** `RefundCredit` after approve + approve retry |

Sample IDs (last green run):

- checkoutId: `01a044fb-5698-7000-9f9d-8b7256eb7f17`
- paymentId: `46a17af2-176c-4a14-8a35-e36bd3db40c5`
- returnRequestId: `f4d8c4bc-f7e8-4a77-aaec-b1bf4f91e40e`

## Browser captures

| File | URL / surface |
| --- | --- |
| `captures/22-wallet-ledger.png` | `http://127.0.0.1:3000/customer-panel/wallet` — OrderPaymentDebit + RefundCredit labels live |
| `captures/23-wallet-paid-order.png` | `http://127.0.0.1:3000/customer-panel/orders/01a044fb-5698-7000-9f9d-8b7256eb7f17` — Paid + Delivered + return CTA |

## Preview URLs

- Wallet: http://127.0.0.1:3000/customer-panel/wallet
- Paid order: http://127.0.0.1:3000/customer-panel/orders/01a044fb-5698-7000-9f9d-8b7256eb7f17
- Seller returns: http://127.0.0.1:3000/vendor-panel/returns?sellerPartyId=01a030d1-40cb-7000-8abe-6d31739956c5
- Admin wallet demo: http://127.0.0.1:5088/v1/admin/wallet/demo-preview
- Host health: http://127.0.0.1:5088/health

## Operational notes

- Applied pending migrations (returns.refund_destination, settlement schema) via MigrationRunner.
- Restored seller party display name encoding + membership-fallback in `SellerDevActorBootstrap` so InMemory `party#view` tuples rehydrate after Host restart.
