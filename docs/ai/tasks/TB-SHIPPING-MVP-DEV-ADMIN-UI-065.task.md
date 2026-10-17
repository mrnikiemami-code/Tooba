PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-MVP-DEV-ADMIN-UI-065
Parent-Task: TB-SHIPPING-MVP-OPERATIONAL-REPAIR-064
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-MVP-OPERATIONAL-CLOSURE
Mode: BOUNDED_EXISTING_DEV_HOST_ADMIN_UI_END_TO_END
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 6f87d7dd54b2aa2e2904361b52af24330a8b538e
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 45 minutes HARD

ARCHITECT DECISION / BUDGET
064 is ACCEPTED for its scoped fixes based on committed evidence; 065 is task 3 of the eight-task Shipping delivery budget (063-070). This task must produce practical working functionality, not another artificial scratch proof. User expressly identifies the current Development database as disposable test data; DO NOT infer permission to touch staging/production or wipe databases. No new scratch container, cloned database, synthetic test server, test harness, or lengthy test suite. Do not loop on blocked build/test or retry the same failure. Stop on genuine unsafe ambiguity.

PREFLIGHT / SAFETY

After BRIDGE-WAKE claim exactly one pending task on tooba-main; persist its exact receipt. Read AGENTS.md, docs/ai/TOOBA-PIPELINE-PROTOCOL.md, Shipping recovery checkpoint, 063/064 planning and evidence, and the existing frontend Shipping screen implementation. Fetch origin/main; verify ancestry from Expected-Base-SHA. Only evidence/bookkeeping fast-forward with individually checked non-overlap is acceptable. No reset/stash/rebase/force push; preserve unrelated dirty files. If source drift overlaps -> RECOVERY_CONFLICT STOP.
Establish the ACTUAL running Development Host identity/config and actual existing Development database target; do not assume PID/port from 064 evidence is still current. Distinguish Development from staging/production with hard proof before connecting or writing. No credential logging. Check migrations for Geography and Shipping on existing Development DB and execute ONLY missing existing module-specific migrations if truly necessary; take lightweight schema-level backup only if needed, NOT a full database clone. No all-module MigrationRunner apply, no schema changes without existing migrations. Only modify test records that this task creates or explicitly identifies; user stated test data is disposable but do not silently wipe unrelated data.
A Development Host restart/rebuild is allowed ONLY if needed to load current committed routes/fixes and after positive identification of the process as the user's Development Host; perform controlled graceful stop/start with a recorded launch command/config and easy rollback. Do not stop any unidentified service, frontend dev server, staging or production process. If safe launch/recovery information is missing, mark BLOCKED and give exact next action; do not kill processes blindly. Avoid in-place Host.Tests DLL locks: do NOT attempt broad Host.Tests while Host is running. Do not spend more than 3 minutes on a blocked build.

MISSION — REAL DEV AND UI, NOT ANOTHER SCRATCH DEMO
A. Establish current code running in an existing Development Host, with existing Development test database, and prove health, route availability and canonical admin auth. Confirm current 064 malformed-body handling is present: unauthenticated 401/403 and authorized malformed POST 400 shipping.validation.body_invalid. Verify Geography countries and Shipping service GET authorized 200. Record deployment/source SHA and effective target (redacted).
B. Use a known existing active Fulfillment service code, not an imaginary svc-065, and real Geography IDs from the existing Development DB. If needed, add only minimal Geography reference seed from reviewed 047 generator; do not fabricate service areas or commercial delivery eligibility. Exercise the EXISTING /admin/shipping-services frontend with authenticated admin against real Development backend (browser/UI or focused existing frontend automation if available): open service, open rates/coverage dialog, select Geography, create TEST rate and TEST grant, read after write, edit rate, toggle both, display visible validation errors, refresh and confirm persistence. Explicitly label inserted fixtures TEST, use an unmistakable temporary naming/scope marker where supported, and log IDs for controlled cleanup. If browser UI automation is unavailable, do HTTP proof plus frontend component interaction test; report UI as UNVERIFIED, never claim UI PASS from mocked fetch.
C. Repair only directly observed Shipping-admin screen/API wiring defects that prevent actual workflow. Source of Truth: Geography owns place IDs, Shipping owns rate/coverage, Fulfillment owns shipping service catalog, Order owns customer promise. Keep Contracts-only cross-module calls and Host composition-only; MediatR 12.5 ISender, ApiResponseFactory/Result, no Results.Json. Do not touch Order/Checkout/Payments/snapshot/pricing authority or enable shadow/cutover. Scope to existing Shipping admin frontend and related Shipping/Geography module files only; if another module or frozen Host/Admin/Security change is genuinely needed, report blocker rather than widen scope.
D. Verify UI save/reload and actual persisted state on Development; verify fail-closed no coverage grants on an untouched service (not implicit nationwide coverage). Avoid expensive tests: existing focused frontend Shipping tests plus narrowly affected backend tests, one actual end-to-end roundtrip. Keep a precise table of what was exercised through UI versus HTTP. If time remains, close any directly observed defect in the same task; do not expand into shadow or cutover.

ALLOWED SOURCE SCOPE

src/frontend/** files exclusively belonging to the existing admin shipping-services screen and its directly owned tests/API adapter
src/backend/Modules/Shipping/** only where an observed defect forces a minimal repair
src/backend/Modules/Geography/** only where an observed defect forces a minimal repair
docs/planning/shipping/SHIPPING-MVP-DEV-ADMIN-UI-065.md
docs/architecture/evidence/TB-SHIPPING-MVP-DEV-ADMIN-UI-065/**
docs/ai/tasks/TB-SHIPPING-MVP-DEV-ADMIN-UI-065.task.md (received Bridge receipt ONLY, NEVER stage)
No other production file without Architect approval. No modifications merely to make a brittle test pass; no weakening guards.

ACCEPTANCE / STOP
PASS only if current Development host with correct live source and existing test DB is available; authorized Geography+Shipping HTTP is healthy; current 064 body regression succeeds; and a genuine existing admin UI create/read/edit/toggle/reload flow for both rate and coverage is demonstrated and persisted, including non-admin denial. A code-only or mocked-UI result is PARTIAL. Inability to safely identify/restart Development host is BLOCKED with precise reproducible steps. Stop by 45 minutes regardless of verdict and preserve all evidence. No repeated container setup, no broad guard suites, no repeated waiting on locked Host binaries.

REQUIRED RESULT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-MVP-DEV-ADMIN-UI-065
Parent-Task: TB-SHIPPING-MVP-OPERATIONAL-REPAIR-064
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-MVP-OPERATIONAL-CLOSURE
Mode: BOUNDED_EXISTING_DEV_HOST_ADMIN_UI_END_TO_END
Bridge-Task-Row: <actual>
Status: PASS | PARTIAL | BLOCKED | RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 6f87d7dd54b2aa2e2904361b52af24330a8b538e
Base-SHA: <actual>
Code-SHA: <actual or NONE>
Evidence-SHA: <actual or NONE>
automaticNextImplementationTask: NONE
<real dev target provenance; restart action/rollback; UI-vs-HTTP matrix; fixtures and cleanup; existing service and geography IDs; defect fixes; focused tests; elapsed; changed paths; source SHA; commit/push; remaining blockers>
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK
