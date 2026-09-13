# R19 integration discovery

Traced as implemented (R13–R18), not assumed.

| Stage | Order | Payment | SupplyStatus | Cycle | Cart | Customer | Admin |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Active Cart, no Order | none | none | n/a | none | Active independent | ATC/checkout | none |
| Commit + Cycle #1 | PendingPayment | none/Pending | Reserved | #1 Active Initial | rotated; proof kept | pay; countdown server ExpiresAt | current cycle; no extend |
| Fail/retry inside #1 | same Order | Failed | Reserved | same #1; ExpiresAt unchanged | independent | pay again same cycle | history immutable |
| Cycle #1 expired | PendingPayment | Expired | AvailableForReacquire / Released | #1 Expired | independent | retryAfterExpiry | compact expired |
| Reacquire + Cycle #2 | same Order | new attempt | Reserved | #2 Retry TTL | independent | pay; retry TTL | history #1+#2 |
| Reacquire fail | same Order | Expired | Unavailable | no new numbered cycle; ReacquireFailed | independent | unavailable copy; no pay | shortages; no parse Status.ToString() |
| Max cycles | same Order | Expired | ended | created >= max; RetryLimitReached | independent | no retry | retry-limit copy |
| Manual evidence | same Order | Pending manual | review hold | same cycle ManualReview | independent | awaitingReview; no pay-again | review TTL audited |
| Admin confirm + active review | Succeeded | Succeeded | Reserved→paid durable | CommittedPaid | independent | pending card gone | confirm |
| Admin confirm after review expiry + stock | Succeeded after reacquire | Succeeded | Reacquired | new cycle then CommittedPaid | independent | none | confirm after Ensure |
| Admin confirm after review expiry + no stock | not Succeeded | Pending/Expired | Unavailable | no resurrection | independent | blocked | do not mark Succeeded |
| Payment succeeded | Paid | Succeeded | durable / ExpiresAt cleared | CommittedPaid | new Active Cart unaffected | pending hidden; paid accessible | paid compact |
| Cancel before success | Cancelled | cancelled/expired | Released | ReleasedByCancel | independent | hidden | cancel close |
| Paid-but-unavailable exceptional | Paid exceptional | Succeeded | recovery/new durable | LatePaymentRecovery / HistoricalRecovery | independent | not pending | recovery |
| New Active Cart + old pending | old PendingPayment | pending/failed | Reserved or reacquire | old cycle scoped by proof | new Active empty/items | pending section + cart both | same Order |

Contradiction found and fixed: commit-time inventory hold TTL (`ResolveInitialCycleExpiresAtAsync`) previously built policy lines with `CategoryId = null`, so Category overrides were ignored for the hold while Cycle #1 used `CategoryIdSnapshot`. Hold TTL now resolves primary category per variant (same catalog lookup as quote). Cycle snapshot path was already correct.

Guest ownership: committed proof only. Auth: PlacedByUserId. Multi-seller: one checkout, min TTL + strictest max. Manual vs online: same cycle coordinator; unpaid worker `CloseExpiredDueAsync`. No reservation polling.
