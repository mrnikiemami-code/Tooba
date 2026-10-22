PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R10
Parent-Task: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R9
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: f3021742944786c2bd5316fb9ba21f2ab5acaf98
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_FULFILLMENT_PENDING_OUTBOX_INDEX_MIGRATION_AND_ISOLATED_PROOF
Hard-Timebox: 35 minutes HARD including cleanup/commit/push/result. Stop new implementation/tests at minute28; reserve7. Disposable setup <=8min; each command <=120s. Report PARTIAL/BLOCKED on expiry, no confidence loops or extensions.
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

MISSION / EXACT ARCHITECT DECISION
Prepare ONE additive Fulfillment migration creating the missing shared-model index:
schema fulfillment; table outbox_messages; name ix_outbox_messages_pending;
nonunique btree on occurred_at;
predicate processed_at IS NULL AND dead_lettered_at IS NULL.
Use normal transactional migrationBuilder.CreateIndex (or exact equivalent strict CREATE INDEX), with Down dropping ONLY that index. NO IF NOT EXISTS, no CONCURRENTLY/suppressTransaction, no automatic adoption/drop/rebuild of a preexisting same-name object. EF history owns repeat-application semantics.
This decision supersedes R9's suggested IF NOT EXISTS/Down IF EXISTS mechanics ONLY for this index. No other proposed reconciliation is approved.

FIRST GATE / REFERENCES
Fetch, verify HEAD == origin/main == Expected-Base-SHA and inspect dirty/untracked work before edits. Mismatch/unsafe divergence => STOP exact state without edits; no reset/stash/rebase/force-push.
Read AGENTS.md, current recovery, R9 ARCHITECT-REVIEW, applicable .cursor backend-feature/AMSC Migrate/Structure-on-changed-surface/migration guidance, R8 metadata proof, shared OutboxMessageMapping and existing module migration conventions.
R9 correction: raw historical/model legacy manifests have192/191 lines, not192/216; difference16missing/17unexpected =3 one-sided +5default pairs+10name pairs. Those are saved disposable-fixture facts, not live DB discovery.

PRODUCTION SCOPE
Only ONE new migration .cs and its canonical generated .Designer.cs/TargetModel under own Fulfillment Infrastructure Persistence/Migrations.
No new project/package or shared configuration. Existing OutboxMessageMapping, runtime FulfillmentDbContext/factory and current snapshot already describe the index and must remain byte-identical. EVERY preexisting migration/designer (including ShippingTypeCatalog) immutable.
Construct the NEW migration TargetModel from unchanged canonical IDesignTimeModel via existing installed generator/code-generation route. Preserve correct context/namespace/new migration id identity. Snapshot content must not change; any task-owned temporary scaffold safely removed by exact paths, no generated legacy Rename/Alter/Drop executable retained. If tooling cannot provide canonical metadata in this bound, STOP; no tool install/upgrade.
New Up operation set exactly ONE CreateIndex; Down exactly its DropIndex; no table/column/data/default/CHECK/key/other index or schema operation. No second outbox authority, no dispatcher/service/DI/Host change.
Add only focused tests in Fulfillment.Tests using existing conventions. Small task-owned fixture/helper allowed, no shared fixture redesign or new runtime abstraction. If an existing guard legitimately pins the complete migration universe, extend only its intentional new member without weakening semantics; explain exact change. Do not overwrite tests that prove the historically pinned ShippingTypeCatalog target.

DISPOSABLE DATABASE — EXPLICITLY AUTHORIZED HERE ONLY
Fresh task-owned postgres:17-alpine container using existing locally available image; no image/package download. Published only 127.0.0.1:55436, role tb196, database fulfillment_tb196, task-only credentials. Verify port free before creation. Parse connection using NpgsqlConnectionStringBuilder BEFORE any connection: exact host127.0.0.1/port55436/databasefulfillment_tb196/usertb196; reject multi-host/localhost/shared targets. Reuse existing fail-closed target parser as appropriate; never use substring checks. Assert live current_database()/current_user and exact container binding before any DDL. No credentials in logs/evidence.
No shared :5432/tooba_alpha/store-alpha or other database; no Host/startup migrator/deployment runner. Setup failure at8min => BLOCKED + cleanup, no rescue loop.
Apply real existing migration chain THROUGH 20261022101415_ShippingTypeCatalog to this fresh target, then capture BEFORE. This historical baseline is not a model-created database. New migration applies only to this disposable DB.
Do not run the known-blocked full-schema fixture as a prerequisite for this independent one-index proof. Do not edit/filter/suppress that gate: CHECK/default/name blockers remain open.

REQUIRED EXECUTED PROOF
A. Offline shape + metadata:

actual new migration Up/Down operation universe is exactly the authorized index pair, correct schema/table/columns/filter/nonunique.
snapshot AND NEW latest TargetModel compare to unchanged canonical IDesignTimeModel via real initialized relational IMigrationsModelDiffer ->0/0; R8 original type-target proof remains intact.
runtime/factory/current snapshot/all older artifacts hash/diff unchanged; no executable legacy operation generated.
B. Actual Up on historical baseline:
Seed test-owned synthetic outbox rows covering pending, processed and dead-lettered states (with valid required metadata); no dispatcher runs. Capture stable complete row values/hash + counts before Up.
Capture normalized pg_catalog manifest with identities/columns/unique/access method/filter/valid-ready flags. Up adds exactly the index plus its migration history row; otherwise whole own-schema manifest byte-equivalent. Compare exact object identities, not broad contains/exclusion tokens.
New-context query asserts index actual shape + indisvalid/indisready; all seeded rows and states byte/value-identical; no performance benchmark/planner claim.
C. Genuine Down / repeat:
Execute actual Down for JUST new migration back to previous ShippingTypeCatalog. Verify new history row absent, only new index removed, baseline manifest and complete synthetic data identical. This is real Down, not cleanup/no-op equality.
Reapply Up then apply migrate-to-same-target AGAIN; history gating produces one index/one history row with no schema/data changes (no need for CREATE IF NOT EXISTS). Record actual execution.
D. Preexisting-name fail-closed:
After genuine Down on the disposable target, create a task-owned conflicting same-name index with WRONG shape. Attempt new migration: must fail with provider duplicate-relation outcome (e.g. SQLSTATE42P07), no new history row, no changed row state, no adoption/drop/rebuild; existing object remains identical.
Also prove existing EXACT-shape same-name index is NOT silently adopted: same fail/no-history/preserved-object behavior. These are disposable test-owned objects only, remove by exact owned identity between cases.
Do not invoke Down on an unapplied migration to delete a preexisting index. No application error mapper change.
If any failure occurs, capture exact behavior and STOP for review, not widen into shared reconciliation.

FOCUSED CHECKS / LIMITS
Build only Fulfillment.Tests and required references (isolated task output if locks; do not stop shared Host). Run new index-specific proof once plus TypeCatalogModelMetadataParityTests and relevant R7 factory metadata tests only. One bounded test-harness-only correction/rerun allowed with failed evidence preserved. No full module/Shipping/Host/solution build/suite, no broad Architecture sweep, no storage/unique/revision-race battery unrelated to this index.
New test fixtures must fail closed if target absent; no skipped facts counted as proof. Keep original full-schema guard and historical classification truthfully blocked; index-only acceptance does not close196.
No CHECK/default/name mapping, DEFAULT drop, constraint/index renames, metadata regeneration of older artifacts or baseline exceptions. Mapping default semantics and other decisions remain deferred.
No Domain/Application/Contracts/Endpoints/auth/resource/shared persistence/Host/frontend/Shipping/dependency/manifest/certification/skill/flag changes. No assignment/UI/quote/payment/promise/carrier/funder/cutoff/activation/deployment. TB185 paused; default-OFF/PendingAllocation untouched.
No unrelated dirty/untracked/stashed edits or resets; no shared apps/ports/cache/bin/obj/.next cleanup. Never dummy/probe POST Bridge /api/results.

EVIDENCE / DELIVERY
Persist received task verbatim docs/ai/tasks/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R10.task.md.
docs/architecture/evidence/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R10/: RESULT.md, sanitized generated forward/down SQL, normalized before/after/down manifests, row-preservation proof, duplicate-name results, canonical metadata proof, hashes, exact commands/counts/UTC/timebox and cleanup.
Record transactional index creation may block writes on large live tables; size/window unknown and no production deployment authorized. Existing index out-of-band requires a separately reviewed adoption/deployment plan, not an IF NOT EXISTS shortcut.
Append worker status pointer to recovery; append R9 evidence correction if desired (model191 and mechanics superseded), do not rewrite historical evidence.
Remove only task-owned container/output/scaffolds, verify no55436listener remains. Commit scoped files, push fast-forward, fetch verify HEAD==origin/main, post complete result and STOP. Do not generate/execute next task.

RESULT FORMAT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R10
Parent-Task: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R9
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_FULFILLMENT_PENDING_OUTBOX_INDEX_MIGRATION_AND_ISOLATED_PROOF
Bridge-Task-Row: ...
Status: PASS|PARTIAL|BLOCKED
Expected-Base-SHA: f3021742944786c2bd5316fb9ba21f2ab5acaf98
Base-SHA: ...
Code-SHA: ...
Evidence-SHA: ...
HEAD-Origin-Parity: ...
OnlyIndex-UpDown-OperationSet: ...
Canonical-Snapshot-NewTarget-ModelParity: ...
HistoricalChain-UpDown-Reapply-And-ManifestDelta: ...
Complete-Outbox-RowPreservation: ...
WrongShape-And-ExactShape-PreexistingName-FailClosed: ...
ParsedTarget-LiveProvenance-And-Cleanup: ...
OlderArtifact-Runtime-Snapshot-ByteParity: ...
Focused-Commands-And-Test-Counts: ...
Remaining-Checks-Defaults-Names-And-Relational-Gates: PENDING_BLOCKED
TB196-Acceptance: BLOCKED_PENDING_HISTORICAL_SCHEMA_AND_RELATIONAL_ACCEPTANCE
Shared-Database-Data-Flag-Changes: NONE
Changed-Files: ...
Claim-UTC: ...
Result-UTC: ...
Elapsed-Minutes: ...
Hard-Deadline-Compliance: ...
Out-Of-Scope-Commands: NONE
Production-Functional-Acceptance: PENDING
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
Automatic-Next-Task: NONE
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK