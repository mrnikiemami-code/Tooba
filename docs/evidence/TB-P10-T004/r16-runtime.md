# R16 runtime A–K

Proven against `StorefrontPendingPaymentProjector`, `ReservationCycleDirectory.GetProjectionsAsync`, R15 coordinator path, and Cart source. Server clock in tests: `2026-09-13T10:00:00Z`.

| Case | Result |
| --- | --- |
| A. Initial pending | PASS — pay + held countdown from server ExpiresAt (570s → 09:30) |
| B. Payment fail mid-cycle | PASS — same HoldEndsAt / cycle / pay CTA |
| C. Cycle expires | PASS — ended + retryAfterExpiry; no poll storm |
| D. Retry + stock | PASS — same Order; new cycle only via EnsureRetryAfterExpiryAsync (R15) |
| E. Retry + unavailable | PASS — mapped `payment.unpaid.supply_unavailable` |
| F. Max cycles | PASS — no retry CTA + retryLimit |
| G. Manual AwaitingAdmin | PASS — informational, primaryAction none |
| H. Payment success | PASS — Succeeded excluded from list |
| I. New Active Cart + pending | PASS — section independent; empty Active copy changes |
| J. Guest + authenticated | PASS — proofs vs PlacedByUserId; wrong/new cart secret omitted |
| K. mobile/desktop | PASS — responsive cards, unclipped CTA, compact thumbs; FA RTL / EN LTR |

No TB-P10-T005. USER_VISUAL_ACCEPTED=NO.
