PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-106
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-105
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: BOUNDED_ISOLATED_CATALOG_PUBLISH_MISSING_CODES_CAPTURE
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 2699e53e877a8cbb6f084d1a93e0410aa9950182
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 30 minutes HARD. Minute 20: end probing, minute 22: begin mandatory cleanup/report. One attempt maximum; no retry loops.

ARCHITECT RULING
105 PARTIAL accepted: Development-only CatalogDevelopmentPublishDiagnostics now emits the real ProductPublishReadiness.MissingRequirements codes on the schema-demo PublishProductAsync refusal, but the actual failing scratch-product codes remain NOT YET OBSERVED. Task 106 is a DIAGNOSIS/PROOF ONLY. Obtain those codes with ONE bounded, isolated startup using the existing 102/103 scratch recipes, then STOP; do not try to make publishing pass in this task. Catalog owns its lifecycle and seed. The old full CatalogDemo reset-and-seed/seed-only path generates hundreds of drafts and is forbidden. Only the startup legacy bootstrap path that fails at the FIRST CatalogAttributeSchemaDevelopmentSeed product may be attempted, with a hard external watchdog and cleanup. Do not enable ShippingRateAuthority or ShippingOperationalShadow.

PREFLIGHT — STOP ON CONFLICT

Claim exactly one pending Bridge task on tooba-main. Keep docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-106.task.md UNSTAGED. Read AGENTS.md, controller/protocol and 102–105 evidence and source; specifically verify the 105 diagnostic call and real logger category. Fetch origin/main, require Expected-Base-SHA ancestor and zero overlapping source drift. Preserve all dirty/untracked files; never reset, stash, rebase or force-push.
Measure shared :5088 /health and /__platform-commerce provenance, :3000 status and tooba_alpha read-only parity (7 methods/14 translations and historical counts), BEFORE any scratch launch. Record no credentials. Never stop/rebuild/restart shared Host/Next.
Confirm that the intended startup flow with Tooba:CatalogDemo:RunLegacyBootstraps=true reaches CatalogAttributeSchemaDevelopmentSeed.PublishProductAsync, where the 105 diagnostic logs MissingRequirements. If the instrumentation is not present in the isolated build, STOP rather than execute an opaque run.

MISSION A — SINGLE ISOLATED DIAGNOSTIC START (MAX 12 MINUTES INCL. PREP)

Create a task-owned disposable PostgreSQL 17 scratch container with no volume, loopback-only port, and scratch tenant + messaging databases. Reuse proven 102/103 isolation overlay scripts, adapted to 106. The effective Host configuration must contain exactly one tenant and only scratch DB references, and MUST NOT point at tooba_alpha. Test offline isolation guard before launch, and inspect scratch DB-side isolation afterward. No shared DB access from isolated process.
Compile/run current code in task-owned output and config directories (not shared locked outputs) on a unique loopback Host port. Set ASPNETCORE_ENVIRONMENT=Development, RunLegacyBootstraps=true, and configure the existing 105 diagnostic logger category/level to ensure the structured missing codes are visible. Do not set extra seed controls, shipping checkout/rate gates or any auth bypass. Ensure no secrets in tracked output.
ONE startup attempt ONLY, controlled by an OUTER process watchdog: maximum 4 minutes from Host launch (shorter if the expected exception occurs). Collect ONLY bounded, redacted log excerpts for stage=catalog-dev.attribute-schema.publish-product and missing=[...], together with original exception type/message. The expected crash itself is a useful diagnostic PASS if real codes are captured. Do not run Catalog demo HTTP reset-and-seed or seed-only; do not wait 5+ minutes for sellable offers; do not repeat the attempt if Missing[] cannot be captured.
Distinguish: OBSERVED codes from actual scratch log versus POSSIBLE codes from source/tests. Record the exact missing codes if present; if absent, record why (log category, crash before diagnostic, timeout, unexpected startup issue), without guessing. No source fix in this task. No raw DML fixtures, no relaxation of category/SEO/media/variant/attribute rules.

MISSION B — CLEANUP AND ARCHITECT HANDOFF

Kill/stop only the verified task-owned scratch Host process if still running; confirm its port free. Destroy scratch container/volumes, ephemeral config/credentials/run dirs and verify no scratch listeners left. Redact logs; never commit credentials/PII. Re-measure the shared Host and DB read-only parity; evidence of zero shared writes is mandatory.
No broad tests, no Host test-suite replay, no frontend tests if no source changes. Write docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-106.md, docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-106/** with sanitized log and exact code-to-prerequisite mapping, or a precise blocker if unavailable. Commit/push ONLY task-owned docs/evidence; master recovery unchanged. No changes in src/, scripts/, appsettings, migrations or tests.
Acceptance: PASS_DIAGNOSTIC = real missing codes captured on the single scratch run, original refusal retained, scratch fully cleaned, shared parity unchanged. PARTIAL = isolated run safe but codes absent and exact reason documented. BLOCKED = isolation cannot be proved, compilation lacks diagnostic seam, or timebox prohibits safe execution; in these cases do not launch/continue. No claim that sellable offers, storefront labels or checkout are proved. End STOP_FOR_ARCHITECT; next task only after Architect reviews the observed facts.

ALLOWED PATHS

docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-106.md
docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-106/**
docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-106.task.md (UNSTAGED receipt only)
Ignored, task-owned .tmp-106-* only during execution, deleted afterward
FORBIDDEN: any src/** edit, long/full demo seed, real tooba_alpha writes, shared Host/Next restart, order/checkout/payment/settlement changes, seed-rule bypass, synthetic readiness codes, automatic task advancement.

RESULT CONTRACT — BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-106
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-105
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: BOUNDED_ISOLATED_CATALOG_PUBLISH_MISSING_CODES_CAPTURE
Bridge-Task-Row: <assigned>
Status: PASS|PARTIAL|BLOCKED
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 2699e53e877a8cbb6f084d1a93e0410aa9950182
Base-SHA: <actual>
Code-SHA: NONE
Evidence-SHA: <actual>
automaticNextImplementationTask: NONE
Report: exact observed Missing[] or NOT OBSERVED, stage, original exception, isolation and watchdog proof, launch count, measured elapsed time, cleanup, shared parity, touched paths, rollback and one recommended smallest next corrective task.
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK