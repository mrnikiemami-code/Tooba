# 05 — Mixed tender deferred

## Status

`WALLET_MIXED_TENDER = DEFERRED`

## Behavior

If wallet balance < payable (remainder would be > 0):

- storefront rejects with `payment.wallet.mixed_deferred`
- clear message: mixed wallet+PSP not enabled

No split-tender ledger, no partial capture, no rollback claim.
