PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-MVP-OPTION-PARITY-069
Parent-Task: TB-SHIPPING-MVP-SHADOW-PARITY-068
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-MVP-OPERATIONAL-CLOSURE
Mode: BOUNDED_OPTION_LEVEL_RATE_PARITY_AND_DESTINATION_REPAIR
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 59c12a70f66393305f551e6bdaae358430e5713c
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 45 minutes HARD

ARCHITECT AUTHORITY
This is task 7 of the user's strictly capped 8-task Shipping MVP closure (063–070). Complete useful production code, not exploration loops. Worker PARTIAL is not Architect ACCEPT. No automatic next task. Follow AGENTS.md, docs/ai/TOOBA-PIPELINE-PROTOCOL.md, .cursor/skills/tooba-backend-feature/SKILL.md. Preserve certified module boundaries: Shipping owns rate policy, Geography identity, Fulfillment service/options, Order legacy quote and customer snapshot. CQRS via MediatR ISender, Contracts-only cross-module seams, canonical ApiResponseFactory; no Results.Json and no Host business logic. User explicitly authorized use of existing disposable Development tooba_alpha; no scratch or parallel database, no Production/staging contact. Running processes may be restarted ONLY when identified and required, with a verified health/restore step.

PREFLIGHT

On BRIDGE-WAKE claim exactly one Pending task, persist received task receipt verbatim unstaged. Fetch origin/main, ensure known ancestry from Expected-Base-SHA; only individually-proven evidence-only fast-forward may be accepted. Source overlap => RECOVERY_CONFLICT and STOP. Protect unrelated dirty/untracked files; never reset/stash/rebase/force-push.
Read 068 evidence/report, 057 shadow design, actual Shipping rate policy Domain/Contracts/Application/EF migration, Shipping admin endpoints/DTO/picker, Fulfillment shipping service/option codes, Order shadow quote input/evaluator, Geography ResolveLegacyAsync implementation. Trace exact call graph and the existing DB schema BEFORE altering it. Do not reread unrelated recovery chapters or run broad suites.
Record 068 fixture ledger and current Development service options; prove explicit Tooba Development target (host env+connection reference+DB), not a remembered PID. Do not log secrets. A bounded backup/rollback for the affected Development Shipping tables/migration is required before schema change; no full DB clones.

MISSION — FIX CONCRETE 068 STRUCTURAL BLOCKER
A. Extend Shipping rate policy to distinguish a method option when a service has multiple options (e.g. tipax:standard vs tipax:express), while preserving service-only policies for single-option/no-option services. Use one canonical identity model: service code + optional option code, with deterministic resolution and explicit fallback semantics. Never equate methodCode with serviceCode; methodCode 'tipax:express' belongs to service 'tipax' and option 'express'. Validation must reject unknown/foreign option combinations via the EXISTING Fulfillment Contracts lookup seam or an ownership-correct minimal Contracts seam, not host/static hardcoded option names. Avoid a cross-module EF reference.
B. Implement a safe additive EF migration (not a rewrite): existing rows without option remain valid and behave exactly as before. New uniqueness/conflict rules must allow separate tipax options for the same Geography scope without arbitrary winner/order; precedence by Geography specificity and explicit option identity must be deterministic. An option-specific policy must never override or affect another option. Explicitly preserve coverage-grant semantics as service+destination if justified (coverage can be shared across options), with an explanation; do not silently expand coverage to all places. Keep inactivity fail-closed. Source-owned API DTOs/admin UI must be sufficient to author and edit two tipax rates; do not add a new page or unrelated redesign. Keep existing shipping validation and authorization behavior, including null/malformed-body -> declared 400 and 401/403 fail-closed. If safe option semantics cannot be implemented without major redesign in 45 minutes, STOP PARTIAL with exact code locations and smallest feasible repair; don't implement a half-migration.
C. Propagate distinct option identity into Shipping Contracts quote lookup and Order shadow observer/evaluator. Use real method/service mapping; existing callers of service-only lookup must remain source-compatible or be intentionally updated. Resolve the 068 mismatch with two explicit Development-only synthetic tipax policies (standard 180000 IRR/lead3, express 260000 IRR/lead1) at the already evidenced alborz area scope. The values are parity fixtures, NOT approved commercial tariffs. Avoid duplicate policies, and do not alter production/default commerce rates or Checkout. Prove existing post/store_courier/in_person rates unchanged. Disable or mark superseded the 068 generic tipax rate ONLY after confirming replacement and easy rollback; never leave multiple ambiguous active rules.
D. Diagnose the exact failing step of Geography ResolveLegacyAsync('IR','البرز',null,null) from 068 with one focused reproducible call/test. Fix only if root cause is certain and within Geography ownership and bounded time. Never make fuzzy text authoritative. When explicit validated ids are present they still win. If unresolved, document as a named blocker for 070 instead of spending time looping.
E. Focused proof on existing Development DB after one necessary controlled Host rebuild/relaunch (stop known Host BEFORE building to avoid DLL-lock retries). Use the existing authorized test cart/projection if safely reusable; otherwise one canonical disposable customer/cart only, never orders/payments. Temporarily enable Shadow only in the Development process, capture the stable observer signal and a per-method explicit evaluation where possible: post=120000, tipax:standard=180000, tipax:express=260000, store_courier=75000, in_person=0; all eligible only where authorized coverage exists. Capture explicit numerical and outcome proof, not only a summary set of outcomes. Keep and verify legacy HTTP projection byte-identical OFF vs ON. Always restore ShadowEnabled=false and PrerequisitesConfirmed=false, verify healthy Host, and record before/after source SHA. If runtime work would jeopardize the 45-minute stop, prioritize compile + focused model tests and report PARTIAL honestly.

MANDATORY SAFETY / QUALITY

Do not create parallel/scratch databases, idle polling, mass tests, exploratory build/restart loops, 100MB log files, or unrelated refactors. Log filtered excerpts only.
No change to legacy customer amounts, Checkout charge/eligibility, Order/Payment snapshots, or commercial rate authority switch. This task proves option parity; it does NOT authorize customer-facing cutover.
Existing synthetic fixtures are NOT user-approved tariffs. Do not provision broad-country coverage or fabricated geographic/service identity. Preserve existing user data unless explicitly identified as task-owned fixture; tooba_alpha is disposable Development but changes must still be scoped.
No invented backend fallback or bypass for invalid method option. Test 401/403 and contract validation with one focused regression, not a full suite.
Repo changes allowed ONLY in directly impacted Shipping Domain/Contracts/Application/Infrastructure/Endpoints/tests, Shipping admin rates dialog/API/model/tests, the Order shadow rate-authority integration and its focused tests, Geography resolver and focused test only if root cause demonstrated, one appropriate Shipping EF migration, and docs/planning/shipping/SHIPPING-MVP-OPTION-PARITY-069.md + docs/architecture/evidence/TB-SHIPPING-MVP-OPTION-PARITY-069/**. Any Host code/config change not explicitly necessary for a module-owned migration is forbidden. Bridge task receipt never stage.

ACCEPTANCE / STOP
PASS only if method-option-aware policy supports different legitimate rates for tipax:standard/express without compromising old rows, minimal migration applied safely to Development, admin authoring available, focused tests pass, runtime (or separately labeled PARTIAL if blocked) shows actual per-method parity, no cross-method leakage, and the Host is restored with Shadow OFF. A failure of a mandatory PASS condition => PARTIAL, not inflated PASS. Stop unconditionally at 45 minutes; do not start 070. No new automatic tasks.

RESULT CONTRACT — BRIDGE-WAKE-V1
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-MVP-OPTION-PARITY-069
Parent-Task: TB-SHIPPING-MVP-SHADOW-PARITY-068
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-MVP-OPERATIONAL-CLOSURE
Mode: BOUNDED_OPTION_LEVEL_RATE_PARITY_AND_DESTINATION_REPAIR
Bridge-Task-Row: <actual>
Status: PASS | PARTIAL | BLOCKED | RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 59c12a70f66393305f551e6bdaae358430e5713c
Base-SHA: <actual>
Code-SHA: <actual or NONE>
Evidence-SHA: <actual or NONE>
automaticNextImplementationTask: NONE
<root cause; policy identity and precedence; migration and backup; exact changed paths; real per-method parity matrix; valid/invalid option guards; destination resolver finding; focused tests; environment+restore evidence; elapsed; commit/push; remaining blockers>
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK
