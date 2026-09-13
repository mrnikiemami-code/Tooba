# R19 concurrency

Re-verified R15 directory + coordinator + R14 paid guard:

| Race | Result |
| --- | --- |
| Double Start / PrepareStart | same CycleId; unique (checkout, cycle_number); DbUpdateException reloads winner |
| Double customer retry after expiry | second Start same correlation; CountCreated unchanged if winner exists |
| Retry vs expiry worker | CloseExpiredDue then retry; CloseActive idempotent |
| Retry vs cancel | ReleasedByCancel; no resurrect |
| Retry vs payment success | CommittedPaid; R14 blocks new initiation |
| Duplicate payment success | projection idempotent (R11/R14) |
| Duplicate manual evidence | same-cycle TransitionManualReview if Active |
| Admin confirm double-click | paid close once; later confirm sees terminal |
| Settings edit during retry | Settings do not write cycle ExpiresAt; retry uses Resolve at Start |

No duplicate Order. Failed reacquire does not insert a numbered cycle. No resurrection.
