PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R6
Parent-Task: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R5
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 6c5098dddac5d48ec8932f5bbfa22874bc8fc0e5
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_TYPE_CATALOG_POSTGRES_SNAPSHOT_STORAGE_AND_ATOMICITY_PROOF
Hard-Timebox: 45 minutes HARD including cleanup, scoped commit/push/result. Stop new test work/execution at minute38; reserve7minutes. Bounded setup <=8minutes; if unavailable report PARTIAL/BLOCKED, no installation loop.
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

MISSION
TEST/EVIDENCE ONLY: close the real PostgreSQL/snapshot/constraint/atomicity acceptance gap for the EXISTING Fulfillment type catalog, and correct the single-active registry fixture. No production, EF mapping, migration/designer/snapshot or new feature change. R2 Domain, R3+R4 API, and bounded R5 DI/listener evidence is reusable by unchanged source identity. Full TB196 acceptance remains Architect-owned.

FIRST CHECK
Fetch and verify HEAD == origin/main == Expected-Base-SHA and inspect dirty/untracked work BEFORE edits. Mismatch/unsafe divergence => STOP without edits; no reset/stash/rebase/force-push. Read AGENTS.md/current recovery, TB-196 and R1/R2/R4/R5 reviews, backend-feature skill, actual type store, Domain, DbContext, latest type migration/designer, regenerated snapshot and historical migration chain. No broad module audit or recertification.
Hash/record the migration/designer/snapshot/EF mapping baseline. These files MUST remain byte-identical at finish. Do not re-scaffold to make a check green. A fresh generated script or has-pending-model-changes alone is NOT migrated-schema parity.

ISOLATION — BEFORE ANY WRITE
Use one FRESH task-owned postgres:17-alpine container, unique name, only 127.0.0.1:55436; verify port free first. Task role tb196; primary DB fulfillment_tb196. A SECOND database fulfillment_tb196_model in this SAME disposable container is authorized solely for expected-model/schema comparison. No other database/role/port/host.
Parse all target strings with NpgsqlConnectionStringBuilder BEFORE connection: exact loopback host,55436, database in the TWO named task databases, exact tb196 role, no multi-host; missing/mismatch/shared target => fail closed. Allowed text in ApplicationName is not proof. Before DDL/data writes assert live current_database()/current_user and actual container port binding. Never log credentials. Include a few OFFLINE negatives for foreign/multi-host/shared5432/sharedDB/role; no shared DB probe.
No Host/MigrationRunner/startup migrator, Redis/RabbitMQ/shared apps/:5432/tooba_alpha/store-alpha/postgres DB use. Apply real module migrations through the explicit test-owned provider path only. No package/image installation loop; if setup cannot fit8minutes stop safely. Cleanup exact task container and verify55436 free even on early blocker.

GATE 1 — HISTORICAL SCHEMA VERSUS REGENERATED SNAPSHOT (FIRST)
TB196 regenerated a stale two-table snapshot to the whole current model. This is not yet proved safe.
A. Primary task DB: apply the ACTUAL checked-in Fulfillment migration chain only up to the migration immediately BEFORE 20261022101415_ShippingTypeCatalog. Capture a normalized pg_catalog manifest of legacy fulfillment objects: tables/columns/types/lengths/nullability/defaults; PK/unique/FK/CHECK definitions; indexes including uniqueness/predicates. Do not simply count tables.
B. Model task DB: materialize expected schema from the checked-in regenerated ModelSnapshot, using canonical EF initialization/model-differ/script mechanisms. If using GenerateCreateScript from current model instead, FIRST prove initialized snapshot and design-time current model are relationally equivalent with EF's actual model differ. has-pending-model-changes is only supplementary.
C. Compare expected LEGACY objects (exclude ONLY the two new type tables and migration-history infrastructure) to actual pre-type historical migrated schema. Normalize ordering/format/provider type aliases explicitly, not missing objects/constraints. Retain meaningful constraint/index names; do not broadly ignore differences or compare only both schemas generated from the same model. Capture concrete differences as machine-readable evidence.
D. ANY substantive legacy discrepancy => BLOCKED, exact objects + provenance, STOP later acceptance. Do NOT patch old migrations/current mapping/snapshot, create reconciliation DDL, silently baseline drift, or claim a pre-existing mismatch as harmless. No permitted legacy schema repair in this task.
E. On parity: apply the existing latest type migration to the primary DB. Manifest diff MUST add only fulfillment.shipping_types and shipping_type_translations with their own constraints/indexes/FK; legacy manifest unchanged. Pin executed/generated forward SQL. After business tests, run latest Down ONLY on disposable primary DB and verify legacy manifest equals the pre-type manifest; no legacy table recreation/drop. Model-comparison DB may be discarded at cleanup.

GATE 2 — OWN STORAGE / FRESH CONTEXT
Use real Npgsql + real ShippingTypeStore/DbContext and real migrations; controlled clock/id/prepared language facts labelled fixtures, NOT live registry.

create at sort0 -> inactive revision1; NEW-context header/translation/Unicode/read-only reader roundtrip; activate/deactivate/reactivate; selected text update preserves other language; identical NONEMPTY patch with advanced clock keeps both revision/timestamp; stale revision conflicts.
parameterized raw SQL negatives, each OWN rolled-back transaction: duplicate code, duplicate (type,LanguageId), invalid own parent, required NULLs, blank name, malformed/uppercase/non-ASCII/whitespace code, sort<0/>1000000, revision<=0 and overlength name/description. Assert actual SQLSTATE AND owned constraint or column identity when provided by PostgreSQL. For varchar overflow22001 acknowledge it can precede a CHECK; do not invent a constraint name. Valid at-limit Persian/BMP Unicode128/512 and valid sort boundaries persist verbatim in NEW context.
No external language FK or cross-schema join. Direct SQL bypasses aggregate intentionally to prove owned storage invariants. Do not infer check truth from generated SQL text.

GATE 3 — REAL ATOMICITY / RACES

Revision race: seed revision1; TWO independent Npgsql contexts update the SAME type with distinct header/text values. Synchronize FIRST SavingChanges AFTER both reads/staging; assert gate two arrivals and BOTH ORIGINAL root Revision==1. Bound gate20s; no sleeps or wait while holding conflicting row locks. Exactly one success/one canonical shipping_type.revision_conflict, NEW context revision EXACTLY2, complete winning header/text with no hybrid; ACTUAL loser tracker has no pending edit, later SaveChanges publishes nothing and rereads winner.
Duplicate-code create race: both independent contexts complete the existence precheck before FIRST SavingChanges using bounded test-only gate. Different server ids, SAME valid code; exactly one create and one canonical duplicate outcome, one persisted type with its complete translations, actual loser tracker clean. No raw provider exception, guessed duplicate classification or precheck-only proof.
Failure/cancellation inside ONE implicit SaveChanges: ensure at least one actual statement writes before an injected later write failure/caller cancellation, then NEW-context readback is the exact old tree/revision/timestamp; no partial text/header. If needed MaxBatchSize(1) is a TEST-ONLY provider option to observe two separate commands, not production config. Record actual first-write completion and injection stage. A SavingChanges failure BEFORE every statement is NOT mid-save rollback proof. Cancellation preserves caller token/OperationCanceledException; retry succeeds. Use bounded interceptors, no production hooks.

GATE 4 — EXACT CONSTRAINT OWNERSHIP, NO MESSAGE MATCH
IsUniqueViolation must recognize ONLY actual owned SqlState23505 + exact constraint name. Add a focused no-DB/synthetic-provider-error or test-only interceptor fact: owned constraint name maps to canonical duplicate; a lookalike name that merely CONTAINS the owned name and a different SQLSTATE propagate the same exception. Exercise the real store catch path (or a focused existing testable predicate without changing production), no production seam added and no message parsing. This proof is distinct from the real duplicate race.

SMALL R5 FIXTURE CORRECTION
One_active_non_fa_default_is_used_without_any_fa_fallback currently has BOTH fa and ar active. Make fa known INACTIVE or omit it, keep ar active/default, and ASSERT actual active count==1/default identity through real adapter before create/read. Existing three-plus case already covers a non-first default. Run that corrected exact test once and append the R5 evidence correction; no extra language feature or production policy.

FOCUSED COMMANDS / FAILURES
Build only Fulfillment.Tests and project references, isolated task-owned output if shared output locked. Run new relational/schema/constraint filters plus corrected exact single-active test; reuse prior accepted API/DI/listener proof by unchanged source. At most one final focused ALL type-catalog test filter while disposable DB is live IF time fits; no full Fulfillment/Shipping/Host/solution or broad Architecture suite. Commands <=120s. No Host build or repeated confidence run.
If a real production/schema flaw fails a required fact, preserve exact assertion/SQLSTATE/schema diff and report PARTIAL or BLOCKED; do not weaken tests, change expected rule, skip required proof or start a repair wave. A schema drift from Gate1 is BLOCKED, not a reason to run later races first or to alter the snapshot.
If time short, record which gates executed and stop; do not label an unrun fact PASS. No package install, cache cleanup, shared app stop/restart or baseline refresh.

ALLOWED FILES / EVIDENCE
Fulfillment TEST files/fixtures only, existing R5 single-active test correction, task/evidence/recovery docs. No production C#/csproj, Contracts, resources, auth/DI, EF mapping, migration/designer/snapshot, manifest/skill/guard, Host/frontend/Shipping changes. No classification assignment, method restriction/grouping, runtime pricing/quote/promise, carrier/funder/Payment/Settlement/Promotion, activation/deployment/flag work. TB185 paused; default-OFF/PendingAllocation preserved.
Persist received artifact verbatim in docs/ai/tasks/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R6.task.md. Sanitized docs/architecture/evidence/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R6/ includes RESULT.md, target/isolation proof, historical-vs-snapshot manifests/diff, additive Up/Down manifest diff and forward SQL, concrete SQLSTATE matrix, new-context roundtrip, actual original-revision/unique race/rollback/cancellation assertions and cleanup. Avoid credentials/customer payloads.
Record exact UTC claim/result, commands/counts, source hashes, test-only injection limits and reused-vs-executed evidence. Update recovery pointer with Worker status only; append R5 fixture correction without rewriting history. Scope commit/push/fetch parity; preserve unrelated dirty/untracked/stashes; no reset/stash/rebase/force-push or Bridge probe/dummy result.
PASS requires ALL bounded required gates actually proved and cleanup/deadline. PARTIAL for missing proof/timebox; BLOCKED for unsafe target or substantive legacy schema discrepancy. Neither PASS nor catalog-active state is production/user acceptance or module certification. Stop; no future task or automatic continuation.

RESULT FORMAT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R6
Parent-Task: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R5
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_TYPE_CATALOG_POSTGRES_SNAPSHOT_STORAGE_AND_ATOMICITY_PROOF
Bridge-Task-Row: ...
Status: PASS|PARTIAL|BLOCKED
Expected-Base-SHA: 6c5098dddac5d48ec8932f5bbfa22874bc8fc0e5
Base-SHA: ...
Code-SHA: ...
Evidence-SHA: ...
HEAD-Origin-Parity: ...
Parsed-Target-And-Live-Provenance: ...
Historical-Migrated-Schema-Versus-Snapshot: ...
Latest-UpDown-OnlyOwnTables-And-Legacy-Parity: ...
FreshContext-Roundtrip-And-Storage-Negatives: ...
Strict-OriginalRevision-Race-And-Actual-Loser: ...
Concurrent-UniqueCode-Create-Proof: ...
MidSave-Failure-Cancellation-Rollback-And-Retry: ...
Exact-Constraint-Lookalike-And-WrongState-Negatives: ...
Corrected-Exactly-OneActive-NonFa-Registry-Fact: ...
Focused-Commands-And-Test-Counts: ...
Exact-Failing-Schema-Or-Behavior-If-Any: ...
Changed-Files: ...
Production-EFMapping-Migration-Snapshot-Changes: NONE
Shared-Database-Data-Flag-Changes: NONE
Remaining-Unexecuted-Gates: ...
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