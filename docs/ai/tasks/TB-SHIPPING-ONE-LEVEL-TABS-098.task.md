PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-098
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-097
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: BOUNDED_SHARED_DEVELOPMENT_ACTIVATION_UI_READ_ONLY_PROOF
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: d6ba104368c974094ff4ab2cd667bcb8ea92bf83
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 30 minutes HARD; STOP at deadline, report PARTIAL/BLOCKED, never stretch

ARCHITECT RULING
097 code is ACCEPTED as source/test proof only. Shared Development evidence in 097 says the running Host lacks the 096 PUT activation route and the one-level UI gate is OFF, despite 093 earlier proving a gate-ON shared stack. Restore an operator-safe, reproducible current-code Development stack and establish real browser READ-ONLY UI proof. Do not perform live activation writes. Legacy option row is activation authority; the new one-level row is its transactionally aligned projection. Three optionless methods remain unsupported for activation. No Checkout/Payment/Settlement cutover.

PREFLIGHT — STOP ON CONFLICT

Claim exactly one pending Bridge task; preserve receipt UNSTAGED. Read AGENTS.md, architecture/pipeline contract, 093/096/097 evidence, scripts/dev/host-stack.ps1, docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md and current git status. Fetch origin/main. If base drift overlaps src or this task's paths, STOP RECOVERY_CONFLICT; preserve all dirty/untracked files.
PROVE process owners, command-line/executable, startup time, effective config/gate and provenance for shared :5088 and :3000 BEFORE any action. Read /health and /__platform-commerce; expected SingleStore / dev-singlestore / store-alpha / tenant-alpha and database tooba_alpha. Never contact staging/production.
Check current Git HEAD contains 096 backend endpoint and 097 frontend control. Confirm pre-action route semantics using SAFE unauthenticated OPTIONS/GET as suitable; PUT route is NOT a GET and GET 404 alone cannot prove PUT absence. Use route-source or server metadata and authorized method-correct probe without mutating state, where possible; never send a real PUT.

MISSION A — CONTROLLED DEVELOPMENT HOST DEPLOYMENT

Use ONLY proven repo-owned scripts/dev/host-stack.ps1 for managed status/restart. Re-read implementation before invoking. It must confirm owning PID, executable path, provenance and rollback; if not owned or cannot prove rollback, STOP BLOCKED_HOST_AUTHORITY rather than killing/restarting blindly. Do not use Stop-Process -Force by default.
Build current Host to an isolated task-owned output directory (avoid locked bin), verify build succeeds and new 096 endpoint compiled/mounted. If deploy is authorized and ownership verified, restart shared 127.0.0.1:5088 with process-only Tooba__ShippingOneLevelCatalog__Enabled=true via the existing script. NEVER persist the gate in appsettings or source. Keep ShippingRateAuthority and ShippingOperationalShadow OFF. Preserve :3000 frontend process unless a verified, nondisruptive refresh is needed.
Validate /health, provenance, admin auth: anonymous=401, nonadmin=403, real logged-in Development admin authorized; catalog returns gate Enabled, ready true and exactly seven canonical flat methods. Both historically inactive methods remain inactive. If any guard fails, use documented gate-OFF rollback and STOP with evidence.

MISSION B — REAL SHARED BROWSER READ-ONLY PROOF

Using authenticated Development admin through shared http://localhost:3000/fa/admin/shipping-services, show five Persian RTL tabs, flat seven-method list, existing 094 name-editor action, and new 097 activation control for one option-backed selected method.
Verify client eligibility via server-owned 086 binding, no method-code splitting; active post:standard should offer explicit confirmation for deactivation; inactive post:express should offer explicit confirmation for activation. OPEN and CANCEL confirmation dialogs only. DO NOT click final confirm / issue PUT / change live activation in any DB. For optionless store_courier, in_person, snapp_courier verify explanatory unsupported text and absence of functional toggle. Confirm no fake success and server truth remains stable after cancel.
Record network/status for actual frontend BFF reads and activation route mounting/authorization without issuing a live write. If current routing cannot be confirmed safely without PUT, state that limitation; never manufacture proof.
Read-only DB parity before/after: 7 methods, 14 translations, legacy services/options and inactive option flags unchanged; historical Order/Fulfillment counts unchanged. No activation update, rate update, policy/schedule write, migration, seed, COD or Checkout changes.

MISSION C — BOUNDED EVIDENCE AND HANDOFF

Prefer minimal tests only if production files changed. No broad loops. If no source changes, report existing 097 204/204 frontend and 096 154/154 Fulfillment as historical evidence, not as newly executed tests.
Evidence: docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-098.md and docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-098/** with source revision/provenance, safe rollout command, read-only browser screenshots/observations, gate/auth/route proof, DB parity and reproducible gate-OFF rollback. Do not leak credentials/session IDs. Update master recovery only if explicitly permitted by its append-only convention; otherwise leave alone.
Do not alter business/backend/frontend source unless required to correct a PROVEN deployment blocker; any repair must be minimal, scoped and tested, else STOP for architect rather than scope-creep.

FORBIDDEN

Real live PUT to /activation, even temporary, and any DML on shared Dev store.
Automatically enabling post:express, snapp_courier or another method; creating options; pretending optionless activation is supported.
Customer Checkout/rate-authority cutover, capacity reservation, COD settlement, delivery promises.
Force stop, blind process takeover, unaudited auth bypass, hard-coded admin actor, new permission grant.
Resets, stashes, rebases, force pushes, unrelated dirty-file edits, undocumented gate persistence.

ACCEPTANCE
PASS: current-code shared Host, gate-ON Development, authenticated five-tab page, both option-backed confirmation controls and optionless refusals browser-proven READ-ONLY, unchanged DB; reversible gate-OFF procedure recorded. PARTIAL: some safe proof missing with precise reason. BLOCKED: safety/ownership/auth condition unresolved. Never claim live activation writes were tested.

RESULT CONTRACT — BRIDGE-WAKE-V1
Return Task-ID, Parent-Task, Channel, Worker-Id, Agent-Type, Program, Mode, Bridge-Task-Row, Status PASS/PARTIAL/BLOCKED, Stop-State STOP_FOR_ARCHITECT, Expected-Base-SHA, Base-SHA, Code-SHA, Evidence-SHA, automaticNextImplementationTask NONE, exact changed paths, measured time, actual commands/test counts, screenshots, before/after process/gate/DB truth, rollback and blockers. Commit/push task-owned evidence, not task receipt; STOP for architect.
END_TOOBA_TASK
