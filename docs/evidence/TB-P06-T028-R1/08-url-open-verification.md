# 08 — URL open verification

Verified after preview rebuild (Host APIs only; FE on `localhost:3000`).

| URL | HTTP | Browser |
| --- | --- | --- |
| Customer Checkout (dev wallet-checkout) | 200 | Wallet option + balance + remaining 0; submit → payment/result Succeeded (no PSP) |
| Customer Wallet | 200 | balance + RefundCredit / order debit ledger |
| Wallet-paid Order (API twin) | 200 | Paid badges |
| Browser-paid Order `…/orders/01a0451c-fabf-7000-99f0-30d410a58638` | 200 | after wallet submit |
| Seller Return `…/returns/e848ec42-…` | 200 | destination کیف پول (after actor storage) |
| Notifications | 200 | refund + wallet payment rows |
| Shopeiva `/payment` | 200 | comparison |
| Shopeiva `/cart` | 200 | comparison |
| Shopeiva `/user-panel/wallet` | 200 | comparison |
| Host `/health/live` | 200 | ok |
| Host `/health/ready` | 200 | ready |

No 404 on Tooba preview URLs. Shopeiva `/checkout` is 404 on this template → use `/payment`.
