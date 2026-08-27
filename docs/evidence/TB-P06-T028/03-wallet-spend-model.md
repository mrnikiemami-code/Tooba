# 03 — Wallet spend model

## Model

`ATOMIC_DEBIT_AT_PAID` (no reservation/hold ledger types).

## API

`IWalletDirectory.SpendForOrderPaymentAsync(customerActorId, amount, currency, paymentId, idempotencyKey, ct)`

- EntryType = `OrderPaymentDebit`
- SourceType = `payment`, SourceId = PaymentId
- Idempotency key: `wallet-order-debit:{paymentId:D}`
- Serializable EF transaction + account row touch
- Rejects: overdraw, currency mismatch, frozen account
- IdempotentReplay safe on duplicate key

## Quote

`QuoteForPayableAsync` → `walletBalance`, `maxUsable`, `remainingPayable`, `canPayFullyWithWallet`
