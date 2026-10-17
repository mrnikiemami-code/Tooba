PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-092
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-091
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: BOUNDED_DEVELOPMENT_DEPLOY_AND_REAL_FIVE_TAB_SMOKE
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 903c5d839146874af88a5e0ce1bbe7b73b46b445
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 30 minutes HARD

ARCHITECT RULING
Tasks 081–091 are CLOSED for the bounded source program, but actual merchant UI on Development :5088 is unproven: shared Host binary is stale (new one-level endpoint returns 404), the one-level gate is OFF, and the DB has already been migrated/backfilled (7 methods/14 translations). The user expressly requests continued bounded tasks until the system is truly operable, with each result reviewed before the next task. This is task 092, ONE task only. Target is live Development operability, NOT commercial checkout cutover. The user authorizes work towards a controlled Development-only rollout, but this instruction does NOT authorize an unverified kill, permanent config change, permission bypass or destructive database mutation. A restart is permissible ONLY when full launch provenance, exact process ownership, safe rollback, and operator-safe stop/start are independently established; otherwise use an isolated, non-disruptive current-code instance and STOP with an exact blocker. No source-wide redesign.

PREFLIGHT — REQUIRED / STOP ON CONFLICT
Claim exactly one Pending task under BRIDGE-WAKE-V1 on tooba-main. Persist received file as docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-092.task.md receipt UNSTAGED. Read AGENTS.md, docs/ai/TOOBA-PIPELINE-PROTOCOL.md, 090–091 planning/evidence, canonical master recovery, backup-manifest and rollback-090.sql. Fetch origin/main. Require exact Expected-Base-SHA or independently prove ONLY non-overlapping 091 result-bookkeeping drift. Source overlap => RECOVERY_CONFLICT/STOP. Preserve pre-existing dirty/untracked work; never reset/stash/rebase/force push.

MISSION A — PROVENANCE / DEPLOYMENT SAFETY (FIRST)

Verify live :5088 GET /__platform-commerce is dev-singlestore / store-alpha / tenant-alpha => tooba_alpha, and record exact current listener PID, executable path, command line, parent, working directory, environment/setup provenance without logging secrets. NEVER trust remembered PID 33548. Verify :3000 frontend, DB migration once, 7 methods/14 translations, 090 backup file/hash and rollback. Recheck legacy order/draft counts without modifying them. No production/staging contact.
Identify a reproducible supported Host launch/stop script from repo/operator evidence. Prefer parallel current-code Host with a separately scoped Development port when production-safe. Build to isolated output paths to avoid locked DLLs. Replacing the shared :5088 process is CONDITIONAL upon a documented, repeatable stop/start procedure, proved rollback to prior binary/process command and safe operator scope. If those facts cannot be proved, DO NOT stop or replace it. Do not spend task time on DLL-lock retries.
Ensure the gate Tooba:ShippingOneLevelCatalog is enabled ONLY in an ephemeral Development process configuration (not committed to appsettings, no broad switch); ShippingRateAuthority and ShippingOperationalShadow remain OFF. No seed/backfill repetition needed. Confirm explicitly that enabling only the catalog cannot cut over Checkout.

MISSION B — REAL BEHAVIOUR, NOT JUST SOURCE TESTS
4. On a VERIFIED current-code Host with safely authorized admin (prefer isolated port plus the real Development catalog read-only if safe; otherwise controlled :5088 rollout): assert /health, /__platform-commerce, GET one-level endpoint, binding resolver. Unauthorized calls must fail 401/403. Authorized gate-ON reply must be ready=true with 7 method codes, including inactive post:express and snapp_courier, without provider/options nesting. If current-code Host routes still 404, fix deploy only; do not invent readiness.
5. Verify the Next page actually reads the intended backend from its real server-side origin. A separate backend on :5099 is not sufficient if the frontend still calls stale :5088; arrange a SAFE task-scoped frontend/Host pair on free ports or a controlled configured rollout, preserve original :3000 and :5088 unless step 2 fully permits replacement. Use real browser/network proof, not only curl/source assertions. Capture screenshots of five-tab main view, list of seven, inactive status, and tab navigation; keyboard and RTL at least once. Do NOT activate inactive methods.
6. For tabs 2,3,4,5 verify actual authenticated reads and server-owned per-method binding: rate/coverage; packaging/payer; schedule/timezone and configured-only capacity; cost-inspector diagnostic. A read-only quote POST is allowed, but it must not create or alter orders. Do not create, mutate, or delete any merchant policy/schedule fixture in this task unless proven disposable with exact reversible cleanup and explicitly recorded separately. If an individual tab fails, record precise reproducible request, status, response and source location; fix ONLY a demonstrated blocker in its module-owned path if bounded and proven. Do not claim successful CRUD without actual roundtrip evidence.
7. Explicitly test legacy Checkout price/paid snapshot unaffected and all three gates scoped correctly: one-level catalog enabled only on task-scoped Development process; ShippingRateAuthority OFF; ShippingOperationalShadow OFF. Confirm rollback gate-OFF behavior and original public service preserved. No new capacity reservation, no COD ledger, no promised delivery, no packed-weight guess.

MISSION C — VERDICT AND HANDOFF
8. Run ONLY the narrowest meaningful tests for changed paths, frontend typecheck if changed, no broad loops. If unchanged, probes and existing 091 tests suffice. Check for leaked credentials and temporary file cleanup. Restore temporary gate/process state on exit UNLESS the separately verified controlled operator deployment is explicitly safe to keep; never leave an insecure, unauthenticated, or accidental listener running. Do not interrupt :5088 or :3000 if uncertain.
9. Write docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-092.md and compact evidence docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-092/ (logs/screenshots with redaction). Append master recovery only if non-overlapping and factual. Include exact current Host/Next URLs, runnable steps, authorized tests, five-tab matrix, whether new UI is visible to user NOW, missing CRUD or commercial blockers, code/evidence SHAs, elapsed, rollback. If blocked, give ONE concrete next corrective action, not generic troubleshooting.

ACCEPTANCE
PASS only with verifiable authenticated, gate-ON, browser-visible 5-tab UI and actual method-scoped backend reads in Development, plus reproducible process/config startup and rollback, without Checkout change. PARTIAL when some genuine evidence exists but live UI or tabs remain unproven; BLOCKED if safety/provenance prevents deployment; RECOVERY_CONFLICT on overlapping source drift. Source-only or isolated 401 proof is NOT live acceptance. Stop at 30 min hard, report actual elapsed, STOP_FOR_ARCHITECT; no autonomous task 093.

ALLOWED PATHS

docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-092.md
docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-092/**
docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md (append-only if safe)
docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-092.task.md (receipt UNSTAGED)
src/frontend/app/admin/shipping-services/** ONLY for one reproduced severe live blocker and corresponding focused tests
src/backend/Modules/Fulfillment/Tooba.Fulfillment.Endpoints/Shipping/** OR src/backend/Modules/Fulfillment/Tooba.Fulfillment.Application/Shipping/** ONLY for a narrowly reproduced read-bridge defect, with focused tests and no unauthorized module edits
Any required wider source edit => STOP_FOR_ARCHITECT with proposal; do not silently broaden.

FORBIDDEN
No random process kill/restart, Host config/credential guessing, permission grants, force push/reset/stash, staging/prod access, unapproved rate/coverage or order writes, Checkout cutover, changing historic method codes, activating inactive methods, monetizing COD, fictitious capacity reservations/delivery promise, enabling ShippingRateAuthority or ShippingOperationalShadow, leaving scratch listener exposed, fabricated evidence or broad test loops.

RESULT CONTRACT — EXACT BRIDGE-WAKE-V1
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-092
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-091
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: BOUNDED_DEVELOPMENT_DEPLOY_AND_REAL_FIVE_TAB_SMOKE
Bridge-Task-Row: <actual>
Status: PASS | PARTIAL | BLOCKED | RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 903c5d839146874af88a5e0ce1bbe7b73b46b445
Base-SHA: <actual>
Code-SHA: <actual or NONE>
Evidence-SHA: <actual or NONE>
automaticNextImplementationTask: NONE
<provenance; deployment authorization/decision; real gate and 7-method read; five tab live matrix; actual browser and backend traces; checkout parity; tests; cleanup/rollback; elapsed; changed paths; remaining exact blocker>
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK
