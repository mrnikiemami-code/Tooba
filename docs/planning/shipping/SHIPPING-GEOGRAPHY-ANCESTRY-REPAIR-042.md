# Shipping — Geography Ancestry Guard Repair Report (`TB-SHIPPING-GEOGRAPHY-ANCESTRY-REPAIR-042`)

> **STATUS: PASS FOR THE REPAIR SCOPE / INSERT + UPDATE CYCLE LOOPS CLOSED AND PROVEN ON A REAL POSTGRESQL / SQL AND MIGRATION SOURCE VALIDATED UP-DOWN-UP / NOT APPLIED TO ANY TOOBA DATABASE / NOT WIRED / NOT CERTIFIED.**
> The 041 guard covered `UPDATE` only and justified that with a claim that was wrong. This Task **falsified
> the claim on a real server**, **repaired** the guard so both `INSERT` and `UPDATE` are covered by an
> immediate (non-deferrable) check, and **verified the repair, the isolation policy, the deferral behaviour
> and two-session concurrency** against a disposable, task-exclusive PostgreSQL container. Migration
> readiness for *production application* is still **not** claimed: module registration/ordering, Host
> wiring and any apply to Tooba's own database remain out of scope and unauthorized.

| Field | Value |
| --- | --- |
| Repository | `mrnikiemami-code/Tooba` |
| Branch | `main` |
| Expected-Base-SHA | `5456cebb6599c0f4f7ea84bc9bda37a990143629` |
| Base-SHA (verified `HEAD == origin/main`) | `5456cebb6599c0f4f7ea84bc9bda37a990143629` |
| Parent task | `TB-SHIPPING-GEOGRAPHY-INTEGRITY-MIGRATION-041` |
| Mode | `BOUNDED_ANCESTRY_TRIGGER_REPAIR_AND_ISOLATED_VERIFICATION` |
| Status | `PASS` (repair scope) |
| Stop-State | `STOP_FOR_ARCHITECT` |
| automaticNextImplementationTask | `NONE` |

---

## 1. Preflight

- Claimed exactly one Pending item on `tooba-main` after `BRIDGE-WAKE` (row `c36e91c5-444b-4d18-af29-a9aeddefabc6`); the received artifact was persisted verbatim at `docs/ai/tasks/TB-SHIPPING-GEOGRAPHY-ANCESTRY-REPAIR-042.task.md` (receipt only, not staged, never replayed).
- `git fetch origin`; `HEAD == origin/main == Expected-Base-SHA == 5456cebb6599c0f4f7ea84bc9bda37a990143629`. No drift, no reset, no stash, no rebase, no force-push, no silent fast-forward.
- Governance read: `AGENTS.md`, `.cursor/skills/tooba-backend-feature/SKILL.md`, the 041 report, the current guard and migration source, and the module's EF conventions. Findings register not consulted.
- Output-path collision check before editing: no pre-existing `SHIPPING-GEOGRAPHY-ANCESTRY-REPAIR-042.md` and no `TB-SHIPPING-GEOGRAPHY-ANCESTRY-REPAIR-042/**` content.
- All pre-existing dirty/untracked files were recorded and left untouched.

## 2. What changed (exact paths)

| Path | Change |
| --- | --- |
| `src/backend/Modules/Geography/Tooba.Geography.Infrastructure/Persistence/AdministrativeAreaAncestryGuard.cs` | Guard source repaired: `TriggerName` split into `UpdateTriggerName` (line 104) and `InsertTriggerName` (line 107); `TriggerSql` now creates **two immediate triggers** (lines 230–243); `DropTriggerSql` drops both (line 245); class documentation rewritten (INSERT coverage, why the 041 claim was wrong, immediate-not-deferrable decision, snapshot/lock ordering, 42P17 finding) |
| `src/backend/Modules/Geography/Tooba.Geography.Infrastructure/Persistence/Migrations/20261014113949_InitialGeography.cs` | Comment above the guard operations updated (lines 344–350); `Up` still emits `FunctionSql` (line 351) then `TriggerSql` (line 353); `Down` still tears down (lines 359–361) |
| `docs/planning/shipping/SHIPPING-GEOGRAPHY-ANCESTRY-REPAIR-042.md` | This report |
| `docs/architecture/evidence/TB-SHIPPING-GEOGRAPHY-ANCESTRY-REPAIR-042/**` | Local evidence: two SQL scenario files, two runner scripts and five captured logs (not staged, matching the repo's evidence convention) |

**Not touched** (byte-identical to Base-SHA): `20261014113949_InitialGeography.Designer.cs` and `GeographyDbContextModelSnapshot.cs` (the EF model did not change — confirmed by `dotnet ef migrations has-pending-model-changes` → *"No changes have been made to the model since the last migration."*), `GeographyDbContext.cs`, the whole Geography Domain and Contracts, the composite country FKs, the four translation parent FKs, all other constraints, the Host, DI, endpoints, frontend, `StorefrontIranGeography.Provinces`, pinned guards, manifests and architecture locks. No second migration was created: the initial migration has never been applied, so its source was amended in place.

## 3. The 041 loophole — falsified on a real server, then closed

041 justified leaving `INSERT` unguarded with the claim *"a freshly inserted row's id is new (PK-enforced), so no pre-existing chain can already contain it"*. **That reasoning only considered one new row at a time, and it is wrong for a multi-row `INSERT`:**

`insert … (A, country, B, …), (B, country, A, …)` — the composite FK is an `AFTER ROW` constraint fired at **end of statement**, so by the time it is checked both rows exist and the pair satisfies it. Two brand-new rows therefore form a committed cycle with no domain code involved.

Reproduced and captured in `log-insert-gap.txt` against a real PostgreSQL 17 (only difference from 041: the new insert trigger disabled):

| Step | Observation |
| --- | --- |
| G1 — identical two-row `INSERT` with the insert trigger disabled | `INSERT 0 2`, table afterwards `G1 → G2` and `G2 → G1` — **a committed cycle, exactly the 041 hole** |
| G2 — same statement with the insert trigger enabled | `ERROR: 23514: geography.administrative_area.ancestry_cycle`, and `rows_after_rejected_insert = 0` (nothing committed) |
| G3 — the same pair built by two `UPDATE`s | `UPDATE 1` then `ERROR: 23514 … ancestry_cycle`; final state remains acyclic |

## 4. The repair

- **Two immediate triggers, one function** (`TriggerSql`, guard file lines 230–243):
  - `trg_administrative_areas_ancestry_guard_update` — `AFTER UPDATE … FOR EACH ROW`, `WHEN (NEW.parent_administrative_area_id IS NOT NULL AND (OLD.parent_administrative_area_id IS DISTINCT FROM NEW.parent_administrative_area_id OR OLD.country_id IS DISTINCT FROM NEW.country_id))`
  - `trg_administrative_areas_ancestry_guard_insert` — `AFTER INSERT … FOR EACH ROW`, `WHEN (NEW.parent_administrative_area_id IS NOT NULL)`
- **Why two triggers instead of one — empirically forced.** The first repair attempt used a single `AFTER INSERT OR UPDATE` trigger whose `WHEN` referenced `OLD`. PostgreSQL rejected it: `SqlState 42P17: INSERT trigger's WHEN condition cannot reference OLD values` (`trigger.c:620`). Rather than delete the `WHEN` clause (which would have made the guard fire on *every* row write, taking the serialization lock for unrelated column updates), the trigger was split so the precise "an ancestry link actually changed" predicate survives for `UPDATE`.
- **Immediate, not deferrable.** The 041 form was a `DEFERRABLE INITIALLY IMMEDIATE` constraint trigger. Deferrability was removed deliberately: a deferred check runs at `COMMIT`, under a commit-time snapshot whose visibility of concurrently committed edges this Task cannot demonstrate, and it also allows a cycle to exist transiently inside a transaction. An ordinary non-deferrable `AFTER` trigger fires at end of statement, which is precisely what makes the multi-row-`INSERT` cycle visible. `SET CONSTRAINTS ALL DEFERRED` can no longer move the check away from the statement that caused it (proven in T6 below).
- **Unchanged:** the recursive walk, the bounded depth (4096, fail closed), the advisory lock `pg_advisory_xact_lock(20261014, 41)`, the READ COMMITTED precondition (fail closed with `0A000`), the stable tokens `geography.administrative_area.ancestry_cycle` (`23514`), `…ancestry_depth_exceeded` (`54001`) and `…ancestry_guard_isolation_unsupported` (`0A000`). The advisory-lock key deliberately keeps its original value: it identifies the invariant, not the slice that amended it.

## 5. Snapshot / lock / deferrable reasoning, and what the tests did to it

1. **Lock ordering.** `pg_advisory_xact_lock` is transaction-scoped and released only after the committing transaction is marked committed; a later acquirer therefore observes everything the previous holder committed.
2. **Snapshot freshness (was an assumption in 041, now measured).** The walk is a separate SQL statement inside a volatile PL/pgSQL function, so in READ COMMITTED it takes a snapshot *after* the lock wait, not the mutating statement's snapshot. Test C1 measured this directly: session 2 blocked **3.67 s** on the lock and then raised the cycle error, i.e. it did observe session 1's committed edge. A stale snapshot would have made session 2 silently succeed.
3. **No row locks in the walk.** The walk uses plain reads only, so it can never block on a row held by another transaction while holding its own row: the advisory lock stays the single shared resource and mutual exclusion is deadlock-free. No deadlock was observed in any test.
4. **Deferrability removed** (see §4) — and T6 confirms a two-statement cycle inside `SET CONSTRAINTS ALL DEFERRED` still fails at the offending statement.

## 6. Isolated verification — environment and isolation proof

A **disposable, task-exclusive** container was created solely for validation. `log-destination-proof.txt`:

| Evidence | Value |
| --- | --- |
| Disposable container identity | `e8e13827c78222a5d415e6b9484f2a62e8f728d7c0fd915bddef7c5abd8931c7 /geo042-verify running postgres:17` |
| Pre-existing container (untouched) | `01d8067a30ce965c2ffc80296c5568c63f8e48eb1b2c5c2d1d3f7963b7b48d9f /postgres-db running postgres:17` |
| Port binding | `5432/tcp -> 127.0.0.1:55432` (Tooba's own service is on 5432) |
| Server that answered | `db = geo042`, `usr = postgres`, `pg_postmaster_start_time = 2026-10-14 11:53:50.865409+00` (fresh) |
| Databases on it | 2 non-template (`geo042`, `postgres`) — no shared data, no customer data |

The only connection string used anywhere in this Task was `Host=127.0.0.1;Port=55432;Username=postgres;Password=geo042localonly;Database=geo042` (design-time env var `TOOBA_DESIGN_TIME_CONNECTION`). All SQL ran through `docker exec` into that container. **The container was removed at the end** (`docker stop geo042-verify` → gone; `postgres-db` still running, never connected to, never inspected, never modified). No other database was contacted at any point.

## 7. Test results (every expected-then-observed outcome)

`log-scenarios.txt` (T0–T4, single session, 4 expected rejections observed):

| # | Scenario | Expected | Observed |
| --- | --- | --- | --- |
| T0 | installed triggers / guard function | 2 triggers, function volatile | `trg_…_guard_insert` (`tgtype 5`), `trg_…_guard_update` (`tgtype 17`); function `v` |
| T1 | root insert, child insert, re-root to `NULL`, re-parent, code-only update | all accepted | `INSERT 0 1`, `UPDATE 1` ×4 |
| T2a | `INSERT` with self-parent | `23514` | `ERROR: 23514: geography.administrative_area.ancestry_cycle` |
| T2b | **same-statement two-row `INSERT` cycle** | `23514` | `ERROR: 23514: … ancestry_cycle` |
| T2c | 2-node `UPDATE` cycle (legit edge then closing edge) | second rejected | `UPDATE 1`, then `ERROR: 23514` |
| T2d | 3-node `UPDATE` cycle `X→Y→Z` then `Z→X` | closing edge(s) rejected | `UPDATE 1`, `INSERT 0 1`, `UPDATE 1`, then `ERROR: 23514` |
| T3 | 5-level legitimate chain + deep-node re-root | accepted | `INSERT 0 4`, `UPDATE 1` |
| T4 | final graph | acyclic | 8 rows, no cycle |

`log-isolation.txt` (T5–T9, 4 expected rejections observed):

| # | Scenario | Expected | Observed |
| --- | --- | --- | --- |
| T5a | ancestry `UPDATE` in READ COMMITTED | accepted | `UPDATE 1`, `COMMIT` |
| T5b | ancestry `UPDATE` in REPEATABLE READ | `0A000` | `ERROR: 0A000: geography.administrative_area.ancestry_guard_isolation_unsupported`, `DETAIL: … transaction_isolation is 'repeatable read'`, `ROLLBACK` |
| T5c | ancestry `UPDATE` in SERIALIZABLE | `0A000` | same token, `transaction_isolation is 'serializable'` |
| T5d | **code-only** `UPDATE` in REPEATABLE READ | accepted (the `WHEN` clause keeps the guard off, so the isolation policy does not over-reject unrelated writes) | `UPDATE 1`, `COMMIT` |
| T6 | `SET CONSTRAINTS ALL DEFERRED` + `A→B` then `B→A` | immediate rejection at the second statement, not at commit | `SET CONSTRAINTS`, `UPDATE 1`, `ERROR: 23514` |
| T7 | `INSERT … ON CONFLICT DO UPDATE` that would close a cycle | `23514` | `ERROR: 23514: … ancestry_cycle` |
| T8 | `INSERT … ON CONFLICT DO UPDATE` without a cycle | accepted | `INSERT 0 1` |
| T9 | graph + walk | acyclic | consistent, no cycle |

`log-concurrency-1.txt` — **two overlapping sessions, opposite reparentings (the race that matters)**:

```text
S1: begin; update P -> Q; select pg_sleep(6); commit;      -> BEGIN, UPDATE 1, COMMIT   (succeeded)
S2: begin; update Q -> P; commit;   (started 2.5 s later)  -> blocks, then
     ERROR: 23514: geography.administrative_area.ancestry_cycle
     DETAIL: administrative area …072 cannot point at parent …071: the parent chain already contains it.
     S2_ELAPSED_SEC 3.67
final state: P2 -> Q2, Q2 root   (acyclic)
```

`log-concurrency-2.txt` — **two overlapping sessions, non-conflicting reparentings** (guard must not over-reject):

```text
S1: begin; update C3 -> R3; select pg_sleep(6); commit;   -> BEGIN, UPDATE 1, COMMIT   (succeeded)
S2: begin; update C2 -> R3; commit; (started 2.5 s later) -> blocks, then UPDATE 1, COMMIT
     S2_ELAPSED_SEC 3.66
final state: C2b -> R3, C3b -> R3   (both applied, no spurious failure, no deadlock)
```

## 8. Migration and model validation

- `dotnet build` of `Tooba.Geography.Infrastructure`: succeeded, 0 errors, 0 warnings (only the pre-existing `NU1900` NuGet feed timeout warning seen on the untouched baseline).
- **Up** applied to the empty disposable database: `Applying migration '20261014113949_InitialGeography'. Done.` — 9 tables, indexes, composite FKs and both guard triggers created.
- **Down** applied (`dotnet ef database update 0`): `Reverting migration … Done.`; afterwards guard triggers = `0`, guard function = `0`, no geography data tables left (the `__ef_migrations_history` relation and the `geography` schema remain — expected EF behaviour).
- **Up again**: `Applying migration … Done.`; guard triggers = `2`. The round trip proves the `Down` teardown order (triggers/function before table drops) is valid SQL and reversible.
- **Model drift**: `dotnet ef migrations has-pending-model-changes` → *"No changes have been made to the model since the last migration."* — the amended migration still matches the EF model exactly, so the designer/snapshot files legitimately stay untouched.
- **Operational observation (recorded, not hidden):** the very first apply attempt (with the invalid single `INSERT OR UPDATE` trigger, `42P17`) left the disposable database with the `geography` schema present and 2 relations in it, i.e. the failed migration was **not fully atomic end-to-end** as executed by the tool for this provider. The DB was discarded/recreated; this is a note for whoever eventually applies migrations operationally, not a defect of the guard.

## 9. What is still NOT proven / open

1. **Production application readiness is not claimed.** The migration has still never been applied to any Tooba database (dev/staging/production), no module registration/ordering, no Host/DI wiring, no seed. SQL semantics are validated; deployment ordering is not.
2. **REPEATABLE READ / SERIALIZABLE ancestry writes are rejected by design** (`0A000`). This is a policy decision the Architect should accept explicitly: a future write path that runs at a higher isolation level and mutates ancestry will fail closed instead of being checked. Unrelated (non-ancestry) writes at that level still work (T5d).
3. **Trigger-based guards are bypassable by superuser DDL** (`DISABLE TRIGGER`, `session_replication_role = replica`, dropping the trigger) — demonstrated deliberately in G1 to reproduce the 041 behaviour. No application write path can bypass the guard.
4. **Race coverage is empirical, not exhaustive.** The tested interleavings (one waiting session on both the cycle and the non-cycle case, plus all single-session routes) passed; exotic interleavings (three or more concurrent ancestry writers, transactions mutating many edges) were not enumerated. The serialization argument covers them, but only the tested ones were measured.
5. **Depth bound 4096** remains a safety net (fail closed), not a business rule.
6. **The three DB error tokens are still not declared in `GeographyErrorCodes`** (Contracts is outside this Task's allowed paths) and need a later authorized slice.
7. **D10 administrative level, migration ordering/registration and Host wiring remain open**, untouched.

## 10. Time, push and lifecycle

- Claim (`11:51:51Z`) → validated commit: **≈11 minutes** of the 30-minute hard timebox.
- Exactly one bounded commit, authorized paths only; `git push origin main`; `git fetch origin`; SHA equality verified.
- No Results-API probe or dummy POST; no automatic next task; no idle polling.
- Disposable container removed; the Tooba database was never contacted.

## 11. Recommendation

Accept the repair at source level. The remaining gates before any application to a real database are administrative rather than technical: (a) explicit acceptance of the READ COMMITTED fail-closed policy, (b) module migration registration/ordering decision, and (c) the D10/level-of-detail question. Worker PASS ≠ Architect ACCEPT; this report makes no claim that the migration is ready to apply to Tooba.
