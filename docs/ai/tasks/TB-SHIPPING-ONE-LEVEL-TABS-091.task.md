PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-091
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-090
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-TABS
Mode: BOUNDED_FINAL_DEVELOPMENT_OPERATIONAL_PROOF_AND_HANDOFF
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: f0a7bc36576d27d3d722b151174225aa4671cb13
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 30 minutes HARD

ARCHITECT RULING
This is task ELEVEN of ELEVEN (081–091). 090 is accepted for isolated-source/code and authorized Development DB migration/backfill only; it is PARTIAL for live UI. The one-level catalog now contains 7 method rows and 14 translations in Development according to 090 evidence, but Tooba:ShippingOneLevelCatalog remains OFF; the running Dev Host is stale and serves 404 for the new endpoints. The goal is an honest, safe operational verification and final handoff, NOT an assumed production go-live. Do NOT silently restart/kill/reconfigure any user-managed Host/Next process. NEVER turn on ShippingRateAuthority or ShippingOperationalShadow; never cut over Checkout. Stop and request explicit architect/user authorization if safe Host relaunch authority/provenance is missing. No automatic task 092.

PREFLIGHT — STOP ON CONFLICT
Claim exactly one Pending task through BRIDGE-WAKE-V1 on tooba-main. Persist task artifact as docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-091.task.md, un-staged receipt. Read AGENTS.md, docs/ai/TOOBA-PIPELINE-PROTOCOL.md, shipping planning 081–090, 090 backup manifest/rollback and evidence, the full one-level Fulfillment endpoint/gate and five-tab frontend. Fetch origin/main and verify expected SHA or demonstrate ONLY nonoverlapping result-bookkeeping drift; overlapping code changes mean RECOVERY_CONFLICT and STOP. Preserve every pre-existing dirty/untracked file, no reset/stash/rebase/force push. Do not spend time retrying known locked in-tree Host builds.

MISSION A — ESTABLISH SOURCE AND DEVELOPMENT SAFETY

Verify exact live target BEFORE writes/probes: GET /__platform-commerce identifies SingleStore/dev-singlestore/store-alpha/tenant-alpha -> tooba_alpha; verify active listener/process, binary version, current service command/environment provenance and backup availability. Never assume PID 33548 still exists.
Read-only verify 090 one-level migration applied once, 7 methods/14 translations, stable method codes and activation states, no bare post/tipax sellable method, old catalog and historical order/fulfillment records unchanged. Confirm 090 backup remains physically present and restorable. Do not print secrets. Confirm gates OFF and Checkout unchanged.
Inspect safety of source: route GET /v1/admin/shipping-services/shipping-methods/one-level and binding; 5 tabs linked to real API, no methodCode split, no second-level merchant option picker, admin authorization, no leaked credentials, inactive methods visible. Reconcile UI screenshots/proof with actual running binaries, not source alone.

MISSION B — HOST DEPLOYMENT DECISION GATE
4. Check for a pre-existing documented, reproducible, authorized Development Host rebuild/relaunch procedure and explicit user/architect permission in task/protocol context. This task itself does NOT grant permission to terminate or replace the shared Dev Host process. If permission is absent or launch provenance cannot be reproduced: DO NOT stop/restart/signal/reconfigure it. Record BLOCKED_LIVE_HOST_RELAUNCH, exact prerequisite and safe operator instructions, and continue feasible offline/isolated proofs within timebox.
5. If AND ONLY IF separately authorized and verified: save process/config provenance without secrets, ensure backup/rollback, stage artifacts using isolated output paths to avoid locked DLLs, perform controlled Development-only deployment and health/route verification; revert safely on regression. No staging/production changes. If authority uncertain, STOP before mutation.
6. Consider a separate isolated current-code Host on an unused port/isolated safe configuration for route auth/serialization proof if it can be done without mutating shared databases, affecting running processes, bypassing auth or overrunning the clock. Never falsely label scratch evidence as proof of the real 5088 process.

MISSION C — LIVE PROOF ONLY IF AUTHORIZED/FEASIBLE
7. Read-only endpoints: verify authorized admin gate OFF yields Disabled, ready=false and no methods; 401/403 remain fail-closed. A query-string bypass must not mount new UI. Do not grant fake permissions.
8. Enable Tooba:ShippingOneLevelCatalog only with explicitly approved Development-only configuration, trustworthy process origin and rollback. If no such authorization, leave OFF. Under allowed gate-on path verify 7 independently displayed methods, post:express/snapp_courier inactive, provider_kind hidden, each of five tabs reachable via method context and URL/keyboard. Prove tab 2 rates/coverage, tab 3 packaging/COD quote fact, tab 4 schedule/timezone/configured-only capacity and tab 5 read-only quote as scoped to correct method. Show actual browser/network evidence; never claim a live PASS from unit tests.
9. Any task-owned temporary rate/coverage/packaging/schedule write needs explicitly isolated records, tenant provenance and reversible cleanup; avoid touching approved/live merchant policies or old fixtures. If unsafe, use read-only assertions and mark CRUD verification incomplete. No Order/Checkout/Payment/Settlement writes.

MISSION D — FINAL VERDICT, EVIDENCE, RECOVERY
10. Run only essential focused frontend tests + typecheck for modified frontend; focused backend/Host guard only via safe isolated OutDir or when locks are absent. No broad loops, no Host test lock retry. Clearly separate green tests, source proof, isolated probe, live Development proof and commercial business approval.
11. Write docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-091.md, evidence under docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-091/, and an append-only short checkpoint in docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md ONLY if safe/no overlap. Final matrix: 5-tab UI source-complete; live gate/Host/backfill state; historical identity safety; open packed-weight provenance, atomic capacity claim/release, COD ledger/settlement, true delivery promise, merchant-approved tariffs and explicit Checkout cutover. Report precise owner, evidence and blockers. This 11-task UI program may close technically despite remaining commercial dependencies; never call it production-ready without proof. State rollback and SHAs; actual elapsed.

ALLOWED PATHS

docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-091.md
docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-091/**
docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md (small append only, zero unrelated edits)
docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-091.task.md (receipt ONLY, un-staged)
src/frontend/app/admin/shipping-services/** ONLY for a reproducible SEVERE final integration defect with minimal fix and focused proof
src/backend/Modules/Fulfillment/** ONLY for a reproducible SEVERE gate/reader/endpoint issue and explicitly proven minimal fix, with precise file list in report
NO other source edits without architect STOP/re-authorization.

FORBIDDEN
No uncontrolled service restart or kill; no live database rollback/restoration by default; no paid snapshots/historical code rewrite; no Checkout cutover; no ShippingRateAuthority or ShippingOperationalShadow switch; no artificial packed grams/item counts; no capacity reservation/slot guarantee; no COD collection/settlement claim; no production/staging contact; no arbitrary permission grant; no invented operational success; no unrelated dirty file edits; no automatic next task.

ACCEPTANCE / STOP
PASS only if the bounded evidence and final handoff are complete and claims are truthful; distinguish source-complete from live-proven and commercial-ready. PARTIAL if live deployment/browsing remains blocked but safe documentation/proof is complete. BLOCKED if necessary access/provenance cannot be established. RECOVERY_CONFLICT on source drift. Hard stop at 30 minutes or earlier. Report actual elapsed. STOP_FOR_ARCHITECT. Automatic next NONE.

RESULT CONTRACT — BRIDGE-WAKE-V1
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-091
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-090
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-TABS
Mode: BOUNDED_FINAL_DEVELOPMENT_OPERATIONAL_PROOF_AND_HANDOFF
Bridge-Task-Row: <actual>
Status: PASS | PARTIAL | BLOCKED | RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: f0a7bc36576d27d3d722b151174225aa4671cb13
Base-SHA: <actual>
Code-SHA: <actual or NONE>
Evidence-SHA: <actual or NONE>
automaticNextImplementationTask: NONE
<concise actual provenance, host/deployment authority, checked 7 methods/14 translations, real live or isolated UI/route proof, all five tabs, tests, outstanding safety/financial/commercial blockers, changed paths, actual elapsed, rollback, final go-live verdict>
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK
