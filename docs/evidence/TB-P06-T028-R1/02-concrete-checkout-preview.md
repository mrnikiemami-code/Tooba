# 02 — Concrete Checkout preview URL

## Exact URL (open in browser)

```text
http://localhost:3000/customer-panel/dev/wallet-checkout?checkoutId=01a04529-84ab-7000-a55d-3dd16c58c915&cartId=01a04529-848a-7000-b143-0646625daa8c&guestSecret=fb11d0cdcd39e05364f426f9afea1c194d22e98fb10c2c03cfad3fca2835f0a3&actor=aaaaaaaa-aaaa-4aaa-8aaa-000000000009
```

Also in `preview-urls.json` → `urls.customerCheckoutConfirmation`.

## What the user sees

- PendingPayment (honest — not fake Paid)
- Wallet method selected when fully coverable
- Real balance / max usable / remaining 0
- Mixed tender labeled DEFERRED
- CTA «پرداخت با کیف پول»

## Shortest steps

1. Host `:5088` + FE `localhost:3000` + Shopeiva `:3001`
2. Optional: `node docs/evidence/TB-P06-T028-R1/build-preview-urls.mjs`
3. Open confirmation URL above
