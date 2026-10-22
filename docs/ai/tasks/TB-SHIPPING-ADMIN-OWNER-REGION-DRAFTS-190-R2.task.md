PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190-R2
Parent-Task: TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 260904c8ca5ae51c7a06af6103c1e2d215050d8f
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_AUTHORIZED_DRAFT_TRANSACTION_GUARD_AND_PROOF_HARDENING
Hard-Timebox: 45 minutes HARD; STOP and report PARTIAL if exceeded.
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

MISSION
Close the TB-190-R1 blocker by correcting the obsolete module-wide transaction prohibition without opening an operational/financial write path, and harden the existing relational evidence. Architect chooses OPTION 1: KEEP the already authorized Shipping-owned staged draft replacement transaction and immediate uniqueness constraints. Do not implement the alternative deferred-constraint migration.
This task changes the relevant tests/guard, evidence and two misleading XML explanations only; it adds no product feature and does not authorize a new architecture wave.

MANDATORY FIRST CHECK
Fetch origin/main and inspect branch, dirty/untracked files, stashes and exact HEAD. Require HEAD == origin/main == Expected-Base-SHA before edits; a safely behind branch may fast-forward only to that exact base. Mismatch/divergence/overlapping dirty work => STOP_FOR_ARCHITECT / RECOVERY_CONFLICT. No reset/rebase/stash/force-push.
Read AGENTS.md, docs/ai/AGENT-SKILLS.md and full .cursor/skills/tooba-backend-feature/SKILL.md. Read current Shipping recovery, TB-190 and R1 receipts/results, docs/architecture/evidence/TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190-R1/ARCHITECT-REVIEW.md and the actual transaction/domain/compatibility/relational test source. Preserve current Shipping decisions and AMSC/Host locks. No findings-register scan or full-module recertification.

ARCHITECT DECISION AND LIMIT
TB-190-R1 expressly authorized ONE module-owned database transaction around staged order writes, final writes and one commit. ShippingAuthoringDraftDirectory.ReplaceRegionGroupsAsync is that reviewed persistence boundary. OperationalQuoteCompatibilityTests.The_operational_slice_adds_no_migration_and_no_money_movement still assumes no transaction exists anywhere in Shipping and rejects this newly authorized authoring behavior.
Replacing that obsolete assumption with a narrow, executable boundary is explicitly authorized. Removing or bypassing the protection is not. No schema/business/production-flow change is authorized to make the test green. The original R1 BLOCKED result and counts remain historical evidence.

BOUNDED IMPLEMENTATION

PRECISE TRANSACTION / OPERATIONAL GUARD
Refactor the old compatibility assertion into precise protections:
Existing operational quote remains read-only: no operational migration/startup migrator, transaction, customer money movement, promise authority, reservation/capacity write or new consumer/cutover.
No AddModuleSchemaMigrator/Database.MigrateAsync production call anywhere in Shipping, INCLUDING the allowed authoring file.
Exactly the reviewed transaction-start site is permitted in ShippingAuthoringDraftDirectory.ReplaceRegionGroupsAsync; unexpected production transaction sites remain forbidden across the module. Do not blanket-exclude Infrastructure/Authoring or even the entire allowed file.
Check member/content as well as path: the reviewed boundary stays on its own ShippingDbContext, one begin and one commit, both SaveChanges stages inside the same boundary, failure/cancellation rollback/disposal and revision/owner semantics preserved. Static source guards have limits; pair them with the relational behavior below, not a claim of arbitrary data-flow proof.
Reuse the existing side-effect-free boundary-detector pattern if helpful; no new Roslyn/package/framework or heavyweight parser. Do not edit approved Order sources/guards.
Add negative detector fixtures proving failures for:
(a) unexpected second transaction in another Shipping file;
(b) extra transaction inside the otherwise allowed authoring file/method;
(c) operational quote transaction/write or money-authority leak;
(d) startup migration inside the allowed file.
Keep the existing forbidden-consumer/Contracts-only/capacity/default-OFF/migration/financial assertions with equal or stronger coverage. No filename-only exemption, Assert.True, skip, filter exclusion, widened baseline, hidden marker obfuscation, alternate API spelling or deleting the failing assertion.

REPEATABLE POSTGRES TEST ISOLATION
Current NewContextAsync only checks raw connection-string substrings. Parse with the existing NpgsqlConnectionStringBuilder BEFORE any opening/migrating. Require exact single loopback Host, exact task-only port (not 5432), exact task database and exact task user; no multi-host or broad substring match. Expected values must be test-owned constants/independently checked fixture facts, never derived from the same untrusted connection string as its supposed validation.
A fresh R2-owned container/database may be used; preserve the current environment variable name if convenient and document the exact target. Never log credentials. Fail closed on absent/invalid configuration.
Offline negative config tests must reject: foreign Host with allowed text in ApplicationName, port 5432, shared/wrong database, multihost, wrong user and missing config. They must not attempt any network connection.
Before real DB writes verify exact task container ownership and loopback listener/database target, not just parsed strings. Keep migration application on this disposable database only through existing module migration machinery; no Host startup or shared runner.
Correct the test XML that currently says missing configuration skips facts: implementation throws. Keep fail-closed semantics; no silent test pass/return when PostgreSQL is absent. Report all actual pass/fail/skip counts and explicit environment requirements. Full module tests must run while the isolated database is available, not after cleanup.

DETERMINISTIC CONCURRENCY / ROLLBACK EVIDENCE
The existing Task.WhenAll race does not prove both contexts read revision 1 before writing, and it assumes A wins. Use a bounded test-only synchronization point AFTER both independently read/prepared the same revision and BEFORE either first persistent write. No production test hook, sleep-based coordination or barrier while holding conflicting write locks. Release both and permit EITHER to win; bound waits/cancellation so a failed participant cannot hang the suite.
Assert exactly one success and one canonical revision conflict; derive the winner/loser from actual results. Verify the winner-specific full tree/revision/order, no loser's extra group/area rows, no staged residue, and the ACTUAL losing context has no pending changes that a later SaveChanges can leak. Prove the real concurrency path rather than only a sequential early stale check; capture sanitized command/predicate evidence or a test interceptor assertion that both read original revision before the first write.
Retain real PostgreSQL reorder, delete-shift, insert-before, selected-row move/identity and injected second-save rollback tests.
Add caller cancellation AFTER the first stage and BEFORE commit. Prove OperationCanceledException/caller cancellation propagates and fresh-context readback keeps the old full tree/revision, no staged residue, and a valid retry succeeds. Do not catch cancellation into a fake business success.
No production behavioral repair is authorized in this task: if a new runtime flaw is demonstrated, report PARTIAL with exact failing proof and STOP_FOR_ARCHITECT instead of widening scope.

TRUTHFUL COMMENTS / EVIDENCE
Comment-only correction in ShippingAuthoringDraftDirectory: it still claims one SaveChanges/no manual transaction. Explain the actual relational two-stage, single-transaction/single-commit strategy and the nonrelational test path. Do not change executable production code.
Append the authorization/proof correction to the Shipping decision record and recovery, preserving historic task/results. Do not mark Architect ACCEPT, product completion or certification. R1's 538/1/539 and 4 relational results stay worker-reported history; publish new measured counts separately.

ISOLATED RUNTIME / TIMEBOX
Use only already available PostgreSQL image/tools and existing Npgsql/EF dependencies. Fresh task-owned container on a verified free 127.0.0.1 port and task-only database/credentials; no shared :5432/tooba_alpha/store-alpha. No package/image download/install, shared server use/restart, schema/data write to a merchant DB, or credentials in logs. The R1 container was removed: do not assume it still exists.
Limit setup to 8 minutes, each command <=180 seconds, no blocking sleep/wait >60 seconds. If setup/proof cannot fit, report PARTIAL and exact missing gate; do not fabricate relational PASS. Cleanup only the exact task-owned container/database/listener/output and verify shared apps untouched.
Run the focused failing compatibility baseline once, detector negatives and isolation tests, then relational filter on the isolated DB. After targeted tests pass, run the whole Shipping.Tests suite ONCE with that same valid DB fixture, if time permits. Build only affected Shipping test dependencies with isolated output. No solution build, full Order/Host suite, repeated broad loop or unrelated fix.
Do not claim the guarded code is runtime-enforced by a string scanner; describe actual static and executed proof separately.

OWNERSHIP / SAFETY
Allowed executable changes: Shipping.Tests compatibility detector/tests and authoring relational proof/config tests ONLY. One small test helper is allowed in the same capability if it improves cohesion, no new project/package. Production ShippingAuthoringDraftDirectory XML only. Docs/receipt/evidence as described.
No production executable C#, public contract, endpoint/handler/validator/DI/permission, schema/migration/snapshot, frontend/Storefront, global CSS, financial/payment/PendingAllocation, currency/language/owner authority, taxonomy/rates/schedules/activation changes. No new coupling or foreign implementation references; preserve Contracts-only and canonical tracing. Default-OFF flags and TB-185 pause remain.
Do not change manifests/certification state or architecture baselines. Preserve dirty/untracked/stashes and shared :3000/:3419/:5088/:5432, bin/obj/.next, Redis/RabbitMQ. No reset/rebase/stash/force-push, no shared cache cleanup, no dummy/probe payload to Bridge /api/results.

ACCEPTANCE VERDICT
PASS only when narrow guard recognizes the authorized draft transaction and rejects all negative expansions, parsed pre-write isolation is proved, deterministic real relational race/cancellation/rollback and unchanged reorder tests pass, final Shipping suite is green without exclusions/skips hiding required proof, cleanup is confirmed, and no production behavior/schema was changed. PARTIAL for missing executed relational/final-suite proof or a discovered new runtime flaw. BLOCKED for unsafe isolation/architectural conflict. Stop; no automatic next task.

EVIDENCE
Persist received task verbatim at docs/ai/tasks/TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190-R2.task.md after claim. Sanitized evidence under docs/architecture/evidence/TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190-R2/: RESULT.md, old/new guard invariants and negative detection, parsed-isolation negatives, deterministic race/rollback/cancellation outcomes, actual commands/counts, cleanup, precise changed files/SHAs. Do not assert user acceptance or full editor functionality.

RESULT FORMAT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190-R2
Parent-Task: TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_AUTHORIZED_DRAFT_TRANSACTION_GUARD_AND_PROOF_HARDENING
Bridge-Task-Row: ...
Status: PASS|PARTIAL|BLOCKED
Expected-Base-SHA: 260904c8ca5ae51c7a06af6103c1e2d215050d8f
Base-SHA: ...
Code-SHA: ...
Evidence-SHA: ...
HEAD-Origin-Parity: ...
Historic-Authorization-And-Guard-Invariant: ...
Unexpected-Transaction-Negative-Proof: ...
Allowed-File-Content-And-Startup-Migration-Negative-Proof: ...
Parsed-PreWrite-Isolation-And-Negatives: ...
Deterministic-Original-Revision-Race-And-Actual-Loser-Proof: ...
Second-Stage-Cancellation-And-Rollback: ...
Relational-Reorder-Shift-Insert-NodeMove: ...
Focused-Commands-And-Test-Counts: ...
Final-Shipping-Suite: ...
Production-Executable-Schema-Flag-Changes: NONE
Changed-Files: ...
Isolation-And-Cleanup: ...
Elapsed-Minutes: ...
Production-Functional-Acceptance: PENDING
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
Automatic-Next-Task: NONE
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK