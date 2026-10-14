# Shipping — Geography Integrity & Initial Migration Source Report (`TB-SHIPPING-GEOGRAPHY-INTEGRITY-MIGRATION-041`)

> **STATUS: PARTIAL — ANCESTRY-CYCLE GUARD DELIVERED AS MIGRATION SOURCE / FIRST MIGRATION + MODEL SNAPSHOT GENERATED (SOURCE ONLY) / NOT APPLIED / NOT WIRED / NOT CERTIFIED.**
> The remaining pre-migration integrity concern is closed *by construction and by design argument*
> (database-enforced, serialized, fail-closed), and the first Geography migration source now exists.
> **One item is deliberately NOT claimed as proven: empirical PostgreSQL behaviour of the guard under
> real concurrent sessions.** No database was contacted in this Task, and a static/algorithmic
> verification cannot substitute for that experiment. Migration readiness is therefore **not** claimed;
> `Status: PARTIAL`, `Stop-State: STOP_FOR_ARCHITECT`.

| Field | Value |
| --- | --- |
| Repository | `mrnikiemami-code/Tooba` |
| Branch | `main` |
| Expected-Base-SHA | `097720258721d72bca08ad44ec86c9cc4b6fd3fe` |
| Base-SHA (verified `HEAD == origin/main`) | `097720258721d72bca08ad44ec86c9cc4b6fd3fe` |
| Parent task | `TB-SHIPPING-GEOGRAPHY-COUNTRY-INTEGRITY-040` |
| Mode | `BOUNDED_INERT_HIERARCHY_INTEGRITY_AND_INITIAL_MIGRATION_SOURCE` |
| Status | `PARTIAL` |
| Stop-State | `STOP_FOR_ARCHITECT` |
| automaticNextImplementationTask | `NONE` |

---

## 1. Preflight

- Task claimed once on channel `tooba-main` (row `60350777-2873-445c-9e2a-d06e2cd8f939`) after `BRIDGE-WAKE`; the received artifact was persisted verbatim at
  `docs/ai/tasks/TB-SHIPPING-GEOGRAPHY-INTEGRITY-MIGRATION-041.task.md` (receipt only, never replayed).
- `git fetch origin` → `HEAD == origin/main == 097720258721d72bca08ad44ec86c9cc4b6fd3fe == Expected-Base-SHA`. No drift, no reset, no stash, no rebase.
- Pre-existing dirty/untracked paths were recorded and left untouched (5 modified: `CheckoutAbuseGate.cs`, three `src/frontend` screens, `docs/architecture/evidence/TB-P0-INVENTORY-CONSUMPTION-JOURNAL-003/summary.md`; plus the previously existing untracked task/evidence files).
- Output-path collision check before editing: no pre-existing `SHIPPING-GEOGRAPHY-INTEGRITY-MIGRATION-041.md`, no pre-existing `Persistence/Migrations/**` under Geography, no `*041*` shipping task file. Historical `TB-P07-T041*` files belong to an unrelated P07 program and were not opened.
- Governance read: `AGENTS.md`, `.cursor/skills/tooba-backend-feature/SKILL.md`, ADR-036 and reports 039–040 plus the current Geography code and module migration conventions (`Media`/`Pricing` migrations, `ToobaNpgsql.ConfigureModuleContext`, `ModuleSchemaMigrator`). The findings register was not scanned.

## 2. What changed (exact paths)

| Path | Kind | Purpose |
| --- | --- | --- |
| `src/backend/Modules/Geography/Tooba.Geography.Infrastructure/Persistence/AdministrativeAreaAncestryGuard.cs` | new | Module-owned PostgreSQL ancestry guard source (function + constraint trigger + teardown DDL) and its documented strategy |
| `src/backend/Modules/Geography/Tooba.Geography.Infrastructure/Persistence/Migrations/20261014113949_InitialGeography.cs` | new | First Geography migration: 9 tables, indexes, then the guard DDL (lines 344–350), teardown in `Down` (lines 356–358) |
| `.../Migrations/20261014113949_InitialGeography.Designer.cs` | new | EF-generated target-model metadata (unchanged from generator output) |
| `.../Migrations/GeographyDbContextModelSnapshot.cs` | new | EF-generated model snapshot (unchanged from generator output) |
| `docs/planning/shipping/SHIPPING-GEOGRAPHY-INTEGRITY-MIGRATION-041.md` | new | This report |

**No other file was modified.** In particular `GeographyDbContext.cs`, `Tooba.Geography.Domain/**`, `Tooba.Geography.Contracts/**`, `Tooba.slnx`, the Host, DI, endpoints, frontend, `StorefrontIranGeography.Provinces`, the old geography route/DTO and every pinned guard are byte-identical to the Base-SHA.

Key line evidence in the guard file: strategy/assumptions `90–118`; function DDL `120`; isolation pre-check `133–138`; advisory serialization `146`; bounded recursive walk `150–163`; cycle raise `166–171`; bound raise `174–179`; constraint trigger `190–196`; teardown DDL `199–204`.

## 3. Strategy comparison and selection

Two concrete viable strategies were compared, as required.

**A. Application/domain ancestor walk with transaction serialization.** A pure domain walk is impossible at the aggregate boundary: an `AdministrativeArea` cannot see sibling rows, so the walk would have to live in an Application handler or an Infrastructure repository, and the pessimistic serialization would have to be owned by each caller. It also cannot guard raw SQL, migrations, support scripts or future bulk imports; and delivering it needs a runtime write layer, DI registration and Host composition that this Task explicitly forbids. **Rejected: larger, weaker, and out of scope.**

**B. PostgreSQL constraint trigger (selected).** One deferrable per-row trigger with a bounded recursive walk, owned by the database. It applies to *every* future write path (there is no way for a future handler to forget it), it needs no DI, no Application layer, no Host change and no runtime registration, and the whole artifact is ~2 SQL objects. **Selected: smallest artifact that can hold the invariant at the boundary where rows actually meet.**

Explicitly *not* claimed: a `CHECK` constraint or a plain/composite `FOREIGN KEY` cannot express a recursive property such as "no ancestor cycle". The composite FK from 040 (`(parent_administrative_area_id, country_id) → (administrative_area_id, country_id)`) guarantees existence and same-country parenting only — it accepts `A → B → A` without complaint. Recursion therefore needs an explicit check; that is exactly what the guard is.

## 4. The enforcement mechanism (precise semantics)

- **Function** `geography.administrative_areas_ancestry_guard()` (`FunctionSql`), `LANGUAGE plpgsql`, `AFTER`/row trigger function:
  1. **Isolation pre-check — fail closed.** `current_setting('transaction_isolation')` must be `read committed`; otherwise `0A000` with token `geography.administrative_area.ancestry_guard_isolation_unsupported`. (Reason: §6.3.)
  2. `ParentAdministrativeAreaId IS NULL` (root) → nothing to validate.
  3. **Serialize:** `pg_advisory_xact_lock(20261014, 41)` — one fixed, geography-owned key pair, taken as the first action of every ancestry mutation.
  4. **Bounded, non-locking ancestor walk** (`WITH RECURSIVE`, anchor = the proposed parent row as already written by this transaction, recursion follows `parent_administrative_area_id` upward, `WHERE ancestry.depth < 4096`), aggregated in a single statement to `(MAX(depth), bool_or(id = NEW.id))`.
  5. Cycle → `RAISE` `23514` with stable token `geography.administrative_area.ancestry_cycle`. Bound reached → `RAISE` `54001` with token `geography.administrative_area.ancestry_depth_exceeded` (fail closed on a corrupt graph instead of looping or accepting).
- **Trigger** `trg_administrative_areas_ancestry_guard` (`TriggerSql`), `AFTER UPDATE`, `DEFERRABLE INITIALLY IMMEDIATE`, `WHEN (parent… IS DISTINCT FROM … OR country_id IS DISTINCT FROM …)`: fires only when an ancestry link actually changed, runs with the mutating statement by default, and remains deferrable so a future authorized multi-statement reorg may opt into `SET CONSTRAINTS … DEFERRED`.
- **`INSERT` is deliberately not guarded** and this is *provable*, not an omission: a freshly inserted row's stable id is new (PK-enforced), so no pre-existing parent chain can already contain it, i.e. a self-parent or indirect cycle cannot exist at insert time. The only insert-like path that can create one is `INSERT … ON CONFLICT DO UPDATE`, which is an `UPDATE` and therefore fires this trigger. Changing `country_id` alone cannot create a cycle either, and it is covered anyway because the `WHEN` clause includes it (and the 040 composite FK pins parent/country consistency).

## 5. Cycle rejection (self and indirect)

- **Self-parent:** the walk's anchor is `NEW.parent_administrative_area_id`; when it equals `NEW.administrative_area_id` the first row of the recursion already satisfies `administrative_area_id = NEW.administrative_area_id` → rejected. (The Domain also rejects this earlier at `AdministrativeArea.Reparent`; the DB guard makes it unforgettable.)
- **Indirect (`A → B → … → A`):** the walk enumerates the *complete* upward chain of the proposed parent. If the node being moved appears anywhere in that chain, the update is rejected.
- **Transitive nodes:** because every edge is checked when it is written, and the check is evaluated on the committed graph plus the current transaction's own change, no path can be inserted that closes a cycle.

## 6. Concurrency: how concurrent reparenting cannot create a cycle

### 6.1 Serialization rule

Every transaction that mutates an ancestry link of `geography.administrative_areas` takes the same transaction-scoped advisory lock as its first action. PostgreSQL releases that lock at the end of the transaction, and a transaction begins its commit-sequence completion (visibility) before locks are released. Therefore at most one ancestry mutation is *inside* the check at any moment, and anyone who acquires the lock afterwards observes everything the previous holder committed.

### 6.2 The walk must not take row locks (and does not)

The mutating statement already holds a row lock on its own row. If the walk used `FOR UPDATE`/`FOR NO KEY UPDATE` on ancestors, two transactions updating different rows could form a genuine lock cycle (T1 holds row A, wants row B held by T2; T2 holds row B, waits for the advisory lock held by T1) → deadlock. A plain read cannot block on uncommitted rows, so the advisory lock remains the only shared resource and mutual exclusion is deadlock-free by construction. This is enforced statically: the guard text contains no `FOR UPDATE` / `FOR NO KEY UPDATE` / `FOR SHARE`.

### 6.3 The transaction-ordering argument

Consider any set of concurrently committed ancestry mutations. Suppose a cycle `C` were committed. Take the transaction `T*` whose edge of `C` committed **last**. Every other edge of `C` was committed before it, therefore every other edge is visible to the statements `T*` executes after it acquires the advisory lock. `T*`'s walk starts at its new parent and follows the chain upward; because the rest of `C` is already committed and visible, the walk reaches the node `T*` is moving → `cycle_found` → exception → `T*` aborts and cannot commit. Contradiction: no cycle can be committed. (Symmetrically, if `T*`'s snapshot were stale it could miss the edge — which is exactly what the isolation pre-check forbids.)

### 6.4 Stated assumptions (recorded, not hidden)

1. **`READ COMMITTED`.** The argument needs each statement of the trigger to take a fresh snapshot. Rather than documenting the assumption and hoping, the guard reads `transaction_isolation` and refuses the write in `REPEATABLE READ`/`SERIALIZABLE` (`0A000`). This is a deliberate fail-closed choice: a write is never accepted on a check that could be unsound. Npgsql/the server default is `READ COMMITTED` (`ToobaNpgsql.ConfigureModuleContext` does not change the level; the two places in the repo that deliberately deviate are `Order` checkout (`ReadCommitted`) and `Wallet` refunds (`Serializable`) — the latter shows that non-default levels do occur in this codebase, which is why the guard enforces the precondition instead of assuming it).
2. **No deadlock-free claim is made about row-lock ordering**, because the walk takes no row locks at all.
3. **Superuser DDL is out of scope of the invariant**: `ALTER TABLE … DISABLE TRIGGER`, `session_replication_role = replica` and dropping the trigger can bypass any trigger-based guard. That is an operational/superuser surface, not an application write path, and it is listed again in §11.

## 7. Initial migration source (source only)

Generated with the module's existing EF design-time conventions, **without any database connection**:

```
dotnet ef migrations add InitialGeography \
  --project src/backend/Modules/Geography/Tooba.Geography.Infrastructure/Tooba.Geography.Infrastructure.csproj \
  --output-dir Persistence/Migrations --no-build
```

`Up` (in order) contains exactly: `EnsureSchema("geography")`; `CreateTable` ×9 (`countries`, `outbox_messages`, `administrative_areas`, `administrative_area_name_translations`, `cities`, `city_name_translations`, `country_name_translations`, `service_area_name_translations`, `service_areas` — all `schema: "geography"`); the `ak_administrative_areas_id_country` unique constraint and both composite FKs exactly as accepted in 040; the four translation tables with their real cascading parent FKs; all previously accepted unique/scoped indexes and the outbox pending index; then the two guard operations appended by hand at lines 344–350 (which is why they are *source*, not generated code).

`Down` removes the guard trigger and function **before** dropping the tables (lines 356–358), then drops all 9 tables.

Deliberately absent: Host composition, module migration registration/ordering, runtime DI, endpoints, seeds, regional coverage defaults, and any `database update`/apply. `Down` is authored for reversibility only, not executed.

No `Tooba.Geography.Contracts` edit was made: the guard's stable error tokens are therefore not yet declared in `GeographyErrorCodes` (Contracts is outside this Task's allowed paths). This is recorded as a follow-up item, not worked around by widening scope.

## 8. Bounded verification actually performed

A throwaway harness **outside the repository** (`%TEMP%\geo041check`) referenced the built `Tooba.Geography.Infrastructure` project, built the EF model through the design-time factory and inspected model metadata plus migration operations. It never called any `Database.*` API, never opened a connection and never touched a database. Build: `0 errors`, `0 warnings` (only the pre-existing `NU1900` NuGet vulnerability-feed timeout warning, which also appears on the untouched baseline). Result: **ALL CHECKS PASSED (36 checks)**.

- **Model metadata:** 9 tables, every table in schema `geography`; single alternate key `ak_administrative_areas_id_country` = `(AdministrativeAreaId, CountryId)`; `administrative_areas` has exactly 2 FKs, the self-parent one being the composite `(ParentAdministrativeAreaId, CountryId) → (AdministrativeAreaId, CountryId)` `Restrict`; all four translation tables keep `UNIQUE(parent, language)` + real cascading parent FK.
- **Model-derived DDL** (`GenerateCreateScript()`, no connection): exactly 9 `CREATE TABLE` statements, no schema other than `geography`, alternate-key unique constraint present.
- **Migration operations** (`UpOperations`/`DownOperations` through the migrations assembly): exactly one migration (`20261014113949_InitialGeography`); `Up` = 9 `CreateTable` (all `geography`) + only `geography` ensured + exactly 2 raw-SQL operations whose text contains the guard function and the constraint trigger, ordered after the `administrative_areas` table; `Down` = 9 `DropTable` + 2 raw-SQL operations, with guard teardown ordered before the table drop.
- **Static invariants of the guard source:** single fixed advisory key; recursion bound present *and* enforced; no row-locking read; `transaction_isolation` fail-closed check present; stable cycle token + `23514`; `WHEN` clause limited to ancestry-link changes; `DEFERRABLE INITIALLY IMMEDIATE`.

**Deterministic predicate vectors** (algorithm level: a C# mirror of the guarded walk + bound, over in-memory graphs) — a valid re-parent is accepted; self-parent `A → A`, 2-node `A ↔ B`, 3-node `X → Y → Z → X` are all rejected; re-rooting to `NULL` and re-asserting the current parent are accepted; an over-deep chain (4104 nodes) fails closed at the bound.

**What these checks cannot prove:** the harness is an EF-metadata and algorithm-level verification. It does **not** execute the guard against a PostgreSQL server and therefore does not — and is not presented to — demonstrate real trigger semantics, real advisory-lock interleaving or real isolation-level behaviour. Per the Task's explicit instruction, static assertions are **not** labelled as proof of PostgreSQL concurrent behaviour. That single item is the reason this Result is `PARTIAL`.

## 9. Database non-actions

No `database update`, no `migrate`, no apply, no connection to any live/staging/development PostgreSQL, no DDL/DML executed anywhere, no seed/import, no Host/DI/module wiring, no endpoint, no frontend, no Checkout/AddressBook/Order/Fulfillment/Payment/Settlement/Catalog/Offer/Shipping-pricing change, no manifest or architecture-lock change, no test/guard weakened, no broad test suite run, no runtime restart, no Results-API probe. Geography remains inert and **not** ARCH-COMPLETE-002 certified.

## 10. Validation commands

| Command | Result |
| --- | --- |
| `dotnet build src/backend/Modules/Geography/Tooba.Geography.Infrastructure/…csproj` (after guard wiring) | Build succeeded, 0 errors, 0 warnings except pre-existing `NU1900` |
| `dotnet ef migrations add InitialGeography … --no-build` | Generated migration + designer + snapshot, no DB connection |
| `dotnet build %TEMP%\geo041check\check.csproj` then `dotnet exec …geo041check.dll` | Build succeeded; `ALL CHECKS PASSED` (37 model/migration/static/predicate checks) |

## 11. Remaining risks and open items (for Architect decision)

1. **Empirical concurrency verification (the only blocking item).** The guard's real behaviour under two overlapping sessions (advisory-lock serialization, deferred-constraint firing, snapshot freshness, PL/pgSQL + `WITH RECURSIVE` + `CREATE CONSTRAINT TRIGGER` syntax acceptance) has not been executed. Suggested smallest closure: a single authorized test that applies this migration to a disposable PostgreSQL and runs two concurrent reparenting sessions (`A↔B` and a 3-node cycle attempt) expecting one to abort with `geography.administrative_area.ancestry_cycle`, plus one session checking the `repeatable read` fail-closed path.
2. **Isolation level is now a hard precondition.** Any future Geography write path that runs in `REPEATABLE READ`/`SERIALIZABLE` will be rejected by design (`0A000`). If the platform ever mandates a higher level for geography writes, the mechanism must be re-chosen (e.g. optimistic path + retry), not patched.
3. **Model-derived schema does not carry the invariant.** `GenerateCreateScript()`/`EnsureCreated()` produce the tables but not the guard; the invariant exists only in migration source. Any future path that creates the geography schema without running migrations would ship the tables without the cycle guard. The repo uses migrations (`ModuleSchemaMigrator`), so this is a guard-rail note, not a defect.
4. **Superuser bypass** (§6.4.3): `DISABLE TRIGGER` / `session_replication_role = replica` / manual `DROP TRIGGER` can defeat any trigger-based integrity. No application path can.
5. **Error tokens not yet in Contracts** (§7): `geography.administrative_area.ancestry_cycle`, `…ancestry_depth_exceeded`, `…ancestry_guard_isolation_unsupported` should eventually be surfaced through `GeographyErrorCodes` and the module's error/result mapping; requires a later authorized slice.
6. **Depth bound is a safety net, not a business rule.** 4096 is far above any real administrative depth; if it were ever hit, the guard fails closed (rejects) rather than accepting.
7. **D10 remains OPEN** and migration ordering/registration/Host wiring remain undecided — none of them was touched here.

## 12. Time

Claim → validated commit: **≈8 minutes** of the 30-minute hard timebox (no background retries, no idle polling).

## 13. Recommendation

Treat §11.1 as the single remaining gate before migration readiness: authorize one disposable-database concurrency test for this migration source. No code change is expected from it; if it passes, the migration source can be considered validated and the next slice can be module migration registration/ordering (still without Host behaviour change). If it fails, the failure mode will be precise (which session aborted with which SQLSTATE/token), and the mechanism — not the caller — must be corrected.
