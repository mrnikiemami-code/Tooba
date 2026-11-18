# TB-279-R1 — rollback-only proof

Canonical target: `tooba_alpha` on `127.0.0.1:5432` (the single canonical store, `store-alpha`). Client target is
pinned to loopback/5432/`tooba_alpha` and the server itself confirms `current_database()` + `inet_server_port()` +
`host(inet_server_addr())` read-only before any write; a non-canonical or unreachable target produces a visible
SkippableFact SKIP and opens no connection.

Boundary: every task row is written inside a CALLER-owned transaction (`await using` ⇒ rollback) — no commit, no
delete/cleanup, no second database. Each fact finishes by re-reading the canonical database on a SEPARATE connection
and asserting `committedRows = 0` for its own tag prefix.

## Facts and observed output

`src/backend/Host/Tooba.Host.Tests/ReturnsClosure279RelationalTests.cs` — **3/3 passed**, `committedRows=0`.

| Fact | Observed receipt |
| --- | --- |
| `The_payment_budget_counts_every_case_of_that_payment_and_no_other_payment` | `TB279R1-A payment=7f0e6ff7… counted=60.0000 otherPayment=0c97f5aa… counted=100.0000` |
| `A_second_session_really_waits_for_the_payment_budget_lock` | `TB279R1-A2 payment=5cff386a… waited=true independent=9c2056df…` |
| `A_stale_reviewed_version_writes_nothing_and_a_lawful_approval_moves_every_fact_together` | `TB279R1-D request=8bd811fe… stale=zero-write approved=1000 requested=2000 attempts=0` |

What these facts prove (a caller-transaction external-effect refusal alone is NOT offered as budget evidence):

1. the budget read is the exact production predicate (`payment_id = @payment AND status IN (Pending, Succeeded)`), so
   a sibling case's reservation counts and another payment's identical amount does not;
2. a second session really waits on `pg_advisory_xact_lock(ReturnBudgetSerialization.AdvisoryLockKey(payment))` and a
   different payment's key is independent under the same namespace — two-connection contention, then rollback;
3. the version-fenced approval refuses a stale review with zero writes and, when lawful, moves the decision, the case
   status and the frozen approved amount in ONE transaction, with the requested amount immutable and no attempt/money.

## Declared limits

* A **committed** winner-vs-loser race is not claimed: the effectful first-refund intent refuses a caller-owned
  transaction before any effect, and a row is invisible to a competing session until it commits — both excluded by
  this boundary. Concurrency is proven by the lock-contention fact plus the controlled in-memory directory suite.
* No provider, PSP, Wallet, Inventory, waybill or notification call is made anywhere in these facts.
* The `FOR UPDATE` outcome-apply branch of recovery stays a production-only path (pre-existing, accepted limit).

## Canonical data after the run (read-only SELECT)

```
decision_revisions=0  item_resolutions=0  custody=0  delivery_allocations=0
refund_attempts=3     return_requests=3   (all Completed, approved_amount NULL)
01a1e6b0-f33a-7000-80ee-beffb6344490  Completed  null  2026-11-17 00:31:26.384627+00
01a1e8fc-c74e-7000-a155-fecfa1a3bf1a  Completed  null  2026-11-17 13:21:05.594614+00
01a1e9c5-c729-7000-b614-79daf3d0e812  Completed  null  2026-11-17 14:03:06.669021+00
```
