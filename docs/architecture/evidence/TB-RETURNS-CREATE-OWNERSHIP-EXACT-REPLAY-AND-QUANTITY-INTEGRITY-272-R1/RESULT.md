# TB-RETURNS-CREATE-OWNERSHIP-EXACT-REPLAY-AND-QUANTITY-INTEGRITY-272-R1 — RESULT

Status: **PASS within the issued A/B/C repair scope**, decision

`EXACTLY_REPRESENTABLE_QUANTITY_AND_FULL_IMMUTABLE_BINDING_SCOPED_COLLISION_RECOVERY_THAT_NEVER_CLEARS_A_CALLERS_TRACKER_OR_COMMITS_A_CALLERS_TRANSACTION_AN_UNCONDITIONAL_ORDINARY_FACTORY_INVARIANT_AND_A_ROLLBACK_ONLY_RELATIONAL_HARNESS_ON_THE_EXISTING_RETURNS_DIRECTORY_WITH_ZERO_MIGRATION`

NOT Architect ACCEPT, NOT user functional acceptance, NOT any refund/provider/financial claim.
Parent `TB-RETURNS-CREATE-OWNERSHIP-EXACT-REPLAY-AND-QUANTITY-INTEGRITY-272` remains **PARTIAL** until the Architect
reviews this repair; its original RESULT/RECEIPTS/proof and `ARCHITECT-REVIEW.md` are unchanged.
`273–276` remain candidates, **NOT ISSUED**.

| Field | Value |
| --- | --- |
| Expected-Base-SHA (envelope) | `71f5170f7a534b8d54facbcac4475efaa11672b3` |
| Actual work base | `71f5170f7a534b8d54facbcac4475efaa11672b3` (== origin/main at claim, verified ancestor of HEAD) |
| Incoming delta | none |
| Code-SHA | `e2a69f931b03caf9d9fa8189c10ea798359a2960` |
| Evidence-SHA | reported in the canonical Bridge result (this file cannot embed its own commit) |
| Final HEAD / origin parity | reported in the canonical Bridge result |

## A — exactly representable quantity and the full immutable binding

Root repaired: the previous comparison rounded quantity to six decimals **only for replay**, while eligibility,
line snapshots and the aggregate refund amount used the raw input and the column is `numeric(18,6)` with money
`numeric(18,4)`. Two different raw quantities could therefore compare identical (`1.0000001` vs `1.0000002` both
`1.000000`), a tiny positive quantity could persist as zero (`0.0000004`), and the tracked answer of the creation
could differ from a fresh read (`0.0000006`).

- One canonical precision rule: `Tooba.Returns.Domain/ValueObjects/PersistedReturnQuantity.cs` (`Scale = 6`,
  `IsExactlyRepresentable`). No money/currency formula was touched.
- Reachable refusal: `CreateReturnCommandValidator` (`returns.validation.quantity_precision`).
- Owning refusal: `ReturnDirectory.CreateInternalAsync` refuses **before** the stored-key read (so an invalid request
  can never be handed an existing snapshot as success) and `ReturnRequest.Build` refuses again for every line, so no
  future caller can bypass it. Refused attempts write no case, no item, no outbox.
- Typed/localized/catalogued: `return.quantity_precision` added to `ReturnsErrorCodes` (HTTP-reachable), the error
  catalog contributor (HTTP 400, Business), and both `ReturnsErrors.resx` / `ReturnsErrors.fa.resx`.
- Exact comparison can no longer conceal a changed accepted quantity: the accepted input is already exactly the
  persisted value, so `NormalizeQuantity` (round to 6) is a no-op on accepted input and only aligns stored facts.
- `OrdinaryCreationIntent` now also compares `CheckoutId` and `SellerPartyId` from `OrderReturnContextSnapshot`
  beside the existing seller order, owning customer, `Origin = CustomerDelivered`, refund destination and trimmed
  reason. A row carrying another checkout/seller binding refuses with `return.idempotency_conflict`, no payload leak,
  no write; actor authorization still runs before any reply.
- Not done (deliberately): the mutable operational refund destination was **not** promoted to a historical authority,
  no creation metadata/schema was added, and approval behaviour is unchanged.

## B — caller transaction and tracking scope preserved; factory invariant unconditional

Root repaired: the collision catch called `_db.ChangeTracker.Clear()`, which detaches unrelated caller work, and the
ordinary factory publicly exposed `enforceOneLinePerOrderLine: false`.

- `ReturnDirectory.Creation.cs` no longer clears the tracker. It snapshots the tracked entity identities
  (`SnapshotTrackedEntities`) immediately before its own `Add`, and on a collision detaches **only** entries this
  attempt introduced (`DetachFailedCreation`), so a caller's pre-existing entity keeps its instance, state and values.
- Transaction ownership: whoever owned the transaction keeps it. An owning transaction is rolled back by the method
  that opened it; a caller transaction is **never** committed or rolled back — the method creates the savepoint
  `tb272_creation_intent` (`ReturnCreationSerialization.CallerSavepointName`) before its insert, releases it on
  success, and restores it on a key collision so the caller's transaction stays usable. If savepoint recovery is
  impossible the honest provider failure propagates; it is never masked as a business conflict.
- Constraint filtering is unchanged and exact: only SQLSTATE `23505` with
  `returns.return_requests` / `ix_return_requests_idempotency_key` is arbitrated. Any other database fault or
  cancellation propagates canonically.
- `ReturnRequest.Create` (the ordinary public factory) now always enforces one line per order line and the precision
  rule; the public `enforceOneLinePerOrderLine` flag was removed. The courier-origin factory keeps its legitimate
  `(order line, condition)` identity through the same private `Build` path — no courier workflow, claim, stock or
  disposition change, and its recorded per-line condition is now produced by `Build` instead of a
  clear-and-rebuild post-step.

## C — rollback-only relational harness

Root repaired: the previous class committed fake cases on the canonical database and ran `ExecuteDeleteAsync`
cleanup, returned silently when `TB272_CONNECTION` was absent, and only documented the target restriction in a
comment.

- Every fact now runs inside a **caller-owned** transaction that is rolled back on disposal; there is no commit, no
  `ExecuteDeleteAsync`, no `TRUNCATE`, no DELETE, no restore. Each fact finishes by counting this run's tagged rows on
  a separate connection and asserting **0** committed rows (`TB272R1 rollback-only ... committedRows=0`).
- The target is validated before any connection is opened for writing: `TB272_CONNECTION` must be loopback, port
  `5432`, database `tooba_alpha`, and the server must confirm its own database name and port read-only. A missing or
  non-canonical target produces a visible **SKIP** (`SkippableFact`), never a silent pass. Secrets are never printed;
  only the non-secret server identity is recorded.
- Kept rollback-safe real relational proof: independent-connection advisory-lock contention with the contender's
  transaction rolled back before any commit; a real `23505` on the real index with the exact constraint/table/schema
  identity and savepoint recovery; exact index classification vs a real `pk_return_items` violation (not
  reclassified); persisted six-decimal quantity vs the tracked answer plus full binding refusals inside one
  transaction; and a refused creation that leaves no row, keeps the caller's transaction and replays exactly.
- The collision-recovery branch that a rollback-only boundary cannot reach (a same-key winner is only visible to
  another connection after a commit) is reached with a controlled `SaveChangesInterceptor` that raises the exact
  provider shape proven separately against real PostgreSQL — not a fabricated business row.

## Checks actually run once, after the final code change

| Command | Result |
| --- | --- |
| `dotnet test Tooba.Returns.Tests --filter ReturnsCreateIntegrity272Tests` | **18/18 passed** |
| `TB272_CONNECTION`(canonical `tooba_alpha`) + `dotnet test Tooba.Host.Tests --filter ReturnsCreateIntegrity272RelationalTests` | **6/6 passed**, `committedRows=0` in every fact |
| Scoped canonical error-set guards (`Declared_code_catalog...`, Returns+ Settlement `Certification_truth_and_stable_code_split_are_preserved`) | **passed** (counts 23 HTTP-reachable / 1 platform fault / 23 contributor entries) |
| `ReturnsModuleAmsc001W3CertGuardTests` (whole class, run as a side effect of that filter) | 9/10 — the one failure is the **pre-existing** migration-file-list pin `Returns_is_contracts_only_and_microservice_extractable` (pinned list predates the TB-265 migrations; identical at base) |
| `dotnet build Tooba.slnx -c Release` | **0 errors** (warnings only: pre-existing analyzers + offline `NU1900`) |
| `dotnet publish Tooba.Host` | **0 errors** |
| Migration plan (`Tooba.MigrationRunner plan --tenant store-alpha`) | 33 modules, `pendingCount=0` everywhere; Returns head unchanged `20261114170000_AddReturnRequestDecisions` |

## Runtime

- One publish after a graceful stop of the DLL-locking Host; restart of **Host 5088 only** with the existing
  configuration.
- old `artifacts/publish-primary/Tooba.Host.dll` SHA256 `389D5A7AC8D9112059F37626345207B999F083942B31DE6F40EEE1375B760954`
- new `artifacts/publish-primary/Tooba.Host.dll` SHA256 `3244E778B2B24884D85AE4537961105CA97729B0A9B36AF075534D1FEF8BEEEB`
- PID `13160` on `5088`; `GET /health` 200 `{"status":"ok"}`; `GET /health/ready` 200 (SingleStore, postgresql
  configured, messaging healthy); `GET /__platform-commerce` 200 → SingleStore / `store-alpha` / `tenant-alpha` /
  `127.0.0.1` / `dev-singlestore` / `theme-alpha` / `IR`.
- `appsettings*.json` in the published output unchanged (same length and timestamp). Startup log: legacy catalog
  bootstraps skipped, language bootstrap skipped, MassTransit transport schema ensure on `tooba_messaging`
  (pre-existing transport effect) — **no business migration, no seed, no backfill**, zero `Error`/`Critical` lines.
- FE untouched: no FE test, build or restart; `http://127.0.0.1:3000/fa` still 200 on the existing build.

## Data

One canonical `tooba_alpha` at `127.0.0.1:5432` (SingleStore / `store-alpha`). No migration, DDL, committed business
record, cleanup, delete, reseed, reset, backfill or restore. All relational task writes happened inside caller
transactions that were rolled back and were verified gone. No user order/draft/history mutation, no
payment/provider/waybill/notification/money operation.

## Limits (declared, not papered over)

- **The committed winner-vs-loser two-writer race is NOT proven and was not authorized.** The prior 272 committed
  race/cleanup observation remains historical evidence only; it was not re-run and is not repaired retroactively.
- The **owned-transaction replay path** is not executed by the repaired harness: a successful owned-transaction
  creation commits by definition and committing fixture rows is forbidden. Only the caller-owned branch is proven
  end-to-end on real PostgreSQL; the owned branch differs from TB-272 only by the commit call receiving the
  own-transaction handle and by scoped detachment instead of a tracker clear.
- No live return case exists in the canonical database, so there is no served HTTP/UI walkthrough of a real creation.
- Two pre-existing `ReturnsCharacterizationTests` failures (EF-InMemory transaction warning) and the two pre-existing
  AMSC migration-list pins are untouched; the former were not re-run in this round (whole-project run not authorized).
- The scoped guard filter also executed the rest of `ReturnsModuleAmsc001W3CertGuardTests`, which surfaced one of
  those pre-existing pins; it is reported rather than repaired.
- No refund/receipt/reconciliation/rejection-reason/queue/detail/mobile/finance work; the shared ordinary/courier
  physical-capacity redesign stays out of this slice.

## Files changed

- `src/backend/Modules/Returns/Tooba.Returns.Domain/ValueObjects/PersistedReturnQuantity.cs` (new)
- `src/backend/Modules/Returns/Tooba.Returns.Domain/Aggregates/ReturnRequest.cs`
- `src/backend/Modules/Returns/Tooba.Returns.Application/Validation/ReturnsRequestValidators.cs`
- `src/backend/Modules/Returns/Tooba.Returns.Application/Validation/ReturnsValidationCodes.cs`
- `src/backend/Modules/Returns/Tooba.Returns.Contracts/Errors/ReturnsErrorCodes.cs`
- `src/backend/Modules/Returns/Tooba.Returns.Contracts/Resources/ReturnsErrors.resx`
- `src/backend/Modules/Returns/Tooba.Returns.Contracts/Resources/ReturnsErrors.fa.resx`
- `src/backend/Modules/Returns/Tooba.Returns.Infrastructure/Directories/ReturnDirectory.Creation.cs`
- `src/backend/Modules/Returns/Tooba.Returns.Infrastructure/Directories/ReturnCreationSerialization.cs`
- `src/backend/Modules/Returns/Tooba.Returns.Infrastructure/Errors/ReturnsErrorCatalogContributor.cs`
- `src/backend/Modules/Returns/Tooba.Returns.Tests/Behavior/ReturnsCreateIntegrity272Tests.cs`
- `src/backend/Host/Tooba.Host.Tests/ReturnsCreateIntegrity272RelationalTests.cs`
- `src/backend/Host/Tooba.Host.Tests/Architecture/ReturnsModuleAmsc001W1MigrateGuardTests.cs`
- `src/backend/Host/Tooba.Host.Tests/Architecture/ReturnsModuleAmsc001W3CertGuardTests.cs`
- `src/backend/Host/Tooba.Host.Tests/Architecture/ReturnsModuleAmsc001W3R1SetEqualityGuardTests.cs`

STOP_FOR_ARCHITECT; `automaticNextImplementationTask: NONE`.
