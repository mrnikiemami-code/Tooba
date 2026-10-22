PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-GROUP-RATE-DRAFTS-191-R1
Parent-Task: TB-SHIPPING-ADMIN-GROUP-RATE-DRAFTS-191
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: b77492052bb2926c522bb23c8cb7e62dfc717675
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_RATE_STORAGE_CHECK_PARITY_AND_CROSS_OPERATION_RACE_REPAIR
Hard-Timebox: 35 minutes HARD INCLUDING cleanup, commit, push and report; STOP and report PARTIAL if exceeded.
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

MISSION
Repair the proven storage-constraint mismatch in TB-191 and execute the missing direct race between manual-rate replacement and region-tree replacement. Keep the existing draft model, owner/currency/business semantics and runtime gates unchanged. This is a small corrective migration plus focused proof, not the next editor feature, broad regression audit or AMSC wave.
TB-191 is NOT Architect ACCEPTED. Its claim that nothing in scope remains is superseded by docs/architecture/evidence/TB-SHIPPING-ADMIN-GROUP-RATE-DRAFTS-191/ARCHITECT-REVIEW.md.

MANDATORY FIRST CHECK
Fetch main and inspect branch/HEAD/dirty/untracked/stashes; preserve unrelated work. Require HEAD == origin/main == Expected-Base-SHA before edits. A safely behind branch may fast-forward only to the exact base. Mismatch/divergence/overlapping dirty work => STOP_FOR_ARCHITECT / RECOVERY_CONFLICT. No reset/rebase/stash/force-push.
Read AGENTS.md, docs/ai/AGENT-SKILLS.md and full .cursor/skills/tooba-backend-feature/SKILL.md; relevant Migrate/Structure rules apply only to changed capability, not a new certification cycle.
Read current Shipping recovery/rules, TB-191 receipt/result/ARCHITECT-REVIEW, current rate-row Domain normalization, EF checks/migration/snapshot, directory save boundaries, real relational tests and parsed target fixture. No findings-register or unrelated-module scan.

PROCESS CORRECTION — ENFORCE THIS TIME
TB-191 reported ~60 minutes despite a 45-minute HARD limit, including an expressly forbidden whole Tooba.Host.Tests run. This is a process deviation, not acceptance evidence. The claim that 132 Host failures are all pre-existing/nonregressions lacks a controlled base comparison and remains UNVERIFIED.
Do NOT rerun Host/other modules to justify that attribution. Append a clear evidence correction without rewriting historic results. No broad regression awareness run.
At start record UTC claim time, monotonic elapsed clock and 35-minute deadline. Reserve the last 5 minutes for exact cleanup/commit/push/result; no new implementation/test may start after minute 30. Each spawned command <=180 seconds under an actual timeout with task-owned process cleanup; no detached orphan tests. No blocking wait >60 seconds and no test/repair loop.
COMMAND ALLOWLIST: affected Shipping.Tests project build and narrowly filtered Shipping tests, at most one final whole Shipping.Tests run if time remains; EF migration generation/script commands for Shipping only, existing local Docker/Npgsql tools for the disposable DB, scoped file/git/evidence/Bridge lifecycle commands. NEVER dotnet build/test Tooba.slnx, Tooba.Host.Tests or another module. Do not build a broad test project to get Shipping output. No package/image/browser install/download.
If remaining time cannot fit, report honest PARTIAL and stop rather than extending the deadline.

FINDING R191-01 — PROVE AND CORRECT SQL PARITY
The current TB-191 interval checks have this shape:
(min IS NULL AND max IS NULL) OR (min >= lower AND ... max > min).
With min=NULL and positive max, the second branch may be UNKNOWN; PostgreSQL CHECK accepts UNKNOWN. Domain/transport reject max-only intervals. This affects weight, count and merchandise amount.
ck_authoring_rate_rows_amount_within_max caps fees and max_merchandise_amount but omits min_merchandise_amount. A lower amount > 1e9 with no upper bound passes storage but not Domain.
ck_authoring_region_groups_currency_canonical checks length and upper() equality only. '123' or 'A$!' passes that SQL but not NormalizeTrustedCurrency (exactly three ASCII letters).

Correct current EF mappings to exactly preserve the EXISTING intended domain rules:

an axis is either BOTH NULL, or explicitly min IS NOT NULL and valid with optional valid max;
upper/lower domain limits on every present bound, including minimum merchandise amount and the unbounded-upper case;
group currency is NULL or exactly three uppercase ASCII letters, not merely an uppercase unchanged string.
Preserve valid empty/unconditioned axes, min-only intervals, half-open adjacency, zero weight/amount, positive count and numeric(18,2). No new price/currency policy, currency registry, data cap or conversion. The API/domain precision rejection remains necessary: numeric(18,2) can coerce/round incoming values before a CHECK and is not itself proof that original input precision was rejected.

MIGRATION — NEW CORRECTION ONLY
Add ONE non-destructive corrective Shipping migration/designer and update snapshot for these OWN authoring checks only. Do not edit TB-191 or any older migration/designer. No legacy table/constraint weakening, new table, owner data backfill, external FK or startup migrator. Forward and rollback SQL should be explicit and match current model; new invalid data must fail validation, never be silently repaired/deleted. Apply only to the disposable test database.
No current production data audit or migration application is authorized. If a migration encounters invalid existing data in the isolated fixture, report exact test provenance and correct the fixture deliberately, never invent a shared cleanup routine.

EXECUTED STORAGE NEGATIVE PROOF
Use parameterized SQL / direct EF persisted storage in test-owned code to BYPASS aggregate/transport checks and prove the database itself rejects invalid rows. Correctly test SQLSTATE 23514 and the relevant owned constraint identity, not just a generic thrown exception. Keep each failing SQL attempt in a rollback-able test boundary so one aborted statement cannot poison later cases.
Cover independently:

each axis min=NULL/max=positive (and ensure it was not rejected by another unrelated invalid field);
minimum merchandise amount > canonical max with max=NULL, plus max above cap and valid at-limit values;
currency '123', mixed/non-ASCII/punctuation/whitespace malformed codes vs valid uppercase ASCII code and valid NULL;
valid both-NULL, valid min-only and adjacent bounded intervals still persist.
Seed only lawful test-owned draft/group parents via existing factories/directories; no foreign fixture/permissions bypass in production.
Generate actual correction SQL and prove original migration files byte-identical. Existing model-string assertions may be updated for the authorized new migration but are not substitutes for these PostgreSQL negatives.

FINDING R191-02 — TWO DIFFERENT OPERATIONS, SAME ROOT REVISION
Current A_same_revision_rate_row_race... races rate replacement against another rate replacement. That does not cover the task's requested rate-write versus region-write race.
Add a deterministic test using independent Npgsql contexts:
A = ReplaceGroupRateRowsAsync, B = ReplaceRegionGroupsAsync, same owner/draft and expected revision.
Synchronize only after BOTH loaded/prepared the SAME original revision and BEFORE either first persistent write; use the existing bounded interceptor/gate pattern, no sleeps or barrier while holding conflicting write locks. Assert both original revision predicates. Either participant may win.
Cover region retention/reorder/selection edit and region removal interacting with a rate write. Use distinct proposed changes so a hybrid snapshot is detectable.
Require exactly one successful operation, canonical revision conflict for the loser, exact winner-specific full tree/rates/currency/ids/revision, no loser rows or staged orders, actual losing-context tracker safe for a later SaveChanges, and fresh-context readback. No deleted group's orphan rates.
Keep the existing rate-rate race, retained-group preservation/deletion, currency binding and failure/rollback tests.
If this regression demonstrates a NEW executable production flaw, record the failing proof and report PARTIAL / STOP_FOR_ARCHITECT with the minimal proposed remedy. Do not redesign transactions, add a second BeginTransaction, remove root concurrency, reclassify FK failures by broad catch, or loosen R2's transaction guard in this task.

ISOLATED POSTGRES ONLY
Reuse the existing parsed exact target fixture and already available PostgreSQL 17 image/tools, on a FRESH task-owned container. Previous container was removed; verify port is free and exact container ownership before starting. Existing expected target is 127.0.0.1:55432 / shipping_tb190r1 / tb190r1; never shared :5432/tooba_alpha/store-alpha. Validate parsed host/port/database/user BEFORE connection and live database/user before writes; do not log credentials.
Apply module migrations only on that disposable DB through the existing test/module migration authority. No shared migration runner, Host startup, server restart or broad seed. Setup <=6 minutes; if unavailable report PARTIAL rather than fetching/installing infrastructure.
Run focused new storage and cross-operation tests, plus affected persistence/compatibility guards and relevant rate regression subset. One final whole Shipping suite only if targeted checks pass and it can finish BEFORE cleanup reserve/deadline, while the disposable DB is still live. No required-case skip/exclusion, baseline refresh, loops or broad suite expansion.
Cleanup exact task container/listener/output even on partial failure, verify shared apps untouched, and report cleanup truthfully.

SCOPE / CLEAN ARCHITECTURE
Allowed changes: ShippingDbContext authoring check expressions; one new correction migration/designer/snapshot; focused Shipping.Tests storage/race/model checks; task/evidence and append-only Shipping decision/recovery corrections. Necessary module-owned mapping constants may reuse current Domain values; no foreign model/DbContext/schema join/dependency.
No new executable API/handler/validator/Domain business policy, new endpoint/permission, public DTO change, frontend/Storefront, rates evaluator, metadata/taxonomy/schedule/calculation/payment modes, currency/language/owner authority, carrier call, financial/Checkout/payment/deployment or certification change. Preserve existing independent fees/duration, canonical API/errors/resources/Contracts/tracing and owner predicates.
Runtime merchandise/discount/tax/group-measurement gate stays OPEN_NOT_IMPLEMENTED; drafts remain inactive. TB-190 acceptance remains; TB-191 acceptance pending this review. Default-OFF flags, PendingAllocation and TB-185 pause stay intact.
Preserve unrelated dirty/untracked/stashes, shared :3000/:3419/:5088/:5432, bin/obj/.next, Redis/RabbitMQ. No reset/rebase/stash/force-push/cache purge. No dummy/probe payload to Bridge /api/results.

ACCEPTANCE / EVIDENCE
PASS requires real database constraints matching the enumerated Domain rules, executed invalid/valid storage matrix, genuine cross-operation race proof, historical migration parity, preserved architecture/behavior/owner boundaries, task scope/deadline compliance and cleanup. PARTIAL if any required relational proof is missing or a newly demonstrated production flaw remains; BLOCKED on isolation/architecture conflict. Whole Host failures are not proof either way.
Persist received task verbatim in docs/ai/tasks/TB-SHIPPING-ADMIN-GROUP-RATE-DRAFTS-191-R1.task.md after claim. Evidence under docs/architecture/evidence/TB-SHIPPING-ADMIN-GROUP-RATE-DRAFTS-191-R1/: RESULT.md, constraint before/after truth/SQL matrix, direct database negative outcomes, cross-operation winner/loser assertions, migration parity, commands/counts, UTC/elapsed/deadline log and cleanup.
Append corrections to TB-191 evidence/Shipping decisions/recovery without erasing history; no Architect ACCEPT or next task scheduling.

RESULT FORMAT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-GROUP-RATE-DRAFTS-191-R1
Parent-Task: TB-SHIPPING-ADMIN-GROUP-RATE-DRAFTS-191
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_RATE_STORAGE_CHECK_PARITY_AND_CROSS_OPERATION_RACE_REPAIR
Bridge-Task-Row: ...
Status: PASS|PARTIAL|BLOCKED
Expected-Base-SHA: b77492052bb2926c522bb23c8cb7e62dfc717675
Base-SHA: ...
Code-SHA: ...
Evidence-SHA: ...
HEAD-Origin-Parity: ...
Nullable-Interval-Check-Truth-And-Postgres-Negatives: ...
Merchandise-Minimum-Cap-And-Currency-ASCII-Parity: ...
Valid-Storage-Boundaries-And-Domain-Compatibility: ...
Corrective-Migration-SQL-And-Historical-Byte-Parity: ...
RateWrite-Versus-RegionRetention-Race: ...
RateWrite-Versus-RegionRemoval-Race: ...
Actual-Loser-Tracker-And-Exact-Winner-State: ...
Focused-Commands-And-Test-Counts: ...
Final-Shipping-Suite-If-Run: ...
TB191-HostFailure-Attribution: UNVERIFIED_NO_HOST_RERUN
Out-Of-Scope-Commands: NONE
Claim-UTC: ...
Result-UTC: ...
Elapsed-Minutes: ...
Hard-Deadline-Compliance: ...
Changed-Files: ...
Shared-Database-Data-Flag-Changes: NONE
Isolation-And-Cleanup: ...
Runtime-Merchandise-Basis-And-Measurement-Gate: OPEN_NOT_IMPLEMENTED
Production-Functional-Acceptance: PENDING
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
Automatic-Next-Task: NONE
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK