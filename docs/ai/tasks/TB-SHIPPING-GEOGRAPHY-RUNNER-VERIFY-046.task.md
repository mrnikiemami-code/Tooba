Received Bridge task artifact (row bcf74e9d-55ff-482e-9da2-236bf071d795) — RECEIPT ONLY, NEVER STAGED.

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-GEOGRAPHY-RUNNER-VERIFY-046
Parent-Task: TB-SHIPPING-GEOGRAPHY-MIGRATION-REGISTRY-045
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ARCHITECTURE-PLANNING
Mode: BOUNDED_DISPOSABLE_GEOGRAPHY_MIGRATION_VERIFICATION
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 6746ca5ae835fa96ef67b99d0ba94095c782b2c2
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 30 minutes HARD

ARCHITECT AUTHORITY

045 ACCEPT: Geography migration order 30 and standalone MigrationRunner descriptor exist, but no Tooba DB migration is authorized.
Exactly one task. Verify end-to-end geography migration metadata against a task-exclusive disposable PostgreSQL. Do not expand into shipping implementation.
Critical source finding: MigrationRunnerCli accepts tenant selection, NOT module selection; its apply command acts on all registered descriptors. NEVER run MigrationRunner apply against a shared/Tooba database or assume module filtering.

PREFLIGHT — STOP ON CONFLICT

Claim exactly one Task after BRIDGE-WAKE; no idle polling. Read AGENTS.md, docs/ai/TOOBA-PIPELINE-PROTOCOL.md, .cursor/skills/tooba-backend-feature/SKILL.md and actual MigrationRunner CLI/registry/orchestrator code.
git fetch origin; require main, HEAD == origin/main == Expected-Base-SHA. Preserve pre-existing dirty/untracked files; never stash/reset/rebase/force-push. On unsafe divergence STOP RECOVERY_CONFLICT.
Verify exclusive scratch database/container provenance (new isolated container, loopback port distinct from Tooba); no use of existing Tooba PostgreSQL/container or real tenant connection. STOP if isolation is uncertain.

MISSION — ONE BOUNDED VERIFICATION SLICE
A. Inspect real runner CLI and target resolver; document whether targeted Geography-only CLI apply exists (do not invent it). Verify that runner descriptor advertises exactly the initial geography migration and that Host GeographyModule still registers no startup migrator.
B. On an isolated disposable PostgreSQL only, verify Geography initial migration UP / pending=0 / DOWN with the standard EF tools or isolated harness, and verify both ancestry triggers and key FKs. Reuse focused 042 validations rather than recreating an exhaustive suite.
C. If a valid isolated runner target configuration is feasible and safe, run its nonmutating status/plan and verify Geography appears once. Only run runner apply if its effect on ALL modules is explicitly confirmed to remain exclusively inside this freshly created disposable database, with bounded runtime and no unrelated effects. Otherwise skip runner apply and report exact reason; direct EF Geography-only apply is sufficient evidence of Geography schema correctness.
D. Remove task-created disposable database/container and temporary secrets/artifacts. No Tooba database contact. No Host restart; no runtime Geography migrator registration or Checkout switch.
E. Write one concise report with observed evidence, commands, actual elapsed, remaining activation blockers and ONE recommended next bounded task. Do not claim more than observed.

ALLOWED PATHS

docs/planning/shipping/SHIPPING-GEOGRAPHY-RUNNER-VERIFY-046.md (new)
docs/architecture/evidence/TB-SHIPPING-GEOGRAPHY-RUNNER-VERIFY-046/** (focused evidence)
docs/ai/tasks/TB-SHIPPING-GEOGRAPHY-RUNNER-VERIFY-046.task.md (received receipt only; NEVER STAGE)
Isolated external temporary harness/container artifacts (delete at end)

FORBIDDEN

No modifications to production C#/TS, migration sources, model snapshot, schema migrator, runner configuration, Host, Checkout, Order, AddressBook, Fulfillment, frontend, manifest, architecture guards or appsettings.
No connection to or query/migration of Tooba dev/staging/prod DB; no service restart; no broad nonessential tests; no real tenants, no seed beyond isolated test fixture.
No probe/dummy POST to Bridge /api/results. No automatic follow-up.

ACCEPTANCE / STOP

PASS when provenance of isolated DB is proven; geography initial migration and critical invariants are verified on scratch DB, Host non-activation confirmed, exact CLI limitations reported, scratch environment removed, and only authorized report/evidence committed and pushed.
PARTIAL if isolated verification or safe runner target cannot be established within timebox; do not weaken safety gates or guess. Stop for Architect review.
Commit only task-owned report/evidence; push main, fetch and verify HEAD == origin/main; report full SHA and actual elapsed. Worker PASS != Architect ACCEPT.

RESULT CONTRACT — BRIDGE-WAKE-V1
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-GEOGRAPHY-RUNNER-VERIFY-046
Parent-Task: TB-SHIPPING-GEOGRAPHY-MIGRATION-REGISTRY-045
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-ARCHITECTURE-PLANNING
Mode: BOUNDED_DISPOSABLE_GEOGRAPHY_MIGRATION_VERIFICATION
Bridge-Task-Row: <actual row>
Status: PASS | PARTIAL | BLOCKED | RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 6746ca5ae835fa96ef67b99d0ba94095c782b2c2
Base-SHA: <actual>
Code-SHA: <actual or NONE>
Evidence-SHA: <actual or NONE>
automaticNextImplementationTask: NONE

<preflight; exact checks and SQLSTATE outcomes; changed paths; isolation proof; cleanup; elapsed; push evidence; remaining blockers>
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK
