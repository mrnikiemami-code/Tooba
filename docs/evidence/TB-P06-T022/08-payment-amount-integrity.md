# 08 — Payment amount integrity

**Task:** TB-P06-T022

## Authoritative totals

- Checkout / Order pricing snapshots (Pricing / Tax / Promotion) remain source of payable amount.
- Payment aggregate stores expected `Amount` + `Currency` at initiation.
- Client-side totals are never authoritative for Succeeded.

## Verification checks

Before accepting webhook-driven verify:

| Check | On mismatch |
|---|---|
| Amount vs Payment aggregate | `payment.webhook.amount_mismatch` (409) |
| Currency vs Payment aggregate | same mismatch path |
| PaymentId correlation | missing / wrong payment rejected |
| Attempt + providerRequestReference | `payment.webhook.attempt_mismatch` |
| Provider code | `payment.webhook.provider_mismatch` |
| Provider transaction uniqueness | duplicate txn reference blocks second success |

## Gateway verify

StatusQuery success still requires a non-empty `providerTransactionReference` before `ApplyVerifiedSuccess`.

## Honest claim

Amount integrity foundations are LIVE for the webhook-backed path.  
No real PSP response payload was used to prove a live bank amount match (`REAL_BANK_PAYMENT_PROVEN` remains false).
