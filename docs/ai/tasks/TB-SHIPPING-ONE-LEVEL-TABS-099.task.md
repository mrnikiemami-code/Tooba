PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-099
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-098
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: BOUNDED_ISOLATED_ACTIVATION_WRITE_READBACK_ROLLBACK
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 0a66c7a2c1c98ba1bee52b3e7a79957020cf3291
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 30 minutes HARD; if deadline reached, STOP and report PARTIAL/BLOCKED, not an overrun.

ARCHITECT RULING
098 PASS is accepted strictly for the shared Development read-only five-tab/activation UI proof. The missing proof is one genuine authorized PUT followed by read-back and reversal through the 096 Fulfillment activation authority. NEVER mutate any method on the shared tooba_alpha database, even temporarily. Prove the write on a disposable, task-owned isolated database and Host instance, with independently verified isolation. Maintain the actual storefront legacy option as authority and shipping_methods as its aligned projection. Current shared :5088/:3000 environment must remain entirely untouched and operational. This task does not authorize Checkout cutover, optionless activation or any production operation.

PREFLIGHT — STOP ON CONFLICT

Claim exactly one Bridge task, keep the task receipt UNSTAGED, fetch origin/main and validate base. Only non-overlapping result/evidence drift may be accepted; overlapping src drift => RECOVERY_CONFLICT. Preserve every pre-existing dirty/untracked path, no reset/stash/rebase/force-push.
Read AGENTS.md, pipeline/controller, docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-096.md through 098 and relevant evidence, Fulfillment activation command/store, 097 activation client, 093 stack manager. Determine whether a task-owned disposable database can be provisioned and a real authorized admin actor established without copying private/customer data or bypassing authorization. If not, BLOCKED_ISOLATED_AUTH_PROOF; do not use shared Dev as fallback.
Prove shared :5088/:3000 health, provenance and 7-method activation baseline through READ-ONLY probes. Record only nonsecret identifiers and aggregate state. No shared database write whatsoever.

MISSION A — SAFE ISOLATED AUTHENTICATED TARGET

Prefer a new scratch PostgreSQL container/database, with isolated schema migrations and the SMALLEST deterministic test fixtures for a legacy active parent plus two option siblings and the matching one-level canonical method rows; include needed Fulfillment language/admin authorization fixture through existing supported Development/test bootstrap only. Do not clone tooba_alpha rows or import real user/business data.
Launch current-code Host on a free loopback port with scratch-only connection/tenant configuration and process-only Tooba__ShippingOneLevelCatalog__Enabled=true. Disable unrelated background jobs when possible. PROVE BEFORE ANY PUT: /__platform-commerce scratch deployment/tenant/connection; database-side pg_stat_activity references only scratch database; scratch DB has no tooba_alpha; authenticated admin 200, anonymous 401, guest 403. If provenance or genuine authentication cannot be proven, STOP BLOCKED; never fabricate a header/bypass.
Use an isolated frontend only if needed to demonstrate the real browser confirm path. Never restart or reconfigure shared :5088/:3000, and never keep scratch credentials in tracked files.

MISSION B — REAL REVERSIBLE ACTIVATION PROOF

Choose a task-owned ACTIVE option-backed method, with a second ACTIVE sibling, both present in the legacy options and one-level projection. Snapshot option IDs, legacy activation, one-level activation, names/codes and storefront eligibility BEFORE a write. All records must be synthetic scratch fixtures only.
With a legitimately authenticated Development admin, execute ONE activation state change via the real 096 PUT endpoint (prefer the 097 browser confirmation UI if the isolated frontend can securely log in). Verify response code + changed=true; re-query legacy option row and one-level method in the same scratch database, and the storefront eligibility read path if the test fixture supports it. Verify only the target two activation fields changed; sibling UUID/activation/translation preserved, and no historical/financial rows modified.
Reverse the exact change through the SAME authorized API/UI and prove final target values and identity equal baseline. Prove a repeated same-state request returns changed=false WITHOUT additional mutation. If any stage fails, stop writes, preserve diagnostic evidence, and prefer dropping the entire scratch database/container rather than attempting repair on shared state.
Demonstrate fail-closed 401/403, inactive-parent/optionless refusal using isolated fixtures or targeted tests; NEVER switch optionless real services or live post:express/snapp_courier.

MISSION C — CLEANUP AND DELIVERABLE

Stop isolated Host/frontend; destroy scratch container/database after read-back proof, check no listeners remain, and recheck shared :5088/:3000 provenance, gate and method states read-only. If using task-owned fixtures, cleanup must not touch shared Dev.
Tests limited to necessary focused activation tests and any true production edit; don't rerun broad suites. NO source change necessary if existing 096/097 contracts work; if a genuine bug is found, make only the smallest module-owned repair within scope and test it, else report PARTIAL for architect.
Add docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-099.md and docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-099/**: isolation proofs, actual HTTP method and statuses, before/change/revert/readback truth, failure outcomes, shared-stack parity, cleanup, SHA and measured duration. Update master recovery only under existing append-only conventions.
Commit/push only task-owned code (if any) and evidence; do not stage task receipt or unrelated dirty files. Source/test completion does not substitute for real authorized write proof; report PARTIAL/BLOCKED honestly.

FORBIDDEN

Any PUT/POST/PATCH/DELETE or other DML to shared tooba_alpha or shared :5088; any shared Host/Next restart.
Seeding actual customer/order/merchant records from shared DB into scratch; bypassing auth or inventing a permission.
Activating an optionless method, rewriting method code/legacy option UUID, touching Payment/Settlement/Checkout, changing rate authority, production or staging contact.
Timebox overrun, speculative service rewrites, retries/loops or unapproved architecture changes.

ACCEPTANCE
PASS only on real authenticated scratch HTTP write + legacy/one-level atomic alignment + actual rollback + unchanged sibling + clean isolation/cleanup + shared-stack parity. PARTIAL if some proof is missing and bounded work completed. BLOCKED if scratch isolation/admin auth cannot be proven. STOP_FOR_ARCHITECT after one bounded execution; no auto-next.

RESULT CONTRACT — BRIDGE-WAKE-V1
Return Task-ID, Parent-Task, Channel, Worker-Id, Agent-Type, Program, Mode, Bridge-Task-Row, Status PASS/PARTIAL/BLOCKED, Stop-State STOP_FOR_ARCHITECT, Expected-Base-SHA, Base-SHA, Code-SHA, Evidence-SHA, automaticNextImplementationTask NONE; exact changed paths, measured minutes, actual tests, isolated before/after write/revert, provenance/auth, cleanup, shared-state parity and blockers.
END_TOOBA_TASK
