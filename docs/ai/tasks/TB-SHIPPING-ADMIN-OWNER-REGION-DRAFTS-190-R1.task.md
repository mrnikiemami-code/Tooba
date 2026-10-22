PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190-R1
Parent-Task: TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 4b5da2b018b64aa14a227c0942d3f6ad14c1b323
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_DRAFT_VALIDATION_PREFLIGHT_AND_RELATIONAL_REPLACE_REPAIR
Hard-Timebox: 45 minutes HARD; STOP and report PARTIAL if exceeded.
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

MISSION
Repair the existing TB-190 draft CRUD slice before adding any new product feature. Architect disposition is REPAIR_REQUIRED despite worker PASS. Fix required/null collection validation, whole-tree identity preflight before mutation, and ordered replacement under PostgreSQL uniqueness/concurrency. Keep the owner-isolated authoring model and stable draft/group IDs. This is one bounded correctness repair, not a new AMSC certification, frontend integration or rate-engine wave.

MANDATORY FIRST CHECK
Fetch main and inspect branch, HEAD, dirty/untracked files and stashes; preserve unrelated work. Require HEAD == origin/main == Expected-Base-SHA before edits; a safely behind branch may fast-forward only to this exact base. Mismatch/divergence/overlapping dirty work => STOP_FOR_ARCHITECT / RECOVERY_CONFLICT. No reset/rebase/stash/force-push. Do not replay TB-190.
Read AGENTS.md, docs/ai/AGENT-SKILLS.md and the full .cursor/skills/tooba-backend-feature/SKILL.md. Read docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md (its newest override supersedes the original pending-190 checkpoint), docs/architecture/evidence/TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190/ARCHITECT-REVIEW.md, TB-190 receipt/result and the actual affected Domain/Application/Infrastructure/Endpoints/tests/model/migration. Preserve SHIPPING-ADMIN-RULES-DECISION.md and existing AMSC locks; use relevant Migrate/Structure rules on changed surface only. No findings-register scan or whole-module recertification.

ARCHITECT FINDINGS — REPRODUCE, THEN REPAIR
R190-01: Both write validators end the RegionGroups chain with .When(x => x.Request?.RegionGroups is not null). This skips the preceding NotNull rule too. Missing/null JSON RegionGroups is not rejected as required; ToDomainGroups(null) yields [], but PrepareAsync subsequently enumerates the original null. Non-nullable record annotations are not a required-JSON contract.
R190-02: ReplaceRegionGroups applies retained groups as it iterates, then discovers an unknown/foreign/repeated GroupId later. ValidateShape has not preflighted the complete identity set. A bad final identity can follow mutation of the first tracked group. Existing late-overlap test stops earlier in the handler and does not cover this.
R190-03: ux_authoring_region_groups_draft_order is an immediate UNIQUE index on (draft_id, group_order). Directly assigning final positions for [A@0,B@1] -> [B@0,A@1] leaves no valid first ordinary UPDATE. EF may reject a command dependency cycle or PostgreSQL may reject transient duplicates. A transaction alone does not defer uniqueness. Architect has NOT executed this database failure; reproduce and prove the repaired strategy.

BOUNDED REPAIR

REQUIRED INPUT SHAPE THROUGH THE REAL PIPELINE
Keep RegionGroups required and [] allowed as an incomplete draft. Separate unconditional NotNull from dependent size/shape checks, with safe guards/cascade for a missing parent Request. Do not silently normalize missing/null collections to empty, catch NullReferenceException/ArgumentNullException as a business policy, rely on C# annotations, or introduce endpoint business validation.
Prove POST and PUT with omitted/null RegionGroups return canonical validation.failed and shipping.validation.region_groups_required before handler/dependency/database work. Exercise null group/Areas/area elements and invalid group IDs; retain existing stable shape/error conventions. Direct validator tests cover null Request safely; existing body-invalid behavior remains canonical over HTTP. Do not invent new commercial limits or languages.

FULL PREFLIGHT BEFORE MUTATING THE AGGREGATE
Validate the complete proposed group tree, retained-ID membership and uniqueness, node shape, and all failure-prone preconditions before any retained group.Apply, Order change, removal, area update, revision/timestamp mutation. Unknown/foreign/repeated retained GroupId must reject coherently and never reattach another draft's group. Preserve existing immutable owner/binding and stable IDs.
Regression: valid retained first group has a different order/selection, bad identity is last. Cover unknown/foreign ID and repeated retained ID. Assert SAME aggregate fields/child values/revision/timestamps and SAME scoped DbContext tracked state remain unchanged after rejection. A subsequent SaveChanges in that same context must not persist a partial edit. Fresh-context-only assertions are insufficient. Include a valid replacement afterwards.

UNIQUENESS-SAFE RELATIONAL REPLACEMENT
Reproduce two retained groups swapping positions, deletion of an early group shifting a later group, insertion before a retained group, and moving a selected node between groups while preserving draft-wide final uniqueness. Keep stable retained GroupIds, final unique group order and draft-node uniqueness; deleting/recreating all retained groups, dropping invariants, or relying on provider batching/row order is not a repair.
Implement the smallest Shipping-owned relational strategy compatible with actual EF/Npgsql ordering. A properly supported deferred uniqueness strategy or an explicitly staged persistence strategy is permissible; document why intermediate states cannot leak or commit. Do not assume BEGIN/COMMIT alone defers an immediate index.
NARROW AUTHORIZED EXCEPTION TO TB-190'S SINGLE-SAVE PREFERENCE: if the correct strategy needs multiple SaveChanges, all stages MUST be inside ONE Shipping-owned database transaction with one commit, a real expectedRevision conditional write, rollback on every failure/cancellation, and no external module call while holding that transaction. No cross-module transaction, partial commit, or second business authority. Preserve exact exception/cancellation behavior and canonical revision-conflict/duplicate errors; classify only the specific owned database constraints, never arbitrary DbUpdateException or provider message text.
Prepare/validate Geography and method facts before tracked/persistent mutations; scope every data read/update by the full authorized owner. Keep all existing owner filters, admin/seller permissions and trusted context rules. Check the loser of a concurrency race cannot leave group/area writes behind.

MIGRATION POLICY
All TB-190 and older migration/designer files remain byte-identical. If a persistence constraint change is essential, add ONE non-destructive corrective migration plus designer/snapshot for TB-190 authoring tables ONLY. It may replace an immediate index with equivalent deferred enforcement if needed; preserve final logical uniqueness and FK/owner boundaries. Do not weaken legacy constraints, backfill/reassign owners, change unrelated tables, or install a startup migrator. Model and emitted SQL must agree. If no schema change is necessary, add no migration.

REAL RELATIONAL PROOF — ISOLATED ONLY
This task narrowly authorizes a disposable task-owned PostgreSQL 15+ database/container on a unique free LOOPBACK port, using already available tools/image. Before any write prove exact listener/database/container ownership and fail-closed isolation: explicit task database name, task credentials, unique port, and neither shared :5432 nor tooba_alpha/store-alpha/shared connection strings. Never log credentials. No image/package/browser download or installation, no shared database/server/process use, no restart of running apps.
Use the existing repository migration/test harness; module migrations may be applied ONLY to this disposable database with the established module/CLI authority (no Host startup migrator). If no existing focused harness supports the task, a small Shipping.Tests fixture using current Npgsql/EF dependencies is allowed. No new dependency, test bypass, production test endpoint or fake published data. A test-owned fixture may replace external read Contracts facts but must use real ShippingDbContext/Npgsql/directory/handlers for persistence. Test authentication fixtures remain clearly labelled; no production bypass.
Executed regressions:

reorder/delete-shift/insert-before/node-move succeed with unchanged retained group IDs, exact final tree and revision;
invalid last-group identity and late persistence failure leave the previous tree/revision intact;
two independent contexts loaded at the same revision race: exactly one succeeds and the loser returns canonical conflict with no partial group/area writes;
same-owner duplicate binding (including null option code) remains constrained; independent owners stay independent;
failure/cancellation rollback for any new staged transaction strategy, followed by new-context readback.
Capture sanitized commands, SQL/concurrency evidence, provider/version, actual test outcomes and cleanup. Do not weaken assertions into model-string checks. InMemory/static SQL do not prove these behaviours.
Bound setup to 8 minutes; each command <=180 seconds, no blocking idle wait >60 seconds. If environment/image/relational proof is unavailable within the bound, complete safe in-scope fixes if feasible, report PARTIAL with precise unexecuted cases, and STOP. Do not install infrastructure or turn a missing database into PASS.

OWNERSHIP / COMPATIBILITY
No frontend, Storefront, Host business logic, foreign module production edit, Contracts signature/route/permission change, new catalog, rate row, taxonomy, schedule, currency/language authority, activation/publish/quote or financial cutover. Do not implement D2/D4/D5 to fill this task. Default-OFF flags, PendingAllocation, existing 44 legacy Shipping error statuses, TB-188/189 semantics and TB-185 pause stay intact.
Canonical ISender/CQRS/FluentValidation/Result/ApiResponseFactory and module-owned errors/resources only. Configured locale/fallback handling remains unlimited by fa/en; add resource text only if a genuinely new stable error is necessary. No foreign Domain/Application/Infrastructure/Endpoints/DbContext/schema join/FK, new Host API or parallel tracing/correlation system. Keep mandatory IModuleCallTracer and current Contracts-only external reads.
Shared :3000/:3419/:5088/:5432, shared bin/obj/.next, Redis/RabbitMQ, dirty/untracked/stashed work remain untouched. Build/test into task-owned isolated output. Never refresh baselines or soften guards to accept this repair.

VALIDATION AND STOP
First run a small targeted baseline/regression set; implement one coherent repair, then affected tests. Run the whole Shipping module suite once after targeted checks pass and only if time remains. No full solution build/test, repeated broad test loops, unrelated cleanup or recertification. A code fix after a new failure permits a targeted rerun; stop on wider blockers.
Cleanup only exact task-owned listener/container/database/output and prove shared services were not modified. If cleanup fails report it. Return STOP_FOR_ARCHITECT with no next-task scheduling.

ACCEPTANCE VERDICT
PASS requires real input rejection before dispatch, same-instance preflight immutability, corrected relational replacement with executed PostgreSQL regression/race/rollback evidence, preserved owner/security/architecture contracts and cleanup. PARTIAL if relational proof/setup is missing even when unit/model tests pass. BLOCKED for unsafe isolation or an architectural conflict. PASS is not Architect ACCEPT, full editor completion, certification or production deployment.

EVIDENCE
Persist this exact task at docs/ai/tasks/TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190-R1.task.md after claim. Evidence under docs/architecture/evidence/TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190-R1/: RESULT.md, input-regression matrix, preflight before/after assertions, relational replacement/race/rollback evidence, generated corrective SQL if any, isolation/cleanup. Update recovery to the actual worker outcome without claiming Architect ACCEPT. Preserve TB-190 result/history and Architect review; append corrections, never overwrite historical evidence. Report commands/test counts/SHA/changed files honestly; no dummy/probe POST to Bridge /api/results.

RESULT FORMAT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190-R1
Parent-Task: TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_DRAFT_VALIDATION_PREFLIGHT_AND_RELATIONAL_REPLACE_REPAIR
Bridge-Task-Row: ...
Status: PASS|PARTIAL|BLOCKED
Expected-Base-SHA: 4b5da2b018b64aa14a227c0942d3f6ad14c1b323
Base-SHA: ...
Code-SHA: ...
Evidence-SHA: ...
HEAD-Origin-Parity: ...
Required-Collection-HTTP-Validation: ...
Same-Aggregate-And-Tracker-Preflight: ...
Postgres-Reorder-Shift-Insert-NodeMove: ...
Real-Revision-Race-And-Atomic-Rollback: ...
Duplicate-Binding-And-Owner-Isolation: ...
Persistence-Strategy-And-Transaction-Boundary: ...
Corrective-Migration-And-Historical-Byte-Parity: ...
Real-Postgres-Acceptance: PASS|PARTIAL|NOT_RUN
Architecture-Authorization-And-Locale-Parity: ...
Focused-Commands-And-Test-Counts: ...
Changed-Files: ...
Shared-Database-Data-Flag-Changes: NONE
Isolation-And-Cleanup: ...
Elapsed-Minutes: ...
Production-Functional-Acceptance: PENDING
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
Automatic-Next-Task: NONE
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK