PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-MVP-ISOLATED-HTTP-061
Parent-Task: TB-SHIPPING-MVP-SAFETY-REPAIR-060
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-MVP-OPERATIONAL-CLOSURE
Mode: STRICTLY_ISOLATED_AUTHENTICATED_HTTP_AND_NULL_SCOPE_REGRESSION
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 60ce0b14d53be693d6d9d53d4119adfa16ec5d37
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 30 minutes HARD

ARCHITECT AUTHORITY
This task is the only authorized follow-up to the Architect-reviewed 060. The previous 059 run accidentally wrote to the real Development database because tenant-alpha was mis-spelled as tenant_alpha in an environment override. The 060 offline guard is not proof of the effective launched process's connection. Treat this as a safety-critical trust boundary. Do NOT repeat a real-Development write. Do not silently deploy, restart or stop the user's running Host. No checkout cutover.

PREFLIGHT — STOP ON CONFLICT

After BRIDGE-WAKE, claim exactly one pending task on tooba-main; preserve received artifact verbatim as an unstaged receipt. Read AGENTS.md, docs/ai/TOOBA-PIPELINE-PROTOCOL.md, .cursor/skills/tooba-backend-feature/SKILL.md, docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md (Shipping checkpoint) and reports/evidence 058–060.
git fetch origin; compare HEAD, origin/main and Expected-Base-SHA. An intervening evidence-only bookkeeping commit is acceptable only after a path-by-path zero-source-overlap proof. Any source conflict => RECOVERY_CONFLICT/STOP. Never reset/stash/rebase/force-push; do not stage unrelated dirty paths.
Review actual Host startup, configuration hierarchy, tenant resolution, DB resolver, auth bootstrap and the new 060 assert-scratch-target.js BEFORE launching anything. Do not trust unexpanded environment strings or an offline guard alone.
Inspect isolation feasibility FIRST. If you cannot PROVE that the launched process's EFFECTIVE target is exclusively a task-created scratch database without touching real Development DB, STOP as BLOCKED. Never try a potentially unsafe write to discover which database was selected.

MISSION A — STRONG ISOLATION (MANDATORY GATE BEFORE ANY HTTP WRITE)
Use a NEW task-exclusive PostgreSQL container on loopback with a unique container/port/database, or a truly isolated alternative whose database credentials cannot access existing Tooba databases. Prefer a container with a fresh role and restricted network/port (deny access to existing 127.0.0.1:5432 and known Tooba database connections). Populate only the task-owned scratch DB with the minimum migrations/fixtures required; never clone real customer/production data or access tooba_alpha. Do not use appsettings.Development.json, real tenant connection references, or inherited Tooba__/ASPNETCORE__ environment as a base configuration. Build an isolated configuration with exactly one tenant and exactly one reference (use the canonical HYPHENATED tenant key in both definition and effective reference). Apply the 060 guard to the exact isolated config/override inputs, with an expected sentinel and matching fingerprint. ALSO independently prove actual process-effective resolved connection: use a container/database permission barrier plus a non-mutating connection provenance query or equivalent verifiable mechanism tied to the launched process. If these cannot be proved, do not launch/write. Never print secrets. Capture safe/failed-closed checks in evidence.

MISSION B — REAL HTTP REGRESSION AND AUTHORIZED CRUD
Only after Mission A passes, start a TEMPORARY current-code Host on a separate loopback port pointing exclusively to task-owned scratch. The user's Host and browser processes must remain untouched. Reuse the existing supported Development actor/admin bootstrap and authorization mechanisms; do not invent roles/capability tuples or bypass security. Verify auth failure paths (401 without actor/token; 403 for an unauthorized actor) and real authorized GET Geography IDs, create/read/update/deactivate one Shipping rate and one coverage grant under a disposable service using the existing endpoints. Verify duplicate and cross-service mutation denials. Verify missing/null scope for BOTH rate and grant POST yields canonical HTTP 400 validation.failed with shipping.validation.scope_required (never 500), measured over actual HTTP. Read-after-write must match. Confirm empty grant set remains NOT COVERED and no invented commercial or nationwide default is written.

MISSION C — CLEANUP AND HONEST VERDICT
Stop only task-owned Host/container, delete only task-owned scratch volume/database and temp credentials; independently verify the user's :5088 Host remains untouched. Do NOT query, edit or clean up tooba_alpha, pricing anomalies or any other existing DB. If task 060 guard fails or cannot prove effective isolation, report BLOCKED with precise missing proof; do not improvise. Save bounded logs and a concise report to docs/planning/shipping/SHIPPING-MVP-ISOLATED-HTTP-061.md and docs/architecture/evidence/TB-SHIPPING-MVP-ISOLATED-HTTP-061/**. No production source changes unless a NEW, directly reproduced task-scoped blocker is discovered; then STOP_FOR_ARCHITECT and report, rather than widen scope.

ALLOWED PATHS

docs/planning/shipping/SHIPPING-MVP-ISOLATED-HTTP-061.md
docs/architecture/evidence/TB-SHIPPING-MVP-ISOLATED-HTTP-061/**
docs/ai/tasks/TB-SHIPPING-MVP-ISOLATED-HTTP-061.task.md receipt only (never stage)

FORBIDDEN

Any use of real Tooba tenant/customer/Development/staging/production databases, including SELECT, dump or connection probe. Specifically no tooba_alpha and no existing 127.0.0.1:5432 PostgreSQL endpoint.
Any change to existing Host, Checkout, Order, Fulfillment, Geography, Shipping production code, frontend, configuration, schema, migrations, service catalog, permissions or guards.
Any user Host restart/deploy, production data edits, fake grants or rates, unapproved authority switch, Results.Json, forced git operation, automatic follow-up.

ACCEPTANCE / STOP
PASS only if the isolated effective target is independently proven BEFORE requests, BOTH actual missing-scope POSTs return 400 with stable validation code, authenticated rate/grant CRUD roundtrips succeed entirely within scratch, unauthorized access denied, cleanup proven and no existing Host or database touched. PARTIAL or BLOCKED for any unmet condition; never claim PASS from mocks, offline tests or a failed isolation gate. Hard stop at 30 minutes even if incomplete; no overrun to finish a test.

RESULT CONTRACT — BRIDGE-WAKE-V1
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-MVP-ISOLATED-HTTP-061
Parent-Task: TB-SHIPPING-MVP-SAFETY-REPAIR-060
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-MVP-OPERATIONAL-CLOSURE
Mode: STRICTLY_ISOLATED_AUTHENTICATED_HTTP_AND_NULL_SCOPE_REGRESSION
Bridge-Task-Row: <actual claimed row>
Status: PASS | PARTIAL | BLOCKED | RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 60ce0b14d53be693d6d9d53d4119adfa16ec5d37
Base-SHA: <actual>
Code-SHA: NONE
Evidence-SHA: <actual>
automaticNextImplementationTask: NONE
Report preflight/drift; exact effective connection isolation and safety proof; real HTTP status/code outcomes including two null scopes; authenticated CRUD; cleanup; tests; changed paths; actual elapsed; git commit/push; blockers. Never include credentials.
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK
