# 03 — Full wallet browser proof

## Exact Checkout URL opened (browser submit)

Proven submit used:

```text
http://localhost:3000/customer-panel/dev/wallet-checkout?checkoutId=01a0451c-fabf-7000-99f0-30d410a58638&cartId=01a0451c-fa9f-7000-bf56-259a4a5636cd&guestSecret=bab8091ba4e303f961ea414ec42b3544bd4dc6f4301dccf777d569d7bbe82922&actor=aaaaaaaa-aaaa-4aaa-8aaa-000000000009
```

Fresh unpaid twin (for Architect open): see `02-concrete-checkout-preview.md` / `preview-urls.json`.

## Observed

| Check | Result |
| --- | --- |
| Wallet method visible | YES |
| Balance shown | ۵۹۸٬۰۰۰ ریال |
| Payable fully covered | remaining ۰ |
| Submit | «پرداخت با کیف پول» → in-progress → result |
| PSP redirect | NONE — navigated to `/fa/payment/result?...` |
| Payment status | Succeeded |
| Order Paid | YES — green «پرداخت شده» |
| Ledger debit | Wallet «پرداخت سفارش» |

## Resulting URLs

- Result: `http://localhost:3000/fa/payment/result?paymentId=88ddad77-9a7f-4e51-8ed9-12e45ecbf791&checkoutId=01a0451c-fabf-7000-99f0-30d410a58638`
- Order: `http://localhost:3000/customer-panel/orders/01a0451c-fabf-7000-99f0-30d410a58638`

Captures: `captures/01-wallet-checkout-preview.png`, `captures/05-wallet-payment-result.png`, `captures/02-wallet-paid-order.png`
