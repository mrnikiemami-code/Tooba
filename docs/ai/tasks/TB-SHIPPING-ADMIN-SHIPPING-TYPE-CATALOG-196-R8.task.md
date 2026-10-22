PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R8
Parent-Task: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R7
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: abf7072aef02783cca6576180c8344bfe3ad09d1
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_CANONICAL_SNAPSHOT_AND_LATEST_TARGET_METADATA_ALIGNMENT
Hard-Timebox: 25 minutes HARD including cleanup/commit/push/result. Stop new work at minute20; reserve5minutes. PARTIAL on expiry; no tooling installation/scaffolding loop.
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

MISSION
Align ONLY the current FulfillmentDbContextModelSnapshot and the latest unshipped ShippingTypeCatalog.Designer BuildTargetModel metadata with the UNCHANGED canonical current design-time model after R7's factory repair. Prove their relational model equality offline. No executable migration, runtime mapping, DB/DDL, business code or legacy reconciliation change.

FIRST CHECK / EXACT AUTHORITY
Fetch and verify HEAD == origin/main == Expected-Base-SHA and inspect dirty/untracked files before edits; mismatch/unsafe divergence => STOP without edits. No reset/stash/rebase/force-push.
Read AGENTS.md/current recovery, R6/R7 Architect reviews, backend-feature skill and applicable migration instructions. R7's restriction to type-only snapshot blocks is explicitly superseded HERE ONLY for TWO metadata artifacts:

Tooba.Fulfillment.Infrastructure/Persistence/Migrations/FulfillmentDbContextModelSnapshot.cs
.../20261022101415_ShippingTypeCatalog.Designer.cs (generated target model only)
The corresponding 20261022101415_ShippingTypeCatalog.cs Up/Down, current runtime DbContext/factory, all older migration/designer, dependencies/flags and every business file remain BYTE-IDENTICAL.
Reason: old pre-type metadata was generated using a design-time factory without canonical snake_case conventions. R7 corrected that factory and new table identifiers, but complete snapshot/current-model agreement remains unproved (reported191 operations). Metadata repair must follow the existing runtime model; do NOT alter runtime model to fit stale metadata.

BOUNDED WORK

Offline baseline:
Construct actual canonical FulfillmentDbContextFactory model / IDesignTimeModel with the existing project's provider/conventions. Resolve and initialize the CHECKED-IN snapshot and latest migration TargetModel using canonical EF design-time services/processing before relational comparison. Capture precise old IMigrationsModelDiffer operations (kind + table/column/index/FK identity + old/new shape where applicable); do not assert an exact count191 from a previous report as the authority.
If initialization path is wrong, establish the actual canonical pipeline from the installed EF APIs and report any remaining uncertainty; do not dismiss differences as "non-design-time" merely in prose, set ignore lists or bypass initialization. No database connection.

Canonical metadata generation:
Use the existing EF design-time code generator/scaffolder and the UNCHANGED canonical model. Regenerate the snapshot's model metadata and latest Designer's target metadata only; keep context type, namespace, migration attribute/id and class identity unchanged. Use existing installed project/tool versions; no install, new package/project or EF upgrade.
Prefer generator APIs that emit metadata without a migration. If a temporary scaffold is the only available route, all scratch output must be task-owned, no generated migration/DDL committed, no older artifact edited, and exact task-created temporary files removed safely. NEVER execute or retain generated Drop/Rename/Alter operations against legacy tables. Do not use git reset/stash to clean a scaffold.
This authorization may update legacy entity NAMING metadata to describe the actual current model; it does not authorize changing that model's properties/constraints/defaults/indexes. No invented claim that regenerated metadata matches the historically migrated database.

Offline proof after generation:
Use real IMigrationsModelDiffer on initialized relational models:

current snapshot vs actual canonical current IDesignTimeModel -> ZERO operations;
latest TargetModel vs the same current model -> ZERO operations;
factory vs runtime configuration type-surface parity retained from R7.
Prove metadata dimensions through a normalized relational description (schemas/tables/columns/types/nullability/defaults, PK/FK/index/check names/definitions), not only source-string/count checks. Add a focused durable metadata-parity regression for snapshot AND latest TargetModel. Keep all existing hard architecture/dependency/schema guard semantics; no skip or baseline refresh.
Record generation provenance/version and detailed before/after semantic diff. Confirm new type metadata retains corrected snake_case columns, owned CHECK/index/FK identities and root Revision concurrency token. Confirm no current runtime model semantics changed.
Preservation:
Hash/diff latest executable migration .cs Up/Down and all older migrations/designers, current DbContext/factory and non-metadata production paths; zero changes. The only production changes are the TWO generated metadata files. No new migration file/id/history, no seed/backfill. Do not overwrite unrelated dirty files or untracked helpers.
Do NOT amend the latest migration's executable body under this task just because it is still unshipped. R7's isolated real Up/Down acceptance remains reusable by exact source identity, not re-run.

KNOWN REMAINING GATES — DO NOT HIDE THEM
The real historical schema differences from R6 (outbox pending index absent from migrations, two migration-only shipping_methods checks, column defaults and names) remain open. Snapshot==current model is NOT snapshot==historical schema. Runtime model unchanged means those concerns remain independently recorded; no adding/removing an index/check/default, no constraint rename, no schema reconciliation DDL or model filter to manufacture equality.
No PostgreSQL/container/migration-runner/Host/server/browser/network/seed/schema operation in this metadata-only slice. Do not run the blocked whole-schema collection or full storage/race suite merely to rediscover the next known gate. The full-schema gate remains fail-closed; do not suppress its substantive legacy discrepancy check.
Storage raw SQL negatives, unique/revision races, mid-save rollback/cancellation and exact-constraint lookalike evidence are STILL UNEXECUTED. Do not claim product readiness, module recertification or all-green release.

FOCUSED CHECKS / FAILURE RULE
Build only Fulfillment.Tests and its required references, task-owned isolated output if locked. Run new offline snapshot/TargetModel parity tests plus R7 factory/type metadata tests and exact relevant golden boundary if needed. No full module/solution/Host build/test, broad Architecture sweep, package install or repeated confidence runs. Commands <=120s.
If actual current model differs in a way requiring production mapping/schema/business change, STOP/PARTIAL with exact delta; no wider fix. One bounded harness-only correction/rerun is allowed; preserve failed evidence.
No Domain/Application/Contracts/Endpoint/DI/resource/Host/Shipping/frontend/manifest/skill/public API change. No assignment/UI/customer grouping/pricing/quote/promise/carrier/funder/Payment/Settlement/Promotion/activation/deployment/flags. TB185 paused, default-OFF/PendingAllocation untouched. No shared ports/apps/bin/obj/.next/cache mutation, reset/stash/rebase/force-push or Bridge probe/dummy result.

EVIDENCE / STOP
Persist received artifact verbatim in docs/ai/tasks/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R8.task.md. Sanitized docs/architecture/evidence/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R8/RESULT.md: canonical generation method/version, exact baseline operations, new zero-operation proof for BOTH artifacts, normalized semantic metadata descriptions/diff, unchanged executable/runtime/older-file hashes, exact commands/counts, cleanup, UTC claim/result.
Update recovery Worker pointer only. PASS means metadata alignment only; whole TB196 remains BLOCKED_PENDING_HISTORICAL_SCHEMA_AND_RELATIONAL_ACCEPTANCE. Future legacy reconciliation needs its own reviewed decision/task; do not produce that next task or execute it automatically.
Commit scoped files, push fast-forward, fetch verify HEAD==origin/main, cleanup only task-created scratch and STOP_FOR_ARCHITECT.

RESULT FORMAT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R8
Parent-Task: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R7
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_CANONICAL_SNAPSHOT_AND_LATEST_TARGET_METADATA_ALIGNMENT
Bridge-Task-Row: ...
Status: PASS|PARTIAL|BLOCKED
Expected-Base-SHA: abf7072aef02783cca6576180c8344bfe3ad09d1
Base-SHA: ...
Code-SHA: ...
Evidence-SHA: ...
HEAD-Origin-Parity: ...
Canonical-Generation-And-Initialization-Provenance: ...
Baseline-Concrete-Relational-Operations: ...
Snapshot-Versus-CanonicalModel-ZeroDiff: ...
LatestTargetModel-Versus-CanonicalModel-ZeroDiff: ...
Normalized-Metadata-Semantics-And-TypeParity: ...
ExecutableMigration-RuntimeModel-OlderFile-ByteParity: ...
Focused-Commands-And-Test-Counts: ...
Changed-Files: ...
Database-DDL-Data-Flag-Changes: NONE
HistoricalSchema-And-FullRelationalAcceptance: PENDING_BLOCKED
TB196-Acceptance: BLOCKED_PENDING_HISTORICAL_SCHEMA_AND_RELATIONAL_ACCEPTANCE
Isolation-And-Cleanup: ...
Claim-UTC: ...
Result-UTC: ...
Elapsed-Minutes: ...
Hard-Deadline-Compliance: ...
Out-Of-Scope-Commands: ...
Production-Functional-Acceptance: PENDING
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
Automatic-Next-Task: NONE
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK