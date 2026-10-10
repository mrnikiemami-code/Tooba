PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_TASK
Task-ID: TB-TMAR-USERPREFERENCE-AMSC-001-W3-R1
Parent-Task: TB-TMAR-USERPREFERENCE-AMSC-001-W3
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Skill: tooba-architecture-certify
Mode: BOUNDED_ROUTE_REQUEST_SET_EQUALITY_PROOF
Repository: mrnikiemami-code/Tooba
Branch: main
Starting-HEAD: 1808ba553b99f4b3c81f0dc89f101152245a91cd
Timebox: 15-20 minutes; one bounded task only

OBJECTIVE
Close the single outstanding UserPreference W3 certification-proof gap: enforce an exact, source-derived mapping from all 6 shipped HTTP routes to their actual dispatched MediatR request types (4 distinct types). No production defect has been established; this is test/evidence work only.

PREFLIGHT — STOP ON MISMATCH
Fetch origin; verify main, expected HEAD == origin/main, and a clean tracked worktree. Read AGENTS.md, the certify skill §6a, current UserPreference W3 evidence, and existing UserPreference architecture guards. If the starting head or scope is different, STOP without modifying anything.

ALLOWED WORK
Add one narrowly scoped Host.Tests architecture guard and the minimum required UserPreference-only evidence/SoT/recovery record. Do not modify existing historical certification facts except an explicitly necessary additive record. Preserve the global Host checkpoint.

ACCEPTANCE CHECKS

Derive all 6 actual route-to-handler-to-Send-to-request mappings from the three UserPreference endpoint files and the route-group prefixes. Assert exact audience, verb, full route and dispatched request; GET/PUT for customer and admin operator share GetUserPreferenceQuery/UpsertUserPreferenceCommand; admin UI GET/PUT /{key} use GetUiPreferenceQuery/UpsertUiPreferenceCommand.
Verify 6 routes, exactly one Send per route, 4 distinct request types with real IRequest/handler registrations, and no missing, duplicate or orphan mapping. Do not require 6 distinct requests.
Cross-check the existing 3 VALIDATOR_REQUIRED + 1 NO_VALIDATOR_REQUIRED matrix, DI discoverability and server-derived actor exemption; do not broaden validation work.
Fail closed for unknown route syntax, missing or multiple dispatch, and untraceable dispatched requests. Include one pure in-memory negative mutation preserving route/Send counts that the exact-map guard rejects; do not mutate production files on disk.
Run the new guard and focused existing UserPreference guards. If an unrelated global test fails, report its exact identity and a grounded starting-head comparison; no unrelated repairs.

HARD LIMITS
NO production changes, endpoint or Contracts changes, schema/migrations, frontend, shared foundations, Skill edits, global pins, unrelated modules, weakened assertions, widened baselines, or repeated AMSC waves. If a real production flaw appears, STOP and report ARCHITECT_DECISION_REQUIRED rather than expanding scope. Complete once; do not enter an iterative review loop.

DELIVERY AND STOP
Commit/push only the allowed test/evidence changes if all checks pass. Report full SHA, actual changed-file inventory, test outcomes, negative-mutation evidence, zero production changes, and HEAD == origin/main. StopGate: USER_REVIEW_USERPREFERENCE_AMSC_001_W3_R1. automaticNextImplementationTask = NONE. Do not start another task.
END_TOOBA_WORKER_TASK
