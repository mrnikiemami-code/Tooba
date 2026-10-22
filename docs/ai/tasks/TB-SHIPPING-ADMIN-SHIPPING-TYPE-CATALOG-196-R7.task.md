PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R7
Parent-Task: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R6
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 917677eb57987d95362eac484827acdf1ace4f83
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_TYPE_MIGRATION_SNAKE_CASE_FACTORY_AND_UPDOWN_REPAIR
Hard-Timebox: 35 minutes HARD including cleanup/commit/push/result. Stop new coding/tests at minute28; reserve7minutes. Disposable setup<=8minutes. STOP/PARTIAL on expiry; no repeated failing-migration loop.
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

MISSION
Repair the concrete latest type-migration identifier defect and its design-time naming cause, then prove isolated latest Up/Down adds/removes ONLY the two new type tables without changing the ACTUAL historical legacy schema. Strengthen the R6 full-schema gate to stop when its prerequisite is unknown/failed.
This is a NARROW Architect-authorized exception for the latest unaccepted/unshipped migration. NOT a legacy schema reconciliation, full snapshot regeneration or completion of every relational acceptance fact. TB196 remains BLOCKED pending the separate legacy/snapshot and storage/atomicity gates even if this repair passes.

FIRST CHECK
Fetch and verify HEAD == origin/main == Expected-Base-SHA and inspect dirty/untracked work before edits; mismatch/unsafe tree => STOP without edits. No reset/stash/rebase/force-push.
Read AGENTS.md/current recovery, TB-196-R6 ARCHITECT-REVIEW.md and failure evidence, repository backend-feature and applicable AMSC Migrate/Structure skills. Inspect exact runtime ToobaNpgsql.ConfigureModuleContext, FulfillmentDbContextFactory, latest type migration/designer/snapshot type blocks and test fixtures.
Record hashes for all older migrations/designers and unrelated snapshot blocks. All must remain unchanged. Current program records no shared/deployed application of this broken latest migration. If evidence shows an applied/deployed copy elsewhere, STOP for an explicit upgrade plan, do not rewrite that environment's history or probe shared DB.

AUTHORIZED CHANGE 1 — DESIGN-TIME/RUNTIME NAMING
FulfillmentDbContextFactory currently uses bare UseNpgsql; production uses canonical ToobaNpgsql module configuration with snake_case and module history conventions. Make this module's factory reuse that canonical existing configuration. Preserve a safe design-time placeholder/explicit design-time input without resolving real tenant credentials or opening a connection. No global ToobaNpgsql/Persistence change, no startup schema application or Host edit.
Test factory-created versus canonical runtime-configured design-time relational metadata for the TWO type entities: same snake_case table/column names, PK/FK/index identifiers and relevant checks. This is type-surface parity, not proof that the complete old snapshot/model is equivalent.
Do not claim has-pending-model-changes==false or re-scaffold every model entity to achieve it. Broader legacy metadata divergence is a recorded blocker.

AUTHORIZED CHANGE 2 — LATEST UNACCEPTED MIGRATION ONLY
Correct ONLY 20261022101415_ShippingTypeCatalog Up/Down and corresponding TWO type-entity/relationship metadata blocks in its .Designer and FulfillmentDbContextModelSnapshot:

actual columns shipping_type_id/code/sort_order/is_active/revision/created_at/updated_at and translation row/type/language/name/description columns must align with canonical provider model;
PK/FK column references and index columns must use those actual names; own PK/FK names follow the provider's actual canonical model, not a guess;
preserve exact owned business-index names ix_shipping_types_code, ix_shipping_types_active_sort, ix_shipping_type_translations_type_language, stable CHECK identities and their business semantics;
no legacy CREATE/ALTER/DROP/rename/index/default/constraint/data change in Up or Down. Down drops ONLY the two new tables in safe dependency order.
Why amending this latest migration is authorized: it is unaccepted/unshipped and its CREATE TABLE intrinsically fails42703 before any successful application; a later corrective migration cannot execute past it. Do not amend ANY older migration/designer. Keep id/class identity; do not create a second replacement chain.
No wholesale snapshot/designer regeneration or reserialization: unrelated entities, properties/defaults/checks/indexes/annotations stay byte-identical. Explicitly list changed metadata blocks and compare normalized type metadata to canonical runtime configuration. No general naming-convention "cleanup" on legacy entities. Runtime EF mapping has no proven naming defect; do not change unrelated mapping or Domain/API behavior.

AUTHORIZED CHANGE 3 — FAIL-FAST TEST GATE, NO FALSE DOWN
R6 InitializeAsync recorded SnapshotEqualsCurrentModel=false and substantive legacy differences yet proceeded to latest migration. In full-schema mode, throw with concrete diagnostic BEFORE dependent model-projection/Up acceptance if equivalence is unproved, and BEFORE latest Up if substantive discrepancies exist. Do not turn unknown into true, skip a guard or ignore a legacy difference.
Teardown must mark DownExecuted ONLY if latest Up actually succeeded/history row existed and actual Down was executed; failed initialization with no latest application is cleanup/no-op, NOT Down parity.
Add a small focused negative proving the full-schema gate does not call the next phase on a failed/unknown prerequisite. Do not run the entire blocked R6 collection repeatedly. Preserve all legacy-drift evidence/history and keep the full-schema gate blocked.

ISOLATED TYPE UP/DOWN PROOF — SEPARATE, EXPLICITLY BOUNDED
Because full legacy/snapshot parity is unresolved, THIS TASK separately authorizes an independent latest-type-only test using a fresh disposable ACTUAL historical schema as before/after baseline. It MUST NOT suppress/replace the strengthened full-schema acceptance gate or call global parity PASS.

Fresh postgres:17-alpine container, unique task name, 127.0.0.1:55436 only, role tb196, primary fulfillment_tb196; verify free port and parsed target before connection. Reuse pure parsed validation helper without initializing the blocked whole-schema fixture. Exact host/port/db/role, reject multi-host/shared targets; assert live current_database/current_user and container binding before DDL. No credentials logged.
Apply ACTUAL older migration chain up to20261016120000_AddFulfillmentShippingGroups; capture full normalized legacy pg_catalog manifest (columns/types/nullability/defaults, CHECK/PK/FK/unique/index definitions including meaningful names/predicates). This historical schema, not a model-generated copy, is the authoritative before baseline for this isolated proof.
Apply corrected latest migration ONCE through actual provider/context conventions; assert latest history row, exactly two new own tables, no quoted PascalCase columns, actual own constraints/indexes/FK exist. Raw manifest diff: no old object removed/changed, ONLY new type-owned objects added.
Compare the TWO new tables to canonical runtime type-surface metadata/expected DDL. Do not declare entire snapshot equivalence or hide legacy drift with a substring filter. Exclude only exact new table identities and migration-history rows from legacy comparison.
Own-store smoke: create valid sort0 inactive revision1 with a real translation; NEW Npgsql context reads exact id/code/name/language/revision. This checks ORM column compatibility, not full storage/race acceptance.
Execute real latest Down on this disposable DB, assert history row removed + both type tables absent, and full legacy manifest EXACTLY equals historical before baseline. Record actual Up and Down executions. Optionally re-Up only if necessary for a focused assertion, not repeated confidence runs.
Clean exact container/listeners;55436 free afterward. No shared5432/tooba_alpha/store-alpha, model comparison DB, Host/MigrationRunner, provider integration, Redis/RabbitMQ or shared apps used.
No storage negative matrix, concurrency/unique race or rollback battery in this task. These remain pending; existing assertions are not labelled executed.

VERIFICATION / FAILURE DISCIPLINE
Build only Fulfillment.Tests and project references with isolated output if needed. Run only factory/type-metadata test, one standalone isolated Up/Down/store-smoke filter and fail-fast/bookkeeping negatives; pure target offline negatives may run outside blocked collection. No entire ShippingTypePostgresStorageTests or full module/solution/Host/Architecture suite. Commands<=120s.
If a legitimate current type defect beyond identifier alignment appears, record it and report PARTIAL/BLOCKED; no broader repair, baseline or expected-rule changes. Missing/unsafe setup=>stop. No package install/cache purge/shared process stop/restart. A failed old snapshot gate is a recorded prerequisite, not a reason to edit old schema in this task.
Allowed production files: design-time factory section of FulfillmentDbContext.cs; latest type migration; ONLY type entity/relationship blocks in its designer/current snapshot. No other production edits, new dependency/project/Contracts/resource/Endpoint/Domain/Shipping/frontend/Host/manifest/skill changes. Test helpers may be minimally edited as described; no folder refactor.
No outbox pending-index creation, old shipping_methods CHECK/default/index changes or naming reconciliation. No shared DDL, backfill/data seed, price/quote/promise/customer grouping, Shipping assignment/UI/carrier/funder/Payment/Settlement/Promotion/activation/deployment/flag change. TB185 paused, default-OFF/PendingAllocation intact.
Preserve unrelated dirty/untracked/stashes, no reset/stash/rebase/force-push, no Bridge dummy/probe payload.

EVIDENCE / STOP
Persist received artifact verbatim in docs/ai/tasks/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R7.task.md and sanitized docs/architecture/evidence/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R7/RESULT.md with before/after script, runtime-vs-factory type metadata, actual Up/Down history/manifests/diff, own-store new-context smoke, fail-fast proof, older-file/unrelated-snapshot hash parity and parsed-target/cleanup. Append R6 correction: proceeding after Gate1 failure was a process deviation; no-op teardown was not Down proof;209 differences were not established as an invalid comparison merely by prose.
Record exact commands/counts, UTC timing and limits. Update recovery Worker pointer only. PASS means corrected type migration and bounded proof, NOT full TB196 acceptance. Keep BLOCKED_PENDING_LEGACY_SNAPSHOT_AND_RELATIONAL_ACCEPTANCE explicit. Commit/push/fetch parity; cleanup and STOP_FOR_ARCHITECT. No future task.

RESULT FORMAT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R7
Parent-Task: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R6
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_TYPE_MIGRATION_SNAKE_CASE_FACTORY_AND_UPDOWN_REPAIR
Bridge-Task-Row: ...
Status: PASS|PARTIAL|BLOCKED
Expected-Base-SHA: 917677eb57987d95362eac484827acdf1ace4f83
Base-SHA: ...
Code-SHA: ...
Evidence-SHA: ...
HEAD-Origin-Parity: ...
DesignTimeFactory-CanonicalRuntime-TypeParity: ...
LatestUnshippedMigration-Only-IdentifierCorrection: ...
DesignerSnapshot-OnlyTypeBlocks-And-OlderByteParity: ...
FullSchemaGate-FailFast-And-HonestDownBookkeeping: ...
ParsedTarget-And-Live-Provenance: ...
ActualHistoricalBaseline-LatestUp-And-OnlyTwoTables: ...
NewContext-OwnStore-Smoke: ...
ActualDown-And-ExactLegacyManifestParity: ...
Focused-Commands-And-Test-Counts: ...
Changed-Files: ...
LegacySchema-SharedDatabase-DataFlag-Changes: NONE
LegacySnapshot-And-FullRelationalAcceptance: PENDING_BLOCKED
TB196-Acceptance: BLOCKED_PENDING_LEGACY_SNAPSHOT_AND_RELATIONAL_ACCEPTANCE
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