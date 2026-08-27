# 04 — Full wallet payment

## Gateway

`WalletPaymentGateway` (`ProviderCode=wallet`)

- Initiate: fail-closed if `!CanPayFullyWithWallet`; redirect null → Host `/payment/result?...` (no sandbox)
- Verify: `SpendForOrderPaymentAsync`; txn ref `wallet:{paymentId}`

## Storefront

- `POST .../checkout/{id}/payments` with `useWallet=true` only when balance covers full payable
- Immediate Initiate+Verify for wallet provider
- `RequiresPspRedirect=false`
- Canonical Payment `Succeeded` → Outbox `payment.succeeded` → Order Paid

## Quote endpoint

`GET /v1/storefront/checkout/{checkoutId}/wallet-quote?cartId=`
