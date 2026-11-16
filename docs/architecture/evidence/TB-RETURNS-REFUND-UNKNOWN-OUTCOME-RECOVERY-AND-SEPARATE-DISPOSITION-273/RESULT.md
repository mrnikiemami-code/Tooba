# TB-273 — RESULT (worker pass; awaiting Architect review)

Task: `TB-RETURNS-REFUND-UNKNOWN-OUTCOME-RECOVERY-AND-SEPARATE-DISPOSITION-273`
Mode: `SAME_DURABLE_REFUND_ATTEMPT_RECOVERY_AND_SEPARATE_PHYSICAL_DISPOSITION_BACKEND`
Parent: `TB-RETURNS-CREATE-OWNERSHIP-EXACT-REPLAY-AND-QUANTITY-INTEGRITY-272-R1`
Channel: `tooba-main` — Worker `tooba-worker-01` / `cursor`

**Status: PASS within the issued backend scope** — decision
`ONE_PURE_STORED_FACT_RECOVERY_DESCRIPTION_SERVING_BOTH_READ_AND_ACTION_AN_EXPLICIT_ADMIN_CQRS_RECOVERY_INTENT_THAT_REPEATS_THE_EXISTING_PENDING_ATTEMPT_WITHOUT_MINTING_OR_REKEYING_A_SEPARATE_PHYSICAL_DISPOSITION_INTENT_WITH_ZERO_MONEY_CALLS_AND_A_SHORT_OWNING_COORDINATION_THAT_NEVER_SPANS_AN_EXTERNAL_CALL_ON_THE_EXISTING_RETURNS_DIRECTORY_WITH_ZERO_MIGRATION`

This is **NOT** Architect acceptance, **NOT** user acceptance, **NOT** any provider/financial readiness claim and
**NOT** full Returns completion. Backend-only: the operator recovery UI (275), the customer receipt/inspection stage
(274) and 276 remain **NOT ISSUED**.

| Fact | Value |
| --- | --- |
| Expected base | `fcb2fe1cd2325b9378e85e55554bacadcef01b42` |
| Actual base | `fcb2fe1cd2325b9378e85e55554bacadcef01b42` (== `origin/main` at claim) |
| Incoming delta | none |
| Code SHA | `05cafa54aa4e2f9347ff37f4ce7295cfab1b12b2` |
| Evidence commit / final HEAD / origin parity | reported in the canonical Bridge result |
| Schema | unchanged — no migration, no DDL, no backfill |
| Data | one canonical `tooba_alpha`; no business write, no provider/money call |

## What the increment actually is

A case can reach an approved-but-unfinished state in two independent ways, and the previous API could not express
either of them:

1. **Money is unknown.** The owning refund attempt is still `Pending` (transport failure, timeout, or an adapter that is
   explicitly not configured), the case is `RefundProcessing`, and the only existing resume path existed inside the
   courier-origin approval flow. `retry_refund` deliberately mints a **new** attempt and is/was reserved for a
   definite `RefundFailed`.
2. **Money is confirmed but the physical units are not settled.** A stock failure after a successful refund was only
   logged; the disposition-only retry was special-cased to completed courier-origin cases and had no read fact.

TB-273 adds exactly one stored-fact description and one explicit intent per owner phase, on the existing directory.

## A — one authoritative recovery description (owned by Returns)

`ReturnRecoveryPolicy.Describe(ReturnSnapshot)` is a pure function over stored facts. It is the only place that decides
what is recoverable, and it is called from two places: the recovery command (to decide executability) and
`ReturnDirectory.MapSnapshotAsync` (so every read carries the same description).

| Phase | Stored shape | Recovery kind | Blocker |
| --- | --- | --- | --- |
| `None` | `Requested` / `Rejected` / `Cancelled` | — | — |
| `PendingMoney` | `Approved`/`RefundProcessing` + exactly ONE `Pending` attempt, no `Succeeded` | `resume_pending_refund` + that exact attempt id | — |
| `Blocked` | same status, zero or several pending attempts | — | `return.recovery.attempt_unbound` |
| `FailedMoney` | `RefundFailed` | — (the separate `retry_refund` intent owns a NEW attempt) | — |
| `PendingDisposition` | `Completed` + bound `Succeeded` + a line with `Disposition=None`, courier origin | `settle_physical_disposition` | — |
| `Blocked` | same, `CustomerDelivered` origin | — | `return.recovery.receiving_required` |
| `Settled` | `Completed` + `Succeeded` + every line disposed | — | — |
| `Blocked` | `Completed` without a bindable succeeded attempt (`!moneySucceeded` or a pending attempt) | — | `return.recovery.attempt_unbound` |

`FinancialComplete` and `PhysicalComplete` are two independent booleans, so a consumer can never read "refunded" as
"all received units settled". Computing the description performs no provider call, no retry, no restock and no write.

Contracts are additive only: `ReturnRecoverySnapshot` (+ `ReturnRecoveryPhaseOption`, `ReturnRecoveryKindOption`) and a
trailing nullable `ReturnSnapshot.Recovery`. No existing numeric enum value or shipped wire member changed, and no
executable menu verb was published to a frontend (275 owns consumption).

## B — the explicit admin same-attempt recovery command

- `RecoverReturnCommand` → `RecoverReturnHandler` (`ReturnsOperation.ExecuteAsync` → `Result`).
- `RecoverReturnCommandValidator`: `returns.validation.recovery_kind_invalid` (enum member) and
  `returns.validation.recovery_attempt_required` (a money intent must name the attempt; a supplied id must not be empty).
- `POST /v1/admin/returns/{returnRequestId:guid}/recover`, inside the existing admin route family, with the existing
  `IReturnAdminAuthorizer` (the actor is resolved on **every** call, including a recorded completion) and the existing
  `ApiResponseFactory`.
- Wire body is `{ kind, expectedRefundAttemptId }` only. Payment, amount, currency, destination and customer are never
  accepted from the client.

Money intent, in order:

1. `_guard.EnsureCanMutateAsync` — a closed/unauthorized seam refuses before any read or effect.
2. Load the case; a durable decision row that is not `Approve` answers `return.already_rejected`.
3. Build the same `ReturnSnapshot` the reads use and describe it. `Settled` is answered from stored state, zero effect.
4. Missing/empty/wrong attempt id → `return.recovery.attempt_mismatch`; a non-`Pending` attempt is answered from the
   stored row (never re-sent); a phase that is not `PendingMoney` for this exact id → `return.recovery.state_mismatch`;
   case/attempt payment, amount or currency incoherence → `return.recovery.attempt_mismatch`.
5. Courier origin: re-read the existing receipt/claim/closure sequence idempotently (creates nothing new).
6. Call the owning adapter with the attempt's stored `IdempotencyKey` / `PaymentId` / `Amount` / `Currency` and the
   case's fixed destination.

| External answer | Classified as | Effect |
| --- | --- | --- |
| typed `payment.refund.gateway.unconfigured` | `Unavailable` | `return.recovery.gateway_unavailable` (503); attempt stays `Pending`, key intact, nothing written |
| success | `Resolved` | durable `Succeeded` + real provider reference, written through the owning coordination |
| definite refusal | `Resolved` | durable `Failed` with the typed failure code |
| timeout / transport / 5xx / unknown | `Unresolved` | attempt stays `Pending`, same key, no synthetic reference, no definite failure |

Wallet destinations reuse the existing Wallet port and the case key `wallet-refund-credit:{caseId}` with zero PSP calls;
`OriginalPayment` never falls back to Wallet because the destination is read from the stored case.

## C — physical disposition is a different intent

`settle_physical_disposition` makes **zero** refund/Wallet calls and creates **zero** attempts. It refuses while the
money is not confirmed (`return.recovery.state_mismatch`). Courier-origin re-reads its existing receipt/claim/closure
evidence idempotently, reuses the existing per-item restock identity, keeps `DamagedNonSaleable` for damaged units and
then records `MarkRefundReleasedWhenFullyDisposed`. `CustomerDelivered` has no receiving/inspection fact today, so the
intent is refused with `return.recovery.receiving_required` and **nothing** is restocked; the confirmed money stays.

`SettlePhysicalDispositionAsync` gained one explicit boundary, `allowOrdinaryDisposition`, and the recovery intent
always passes `false` for an ordinary RMA. A recovery route therefore cannot reach the legacy unconditional ordinary
restock; the approve and terminal-retry paths keep their previous default.

## D — durability, lost and late answers

- The external call happens with **no** transaction and **no** row lock held.
- The outcome is applied afterwards in `RecordRefundOutcomeAsync`: it opens a short transaction only when it owns one,
  takes `SELECT 1 FROM returns.refund_attempts … FOR UPDATE` (own schema), `Reload`s both the attempt and the case, and
  returns `false` for a stale answer when a terminal status is already durable.
- The caller's transaction is never committed or rolled back; there is no new `DbUpdateException` catch and no
  `ChangeTracker.Clear()`.
- A local `SaveChanges` failure propagates: an external success whose write did not land is never answered as durable
  money, and the next call reconciles the **durable** attempt in a fresh request scope.
- Approve-vs-reject authority is unchanged; a rejected, cancelled or unapproved case cannot gain a recovery action and
  there is no first/latest attempt fallback anywhere.

## Changed surface (24 files, +1884 / −49)

New: `ReturnRecoveryDescription.cs` (policy + phase/kind), `RecoverReturnCommand.cs`, `ReturnRecoveryKindParser.cs`,
`ReturnRecoveryContracts.cs`, `ReturnDirectory.Recovery.cs`, `ReturnsRecovery273Tests.cs`.
Modified: `ReturnDirectory.cs` (refund execution outcome, owning coordination, snapshot recovery projection),
`ReturnDirectory.CourierOrigin.cs` (disposition boundary), `ReturnAdminEndpoints.cs`, `ReturnAdminOperationsAdapter.cs`,
`ReturnsErrorCodes.cs` (+6 codes), `ReturnsErrorCatalogContributor.cs`, both `ReturnsErrors*.resx`,
`ReturnsValidationCodes.cs`, `ReturnsRequestValidators.cs`, `IReturnDirectory.cs`, `ReturnSnapshot.cs`,
`ReturnAdminOperationsContracts.cs`, three AMSC guard pin sets, and two Order test doubles implementing the new
Contracts member.

## Verification (once, after the final change)

| Check | Result |
| --- | --- |
| `ReturnsRecovery273Tests` | **15/15 passed** |
| `Tooba.Returns.Tests` whole project *(disclosed: broader than the named suite)* | 52 passed / **2 pre-existing failures** |
| Host `ReturnsModuleAmsc*` filter *(disclosed: broader than the named suite)* | 32 passed / **2 pre-existing pins** |
| `dotnet build Tooba.slnx -c Release` | **0 errors** (293 pre-existing warnings) |
| `dotnet publish` Host | exit 0 (1 pre-existing CS0105 warning) |
| Host 5088 (`/health`, `/health/ready`, `/__platform-commerce`) | 200 / 200 SingleStore / 200 `store-alpha` |

The two Returns test failures are `ReturnsCharacterizationTests.Create_approve_refund_preserves_amount_currency_and_is_idempotent`
and `...Wallet_destination_credits_wallet_without_psp_refund`, both failing on the EF-InMemory
`TransactionIgnoredWarning` raised by `RecordReturnDecisionAsync` — the same pre-existing pair recorded for 272/272-R1,
left untouched. The two guard failures are the pre-existing migration-file-list pins
(`Returns_is_contracts_only_and_microservice_extractable`, `Migration_did_not_change_the_returns_schema_migrations`);
the route/validator/error-catalog count pins updated for this task pass.

## Live read-only receipts (no business write)

Host refreshed: old dll `3244E778…BEEEB` → new dll
`2E80BFFBB3D595A685430382F00ACFC61D09A13A9636C469CC2D963D4AD4802A`, listener PID 28580.

- No dev actor → **403** `admin.authorization.denied` (authority before any read/effect; the route exists, not 404).
- `{}` / `{"kind":"not_a_kind"}` → **400** `return.recovery.kind_invalid`.
- `{"kind":"resume_pending_refund"}` → **400** `validation.failed` with
  `ExpectedRefundAttemptId: returns.validation.recovery_attempt_required`.
- `{"kind":"resume_pending_refund","expectedRefundAttemptId":<random>}` → **404** `return.missing`.
- `{"kind":"settle_physical_disposition"}` → **404** `return.missing`.
- `POST /v1/admin/returns/query` → **200** `{"items":[],"page":1,"pageSize":5,"totalCount":0}`.

## Limits (declared, not papered over)

- The live Returns table is empty (`totalCount=0`), exactly as the envelope pre-declared: there is **no** served
  operator walkthrough of a real recovery. The wire receipts above are real but read-only; the effects are proven by
  the focused suite.
- Money/stock behaviour is proven with controlled, task-owned scripted ports plus a fault-injected local save failure;
  no real PSP, Wallet or Inventory operation occurs and no "one HTTP provider call" property is claimed (same-key
  concurrent recoveries may each call the owning gateway, which its idempotency must fold).
- Concurrency is proven by re-read + monotonic application with a stale-answer case, not a stress harness.
- No relational test was added (the envelope permits them only when needed), so the `FOR UPDATE` branch is exercised
  only by the running relational Host, not by an automated rollback-only fact.
- Two broader-than-named suites were run once each and are disclosed above; no whole-solution test run was performed.
- `ExecuteRefundAsync` now short-circuits **any** non-`Pending` attempt (previously only `Succeeded`). This was
  directly necessary so a definite failure cannot be re-sent by a same-intent resume; the separate terminal
  `retry_refund` new-attempt intent is unchanged and is **not** audited by this task.
