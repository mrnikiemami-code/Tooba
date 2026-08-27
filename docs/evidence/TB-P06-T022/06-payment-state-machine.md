# 06 — Payment state machine

**Task:** TB-P06-T022  
**Aggregate:** `Payment` / `PaymentAttempt` (`PaymentDomain`)

## Statuses

| Status | Meaning |
|---|---|
| Created | Aggregate exists; gateway not started |
| Pending | Initiate recorded; awaiting independent Verify |
| Succeeded | Verified success with unique provider transaction reference |
| Failed | Definitive verified failure only |
| Cancelled | Terminal cancel |
| Expired | Terminal expiry |

Attempt statuses include VerifiedSucceeded / VerifiedFailed / Cancelled.

## Hard rules

1. Initiate moves Created → Pending; **never** Succeeded.
2. Succeeded only via `ApplyVerifiedSuccess` after gateway Verify returns success + transaction reference.
3. Browser/callback success text is **not** sufficient.
4. Terminal Succeeded: duplicate Verify returns already-succeeded (no second charge intent).
5. Duplicate provider transaction reference → no second success apply.
6. Indeterminate gateway codes → **stay Pending** (no `ApplyVerifiedFailure`).
7. Definitive reject codes → Failed.
8. Order Paid is projected from outbox `payment.succeeded.v1`, not from callback text.

## Refund states

Refund foundation is separate (`IPaymentRefundGateway`). Production refund execution remains fail-closed without provider configuration. No fake provider refund success in Production.

## Forged / out-of-order protection

- Invalid HMAC → rejected before state mutation.
- Amount/currency mismatch → rejected.
- Attempt/reference mismatch → rejected.
- Out-of-order duplicates safe via inbox + terminal guards.
