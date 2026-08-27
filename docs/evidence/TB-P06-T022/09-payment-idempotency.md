# 09 — Payment idempotency

**Task:** TB-P06-T022

## Covered paths

| Path | Behavior |
|---|---|
| Payment initiation | Existing Payment/Attempt model; repeated submit does not invent a second Succeeded |
| Duplicate user button | UX may retry; backend Verify/Succeeded is idempotent |
| Retry after timeout | Indeterminate leaves Pending; later Verify/Reconcile may succeed once |
| Duplicate callback | Inbox dedup on `(providerCode, providerEventId)` |
| Duplicate Verify | Succeeded short-circuits; `firstSuccess=false` |
| Duplicate reconcile | Same Verify path; terminal Succeeded safe |
| Order Paid projection | Driven by outbox `payment.succeeded.v1`; duplicate consumer must not double-pay |

## Duplicate charge prevention

- Unique provider transaction reference check before ApplyVerifiedSuccess.
- Terminal Succeeded blocks re-application.
- Sandbox complete path remains Development-only.

## Honest note

Idempotency is proven in unit/foundation/contract tests and domain guards.  
No real-bank duplicate-callback capture is claimed.
