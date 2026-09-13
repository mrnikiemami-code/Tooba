# R19 canonical lifecycle matrix

Proven in `ReservationLifecycleIntegrationGateTests` + R13–R18 projectors. No contradictory CTA pair.

| Case | Cycle | Payment | Customer CTA | Admin |
| --- | --- | --- | --- | --- |
| Cart active, no Order | none | none | cart only | n/a |
| Order committed, Cycle #1, no attempt | Active #1 | none/Pending | pay | current #1; no extend |
| Payment failed, Cycle #1 active | same #1 ExpiresAt | Failed | pay (failedRetryable) | same history |
| Retry inside Cycle #1 | same #1; coordinator `allowReacquire: false` | new attempt | pay | no new cycle number |
| Cycle #1 expired | Expired | Expired | retryAfterExpiry | expired compact |
| Retry after expiry + stock | #2 Retry TTL | new | pay | history #1+#2 |
| Retry after expiry + no stock | no new number; ReacquireFailed | Expired | unavailable; no pay | shortages |
| Max cycles | created >= max | Expired | none / retryLimit | retry-limit |
| Manual AwaitingAdmin | Active ManualReview | Pending manual | none; awaitingReview | review TTL |
| Manual review hold expired | ended / needs reacquire | Pending manual | no pay-again | reacquire path |
| Admin confirm + active review | CommittedPaid | Succeeded | pending gone | confirm |
| Admin confirm after expiry + stock | new cycle then CommittedPaid | Succeeded | pending gone | confirm after Ensure |
| Admin confirm after expiry + no stock | no Succeeded | not Succeeded | blocked | do not Succeeded |
| Payment succeeded | CommittedPaid | Succeeded | hidden from pending | paid |
| Cancel before success | ReleasedByCancel | cancelled | hidden | cancel |
| Paid-but-unavailable | recovery cycle | Succeeded | not pending-pay | recovery |
| New Active Cart + old pending | old cycle | old payment | pending + empty/active cart | same Order |

Same Order across retries. New cycle only after previous ended + successful EnsureOrderSupply. Released/Expired never resurrect.
