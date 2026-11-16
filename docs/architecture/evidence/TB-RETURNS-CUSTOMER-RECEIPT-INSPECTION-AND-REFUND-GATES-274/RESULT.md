# TB-RETURNS-CUSTOMER-RECEIPT-INSPECTION-AND-REFUND-GATES-274 — WORKER RESULT

Status: **PASS within the issued A–E backend scope**
Decision:
`ONE_SHARED_STORED_CUSTOMER_RECEIVING_INSPECTION_AUTHORITY_GATING_EVERY_ORDINARY_MONEY_AND_STOCK_PATH_WITH_CUMULATIVE_PARTIAL_OCCURRENCES_MIXED_USABLE_DAMAGED_PORTIONS_AN_EXACT_OCCURRENCE_REPLAY_AND_A_PERSISTED_WINNING_REJECTION_REASON_ON_THE_EXISTING_RETURNS_DIRECTORY_WITH_ONE_ADDITIVE_MIGRATION_APPLIED_AFTER_A_VERIFIED_PRE_APPLY_BACKUP`
Stop-State: `STOP_FOR_ARCHITECT`
`automaticNextImplementationTask: NONE`

NOT Architect ACCEPT, NOT user functional/visual acceptance, NOT any provider/financial/UI claim.

| Item | Value |
| --- | --- |
| Expected-Base-SHA | `ca2dbed34ba67251dea5b60fd048d1040eef02dd` |
| Base-SHA (actual) | `ca2dbed34ba67251dea5b60fd048d1040eef02dd` (verified ancestor; no incoming delta) |
| Parent | `TB-RETURNS-REFUND-UNKNOWN-OUTCOME-RECOVERY-AND-SEPARATE-DISPOSITION-273-R2` (ACCEPT within its three-gate scope) |
| Mode | `CUSTOMER_DELIVERED_PHYSICAL_RECEIPT_INSPECTION_AND_LAWFUL_REFUND_GATES_BACKEND` |
| Migration | `20261116122318_AddCustomerReceiptsAndDecisionReason` (one cohesive additive Returns migration) |
| Host | 5088, PID `7804`, `/health` 200, `/health/ready` 200 (SingleStore + messaging Healthy) |
| Host dll | `A3A67D8D129A76D6E6BBBB42371CE7E224735436F2A499FC97232FF59DDBD805` |
| FE | untouched (no build/restart/screenshot) |

## Why this increment exists

`CustomerDelivered` return items carried `Condition = Usable` by default and no receiving/inspection fact at all, so
`ApproveAsync` could start money and the shared disposition helper could restock the *requested* quantity as if it had
physically arrived and been inspected. TB-273 made ordinary physical recovery fail closed with
`return.recovery.receiving_required`, but nothing could ever *satisfy* that blocker, and `RejectAsync` silently dropped
the operator's reason. TB-274 adds the owning physical fact, gates every ordinary money/stock path on it, and persists
the winning rejection reason.

## A — Returns-owned customer receive + inspect facts

| Element | Implementation |
| --- | --- |
| Entities | `Tooba.Returns.Domain/Aggregates/ReturnReceipt.cs` (one physical occurrence per case: id, case, occurrence key, actor, recorded time) + `ReturnReceiptLine.cs` (per (item, condition) quantity, owning receipt + case + item) |
| Pure summary | `Tooba.Returns.Domain/ValueObjects/ReturnReceivingSummary.cs` — requested/received/usable/damaged/remaining per requested item, `IsComplete` (coherent **and** every requested unit received) and `IsCoherent` (no foreign line, no over-receive) |
| Command / port | `RecordReturnReceiptCommand` + `IReturnDirectory.RecordReceiptAsync` |
| Wire condition | `ReturnItemConditionParser` — `usable` / `damaged` only; an undefined token is refused, no Unknown→Usable fallback |
| Directory | `ReturnDirectory.Receipts.cs`: `RecordReceiptAsync`, `ReadReceivingAsync`, `EnsureOrdinaryReceivingCompleteAsync` |
| Endpoint | `POST /v1/admin/returns/{returnRequestId:guid}/receipt` (`ReturnAdminEndpoints`) |

Write ordering inside `RecordReceiptAsync` (no external effect anywhere):

1. mutation guard; 2. normalize occurrence key (trim, 1..128, required) and lines (positive, exactly 6-decimal scale,
   defined condition, no duplicate (item, condition) in one submission, non-empty);
3. relational only: open own transaction **and** `SELECT 1 FROM returns.return_requests … FOR UPDATE` on the case row, so
   the cumulative read+insert of the same case serializes;
4. read the case `AsNoTracking`; a courier-origin case is refused (`receipt.origin_invalid`) — Fulfillment owns that
   receipt and no second one is created;
5. exact stored occurrence: same key + same normalized (item, quantity, condition) set answers the stored receipt with no
   new row; a different intent under that key is `return.receipt.conflict`;
6. state gate: a NEW receipt is lawful for `Requested`, and for a money-started case (`Approved`, `RefundProcessing`,
   `RefundFailed`, `Completed`) **only** while no item carries a durable disposition; `Rejected`/`Cancelled` and any case
   with physical history are `return.receipt.state_invalid`;
7. every line must belong to this case (`return.receipt.line_invalid`);
8. cumulative coverage is re-read **after** the lock; `alreadyReceived + submitted > requested` is
   `return.receipt.over_received` with zero writes;
9. insert header + lines, one save, commit, map snapshot. No refund, Wallet, Inventory, hold or status transition.

A mixed item is represented as **two inspected portions of the same requested line** (`Usable` + `Damaged`), never as a
split/duplicated return line, so TB-272's one-line-per-order-line invariant is untouched and the money is never
duplicated.

## B — One shared receiving authority gates every ordinary money/stock path

`ReturnReceivingSummary` is the only authority; it is read by the snapshot projection, the money gates and the physical
settlement, so a read and an action cannot disagree.

| Entry | Gate |
| --- | --- |
| `ApproveAsync` (new decision + attempt + money) | `EnsureOrdinaryReceivingCompleteAsync` before the decision row, the attempt, the gateway and any restock |
| `RetryRefundAsync` (definite-failure new attempt) | same gate before minting a new attempt |
| `RecoverAsync(ResumePendingRefund)` — unknown ordinary money | same complete+coherent evidence before any gateway call; otherwise `return.recovery.receiving_required` with the SAME attempt id/key retained (no new key, no re-key bypass). The policy reports the identical blocker with **no** `RecoveryKind`, so the read never advertises an execution the action refuses |
| `RecoverAsync(SettlePhysicalDisposition)` — ordinary | complete+coherent receipt required; restocks **only** `Usable`, `Damaged` becomes `DamagedNonSaleable` with zero saleable Inventory calls |
| Money already succeeded | stays `FinancialComplete`; never re-sent by any physical intent |
| Courier-origin | untouched: Fulfillment owns its receipt/claim/closure, no second receive, no ordinary release |

Restock identity is the existing per-item key `return-restock-{returnRequestId:N}-{returnItemId:N}`, reused on retry; the
disposition is marked only after Inventory returns, and a failed save reloads only that line.

## C — Rejection reason on the existing winning decision

- `ReturnDecisionRecord.Reason` — nullable `varchar(512)` on `returns.return_request_decisions` (old null stays unknown,
  no backfill).
- `RejectAsync` requires a meaningful trimmed reason at the reachable validator **and** the owning directory; actor, time
  and reason are written atomically with the winning decision row under the case-row lock, before source-claim release.
- Same decision + same normalized reason replays the stored outcome; a changed reason is
  `return.decision.reason_conflict`; an approval never carries a rejection reason; approval/rejection can never be
  converted into one another.
- `ReturnSnapshot.DecisionReason` is exposed separately from the customer's request `Reason`.
- The decision helper's unique-constraint arbitration is narrowed to `pk_return_request_decisions` and its broad catch /
  global tracker clear is replaced by scoped failure handling.

## D — Bounded additive schema + safe apply

Authorized and applied exactly once via the repository `Tooba.MigrationRunner` (`store-alpha`), after a **real**
pre-apply `pg_dump` of the canonical database:

| Item | Value |
| --- | --- |
| Migration | `20261116122318_AddCustomerReceiptsAndDecisionReason` |
| Backup | `D:\temp\tb274-backup\tooba_alpha-pre-tb274.dump`, 2,966,890 bytes, SHA256 `ADCE728BD0D5732C09F5EECDC219287859CB7FE56173F9F5BD07A10BDC64CD16`, taken 2026-11-16 16:13:46 |
| Pre-apply plan | `D:\temp\tb274-backup\plan-pre-apply.json` — the only pending Returns migration in the run |
| Apply log | `D:\temp\tb274-backup\apply.log` — `succeeded: true`, `durationMs: 122`, `pendingMigrations: []` |
| History | `returns.__ef_migrations_history` newest = `20261116122318_AddCustomerReceiptsAndDecisionReason` (7 rows) |
| New tables | `returns.customer_return_receipts`, `returns.customer_return_receipt_lines` — **0 rows** |
| Constraints | PKs, FK receipt→`return_requests`, FK line→receipts (cascade), FK line→`return_items` (restrict) |
| Indexes | unique `(return_request_id, occurrence_key)`; unique `(return_receipt_id, return_item_id, condition)`; supporting item/case indexes |

No DDL outside Returns, no status rewrite, no seed/backfill/cleanup/restore, no destructive `Down`, no second/disposable
database, no committed business row.

## E — Focused proof (once, after the final code change)

| Suite | Result |
| --- | --- |
| `ReturnsCustomerReceipts274Tests` (new) | **25/25** |
| Whole `Tooba.Returns.Tests` (incl. retained 273 recovery + 272 creation + characterization) | **93/93** |
| `ReturnsCustomerReceipts274RelationalTests` on real `tooba_alpha` (rollback-only) | **6/6**, every fact prints target `tooba_alpha:5432/172.18.0.2` and `committedRows=0` |
| `ReturnsModuleAmsc001*` architecture guards | **34/34** |
| `ReturnsRecovery273RelationalTests` (with `TB273R1_CONNECTION`) | **3/3** |
| `dotnet build src/backend/Tooba.slnx -c Release` | 0 errors |
| Host publish | 0 errors |

Served artifact: Host-only restart on 5088 (PID `7804`), `/health` 200, `/health/ready` 200
(`edition SingleStore`, `admin.actor.missing` routes intact). Read-only wire: the new receipt route returns
401 `admin.actor.missing` for an unauthenticated POST, and `POST /v1/admin/returns/query` returns 401 for a missing
actor — no live case, order, hold, stock or money was created or mutated.

## Test pin / harness changes (declared, not hidden)

These are test-only edits made because this increment legitimately changed the pinned facts, plus the two AMSC pins that
were already failing at the base:

| File | Change | Why |
| --- | --- | --- |
| `ReturnsModuleAmsc001W1MigrateGuardTests.cs` | `HttpReachable` pin 31 → **40**; validator count 5 → **6**; `RecordReturnReceiptCommandValidator` added; migration file list now includes the real TB-265/265-R2/274 files | nine new catalogued `return.receipt.*` / `return.decision.*` codes, one new transport validator, one new migration pair — and the migration list pin had been stale/failing at base for the already-shipped 265 files |
| `ReturnsModuleAmsc001W3CertGuardTests.cs` / `W3R1SetEqualityGuardTests.cs` | `ValidatorRequired` + the validator/error-count sets gain `RecordReturnReceiptCommand` | same one-new-request fact |
| `ReturnsCharacterizationTests.cs` | records a real receipt before `ApproveAsync` in its two flows; ignores the in-memory `TransactionIgnoredWarning` | TB-274 now requires the physical fact before money, and the durable decision has been written inside a transaction since 265-R2, which the in-memory provider reports as a warning (this was one of the two pre-existing failures) |
| `ReturnsRecovery273Tests.cs` | ordinary fixtures now seed real receipt evidence; missing-receipt gating is asserted explicitly | retained 273 recovery behaviour is unchanged, but 274 lawfully blocks an unreceived ordinary case |
| `AdminOrderPostDispatchReplayTests.cs` / `MarketplaceOperationalPackingChoice259Tests.cs` | fixture-only adjustments | they consume the Returns aggregate/test builders this increment extended |

No production behaviour was changed to make a test pass, and no assertion was weakened.

## Declared limits

- No DB `CHECK` constraint for positive quantity / defined condition: those are enforced by the owning aggregate
  (`ReturnReceipt.Create`), the persisted six-decimal rule and the unique (receipt, item, condition) index. The database
  enforces PK/FK/unique occurrence only. Declared honestly rather than claimed.
- The receipt state gate intentionally allows a NEW receipt on a money-started case **while no item has a disposition**;
  that is the lawful unblocking path for pre-274 progressed cases. A case with any durable disposition can never gain a
  receipt (double-restock guard).
- Because `ApplyRefundSuccessAsync` settles the physical side right after a confirmed refund, an ordinary case with a
  complete receipt reaches `Settled` in the same action; the `PendingDisposition` ordinary phase is therefore mostly
  reachable for legacy/incomplete-save states (proved by the focused facts, including the lost-save retry).
- Money/stock behaviour is proved with controlled ports (in-memory EF + scripted gateway/Wallet/Inventory), not a real PSP
  or a real Inventory effect. The relational facts are real PostgreSQL but rollback-only.
- Concurrent two-writer receipts are serialized by the case-row `FOR UPDATE`; no committed two-writer race is created or
  required.
- **No live return case exists**, so there is no served operator walkthrough and no demo case/order/hold was created.
  The backend endpoint's availability is explicitly not a UI acceptance (275 owns the operator surface).
- Declared deviation: the new unit suite was executed several times while correcting test-only expectations (the legacy
  fixture lacked its durable decision row, and retained 273 fixtures needed real receipt evidence); only the final passing
  runs are reported.

Worker PASS != Architect ACCEPT. 275 (operator UI/mobile) and 276 (connected acceptance) remain **NOT ISSUED**.
