# 05 — Refund-to-Wallet browser proof

## Exact URLs

| Surface | URL |
| --- | --- |
| Customer return entry | `http://localhost:3000/customer-panel/orders/01a0451c-f1f8-7000-b291-db065075733f` |
| Seller return | `http://localhost:3000/vendor-panel/returns/e848ec42-0a0f-4b99-a85e-78444033b21e?sellerPartyId=01a030d1-40cb-7000-8abe-6d31739956c5` |
| Wallet | `http://localhost:3000/customer-panel/wallet` |
| Notifications | `http://localhost:3000/customer-panel/notifications` |

## Proof

- Return created with `destination`/`refundDestination` = Wallet via Host APIs
- Seller approve 200; retry approve 400 (no duplicate credit)
- Seller UI shows **مقصد بازپرداخت = کیف پول** (after FE mapper fix for Host numeric `refundDestination: 1`)
- Wallet ledger shows **اعتبار مرجوعی**
- Notifications show «بازگشت وجه موفق» / «بازگشت به کیف پول» / «پرداخت با کیف پول»

Captures: `captures/03-seller-return-wallet-destination.png`, `captures/04-notifications.png`
