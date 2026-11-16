# TB-272 — RESULT

Task: `TB-RETURNS-CREATE-OWNERSHIP-EXACT-REPLAY-AND-QUANTITY-INTEGRITY-272`
Parent: `TB-SHIPPING-EMPLOYEE-WORK-QUEUE-JOB-STAGES-AND-TIME-SCOPES-271-R1`
Channel: `tooba-main`

**Status: PASS within the issued A–D scope**
Decision: `AUTHORIZED_EXACT_CREATE_REPLAY_ONE_LINE_PER_ORDER_LINE_REFUND_FAILED_KEEPING_CAPACITY_AND_ONE_SERIALIZED_CHECK_AND_INSERT_PER_SELLER_ORDER_ON_THE_EXISTING_RETURNS_DIRECTORY_WITH_ZERO_MIGRATION_AND_ZERO_LIVE_BUSINESS_WRITE`

NOT Architect ACCEPT, NOT user visual/functional acceptance, NOT any customer/provider/financial/full-Returns claim.
`273–276` remain roadmap candidates, **NOT ISSUED**. `automaticNextImplementationTask: NONE`.

## Base / commits

| Item | Value |
| --- | --- |
| Expected-Base-SHA | `9d1a64e49102dd4e4429caad987beb7219f070ae` |
| Actual Base-SHA | `9d1a64e49102dd4e4429caad987beb7219f070ae` (ancestor of HEAD and identical to `origin/main` at claim; no incoming delta) |
| Code-SHA | `4d0ba4e169bff5c529ff387509c4fa920a31930f` |
| Evidence | this directory (evidence commit reported in `result-contract.txt` and the Bridge result) |
| Worktree state before coding | clean `main` at the expected base; untracked `scripts/tb266r1-*.mjs` / `scripts/tb270r2-*.mjs` scratch files were already present and were left untouched and uncommitted |

## What changed (A–D)

| Area | State | Consequence |
| --- | --- | --- |
| A Authorized exact replay | implemented | gate → order binding → (customer) ownership → normalized intent → critical section → stored read → exact comparison; a mismatch is `return.idempotency_conflict` with zero writes; a courier-origin key can never answer an ordinary request; exact replay still answers after the window elapsed / with the quantity occupied by that case |
| A Unique-index arbitration | exact identity only | only `23505` on `ix_return_requests_idempotency_key` (`returns.return_requests`) is treated as the key collision; the winner is re-read and passed through the same authorization + intent rule; any other provider failure propagates |
| B One line per order line | two layers, one rule | `CreateReturnCommandValidator` (`returns.validation.duplicate_order_line`) and `ReturnRequest.Create` / `return.duplicate_line`; courier-origin keeps its `(line, condition)` claim identity |
| C RefundFailed capacity | retained | `RefundFailed` now occupies its requested line quantities in the canonical evaluator; only `Rejected`/`Cancelled` release capacity |
| D Atomic check + insert | one transaction per seller order | `pg_advisory_xact_lock` over a deterministic namespaced key inside Returns' own transaction; eligibility re-read **after** the lock; released on commit/rollback/cancellation; no foreign table lock, no foreign-schema SQL, no external effect inside the transaction |
| Errors | two new stable codes | `return.idempotency_conflict` (409) and `return.duplicate_line` (400), catalogued in the module contributor and localized in both cultures; the three architecture pins that enumerate the code set were updated 20→22 / 21→23 (and 20→22 contributor entries) to keep the certified split truthful |
| Schema | unchanged | no migration, no DDL, no backfill, no data fix; the invariant is enforced by the existing unique index + advisory lock |

### Changed paths

```
src/backend/Modules/Returns/Tooba.Returns.Application/Validation/ReturnsRequestValidators.cs
src/backend/Modules/Returns/Tooba.Returns.Application/Validation/ReturnsValidationCodes.cs
src/backend/Modules/Returns/Tooba.Returns.Contracts/Errors/ReturnsErrorCodes.cs
src/backend/Modules/Returns/Tooba.Returns.Contracts/Resources/ReturnsErrors.resx
src/backend/Modules/Returns/Tooba.Returns.Contracts/Resources/ReturnsErrors.fa.resx
src/backend/Modules/Returns/Tooba.Returns.Domain/Aggregates/ReturnRequest.cs
src/backend/Modules/Returns/Tooba.Returns.Infrastructure/Directories/ReturnDirectory.cs            (creation removed → partial file)
src/backend/Modules/Returns/Tooba.Returns.Infrastructure/Directories/ReturnDirectory.Creation.cs   (new)
src/backend/Modules/Returns/Tooba.Returns.Infrastructure/Directories/ReturnCreationSerialization.cs (new)
src/backend/Modules/Returns/Tooba.Returns.Infrastructure/Errors/ReturnsErrorCatalogContributor.cs
src/backend/Modules/Returns/Tooba.Returns.Infrastructure/Evaluators/ReturnEligibilityEvaluator.cs
src/backend/Modules/Returns/Tooba.Returns.Tests/Behavior/ReturnsCreateIntegrity272Tests.cs          (new)
src/backend/Host/Tooba.Host.Tests/ReturnsCreateIntegrity272RelationalTests.cs                       (new)
src/backend/Host/Tooba.Host.Tests/Architecture/ReturnsModuleAmsc001W1MigrateGuardTests.cs
src/backend/Host/Tooba.Host.Tests/Architecture/ReturnsModuleAmsc001W3CertGuardTests.cs
src/backend/Host/Tooba.Host.Tests/Architecture/ReturnsModuleAmsc001W3R1SetEqualityGuardTests.cs
```

16 files, +1605 / −74. No frontend, no contract type removal, no endpoint change.

## Checks actually run (once, after the final change)

| Check | Result |
| --- | --- |
| `dotnet test Tooba.Returns.Tests --filter ReturnsCreateIntegrity272Tests` | **13/13 passed** |
| `dotnet test Tooba.Returns.Tests` (whole project) | 34 total / **32 passed** / 2 failed — both failures are `ReturnsCharacterizationTests` (`Create_approve_refund_preserves_amount_currency_and_is_idempotent`, `Wallet_destination_credits_wallet_without_psp_refund`) failing with `InMemoryEventId.TransactionIgnoredWarning` raised by `ReturnDirectory.RecordReturnDecisionAsync` (TB-265 code, untouched here). Verified **identical at the issued base** in a detached worktree of `9d1a64e49` (3 total / 1 passed / 2 failed, same stack) ⇒ pre-existing. |
| `dotnet test Tooba.Host.Tests --filter ReturnsCreateIntegrity272RelationalTests` with `TB272_CONNECTION` = canonical `tooba_alpha` | **5/5 passed**, including the two-connection lock contention, the 3-round two-writer race and the real unique-index collision |
| `dotnet test Tooba.Host.Tests --filter ReturnsModuleAmsc` | 34 total / **32 passed** / 2 failed — `Migration_did_not_change_the_returns_schema_migrations` and `Returns_is_contracts_only_and_microservice_extractable`, both pinned to a migration file list that predates the TB-265 migrations. Verified **identical at the issued base** (32/34, same two names) ⇒ pre-existing, not introduced. |
| `dotnet build src/backend/Tooba.slnx -c Release` | **0 errors** (warnings only: pre-existing analyzers + offline `NU1900`) |
| Host publish (`Tooba.Host.csproj -c Release -o artifacts/publish-primary`) | **0 errors** |

## Runtime refresh (Host only)

Graceful stop of the DLL-locking process (PID 24008, old `Tooba.Host.dll` SHA256 `E204C33AA3765BD8F6073C30FA96646B433D60154DD621A8919822CC4AE18EDB`), one publish, one detached restart with the existing configuration
(`ASPNETCORE_ENVIRONMENT=Development`, `ASPNETCORE_URLS=http://127.0.0.1:5088`, working dir = published output).

| Fact | Value |
| --- | --- |
| New `Tooba.Host.dll` SHA256 | `389D5A7AC8D9112059F37626345207B999F083942B31DE6F40EEE1375B760954` |
| Listening process | PID 51448 (launcher `cmd.exe` PID 33468) on 5088 |
| `GET /health` | 200 `{"status":"ok"}` |
| `GET /health/ready` | 200 |
| `GET /__platform-commerce` | 200 `edition=SingleStore`, `connectionReference=tenant-alpha`, `tenantId=store-alpha`, `resolvedHost=127.0.0.1`, `deploymentId=dev-singlestore`, `themeReference=theme-alpha`, `defaultMarketReference=IR` |
| Startup effects inspected | catalog legacy bootstraps **skipped** (`RunLegacyBootstraps=false`), language bootstrap skipped (no commerce context at startup, pre-existing wording), only MassTransit's own `transport` schema in `tooba_messaging` created/updated; **no business migration applied, none pending, no seed, no backfill** |
| Configuration | `appsettings.json`, `appsettings.Development.json`, `appsettings.Production.json` in the published output unchanged (same length and timestamp after publish) |
| Frontend | untouched; no FE build/restart (FE unchanged) |

## Data boundaries

One canonical `tooba_alpha` @ `127.0.0.1:5432` (SingleStore / `store-alpha`). No second database, Docker, 15432/15433,
no reset/reseed/backfill/cleanup/restore, no DDL, no provider/PSP/waybill/notification/money operation, no user shipping
draft or completed history mutation.

Rows written by the relational proof were task-labelled and deleted again; the final read-only check reports
`return_requests` liked `tb272-rel-%` = **0** and `return_items` for those = **0**. The committed rows were required by
the two-writer race (a rolled-back transaction is invisible to a competing connection) and this deviation is declared
in `creation-integrity-proof.md` §D rather than hidden.

## Limits (explicit)

- **No live case exists**: `returns.return_requests` is empty in the canonical database, so there is no served
  HTTP/UI walkthrough of a real creation — the owning Directory behavior is proven by the focused suites and the real
  relational suite, not by a live customer click. That is a declared limit, not a substituted fixture.
- The two pre-existing failures in `ReturnsCharacterizationTests` (EF InMemory does not support the TB-265 decision
  transaction) and the two pre-existing `ReturnsModuleAmsc` migration-list pins were **not** repaired: they are outside
  this task's scope and fail identically at the issued base.
- Concurrency is proven with two independent connections/contexts on the real database (including a deterministic
  external lock holder), not with a stress harness.
- No refund execution/retry/reconciliation, receipt/inspection stage, rejection reason persistence, queue/detail/mobile
  or finance work was performed; a shared physical-capacity redesign across the ordinary and courier-origin paths
  remains out of slice and is reported, not implemented.
- Shipping `271-R1` and `270-R4` acceptance and the `R2/R3` PARTIAL history (including R2's missing pre-apply backup)
  are preserved unchanged; nothing in this result reopens or retells them.

## Evidence

- `TASK.md` — the issued envelope, verbatim
- `RESULT.md` — this file
- `RECEIPTS.md` — raw command/result receipts
- `result-contract.txt` — machine-readable contract
- `creation-integrity-proof.md` — A–D mapped to source lines and to the exact tests

STOP_FOR_ARCHITECT; `automaticNextImplementationTask: NONE`.
