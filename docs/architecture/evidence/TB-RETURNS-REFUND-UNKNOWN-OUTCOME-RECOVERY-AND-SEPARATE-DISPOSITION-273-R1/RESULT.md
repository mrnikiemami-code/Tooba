# RESULT — TB-RETURNS-REFUND-UNKNOWN-OUTCOME-RECOVERY-AND-SEPARATE-DISPOSITION-273-R1

Task: `TB-RETURNS-REFUND-UNKNOWN-OUTCOME-RECOVERY-AND-SEPARATE-DISPOSITION-273-R1`
Parent: `TB-RETURNS-REFUND-UNKNOWN-OUTCOME-RECOVERY-AND-SEPARATE-DISPOSITION-273` (stays **PARTIAL**)
Channel: `tooba-main` · Worker: `tooba-worker-01` · Agent: `cursor` · Program: TOOBA
Mode: `REPAIR_RECOVERY_EVIDENCE_CLOSURE_TRANSACTION_BOUNDARY_AND_TRUTHFUL_REFERENCE`
Status: **PASS within the issued A–D repair scope** · Stop state: `STOP_FOR_ARCHITECT`

Decision:

`ONE_FAIL_CLOSED_STORED_EVIDENCE_POLICY_SHARED_BY_READ_AND_ACTION_A_RECORDED_APPROVAL_AND_A_FULLY_BOUND_ATTEMPT_REQUIRED_AN_EXPLICIT_PENDING_FINAL_MARKER_PHASE_A_CALLER_OR_AMBIENT_TRANSACTION_REFUSED_BEFORE_ANY_EFFECT_AND_A_TRUTHFUL_ABSENT_PROVIDER_REFERENCE_ON_THE_EXISTING_RETURNS_DIRECTORY_WITH_ZERO_MIGRATION`

**Worker PASS is not Architect ACCEPT.** Backend only. No user functional/visual acceptance, no provider/PSP/Wallet
money, no stock mutation, no live case execution.

## Base / code

| Item | Value |
| --- | --- |
| Expected base | `1e4e7cb677c25f2746a0a5b4c870808015aa95fc` |
| Actual base (`git rev-parse HEAD` before any edit) | `1e4e7cb677c25f2746a0a5b4c870808015aa95fc` (equal; ancestor check trivially satisfied) |
| Incoming delta | none — working tree was clean except the three pre-existing untracked `scripts/tb266r1-*.mjs` / `scripts/tb270r2-*.mjs` scratch files, deliberately preserved and not staged |
| Code SHA | `150179bbc6acae82d915c1919f3fd8cfc4884a93` |
| Schema | **no migration, DDL, backfill, seed, restore or cleanup** (see RECEIPTS §Schema) |

## A — One fail-closed stored evidence rule, including the last closure step

| Required | Delivered |
| --- | --- |
| One pure policy, read and command must not diverge | `ReturnRecoveryPolicy.Describe(ReturnSnapshot)` is still the **only** rule. `MapSnapshotAsync` (every admin/customer/seller/detail read) and `ReturnDirectory.RecoverAsync` both call it; `RecoverAsync` no longer keeps its own permissive decision read |
| Effectful recovery requires a **recorded** APPROVE | `ReturnSnapshot.RecordedDecision` (trailing nullable member, populated by one `AsNoTracking` read of `returns.return_request_decisions`). `ReadMoney` returns `RecoveryEvidenceConflict` for a missing row and `AlreadyRejected` for `Reject`. No decision/approval is created or repaired anywhere |
| Attempt must bind to the case | `IsBoundToCase` requires non-empty `RefundAttemptId`, non-blank idempotency key, a non-zero case `PaymentId`, equality of payment id, **exact** amount, and case-insensitively trimmed currency. Foreign live rows ⇒ `RecoveryAttemptUnbound`; ≥2 live bound rows or succeeded+pending ⇒ `RecoveryAttemptUnbound`; no "first/latest" fallback |
| Requested/Rejected/Cancelled with attempts are inconsistent | described `Blocked(RecoveryEvidenceConflict)`, never `None`; both action intents refuse with that same code |
| Wrong/missing expected attempt cannot bypass validation because the case looks settled | `RecoverPendingRefund` answers the stored snapshot **only** when the requested id equals the policy's single bound attempt id; otherwise `RecoveryAttemptMismatch`; the `Phase == Blocked` check runs **before** the pending check, so a contradictory case can never be resumed |
| Completed + foreign/mismatched succeeded attempt must never authorize Inventory | `ReadMoney` classifies the succeeded row as foreign ⇒ `Blocked`, `FinancialComplete = false`; `RecoverPhysicalDisposition` refuses on `Blocked` before `SettlePhysicalDispositionAsync` |
| Preserve positively bound confirmed money while other work is blocked | the same `MoneyEvidence.AttemptId` is carried into every blocker, `FinancialComplete: true` is kept where the money is genuinely bound (receiving-required, marker-pending) and never re-sent |
| Courier source coherence | `HasCoherentCourierSource`: non-empty fulfillment/shipment/receipt/claim ids + occurrence key, every item pointing at that same receipt+shipment, and condition-legal disposition (`Damaged` ⇒ `None|DamagedNonSaleable`, otherwise `None|SaleableRestock`). Incoherent ⇒ `Blocked(RecoveryEvidenceConflict)` with `FinancialComplete: true` and zero stock calls |
| Distinguish durable dispositions + pending final marker from Settled | `Physical()` requires `allItemsDisposed && ClosurePhase == RefundReleased` for `Settled`; all items disposed with `StockSettled` (or an earlier marker) is `PendingDisposition` with `RecoveryKind = SettlePhysicalDisposition` |
| Marker-pending state remains recoverable with zero money | `RecoverPhysicalDispositionAsync` runs `EnsureCourierOriginClosureAsync` (a no-op when the closure markers are already durable), `SettlePhysicalDispositionAsync` (skips every disposed line) and then writes only the final marker, which now saves itself through `MarkRefundReleasedWhenFullyDisposedAsync` |
| Do not invent receiving evidence for an ordinary legacy case | ordinary origin with any `None` line stays `Blocked(RecoveryReceivingRequired)`; nothing is restocked |
| No new table/state engine; no sensitive decision metadata | one nullable `RecordedDecision` string on the internal snapshot (the exact `"Approve"`/`"Reject"` token only). No enum value, wire member or numeric value changed |

## B — No caller/ambient transaction may span an external effect

- `RefuseConstrainedTransaction()` rejects `_db.Database.CurrentTransaction is not null` **or**
  `System.Transactions.Transaction.Current is not null` with the new typed code
  `return.recovery.transaction_owned`.
- It is enforced at **three** places, all **before** any effect:
  1. `RecoverAsync` entry — before the mutable case read and before any closure/gateway/Wallet/Inventory step;
  2. `ExecuteRefundAsync` — the **shared external-call boundary**, so a future direct caller cannot bypass it;
  3. `RecordRefundOutcomeAsync` — before its own `FOR UPDATE` transaction is opened.
- Nothing commits, rolls back or disposes the caller's transaction, and no entity is mutated before the refusal; the
  relational facts assert the caller transaction is still open and the tracked attempt keeps `Unchanged`/`Pending`.
- Short owning coordination is unchanged: `FOR UPDATE` + `Reload` + apply-only-from-`Pending`, its own transaction,
  disposed in `finally`. No transaction, row lock or lease spans a gateway/Wallet/Inventory call.
- No distributed transaction, lease table, daemon or generic UoW was added.

## C — Truthful reference and durable physical flags

- `RefundAttempt.MarkSucceeded(string? providerReference, …)` stores `null` for null/blank input (trimmed otherwise);
  the synthetic `refund-{attemptId:N}` fallback is **gone**. `ApplyRefundSuccessAsync` passes
  `GatewayRefundResult.ProviderReference` through unchanged, so the authoritative success boolean still records a
  success with a truthfully absent reference. Wallet's own `wallet-refund:{caseId}` label is untouched and is never
  presented as a PSP reference.
- `MarkRefundReleasedWhenFullyDisposedAsync` (replacing the in-memory-only method) does a **durable re-read** of
  `return_items.disposition` and of `ClosurePhase/ObligationClosedAt/StockSettledAt` before writing the final marker,
  saves it itself, and on failure `RestoreTrackedAsync` (a scoped `EntityEntry.ReloadAsync`, detaching only if the
  store cannot be re-read) — there is no `ChangeTracker.Clear()`.
- `ReturnDirectory.CourierOrigin.cs` item-save failure paths now reload **only** the affected line instead of flipping
  its state to `Unchanged`, so an unsaved `SaleableRestock` can never be promoted by a same-scope retry.
- Per-item Inventory identity, `DamagedNonSaleable` semantics and "damaged is never saleable" are unchanged.

## D — Focused regression proof

`ReturnsRecovery273Tests` **24/24** (15 retained + 9 new R1 facts), through the real `ReturnDirectory`/handler:

1. `A_case_without_a_recorded_approval_is_blocked_in_read_and_refused_with_zero_effect` — positive fixtures now carry a
   real `ReturnDecisionRecord`; dropping it blocks read **and** action with zero calls.
2. `An_opposite_recorded_decision_is_blocked_and_never_refunded`,
   `A_terminal_status_holding_a_live_attempt_is_blocked_not_quietly_empty`.
3. `A_succeeded_attempt_that_does_not_bind_to_the_case_never_authorizes_money_or_stock`,
   `A_pending_attempt_with_a_mismatched_amount_is_blocked_in_read_and_action` — same read blocker and zero effect.
4. `Every_item_disposed_with_only_the_final_marker_pending_is_recoverable_with_zero_money` — marker-only completion,
   0 gateway/Wallet/Inventory calls, 1 attempt, settled replay zero-effect.
5. `A_lost_item_save_never_promotes_an_unsaved_disposition_or_double_restocks` and
   `A_lost_final_marker_save_never_reports_settled_and_the_same_scope_retry_completes_it` — durable truth checked
   through a **separate** `DbContext`; the same scope retries without trusting an unsaved flag.
6. `A_blank_provider_reference_is_stored_truthfully_and_never_invented` — `ProviderReference` stays `null`, 1 gateway
   call after a repeat, 1 attempt.
7. Retained same-key unknown/timeout/unconfigured/Wallet/physical-only facts still pass unchanged.

`ReturnsRecovery273RelationalTests` — **2/2 on the real canonical database**, rollback only:
caller-owned and ambient `TransactionScope` both refused with `return.recovery.transaction_owned`, 0 external calls,
caller transaction still current, tracked attempt `Unchanged`/`Pending`, `committedRows=0` in both facts.
Target validated before use: client loopback + 5432 + `tooba_alpha`, and the server confirmed
`tooba_alpha:5432/172.18.0.2` (private container address).

## Validation (once after the final code change)

| Check | Result |
| --- | --- |
| `dotnet test … --filter FullyQualifiedName~ReturnsRecovery273Tests` | **24 passed / 0 failed** |
| `dotnet test … --filter FullyQualifiedName~ReturnsRecovery273RelationalTests` | **2 passed / 0 failed**, `committedRows=0` |
| `dotnet build src/backend/Tooba.slnx -c Release` | **0 errors** (369 warnings, pre-existing) |
| `dotnet publish …Tooba.Host.csproj -c Release -o artifacts/publish-primary` | **0 errors**; dll SHA256 `FAF2D791DAFB1C55A33E5C5A533AEC9B7061194336ABA9E7A75BB274F630E440` |
| Host 5088 | PID **37876** listening, `/health` 200, `/health/ready` 200 (`SingleStore`, postgresql configured), `/__platform-commerce` → tenant `store-alpha`, edition `SingleStore` |
| FE | untouched — no build, no test, no restart, no screenshots |

Read-only wire (the Returns table is empty, so **no real recovery can be walked through**):

| Probe | Answer |
| --- | --- |
| `POST /v1/admin/returns/<random>/recover` no actor header, `{}` | 401 `admin.actor.missing` |
| same with `X-Tooba-Dev-Actor-User-Id`, `{}` | 400 `return.recovery.kind_invalid` |
| `{"kind":"not_a_kind"}` | 400 `return.recovery.kind_invalid` |
| `{"kind":"resume_pending_refund"}` | 400 `validation.failed` / `returns.validation.recovery_attempt_required` |
| `{"kind":"resume_pending_refund","expectedRefundAttemptId":"<random>"}` | 404 `return.missing` |
| `{"kind":"settle_physical_disposition"}` | 404 `return.missing` |
| `POST /v1/admin/returns/query {"page":1,"pageSize":5,"filters":[],"sort":[]}` | 200 `totalCount=0` |

## Deviations and known limits (declared, not papered over)

- **Out-of-scope suite runs happened.** After the focused suite, the worker ran the whole `Tooba.Returns.Tests`
  project (**60 passed / 2 failed**, both pre-existing `ReturnsCharacterizationTests` EF-InMemory transaction
  failures) and the `ReturnsModuleAmsc001*` architecture filter (**32 passed / 2 failed**, both pre-existing
  migration-file list pins: `W1MigrateGuardTests.Migration_did_not_change_the_returns_schema_migrations` and
  `W3CertGuardTests.Returns_is_contracts_only_and_microservice_extractable`, whose pinned expected list predates the
  TB-265 migrations that are already in the base). The task forbade whole-module/AMSC runs: this is a **worker scope
  deviation**, disclosed; no repair, pin relaxation or baseline change was made, and nothing was fixed from those
  runs. The whole-project number was taken **before** the last added marker-save fact, so it reads 60/2 rather than
  61/2.
- `return.recovery.transaction_owned` is catalogued + localized and classified with the other HTTP-reachable Returns
  codes, but the shipped admin endpoint opens no transaction of its own, so it is **not observable over HTTP today**:
  the refusal is proven at the directory boundary by the two rollback-only relational facts, not by a wire probe.
- The relational facts prove the **transaction refusal** on real PostgreSQL. They do **not** exercise the
  `FOR UPDATE`/reload/apply branch: that needs a committed case row, which the rollback-only boundary forbids. The
  outcome coordination itself is covered by the controlled-ports facts only.
- No live return case exists (`totalCount=0`), so there is no served operator walkthrough of a real recovery. The wire
  receipts above are genuine but read-only, and a validation/missing-id POST is not a successful recovery.
- Money and stock claims rest on controlled ports with an injected local-save failure; no PSP, Wallet, Inventory,
  waybill or notification operation was performed and no money or stock moved anywhere.
- Concurrency is a re-read/monotonic proof, not a stress harness; no two-writer demand was in scope.
- `RecoveryTransactionOwned`/`RecoveryEvidenceConflict` changed the three exact catalog pins (31 HTTP / 1 platform /
  32 total); only those three classes were touched, with their comments.

`automaticNextImplementationTask: NONE`
