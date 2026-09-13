# R19 manual payment

- Cycle #1 ManualInitial hold at commit
- Evidence → `TransitionManualReviewAsync` same CycleNumber; ExpiresAt = review hold
- Projector: `IsManualAwaitingReview`, PrimaryAction=none, no pay-again
- Admin reason «در انتظار بررسی پرداخت»; review TTL on current cycle
- Confirm with active review hold → CommittedPaid / Succeeded
- Confirm after review expiry: `OrderSupplyComposer` EnsureReviewHold / reacquire; stock → new cycle then confirm; no stock → not Succeeded; Released never resurrect
- Domain: `ReservationCycle.TransitionManualReview` keeps CycleNumber
