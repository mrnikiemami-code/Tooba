# TB-P0-INVENTORY-ATOMIC-HOLD-EXTENSION-017 — analysis

Program: TOOBA-CHECKOUT-PAYMENT-COMMIT-BOUNDARY (parent TB-P0-CHECKOUT-TRANSACTION-BOUNDARY-016)
Base: `08338e8647eca4b0cac3b5d43486fc6d436c489d` (HEAD == origin/main verified before work)
Mode: BOUNDED_IMPLEMENTATION_OR_BLOCKED

## 1. What was built

One Inventory-owned building block: an atomic, all-or-none extension of the expiry of *existing*
`Held` reservations, to be called by a future, separately authorized payment-initiation orchestration.

- `IInventoryPaymentHoldExtensionPort.ExtendHeldForPaymentAsync(ids, deadlineUtc, ct)` in
  `Tooba.Inventory.Contracts/Orders`.
- Implementation as a cohesive partial of the Inventory-owned `InventoryDirectory`
  (`Directories/InventoryDirectory.HoldExtension.cs`), registered in `InventoryModule`.
- The 015 verification primitive (`IInventoryPaymentHoldGate` / `VerifyHeldForPaymentAsync`) is
  **untouched**: no signature, semantics, outcome or registration change, and its evidence and guard
  tests still pass unmodified.

This is a building block, **not** payment authorization. It does not initiate payment, does not call a
PSP, does not debit a wallet and does not mutate Order/Cart. It also does not claim ownership: it does
**not** assert that the supplied reservation ids belong to any checkout. Order must later supply
authoritative, actor-checked, checkout-bound ids plus the deadline derived from the effective
configured policy.

## 2. Transaction and locking behaviour (the core of the task)

Exactly one local Inventory-owned PostgreSQL transaction and connection per call:

1. Refuse an ambient scope **before** opening anything: `Transaction.Current is not null` returns
   `AmbientTransactionUnsupported`. 016 proved that a `TransactionScope` spanning several module
   DbContexts promotes to two-phase commit and fails on PostgreSQL
   (`55000 prepared transactions are disabled`), so refusing is the fail-closed behaviour.
2. `_db.Database.BeginTransactionAsync()` — EF opens the connection and owns it; the method never calls
   `OpenConnectionAsync` and never changes connection ownership by hand.
3. `SELECT r.reservation_id … WHERE r.reservation_id = ANY(@ids) AND r.status = 'Held' AND
   (r.expires_at IS NULL OR r.expires_at > clock_timestamp()) ORDER BY r.reservation_id FOR UPDATE`.

   * Deterministic `ORDER BY reservation_id` lock order avoids deadlock with the reclaimer, whose
     `FOR UPDATE SKIP LOCKED` only ever targets already-expired rows.
   * `FOR UPDATE`, never `SKIP LOCKED`: the extension must **wait** for a competing writer rather than
     silently skip a row.
   * Validity is measured with `clock_timestamp()` (database time), not with an app clock.
4. All-or-none gate: if the locked live set is smaller than the requested distinct set, the missing /
   `Released` / `Consumed` / expired / `Guid.Empty` ids are reported and the transaction is
   **rolled back** — no row is touched.
5. Deadline gate: `SELECT clock_timestamp()` and reject `deadline <= databaseNow` with
   `DeadlineInvalid` after rollback. `deadline == default` is rejected before any database access.
6. Update: `expires_at = CASE WHEN expires_at IS NULL THEN NULL ELSE GREATEST(expires_at, @deadline) END`,
   `updated_at = @updatedAt` (module clock), re-checking `status = 'Held'` and
   `expires_at IS NULL OR expires_at > clock_timestamp()`, with `RETURNING expires_at`.

   * `NULL` (durable) is preserved as `NULL` and is never given a deadline.
   * Finite expiries only ever move forward: replay is monotonic and can never shorten a hold.
   * A `Released`/expired hold is never revived (status and unexpired predicate re-checked).
   * `GREATEST` is intentionally not applied directly to `NULL` (PostgreSQL `GREATEST` ignores NULLs),
     hence the explicit `CASE`.
7. If the updated row count does not equal the requested distinct count, roll back and return
   `NotHeld` (defensive; unreachable while the rows stay locked).
8. `CommitAsync()`; the transaction is disposed by `await using`, so **no row lock survives the method
   return**, and EF closes the connection it opened.

Result: `InventoryPaymentHoldExtensionResult(Outcome, RequestedCount, GuaranteedUntil,
UnavailableReservationIds)` with `Outcome ∈ {Extended, NotHeld, DeadlineInvalid,
AmbientTransactionUnsupported}` and `AllHeldAndExtended` true only for `Extended`. `GuaranteedUntil` is
the weakest guarantee of the set (earliest finite resulting expiry), `null` when every reserved hold is
durable.

Deadline policy: caller-supplied absolute instant, normalized with `ToUniversalTime()` ("rigorously
validated equivalent" of UTC). No duration is hardcoded anywhere; no store/offer/category/provider
setting or limit was changed.

## 3. Changed files

| File | Change |
| --- | --- |
| `src/backend/Modules/Inventory/Tooba.Inventory.Contracts/Orders/InventoryPaymentHoldExtensionContracts.cs` | New. `InventoryPaymentHoldExtensionOutcome`, `InventoryPaymentHoldExtensionResult`, `IInventoryPaymentHoldExtensionPort` (109 lines). |
| `src/backend/Modules/Inventory/Tooba.Inventory.Infrastructure/Directories/InventoryDirectory.HoldExtension.cs` | New cohesive partial, 243 lines (ceiling 500). |
| `src/backend/Modules/Inventory/Tooba.Inventory.Infrastructure/Directories/InventoryDirectory.cs` | Interface list only: `+ IInventoryPaymentHoldExtensionPort`. |
| `src/backend/Modules/Inventory/Tooba.Inventory.Infrastructure/DependencyInjection/InventoryModule.cs` | One registration line for the new port. |
| `src/backend/Modules/Inventory/Tooba.Inventory.Tests/Behavior/InventoryHoldExtensionTestContext.cs` | New test seam builder (DbContext-only; guard is a pass-through). |
| `src/backend/Modules/Inventory/Tooba.Inventory.Tests/Behavior/InventoryPaymentHoldExtensionTests.cs` | New fail-closed outcome tests. |
| `src/backend/Modules/Inventory/Tooba.Inventory.Tests/Behavior/InventoryHoldExtensionPostgresTests.cs` | New disposable-database PostgreSQL behavioural proof. |
| `src/backend/Modules/Inventory/Tooba.Inventory.Tests/Architecture/InventoryPaymentHoldExtensionGuardTests.cs` | New durable source guards. |
| `docs/ai/tasks/TB-P0-INVENTORY-ATOMIC-HOLD-EXTENSION-017.task.md` | Received task artifact. |
| `docs/architecture/evidence/TB-P0-INVENTORY-ATOMIC-HOLD-EXTENSION-017/**` | This evidence. |

Nothing outside the permitted scope: no Host, Host.Tests, Program.cs, Frontend, Order, Payment,
migration, schema, runtime-setting or 015 file change.

## 4. Verification

`dotnet build src/backend/Modules/Inventory/Tooba.Inventory.Tests/Tooba.Inventory.Tests.csproj` →
succeeded, 0 warnings (CS1591 XML-doc gate satisfied).

One focused run:
`dotnet test … --filter "FullyQualifiedName~HoldExtension"` → **19 passed / 19 total, 0 failed, 4.32 s**.
No test retries, no solution-wide run.

PostgreSQL behavioural proof (`InventoryHoldExtensionPostgresTests`) — 9 facts, each creating and
dropping its **own disposable database** (`tooba_hold017_<hex>`) on the local server:

| Fact | Evidence |
| --- | --- |
| `Extends_every_finite_hold_to_the_requested_deadline` | Two `Held` rows raised to `max(existing, deadline)`; `GuaranteedUntil == deadline`; `DbConnection.State == Closed` after the call. |
| `One_released_hold_rejects_the_whole_set_and_changes_nothing` | One `Released` row → `NotHeld`, unavailable `=[released]`, and **both** provable holds still at their seed expiry (all-or-none). |
| `Expired_and_consumed_and_missing_holds_fail_closed` | `expired` / `Consumed` / absent id / `Guid.Empty` each → `NotHeld` with exactly that id unavailable; expired row not revived, `Held` row untouched, status still `Held`. |
| `Replay_is_monotonic_and_never_shortens_a_hold` | After `+6 h`, a later `+20 min` request leaves the stored expiry at `+6 h`; a `+9 h` request moves it forward. |
| `Durable_null_expiry_is_preserved_and_never_given_a_deadline` | `expires_at IS NULL` stays `NULL` while the finite sibling is extended. |
| `Only_durable_holds_report_no_guaranteed_deadline` | All-durable set → `Extended` with `GuaranteedUntil == null`. |
| `Deadline_already_past_the_database_clock_fails_closed` | `now − 1 h` and `default` → `DeadlineInvalid`, stored expiry unchanged. |
| `Ambient_scope_is_refused_and_leaves_the_hold_untouched` | Inside a live `TransactionScope` → `AmbientTransactionUnsupported`, hold unchanged (no distributed promotion attempted). |
| `Row_lock_blocks_a_competitor_for_the_whole_call_and_is_released_afterwards` | A competitor transaction holds the row first; after 700 ms the extension call **still has not completed** (`IsCompleted == false`), proving a genuinely blocking `FOR UPDATE` on the database; after the competitor rolls back the call returns `Extended`, the connection is closed, and a competing `FOR UPDATE` with `lock_timeout = 750ms` then succeeds immediately → no lock leak. |

Isolation: the proof reads the Development settings **only** to obtain server credentials, connects to
`postgres` to `CREATE DATABASE` / `DROP DATABASE … WITH (FORCE)` a throwaway database, and creates a
minimal `inventory.reservations` table inside it. It never connects to, seeds or locks `tooba_alpha`,
does not touch the running Host on `:5088`, and creates no migration or schema change in the repository.
Post-run check: PostgreSQL on `127.0.0.1:5432` reachable, zero `tooba_hold017*` databases left behind.

Source guards (`InventoryPaymentHoldExtensionGuardTests`, 6 facts) additionally assert: one
`BeginTransactionAsync`/`CommitAsync`/`RollbackAsync` with `Transaction.Current` refusal and no
`TransactionScope`/`EnlistTransaction`; deterministic `ORDER BY … FOR UPDATE` with
`clock_timestamp()`; rollback on both gates; `CASE … GREATEST` preservation; no
`SaveChangesAsync`/`OpenConnectionAsync`/`DateTimeOffset.UtcNow`/`Guid.NewGuid()`/foreign lookups; and
module registration.

## 5. Known limitations (explicitly not claimed)

- This primitive alone does **not** eliminate expiry during PSP processing and does **not** make
  payment initiation race-free. Order/Payment must still make the deadline window meaningful and handle
  late callbacks and compensation.
- The deadline is only as good as the effective configured policy the caller derives; this seam
  validates absoluteness and future-ness against the database clock but does not invent or clamp policy.
- The seam does not prove checkout ownership of the ids; it proves only "Held and unexpired now".
- Lock contention is not converted into a business outcome: an unexpected provider fault (including a
  caller-imposed `lock_timeout`) propagates and rolls back. No new error code or `lock_timeout` policy
  was introduced (out of scope).
- `updated_at` uses the module clock (`_clock`) while validity uses the database clock — a deliberate
  split matching the existing module convention.
- The PostgreSQL proof skips silently when no local PostgreSQL is reachable (it then proves nothing);
  in this run it executed for real (see timings and the blocking-lock fact above).
- Host.Tests were **not** built or run: building `Tooba.Host.Tests` recompiles `Tooba.Host` and would
  overwrite binaries locked by the running Development Host. Compatibility with the Inventory structure
  guards was verified by reading them (500-LOC ceiling in `Directories`, canonical folder allowlist,
  one-capability-per-port-file, unchanged 015 assertions).
