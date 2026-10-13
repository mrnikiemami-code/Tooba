# TB-P0-INVENTORY-PAYMENT-HOLD-GATE-015 — Analysis

Program: TOOBA-CHECKOUT-PAYMENT-COMMIT-BOUNDARY
Parent-Task: TB-P0-CHECKOUT-PAYABILITY-ATOMIC-014
Mode: BOUNDED_IMPLEMENTATION_OR_BLOCKED → implemented, STOP_FOR_ARCHITECT
Expected-Base-SHA: ba424e68a9f9eca11ede109da2b26b5c7279fed6
Channel: tooba-main · WorkerId: tooba-worker-01 · AgentType: cursor

## 1. Goal and boundary

Add the smallest Inventory-owned, Contracts-only primitive that verifies — under PostgreSQL row
locks, inside a **caller-owned ambient transaction** — that every supplied reservation is still
`Held` and unexpired. This is a **building block**: it does not authorize charging, does not
guarantee payability end to end, adds no endpoint, and wires nothing into Payment.

Explicitly unchanged: Host root, Host.Tests, `Program.cs`, the running Development Host on `:5088`,
frontend, database schema/data, migrations, settings, reservation TTLs, workers, checkout commit
behavior. No SQL writes, no external provider calls, no guard weakening, no new typed error code.

Base verified before work: `HEAD == origin/main == ba424e68a9f9eca11ede109da2b26b5c7279fed6`.

## 2. Delivered surface

| File | Change |
| --- | --- |
| `src/backend/Modules/Inventory/Tooba.Inventory.Contracts/Orders/InventoryPaymentHoldGateContracts.cs` | New. `InventoryPaymentHoldGateOutcome`, `InventoryPaymentHoldGateResult`, `IInventoryPaymentHoldGate`. |
| `src/backend/Modules/Inventory/Tooba.Inventory.Infrastructure/Directories/InventoryDirectory.PaymentHoldGate.cs` | New cohesive partial: the gate implementation plus the pure verdict `EvaluatePaymentHoldGate`. |
| `src/backend/Modules/Inventory/Tooba.Inventory.Infrastructure/Directories/InventoryDirectory.cs` | Interface list only: added `IInventoryPaymentHoldGate`. |
| `src/backend/Modules/Inventory/Tooba.Inventory.Infrastructure/DependencyInjection/InventoryModule.cs` | One DI line; no Host edit. |
| `src/backend/Modules/Inventory/Tooba.Inventory.Tests/Behavior/InventoryPaymentHoldGateTests.cs` | New. 9 focused verdict tests. |
| `src/backend/Modules/Inventory/Tooba.Inventory.Tests/Architecture/InventoryPaymentHoldGateGuardTests.cs` | New. 5 durable string guards on the gate source. |

Placement respects the Inventory AMSC-001 W2 structure lock: the port lives in the existing
`Contracts/Orders` capability folder with namespace `Tooba.Inventory.Contracts.Orders`, the
implementation in `Infrastructure/Directories` with namespace `…Directories`, and the new partial is
132 LOC (ceiling 500). `Domain/Policies` is **forbidden** by the Inventory manifest
(`forbiddenTopLevelFolders: ["Entities", "Policies"]`), so the pure verdict lives next to the
implementation instead of in Domain.

## 3. Fail-closed design (four layers)

1. **Nothing nameable ⇒ nothing proven.** Empty, duplicate-only, or `Guid.Empty`-bearing input can
   never produce `Verified`; duplicates are ignored and the empty Guid is removed from the live set.
2. **No ambient transaction ⇒ refuse.** `Transaction.Current is null` returns
   `AmbientTransactionRequired`. The gate never opens its own transaction: a self-owned transaction
   would release the row locks at its own end and fake the boundary the caller asked for.
3. **Unprovable enlistment ⇒ refuse.** If the `DbContext` connection is already open, enlistment
   cannot be proven through a provider-neutral API (`NpgsqlConnection.EnlistedTransaction` is not
   public in Npgsql 8.0.7 — verified by reflection), so the gate refuses instead of assuming. When the
   connection is closed, it is opened **inside** the ambient scope, which is what enlists it.
4. **Lock guarantee must be observed.** After the locking statement, the gate confirms on the same
   connection that a transaction block is really open (`txid_current_if_assigned()`). Without that
   observation the statement ran in an implicit single-statement transaction and the locks are
   already gone, so the outcome is `AmbientTransactionRequired`.

Validity is judged by the **database clock** (`clock_timestamp()`), never by a caller-supplied or
application timestamp; `expires_at IS NULL` follows the existing durable-hold policy (no time expiry).

## 4. Locking shape

```sql
SELECT r.reservation_id AS "Value"
FROM inventory.reservations AS r
WHERE r.reservation_id = ANY($1)
  AND r.status = 'Held'
  AND (r.expires_at IS NULL OR r.expires_at > clock_timestamp())
ORDER BY r.reservation_id
FOR UPDATE
```

* deterministic lock order (`ORDER BY r.reservation_id`) keeps this blocking `FOR UPDATE` free of
  deadlock with the reclaimer's `FOR UPDATE SKIP LOCKED`;
* only rows that are live are locked, and the reclaimer only ever targets **expired** rows, so the
  two lock sets cannot overlap while the gate holds its locks;
* no `UPDATE`/`INSERT`/`DELETE`, no `SaveChanges`, no journal, no `_clock`, no external call.

## 5. Isolated read-only harness — measured facts

Harness: temporary console app outside the repository (`%TEMP%\gateprobe`), Npgsql 8.0.7 against the
Development database `tooba_alpha` on `127.0.0.1:5432`. The live Host was **not** stopped, rebuilt,
restarted, or touched; the harness only reads and takes row locks that it rolls back. The running
Host held no conflicting locks during the run.

Server: **PostgreSQL 17.10 (Debian 17.10-1.pgdg13+1)**.

| Probe | Observation | Meaning |
| --- | --- | --- |
| `BINDING_ANY_COUNT=3` | `Guid[]` → `= ANY($1)` returns the 3 existing rows | array-parameter plumbing is correct |
| `E_XID_ASSIGNED=True` | explicit connection transaction + `FOR UPDATE` | a real transaction block is open |
| `F_XID_ASSIGNED=False` | **no** transaction + `FOR UPDATE`, next statement | implicit single-statement transaction: locks already released |
| `G_XID_ASSIGNED=True` | ambient `TransactionScope` + connection opened inside it | Npgsql **enlisted** the connection; the block is real |
| `J_NO_TRANSACTION_CONFIRMATION=0` | shipped confirmation query, no transaction | the shipped check fails closed |
| `K_AMBIENT_CONFIRMATION=1` | shipped confirmation query, ambient scope | the shipped check proceeds |
| `H_COMPETING_FOR_UPDATE=BLOCKED_BY_HELD_ROW_LOCKS` | second backend, `lock_timeout=750ms` | locks persist well **beyond** the statement and block others |
| `I_AFTER_SCOPE_FOR_UPDATE=NOT_BLOCKED_LOCKS_RELEASED` | same probe after the scope ended | lock lifetime is exactly the caller's transaction |

Taken together: enlistment works, the row locks survive until the caller's transaction ends, and the
shipped confirmation query discriminates the two transaction states.

### Negative finding that changed the design

The first implementation confirmed the guarantee by inspecting `pg_locks` for granted
`locktype='tuple'` rows. The harness proved that unusable: **`pg_locks` reported no tuple locks in
any case** (all of `A/HAS_ROW_LOCKS`, `B`, `C`, `E`, `F`, `G` were `False`), because PostgreSQL only
materialises tuple-lock entries when a waiter contends. That check would have made the primitive
return `AmbientTransactionRequired` forever. It was replaced by the `txid_current_if_assigned()`
observation, which the harness shows is both reachable (`K=1`) and discriminating (`J=0`).

### Integration constraint discovered

Enlisting **two** connections in one ambient scope promotes it to a distributed transaction; on commit
the server attempted two-phase commit and failed (`55000 prepared transactions are disabled`,
`max_prepared_transactions = 0` in Development). Callers must therefore keep **one connection per
ambient scope** when using this gate.

## 6. Verification status

Verified: verdict logic (9 tests), source guards (5 tests), full Inventory module suite (27 tests,
`Passed! — Failed: 0`), and the enlistment/lock-lifetime/discrimination facts above.

**Still UNVERIFIED:** the gate has not been exercised end to end against a genuinely `Held`
reservation, because the Development `tooba_alpha` inventory holds no live rows at all
(`STATUS_TALLY Consumed=6`, `STATUS_TALLY Released=25`, zero `Held`); the locking probes therefore used
the same `FOR UPDATE` shape without the status predicate. A later isolated harness should seed a
`Held` reservation with a future `expires_at` in a throwaway database, then assert
`Verified`, one-released ⇒ `NotHeld`, one-expired ⇒ `NotHeld`, one-missing ⇒ `NotHeld`, and the
all-or-none verdict across several lines.

## 7. Remaining race — what Order and Payment must still do

This primitive only proves "these holds are alive **right now**, and nobody can release or expire them
until my transaction ends". It does not:

* prevent the hold from expiring **after** the transaction commits — payment capture must clear the
  TTL (or settle) before the ambient transaction closes, or Order must re-verify;
* make the Order reservation cycle status agree with Inventory: TB-P0-CHECKOUT-PAYABILITY-ATOMIC-014
  measured a 6–15 s lag where `reservation_cycles.status` is still `Active` while the stock hold is
  already `Released`, so a status-only check on the Order side remains insufficient;
* authorize the charge. No PSP call may run inside the ambient transaction that holds these locks.

Next steps that remain outside this task: Order must open the ambient transaction, call the gate, and
only then hand off to Payment's initiation seam inside the same transaction (one connection per scope),
with the payability error-surface decision from TB-P0-CHECKOUT-PAYABILITY-GATE-013 still open.

## 8. Validation commands

```text
dotnet build src/backend/Modules/Inventory/Tooba.Inventory.Tests/Tooba.Inventory.Tests.csproj -v q --nologo
  → Build succeeded. 0 Warning(s) 0 Error(s)

dotnet test src/backend/Modules/Inventory/Tooba.Inventory.Tests/Tooba.Inventory.Tests.csproj --no-build --nologo
  → Passed! - Failed: 0, Passed: 27, Skipped: 0, Total: 27
```

Host.Tests was **not** built or run: the live Development Host holds its DLLs, and the task forbids
stopping it. The Host.Tests inventory guards that read the touched surfaces were reviewed by reading
instead: `InventoryModuleAmsc001W2StructureGuardTests` requires the listed partials to exist (it is
not a set-equality check) and caps every `Directories/*.cs` at 500 LOC — satisfied (132).
`OrderSupplyFoundationTests.InvInfra()` aggregates `InventoryDirectory*.cs` and forbids the literal
`Status = StockReservationStatus.Held`, which does not appear (the SQL uses `r.status = 'Held'`).
`InventoryModuleAmsc001W3CertGuardTests` pins 29 declared error codes and rejects raw
`ContractOperationException("…")` literals — no code was added.

automaticNextImplementationTask=NONE.
