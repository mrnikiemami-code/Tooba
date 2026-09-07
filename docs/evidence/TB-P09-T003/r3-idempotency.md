# R3 idempotency

Runtime smoke checkout `01a079c5-8077-7000-8501-b93db0e23aab`:

- first confirm_deposit → 200 NewlySucceeded path
- second confirm_deposit → 400 invalid / not projected (no double success)
- payment txn ref `manual-confirm-{paymentId}` unique guard

No duplicate settlement accrual from duplicate confirm.
