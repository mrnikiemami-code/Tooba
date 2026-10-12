# TB-P0-INVENTORY-CONSUMPTION-JOURNAL-003 — residual risks & non-goals

## Residual risks (accepted, bounded)

| # | Risk | Impact | Mitigation / status |
|---|---|---|---|
| R1 | Historical `Consumed` reservations have no journal row | A keyed replay of a *legacy* consumption cannot be proven, so it returns `inventory.reservation.consume_mismatch` instead of silent success | Intentional and required by the task stop gates ("no fabricated backfill", "same-reservation retry must not silently double-deduct"). The legacy single-arg `ConsumeAsync` path is untouched and still works; only the new keyed path is strict. Ordering note: today's production callers use the legacy path, so this is not user-visible yet. |
| R2 | The new keyed path is not yet called by Fulfillment | Until a follow-up wires a durable consumer key into Fulfillment, the replay-safety guarantee is available but unused | Explicitly out of scope: the task forbids touching Fulfillment orchestration. The port method is additive so the follow-up is a wiring-only change. |
| R3 | `inventory.reservation_consumptions` grows unbounded | Table growth over time | One row per consumed reservation (~`reservations` cardinality), same order of magnitude as the reservation table itself. Retention/pruning is a follow-up operational concern, not a correctness risk. |
| R4 | Pre-existing EF snapshot drift around `return_restock_inbox` remains | `migrations has-pending-model-changes` still reports changes for the module | Deliberately not fixed here: the scaffolded fixes would rewrite a historical column's precision and rename its PK/index, which is out of task scope and riskier than the drift. Documented in `migration-safety.md` §2. |
| R5 | Concurrent keyed consumers rely on DB row-lock semantics | On a database without working `UPDATE … WHERE` row locks the CAS could race | Proven against real PostgreSQL 16 in Testcontainers (2 racing consumers, both orderings). Production is PostgreSQL; the code path does not assume anything beyond standard `READ COMMITTED` + `UPDATE` lock behaviour. |
| R6 | Consumer key is caller-supplied, not validated against a Fulfillment-side registry | A buggy caller could reuse one key across two reservations | Out of scope (no cross-module registry exists yet). Detection is still safe: the journal is keyed by `reservation_id`, so a reused key on a *different* reservation produces a mismatch conflict rather than a silent double-deduct. |
| R7 | Blank/whitespace consumer key | Would degrade to the unprotected path | Rejected up-front with the typed `inventory.reservation.consume_mismatch` before any write (`Blank_consumer_key_…` test). |

## Non-goals (explicitly untouched)

* No change to Fulfillment package-dispatch orchestration, skip behaviour, or compensation.
* No change to `ReleaseAsync` / reservation-expiry semantics.
* No change to the legacy `ConsumeAsync(Guid, CancellationToken)` signature or its call sites.
* No change to `inventory.reservations`, `inventory.stock_positions` or any other historical table.
* No new error codes; no localization resx change; no W3 catalog change.
* No Host, module-manifest, `.slnx`, or cross-module Contracts change beyond the additive port method.
* No live database migration, deploy, restart, or data mutation.
* No `ARCHITECTURE-FINDINGS-REGISTER.md` entry (opt-in only) and no AMSC recertification claim.
