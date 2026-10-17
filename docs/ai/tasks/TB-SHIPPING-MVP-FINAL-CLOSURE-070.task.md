PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-MVP-FINAL-CLOSURE-070
Parent-Task: TB-SHIPPING-MVP-OPTION-PARITY-069
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-MVP-OPERATIONAL-CLOSURE
Mode: BOUNDED_FINAL_RESOLVER_ADMIN_OPTION_AND_DELIVERY_GATE
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 8351f66e550f89dc8f64d3efdc62ba1c138155d4
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 45 minutes HARD; do not exceed; stop with honest PARTIAL and precise evidence if unfinished.

ARCHITECT DECISION / PRODUCT BOUNDARY
This is task 8 of the agreed 8-task closure budget (063–070). Prioritize working product over repeated test rigs. Developer database tooba_alpha is TEST DATA; authorized scoped writes and clean-up of task-owned test fixtures are allowed. Do NOT contact production/staging, rewrite historical paid snapshots, assume simulated parity prices are approved tariffs, or silently switch customer-facing pricing authority. No extra task, no self-continuation. Product approval of real tariffs/coverage cannot be fabricated. If that alone prevents commercial launch, report NOT_READY_FOR_COMMERCIAL_CUTOVER honestly even if all code is complete. Keep current legacy checkout functional and unchanged unless verified and explicitly authorized cutover requirements are met. This task is not commercial approval.

PREFLIGHT (fast, bounded; stop on source drift)

Claim exactly one pending Bridge task after BRIDGE-WAKE; preserve received .task.md verbatim and never stage it. Follow AGENTS.md, docs/ai/TOOBA-PIPELINE-PROTOCOL.md, .cursor/skills/tooba-backend-feature/SKILL.md and Shipping checkpoint. Fetch origin/main. Expected base 8351f66e... includes 069 result bookkeeping plus the pipeline protocol documentation-only commit; verify ancestry and any subsequent commits path-by-path; any overlap -> RECOVERY_CONFLICT. Preserve unrelated dirty/untracked files; no reset/stash/rebase/force-push.
Read docs/planning/shipping/SHIPPING-MVP-OPTION-PARITY-069.md, 068 evidence for legacy text non-resolution, 065 admin UI evidence, 067 real Geography identity path, Shipping OptionCode contracts and 069 UI DTOs. Identify current Development Host and tooba_alpha independently; never assume PID. Do not restart unless edited backend must be loaded and verified. Stop Host cleanly BEFORE rebuilding to avoid MSB3027; avoid repeated restart, uncontrolled stdout, or Host.Tests DLL-lock loop.

MISSION A — CLOSE THE LEGACY TEXT DESTINATION GAP (backend; precise, minimal)
Trace IGeographyLookup.ResolveLegacyAsync("IR", "البرز", null, null) end-to-end through Geography Contracts/Application/Infrastructure implementation with focused evidence (country lookup, area label/code, active state, tenant/culture query semantics). Prove EXACT failure step. Fix only an actual demonstrated resolver bug in Geography-owned code, preserving deterministic exact-match and ambiguity refusal, no fuzzy fallback, no guessing IDs, and no broadened authorization. Add the smallest focused tests: canonical Persian label and stable province code resolve to the same real area where unambiguous; ambiguous, unknown, cross-parent or inactive inputs remain non-Resolved. If the resolver cannot be fixed safely within timebox, classify BLOCKER and continue only independent Mission B before timebox, never speculate.

MISSION B — COMPLETE THE REAL ADMIN RATE EDITOR (frontend)
Existing /admin/shipping-services rates-and-coverage dialog must allow selecting the existing Fulfillment option code (for multi-option services, e.g. tipax standard/express) and creating/editing distinct rate policies through the EXISTING Shipping Admin API (069 optionCode DTO). Do not invent option values or parse methodCode strings into service codes. Read options from established Fulfillment service tree / existing admin response as appropriate, using the smallest existing endpoint; no new Backend route unless absolutely unavoidable and source-proven. For single-option or no-option services, preserve current UX/semantics and existing service-level null option fallback. When editing an option-specific policy, show its immutable option identity (no silent reassignment). Clearly label service-level fallback. Render errors alongside option field, including shipping.service_option.unknown. Keep coverage-grant editor service-scoped and unchanged. Preserve D1/D2 and 066 stale-response guard. No new page and no unrelated frontend rewrite.

MISSION C — REAL, BRIEF END-TO-END VERIFICATION ON EXISTING DEVELOPMENT
Reuse tooba_alpha and existing approved Development Host (:5088), frontend (:3000), existing admin actor. No scratch DB, clone, mass migration, fixture farm or test rerun loop. If backend changed: one controlled stop-build-start, verify Development target by /health, /v1/admin/dev-context and /__platform-commerce; do not retain shadow-enable overrides. Reuse the existing 068/069 area-scoped synthetic fixtures and real auth cart if still available. Verify a public Geography read, canonical admin authorized path, and UI create/read/edit/active toggle for tipax standard+express using real service and option IDs/codes; avoid duplicate scope by reusing/updating existing fixture where possible. Show HTTP evidence, UI evidence, exact IDs and no cross-option rate leakage. Run ONE authenticated projection request with explicit validated destination IDs using a real cart, briefly turn on Shadow only in Development when necessary to capture five per-method comparisons, and restore OFF before finishing. Expected 5 Match with the known synthetic Alborz fixtures, no false production/commercial claim. If legacy Persian text issue fixed, run one focused province-only legacy request and show observer result is Resolved/Observed, but acknowledge parity at area scope only where data truly covers. Price and method list returned to customer must remain unchanged OFF/ON. Do not activate customer authority/cutover unless commercial authorization and complete data separately exist; no automatic switch.

MISSION D — DELIVERY GATE, SAFETY, CLEANUP
After implementation: focused Geography resolver tests, focused Shipping/Order shadow tests only if related code changed, and the three Shipping admin frontend tests + TypeScript typecheck; avoid broad Host.Tests/architecture suites when DLL-locked. Reconcile only architecture guards directly touched, do not weaken any. Record known unrelated baseline failures distinctly. Ensure live Host healthy, production/staging untouched, flags ShadowEnabled and PrerequisitesConfirmed OFF, user frontend running and not unnecessarily restarted, zero modified real orders/payments/snapshots. Record unapproved synthetic fixtures and whether active; do NOT mistake their parity for approved commercial rates. Cleanup temp credentials/logs and task-owned disposable artefacts only; protect unrelated dirty files. Update Shipping master recovery checkpoint if and only if safe and bounded; otherwise record full accurate closure in this task plan and evidence. Make an honest final DELIVERY VERDICT with independent categories: (1) Admin authoring/UI ready or not; (2) Geography identity ready or not; (3) Shadow parity on representative tested data or not; (4) Commercial tariffs & destination coverage approved/complete or not; (5) Customer authority cutover authorized/ready or not. Declare FULL COMMERCIAL READY only if every mandatory condition is independently proven; otherwise state exactly which external approval/operational gate is absent and deliver working legacy-safe MVP. Do not invent eighth-plus implementation task.

ALLOWED PATHS

Geography module exact resolver implementation and narrow focused test files necessary for Mission A.
Existing Shipping admin UI under src/frontend/app/admin/shipping-services/ and directly relevant focused tests, plus the smallest existing frontend API helper only if truly needed.
docs/planning/shipping/SHIPPING-MVP-FINAL-CLOSURE-070.md
docs/architecture/evidence/TB-SHIPPING-MVP-FINAL-CLOSURE-070/**
docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md (Shipping checkpoint only, if bounded)
docs/ai/tasks/TB-SHIPPING-MVP-FINAL-CLOSURE-070.task.md (received Bridge receipt ONLY, unstaged)
No Shipping/Order policy engine redesign, additional migration, Host/security bypass, payment/checkout mutations, pricing config changes, new admin page, permission catalog edit, or production/staging connection. Genuine blocker outside allowed files -> evidence and stop/partial, not unilateral expansion.

ACCEPTANCE
PASS for task only if demonstrated resolver closure, option-aware admin UI operational roundtrip on existing Development, five per-method Shadow Match outcomes on representative Alborz test fixtures with legacy projection invariant, gates restored OFF, host health, targeted test passes, evidence/commit/push. Distinguish task PASS from FULL COMMERCIAL READY. If any requested result absent, PARTIAL with precise blocker, elapsed, code/evidence SHA, and no automatic continuation. Timebox stops even with incomplete work.

FORBIDDEN
No production/staging DB or deployment; no automatic Checkout pricing cutover; no commercial tariff approval by inference; no broad tests, repeated Host rebuilds, scratch DB, polling loop, or unbounded troubleshooting; no unauthorized Host/security edits, permission weakening, reset/stash/rebase/force-push; no fictional PASS.

RESULT CONTRACT — BRIDGE-WAKE-V1
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-MVP-FINAL-CLOSURE-070
Parent-Task: TB-SHIPPING-MVP-OPTION-PARITY-069
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-MVP-OPERATIONAL-CLOSURE
Mode: BOUNDED_FINAL_RESOLVER_ADMIN_OPTION_AND_DELIVERY_GATE
Bridge-Task-Row: <actual row>
Status: PASS | PARTIAL | BLOCKED | RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 8351f66e550f89dc8f64d3efdc62ba1c138155d4
Base-SHA: <actual>
Code-SHA: <actual or NONE>
Evidence-SHA: <actual or NONE>
automaticNextImplementationTask: NONE
<root-cause exact trace; files changed; admin UI+API evidence; legacy text resolution; real-cart shadow 5-line parity; gates/host restored; focused tests; commercials/cutover readiness verdict; process/data safety; timebox actual; commit/push and remaining blockers>
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK