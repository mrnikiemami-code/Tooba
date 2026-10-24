TB-SHIPPING-ADMIN-LIVE-EXISTING-DRAFT-READINESS-206

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Channel: tooba-main
Worker-Id: tooba-worker-01
Repository: mrnikiemami-code/Tooba
Branch: main
Parent-Task: TB-SHIPPING-ADMIN-AUTHORING-BROWSER-ACCEPTANCE-205-R3
Expected-Base-SHA: 8fcd9df9e44f7bc283e0e6ccf0c6cd95d9b0d8c2
Mode: BOUNDED_REAL_ENVIRONMENT_EXISTING_DRAFT_READ_ONLY_HANDOFF
Hard-Timebox: 20 minutes
Stop-New-Work: minute 14; final 6 minutes cleanup/evidence/commit/push/fetch/report
Automatic-Next-Task: NONE
Stop-State: STOP_FOR_ARCHITECT

Goal

The user explicitly wants continued work and fast delivery BEFORE being asked to test.
Establish whether the existing-draft editor really opens against the existing local backend and configured real data.
Previous 205 browser evidence used synthetic intercepted /v1 traffic. It does NOT establish live readiness.
Deliver either an exact usable live route for user trial or the FIRST concrete environment/data/integration blocker and the smallest next repair.
Do not replay 190–205, create a new feature, or turn this into a test program.

Recover / preserve

Read root AGENTS.md and only the TOP current recovery section:
docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md
Read 205-R3/ARCHITECT-REVIEW.md for current acceptance limits.
Persist this task verbatim under docs/ai/tasks/.
Verify current main/origin parity and safe tree. If expected base is an ancestor with only understood docs changes, preserve the latest main and record it; source divergence or unrelated uncertain changes -> stop.
This task supersedes the prior active-task=NONE checkpoint ONLY for the live read-only readiness work below.
All accepted work stays accepted; TB185 paused, default-OFF/PendingAllocation unchanged.

Authority / exact surface

Frontend existing method display: /admin/shipping-services, View existing method, General -> authorized draft link.
Canonical editor route: /admin/shipping-services/authoring/[draftId]?mode=view.
Existing list: GET /v1/admin/shipping-services/authoring-drafts, match exact serviceCode + nullable optionCode; never split methodCode to guess a binding.
Existing editor: GET /v1/admin/shipping-services/authoring-drafts/{draftId}/editor-document.
Type choices: GET /v1/admin/shipping-services/authoring-types.
Geography reads: use the existing scoped routes and configured default language only when needed to confirm actual selected facts.
Read actual source/current configuration before inferring ports/paths/header conventions.
Historical FE3000/Host5088 are hints, NOT proof of current processes or health.
Use existing documented authorized session/dev context; no invented owner, token, grants, tenant or seller.
Do not expose headers, cookies, credentials, connection strings or personal data in evidence.

Execute once, follow the first failing layer

Inspect the EXISTING frontend/backend process, current origin/rewrite target and runtime source/binary provenance.
Establish actual listening ports and which task/user owns each process. Never restart/kill/reconfigure shared FE/Host.
Read-only probes should be bounded (about 10s each). If stopped, do not launch migration-capable Host or assume a stale DLL is current.
A task-owned frontend with isolated output may be reused/started ONLY using the already-proven installed-dependency procedure, <=3min, and its origin MUST point to the verified real backend (NOT127:1). No mock/interception and no install/download.
If a prerequisite cannot be established within budget, capture its concrete cause; no runtime chase or broad build.

Confirm the real API/auth boundary through normal shipped read paths.
Distinguish connection failure, wrong/stale backend route, 401/403, missing migration/schema, invalid response contract and empty legitimate data.
Do not convert auth failure into a guessed account or fake permission.
If a sign-in wall is the blocker, stop with the exact existing URL and required user action; no credential collection.

Read the real authorized draft list and existing method binding. Select ONE existing authorized draft.
Record only minimal synthetic/redacted identity references needed to reproduce (draft id/binding when non-sensitive, no payload dumps).
Exactly one binding is valid; 0 drafts or ambiguous matching is a truthful BLOCKED_DATA condition.
NO POST/create/seed/backfill to manufacture a passing scenario.
If no draft exists, inspect the existing create capability/source only enough to name the smallest separately reviewable next step.

Open the ACTUAL mounted route with LIVE read responses and NO /v1 fulfilment, stubbing or mocks.
Use installed browser/CDP tooling and an authorized existing session where available; fresh headless profile alone is not proof of the user's signed-in session.
Confirm document reads correctly: owner scope, actual registry languages/default, selected type resolution, stored groups/labels/rows/timezone/week/settings and revision.
Inactive/missing/null are legitimate truth states, not readiness fabricated by fallback.
Check no entry-failed/load error and no new console exception directly blocking this route.
No Apply/save/delete or merchant mutation; optional entering local Edit and leaving clean is enough to show controls reachable.
If browser session cannot be reused lawfully or tooling setup exceeds3min, report API-read outcome separately from browser readiness.

If reads+mount succeed, hand off the EXACT local URL and concise navigation steps, with real-data read-only readiness stated.
User may then perform deliberate trial; do not claim live write acceptance from reads.
If any layer fails, stop at the first blocker, provide exact status/error code/source cause and minimal bounded repair recommendation.
No speculation, no unrelated scan, no broad suite. Do not label a legitimate no-draft as a UI defect.

Bounds

Production-code changes: NONE in this task. Docs/evidence/task-owned read-only scripts only.
Shared database/schema/data/flag changes: NONE. Do not run migrations, schema checks that perform DDL, seeding, MigrationRunner or startup paths that implicitly migrate.
No Host/solution/module/frontend build, package or image install, new framework, broad tests, full geography matrix or repeat synthetic save.
No alteration of shared ports/processes/config, .next/bin/obj/cache cleanup, tsconfig, next-env, auth/edition authority or user profiles.
No task-owned response interception. Do not run any historical post-result.js or dummy Bridge request.
One task-owned harness correction maximum. No source repair loop; a discovered product defect belongs in the result for Architect's narrow follow-up.
When browser automation is used, finite timeouts and real result inspection; DOM/read reports are not user Visual ACCEPT.
Clean only task-owned processes/outputs created by this task; preserve shared runtimes.

Evidence and result

Write docs/architecture/evidence/TB-SHIPPING-ADMIN-LIVE-EXISTING-DRAFT-READINESS-206/RESULT.md:

expected/actual base, source SHA and runtime provenance (known vs UNKNOWN)
actual FE/Host ports/origin and whether existing processes remained untouched
REAL vs intercepted boundary explicitly: must be REAL reads, PUT/POST/PATCH/DELETE count0
authorized list status; exact binding cardinality; real editor/types read outcomes
mounted route result vs API-only result, no fabricated readiness
verdict READY_FOR_USER_READ_TRIAL or BLOCKED_<first concrete layer>
usable route/navigation if green; first blocker + smallest repair if red
live write acceptance PENDING, no production/full-program completion claim
actual elapsed, any deviation, cleanup, scoped diff, commit/push/fetch parity
Stop-State STOP_FOR_ARCHITECT, Automatic-Next-Task NONE.

Update ONE current worker pointer in canonical recovery; historical evidence untouched.
Commit only task-owned files, fast-forward push, fetch parity. Post complete worker result, then stop.
Do not ask the user to test before the actual readiness outcome is known.
