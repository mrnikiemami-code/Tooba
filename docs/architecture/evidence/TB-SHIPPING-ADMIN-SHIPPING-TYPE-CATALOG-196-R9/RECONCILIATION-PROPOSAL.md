# TB-196-R9 — Reconciliation proposal (PROPOSED, NOT IMPLEMENTED, NOT APPROVED)

Analysis-only. Every change described here is a **proposal** requiring explicit Architect authorization.
No `src/**` change, migration, DDL, database operation, scaffold or test edit was performed in R9.

## 0. Decision frame

Three statements must not be conflated:

1. `snapshot == current model` — proved by R8 (0/0 differ operations).
2. `migrated database == current model` — **false today**; the 18 objects in `SOURCE-OF-TRUTH-MATRIX.md` differ.
3. `migrated database == intended production contract` — a product decision, not a metadata fact.

The R6 full-schema gate compares (2) exactly, so it cannot go green while both "the live DBs keep the extra guards" and "the model does not describe them" hold. Equality has to be *produced* by one of two lawful directions per object:

* **DB follows model** — additive/rename DDL;
* **model follows DB** — model annotations describing objects that already exist, then regenerating snapshot + latest designer metadata with **no DDL** (this is exactly the R8-style pipeline, so no new tooling).

Recommended overall stance: **model follows DB wherever the DB object is an existing, deliberate protection or a harmless historical default, and DB follows model only for the one object that is a genuine missing invariant (S1).** This preserves database protection, avoids destructive or rewrite DDL on populated tables, and keeps the runtime model semantics unchanged except where explicitly decided.

## 1. Object-by-object decision

### S1 — outbox pending index → **DB follows model** (additive, first slice)

* Owner: the shared outbox invariant lives in `BuildingBlocks/Tooba.Persistence/OutboxMessageMapping.cs`; the *module* owns its physical schema, and every module creates the index in its own migration. Fulfillment simply never did. So: **do not** touch the shared mapping (it is already correct and shared), add **one Fulfillment migration** that creates the index exactly as the mapping describes.
* Shape: `ix_outbox_messages_pending` on `(occurred_at)` with `WHERE processed_at IS NULL AND dead_lettered_at IS NULL`, schema `fulfillment`. Write it with `CREATE INDEX IF NOT EXISTS` raw SQL, matching the raw-SQL style already used by the neighboring shipping migrations and making the migration re-runnable on an environment that already has the index out-of-band.
* Up/Down: Up additive and data-free (no table rewrite; a `CONCURRENTLY` variant is not possible inside EF's transactional migration and is not needed at this size — flag as a decision). Down = `DROP INDEX IF EXISTS` restores the exact historical shape, so the change is lawful and reversible.
* No model/metadata edit is needed (the model already declares the index) and no app-code change is needed.
* Why first: independent of every other finding, no mapping semantics change, no data movement, and it removes the only **missing** object.

### S2/S3 — `shipping_methods` CHECK constraints → **model follows DB** (metadata only, second slice)

* The checks are deliberate defence-in-depth written in `20261015120000` with an explicit comment; **do not drop them** and do not assume "the current model always wins". The domain validator is stricter, so keeping the DB checks cannot reject any domain-valid code.
* Cheapest lawful way to remove the asymmetry **without DDL**: describe the existing constraints in the owning configuration (`HasCheckConstraint("ck_shipping_methods_code_canonical", "<verbatim SQL>")` and the option-code variant) and regenerate the snapshot/latest-designer metadata with the R8 pipeline. The database already contains both constraints, so the model then matches reality and **no migration is generated or needed**.
* Risk to control: EF compares check SQL text normalized; the model string must match the manifest expression (`length(method_code) BETWEEN 1 AND 32 AND method_code = lower(method_code) AND method_code !~ '[^a-z0-9_:]'` etc.) or a later migration will emit a drop/add pair. This must be verified by an offline differ (model vs. a model projection that carries the manifest text), not by eye.
* Alternative (not recommended): drop the constraints in a migration. Rejected — it removes database protection and widens accepted writes.
* Alternative (not recommended): leave them DB-only forever. Acceptable only if the Architect also formally excludes "DB-only guards" from the full-schema gate definition; otherwise the gate can never pass.

### Defaults — **model follows DB** (metadata only) *or* accepted asymmetry (decision D3)

* All five defaults came from migration mechanics (NOT NULL `ADD COLUMN` over populated tables) and are present in every migrated database. Two lawful routes:
  * **(a) describe them**: add `HasDefaultValue(0)` / `HasDefaultValue(string.Empty)` and regenerate metadata. Consequence to accept explicitly: `HasDefaultValue` marks the property store-generated-on-add, so EF may omit a CLR-default value from INSERT and read the DB default back. For `0`/`''` the stored result is identical, but this **is** a mapping-semantics change and must be covered by insert-path tests, not assumed harmless.
  * **(b) keep the model strict** and record the defaults as an accepted historical artifact, with the gate treating them as known-and-accepted rather than "diff = 0".
* Recommendation: **(b) for `items.quantity_*` and `shipments.shipping_method_code/_label/provider_metadata_version` until the Architect has decided whether the database should stay permissive.** Rationale: the permissive defaults can *mask* an application defect (an insert that forgets the column succeeds on the historical DB and fails on a model-built DB). Aligning the model to the DB would lock in the permissive behaviour; removing the defaults from the DB would require `ALTER COLUMN … DROP DEFAULT`, which is instant and data-free but does change behaviour for any omit-column writer. That is a contract decision, not a metadata cleanup.
* If the Architect chooses (a), it is strictly metadata + snapshot/designer regeneration: **no migration, no DDL, no data change**, because the DB already has the defaults.

### N1–N10 naming — **model follows DB** (metadata only) or **defer** (decision D4)

* Renaming is **not necessary** for runtime, query planning, index usage, uniqueness enforcement or future migration generation: the model snapshot is stable against the current model, so no future `migrations add` will touch these objects; PostgreSQL binds by OID.
* It **is** necessary only for (i) exact metadata equality in the R6 gate and (ii) operator-facing constraint names in error details.
* Preferred route when the Architect wants equality: pin the historical names in the owning configurations (`HasDatabaseName(...)` on the 9 indexes, `HasConstraintName("fk_shipping_method_translations_method")` on the FK) and regenerate snapshot/designer metadata — **zero DDL, zero data movement, zero lock, fully reversible in code**, and it makes a model-built database produce the historical shapes.
* Rejected alternative: `ALTER INDEX … RENAME TO` / `ALTER TABLE … RENAME CONSTRAINT` (10 statements). Instant and metadata-only, but it mutates every environment including production, changes operator-visible error text, and needs its own reviewed Up/Down. Only justified if the Architect prefers the *database* to converge on code-generated names instead of the opposite.
* Rejected alternative: make the gate ignore names wholesale, or accept a blanket baseline. That hides real differences (e.g. a genuinely missing index) and is explicitly out of bounds.
* Note on truncated names: three model-side names are 63-character truncations of longer generated names; they are still valid identifiers, but if names are pinned to the historical short forms this awkwardness disappears.

## 2. Dependency order and the recommended FIRST slice

| Slice | Content | Depends on | DDL | Model change | Gate effect |
| --- | --- | --- | --- | --- | --- |
| **1 (recommended next)** | Outbox pending index, one Fulfillment migration (`CREATE INDEX IF NOT EXISTS`, Down `DROP INDEX IF EXISTS`) | nothing | additive, data-free | none | removes the only *missing* object |
| 2 | CHECK constraints described in the model + metadata regeneration | Architect approval of "model follows DB" | none | metadata only | removes 2 rows |
| 3 | Defaults decision (describe or accept) + metadata regeneration if (a) | D3 | none (or `DROP DEFAULT` if the DB is tightened instead) | metadata only | removes 5 or 0 rows |
| 4 | Names pinned to historical identities + metadata regeneration | D4 | none | metadata only | removes 10 rows |

Slices 2–4 are independent of each other and of Slice 1; they can be reordered. Slice 1 is recommended first because it is the only *functional* finding and the only one whose change is unconditional.

## 3. Decisions the Architect must make (no worker may assume them)

* **D1** — Is `ix_outbox_messages_pending` to be added in Fulfillment, and is `CREATE INDEX IF NOT EXISTS` (non-concurrent, inside the migration transaction) acceptable, or is a concurrent/production-window note required?
* **D2** — Keep the two `shipping_methods` checks as database protection (recommended) or drop them? If kept, are they described in the model, or excluded from the parity definition?
* **D3** — Defaults: describe them in the model (permissive contract becomes explicit and EF insert semantics change subtly) or keep the model strict and treat the historical defaults as an accepted artifact? Is `ALTER COLUMN … DROP DEFAULT` ever authorized on live data?
* **D4** — Names: pin the historical names in the model (zero DDL, recommended if equality is required), rename the database objects (10 DDL statements), or declare naming-only equivalence for the gate with full-shape verification?
* **D5** — Which authority signs off "the historical database shape is the production contract" versus "the model is the contract"? R9 can prove facts but cannot choose the contract.

## 4. Future proof plan for the eventual repair task(s) — not executed here

For each authorized slice, the accepting task must require:

1. throwaway PostgreSQL (never a shared port), apply the **real historical chain** to `20261016120000`, capture the manifest;
2. apply the proposed migration(s) **and** the regenerated metadata; capture the manifest again;
3. prove the delta is exactly the intended object set and nothing else (no unrelated table/column/default/index movement, no row changes — verify row counts and a sample payload hash for `outbox_messages`);
4. prove model↔snapshot↔latest-TargetModel zero-diff again (R8 pipeline) — snapshot equality must be re-proved after any metadata regeneration;
5. run the real `Down` where lawful and prove the pre-slice manifest is restored byte-for-byte;
6. only then, separately, run the storage/constraint/race battery (raw-SQL negatives, unique/revision races, mid-save rollback/cancellation, lookalike-constraint evidence) — currently unexecuted and not covered by any of these slices;
7. report `PARTIAL` for anything not actually run; never assert gate-green from a metadata-only proof.

Old applied migrations and designers stay immutable: no slice may amend executable history, seed, backfill or destroy rows.

## 5. Constraints honoured while writing this proposal

No `src/**`, build, test, `dotnet ef`, scaffold, PostgreSQL/container/server/browser/HTTP call, manifest/certification/skill change, guard weakening, baseline filter, or Bridge probe. Only documentation plus the received-task receipt and the recovery worker pointer were written.
