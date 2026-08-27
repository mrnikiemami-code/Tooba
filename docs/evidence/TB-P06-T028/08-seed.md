# 08 — Seed (Development only)

Customer actor: `aaaaaaaa-aaaa-4aaa-8aaa-000000000009`

Idempotent ledger keys:

- `wallet-seed-admin-credit-v1`
- `wallet-seed-checkout-topup-v1`
- `wallet-order-debit:{DemoWalletPaymentId}`
- `wallet-refund-credit:{DemoWalletReturnRequestId}`

Stable IDs (see `WalletDemoIds`):

- Account / Payment / Checkout / SellerOrder / ReturnRequest
- Demo order amount 75_000 IRR debit
- Demo refund amount 25_000 IRR credit

Preview: `GET /v1/admin/wallets/demo-preview` (Development)
