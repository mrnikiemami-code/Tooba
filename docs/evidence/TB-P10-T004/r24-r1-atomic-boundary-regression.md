# TB-P10-T004-R24-R1 — Atomic boundary regression

R21 assertions re-run: `AtomicCheckoutCommitTests` (2) PASS.

Cart Converted only inside `CheckoutDirectory` ambient transaction after reserve → order → cycle #1 → `ConvertAsync` → `scope.Complete()`.

FE does not clear cart until Host success (`persistCommittedCheckoutAndDetachActiveCart` after `response.ok`).

Failure path: no optimistic clear; Cart remains Active.
