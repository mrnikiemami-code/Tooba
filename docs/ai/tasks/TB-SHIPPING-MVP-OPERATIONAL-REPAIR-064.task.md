PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-MVP-OPERATIONAL-REPAIR-064
Parent-Task: TB-SHIPPING-MVP-SCRATCH-HTTP-063
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-MVP-OPERATIONAL-CLOSURE
Mode: BOUNDED_SHIPPING_MALFORMED_BODY_AND_FRESH_LANGUAGE_BOOTSTRAP_REPAIR
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 81dea23234841d6dade3eca6dd0b808080a0933b
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 30 minutes HARD

ARCHITECT AUTHORITY
One task only. Task 063 evidence is ACCEPTED for its stated scratch HTTP scope; the current work fixes two reproduced operational defects, F-063-1 and F-063-2, without changing shipping semantics, Checkout, commercial policy, or frozen Host composition. This is task 2 of a maximum 8-task closure budget counting 063. Worker PASS != Architect ACCEPT. Never auto-advance. Follow AGENTS.md, docs/ai/TOOBA-PIPELINE-PROTOCOL.md, .cursor/skills/tooba-backend-feature/SKILL.md.

PREFLIGHT / SAFETY GATE

On BRIDGE-WAKE claim exactly one Pending task; persist receipt verbatim, never stage receipt. Fetch origin; confirm HEAD == origin/main and expected ancestry. If additional commits exist, examine every changed path; only demonstrably nonoverlapping evidence/bookkeeping drift may be accepted, otherwise RECOVERY_CONFLICT STOP. Preserve unrelated dirty/untracked files; no reset, stash, rebase or force-push.
Read 063 plan, RESULT.bridge.txt, log-crud-063.txt S1/S2, 062 bootstrap, 060 safety guard, current HTTP exception/error mapping, Shipping endpoint binding, Localization LanguageBootstrapHostedService and context resolution. Trace exact source cause BEFORE changing code. Do not assume a generic global handler is the correct repair location.
No access to original :5088 Host, existing databases including tooba_alpha, or any production/staging service. No original database query even read-only. Avoid services unless strictly necessary. Any integration DB/Host must be task-exclusive disposable scratch with 060 offline effective-target guard and independent database-side target proof BEFORE any write; otherwise use focused no-DB tests and report scope honestly. No broad test suite or idle polling.

MISSION A — F-063-1, STRICT CLIENT-ERROR MAPPING
Repair malformed/undeserializable JSON on Shipping admin rate AND coverage-grant create/edit routes so invalid body shapes return HTTP 400 with a stable declared client-input error code, never 500 platform.unexpected. Cover (at minimum) syntax-invalid JSON and invalid/empty GUID in scope (063 S1/S2), plus valid-json/absent-or-null scope continuing to return shipping.validation.scope_required as proven in 063. Keep access authorization fail-closed: unauthenticated/unauthorized requests stay 401/403; do not mask real infrastructure/programmer faults as 400. Prefer the established presentation/validation error contract; only introduce an additional code if necessary and wire its EN/FA presentation and guards consistently. Do not weaken global exception handling or alter unrelated modules. Focused tests must reproduce failure before and pass after, including coverage-grant as appropriate.

MISSION B — F-063-2, FRESH-DB LANGUAGE BOOTSTRAP
Trace why LanguageBootstrapHostedService hits ToobaNpgsql.ResolveForContext without a bound commerce context in a fresh isolated Development startup. Apply the minimum ownership-correct change to ensure canonical fa-IR Localization language bootstrap works in the supported Development initialization order without manual authenticated HTTP POST. Preserve tenant isolation, Localization ownership, idempotence, production safety, and exact path/namespace rules. If resolving correctly would require broad Host boot-order or production architecture redesign, DO NOT improvise: record precise root cause and classify PARTIAL with next-action evidence. Never hardcode a language GUID, bypass DbContext/tenant resolution, or enable legacy Catalog bootstraps. A focused regression should demonstrate one fresh Development scratch initialization and a repeat start where language count remains one; if full scratch verification would exceed timebox, prefer truthful PARTIAL.

PROOF / GUARDS

Run only narrow relevant test projects/tests and build of modified projects. No unnecessary broad suites. Do not stop or restart the user's Host; honor DLL locks.
If real-HTTP proof is feasible inside the task-exclusive scratch timebox, execute 063 S1/S2, missing-scope, valid request, auth fail-closed, and language bootstrap before/after. Otherwise report EXACTLY what was verified in unit/integration scope and mark PARTIAL for any unverified mandatory acceptance item.
Check unchanged: Shipping authorization, country lookup, rates/coverage semantics, Order/Checkout/shadow feature flags, no Results.Json, no custom capability/role inserts. No invented tariffs or nationwide grants.
Document root causes, path-by-path diff, redacted tests and statuses, elapsed, limitations, clean shutdown of task-owned resources only; no credential logging.

ALLOWED PATHS

Narrow existing Shipping API binding/validation/presentation files actually responsible for F-063-1, plus directly associated Shipping test and EN/FA resource files if needed.
Narrow existing Localization Development bootstrap/tenant-context ownership files directly responsible for F-063-2, plus directly associated tests if needed.
docs/planning/shipping/SHIPPING-MVP-OPERATIONAL-REPAIR-064.md
docs/architecture/evidence/TB-SHIPPING-MVP-OPERATIONAL-REPAIR-064/**
docs/ai/tasks/TB-SHIPPING-MVP-OPERATIONAL-REPAIR-064.task.md (unstaged receipt only)
Explicitly FORBIDDEN: changes to Host Program.cs/Startup, Host/Admin/Security frozen trees, global exception mapping without separately demonstrated minimal ownership and explicit Architect authorization, unrelated modules, schema/migrations, Order/Checkout, frontend, production appsettings, permission catalogs. If genuine defect cannot be fixed within allowed surface, STOP and report; do not expand scope.

ACCEPTANCE / STOP
PASS only if BOTH defects are source-corrected, focused automated regressions pass, and an isolated fresh start proves automatic fa-IR initialization/idempotence AND authenticated invalid-JSON/invalid-GUID 400 stable declared error without weakening prior missing-scope 400/auth behavior. PARTIAL when any acceptance proof is absent. BLOCKED for safety/authority prerequisite. RECOVERY_CONFLICT for unsafe drift. HARD STOP at 30 min; no new task.

RESULT CONTRACT — BRIDGE-WAKE-V1
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-MVP-OPERATIONAL-REPAIR-064
Parent-Task: TB-SHIPPING-MVP-SCRATCH-HTTP-063
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-MVP-OPERATIONAL-CLOSURE
Mode: BOUNDED_SHIPPING_MALFORMED_BODY_AND_FRESH_LANGUAGE_BOOTSTRAP_REPAIR
Bridge-Task-Row: <actual row>
Status: PASS | PARTIAL | BLOCKED | RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 81dea23234841d6dade3eca6dd0b808080a0933b
Base-SHA: <actual>
Code-SHA: <actual or NONE>
Evidence-SHA: <actual or NONE>
automaticNextImplementationTask: NONE
<root causes; precise source edits; pre/post 400 + language proof; tests; isolated provenance; safety; elapsed; changed paths; commit/push; remaining blockers>
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK
