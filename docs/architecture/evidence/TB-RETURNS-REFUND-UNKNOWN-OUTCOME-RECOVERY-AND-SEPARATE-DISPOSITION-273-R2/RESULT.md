# RESULT — TB-RETURNS-REFUND-UNKNOWN-OUTCOME-RECOVERY-AND-SEPARATE-DISPOSITION-273-R2

Status: **PASS within the issued three-gate repair scope**
Decision: `COURIER_SOURCE_COHERENCE_BEFORE_ANY_EFFECTFUL_PENDING_MONEY_RECOVERY_A_RECORDED_FINAL_MARKER_REQUIRING_ITS_PRIOR_DURABLE_CLOSURE_TIMESTAMPS_AND_AN_EXPLICIT_PROVIDER_ENLISTED_TRANSACTION_REFUSED_BY_THE_SAME_SINGLE_GUARD_ON_THE_EXISTING_RETURNS_DIRECTORY_WITH_ZERO_MIGRATION_AND_ZERO_COMMITTED_ROW`
Stop state: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

NOT Architect ACCEPT, NOT user functional acceptance, NOT any provider/financial/UI claim. The parent 273 and its
R1 repair remain **PARTIAL** until the Architect reviews this result; their reports and the R1 `ARCHITECT-REVIEW.md`
are unchanged. 272-R1 acceptance, the original 272 fixture/delete audit and the shipping acceptances are preserved.
**274–276 remain candidates, NOT ISSUED.** Single worker, one task, no delegation, no historical agent resumed.

| Fact | Value |
| --- | --- |
| Expected base | `02af2261032adf3aeb299fa9c369ee3b5154718d` |
| Actual base at claim | `02af22610` (== local `main` after `git fetch` + `merge --ff-only`; incoming delta = the Architect docs-only review) |
| Code | `2481d40b98182bd17cf72614ff5c45173ef310ad` |
| Evidence | this docs commit |
| Final HEAD / origin parity | recorded in `result-contract.txt` and the Bridge result |

## What changed (three gates only)

### A — courier source coherence is required BEFORE pending money

- `ReturnRecoveryPolicy.Describe` (`ReturnRecoveryDescription.cs` lines 127–139) evaluates the EXISTING
  `HasCoherentCourierSource` rule before constructing `PendingMoney`. A courier case with a missing/empty
  source/receipt/claim/occurrence key, an item receipt/parcel mismatch or an illegal recorded
  condition/disposition is `Blocked` with `return.recovery.evidence_conflict`, **no** `RecoveryKind`, no attempt
  offered, before any gateway/Wallet/Inventory/owning-closure call.
- The predicate is still written once and shared by `MapSnapshotAsync` and `RecoverAsync`; nothing was duplicated in
  the command. A positively bound SUCCEEDED money fact stays `FinancialComplete = true` and blocked — never re-sent.
- A lawful unknown-money courier case still resumes the SAME stored attempt with its SAME key/payment/amount/currency
  after the existing idempotent closure path.

### B — `Settled` requires the prior durable closure timestamps

- `Physical` (`ReturnRecoveryDescription.cs` lines 319–333) now treats a `RefundReleased` marker with a null
  `ObligationClosedAt` or `StockSettledAt` as `return.recovery.evidence_conflict`
  (`FinancialComplete = true`, `PhysicalComplete = false`, no kind) — the same prior facts
  `MarkRefundReleasedWhenFullyDisposedAsync` already requires. No repair/backfill/inference; no new lifecycle stage,
  SLA or window rule.
- Coherent `StockSettled` + both timestamps + all items disposed stays `PendingDisposition` and still completes by
  writing only the final marker with zero money calls; coherent `RefundReleased` + required facts stays `Settled` and
  replays with zero effect.

### C — explicit provider enlistment is refused by the same single guard

- `RefuseConstrainedTransaction` (`ReturnDirectory.cs` lines 685–709) additionally inspects EF's owning transaction
  manager: `_db.Database.GetService<IRelationalConnection>() is RelationalConnection { EnlistedTransaction: not null }`
  (relational provider only). Same typed `return.recovery.transaction_owned`, same three existing call sites, no new
  transaction framework, no reflection, no commit/rollback/dispose of a caller transaction and no tracked-entity
  mutation.
- Proven by ONE new rollback-only real PostgreSQL fact whose pre-action assertions are `CurrentTransaction == null`
  AND `Transaction.Current == null` while explicit enlistment is present; caller enlistment and tracked entities
  survive the refusal; the caller rolls back; a separate read confirms `committedRows = 0`.

## Validation actually executed (once, after the final code change)

1. `dotnet test … --filter "FullyQualifiedName~ReturnsRecovery273Tests"` → **29/29 passed** (24 retained + 5 new).
2. `dotnet test …Tooba.Host.Tests… --filter
   "FullyQualifiedName~ReturnsRecovery273RelationalTests.An_explicitly_enlisted_caller_transaction_refuses_effectful_recovery_before_any_effect"`
   → **1/1 passed**, target `tooba_alpha:5432/172.18.0.2`, `committedRows=0`. The two R1 caller/ambient facts were NOT
   re-run.
3. `dotnet build src/backend/Tooba.slnx -c Release` → **0 errors** (679 warnings, pre-existing analyzer noise).
4. Host publish once → 0 errors; Host restarted alone on 5088 with the current config; `/health` 200,
   `/health/ready` 200 (SingleStore, PostgreSQL configured, messaging Healthy), `/__platform-commerce` →
   `store-alpha`; new Host dll SHA256 `3DA1453E7B8F77A592D1AC5F41E5BA95189A4CEE77423AAB1485FBCD8535C60C`;
   PID 13284.

No whole Returns project, no Host project, no broad `ReturnsModuleAmsc*` filter, no cross-module/parity suite, no
stress run, no detached base comparison. No new error code (both codes are reused), so no catalog/route/validator
count churn was needed and no architecture guard was run.

## Runtime / environment / data boundaries

- ONE canonical `tooba_alpha` `127.0.0.1:5432` SingleStore/store-alpha; Host 5088, FE 3000 untouched.
- No migration/DDL/seed/backfill/cleanup/delete/restore; no connection/edition/ports/appsettings/keys/CapturedPreview
  change; no committed business test row.
- No live approval/reject/refund/recovery mutation, no provider/PSP/Wallet/Inventory/waybill/notification/money
  operation. No user order/draft/history/hold/stock mutation.
- Live Returns table is EMPTY (`POST /v1/admin/returns/query` → `totalCount=0`), so **no live case/served recovery
  walkthrough** is claimed. The read-only wire probes below are real served errors, not recovery proof.

## Served read-only wire (Host 5088, new build)

| Probe | Observed |
| --- | --- |
| `POST /v1/admin/returns/{id}/recover` no actor, `{}` | 401 `admin.actor.missing` |
| with Development actor, `{}` | 400 `return.recovery.kind_invalid` |
| `{"kind":"resume_pending_refund"}` (no attempt id) | 400 `validation.failed` / `returns.validation.recovery_attempt_required` |
| resume with a random attempt id on a missing case | 404 `return.missing` |
| `{"kind":"settle_physical_disposition"}` on a missing case | 404 `return.missing` |
| `POST /v1/admin/returns/query` | 200 `totalCount=0` |

## Proof limits (declared, not papered over)

- Controlled unit proof uses in-memory EF with controlled gateway/Wallet/Inventory ports; it proves the policy and the
  directory behaviour, not a real PSP/Wallet/Inventory effect.
- The explicit-enlistment proof is real PostgreSQL but rollback-only; the case row is intentionally absent at action
  time (its staging transaction was rolled back), which is why the observed `transaction_owned` (not `return.missing`)
  also proves the guard runs before any lookup.
- `NpgsqlConnection.EnlistedTransaction` is internal; a caller that bypasses the owning context and enlists the raw
  `DbConnection` is not separately observable through supported APIs — the supported owning-context path is what the
  guard enforces and what the fact proves.
- The `FOR UPDATE` outcome coordination remains relationally unexercised, as accepted by the Architect; this task adds
  no proof requirement there and did not chase it.
- No live return case exists; no demo case was created.
- The declared R1 broad-suite deviation remains history and was not repeated.

## Declared execution deviation

The focused unit suite had to be executed twice: the first run failed because the newly added test helper used
`Single()` over a two-item courier fixture (a test-harness defect; product code unchanged). The helper was corrected and
the suite then passed 29/29. The final run is the one reported above; nothing else was re-run for reassurance.
