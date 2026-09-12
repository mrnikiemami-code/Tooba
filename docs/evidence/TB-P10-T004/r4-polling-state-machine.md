# TB-P10-T004-R4 — Polling State Machine

## Implementation

- `shouldPollStorefrontPayment(payment)` in `storefront-payment-api.ts`
- Result page starts interval only if first load returns a pollable state
- Clears interval/timeout on terminal/stable, 401/ownership error, unmount
- `inFlight` prevents overlapping GETs
- Hard bound still 20s for transient online states

## Matrix

| Condition | Poll |
| --- | --- |
| Pending/Processing/Verifying (non-manual awaiting) | yes |
| manual + canSubmitManualEvidence | no |
| manual + evidenceSubmittedAt (AwaitingAdmin) | no |
| Succeeded/Failed/Cancelled | no |
| load error / 401 | stop |

## Manual AwaitingAdmin UX

Shows «در انتظار تایید»; no 1.5s Admin wait loop. Customer refresh/reopen later.
