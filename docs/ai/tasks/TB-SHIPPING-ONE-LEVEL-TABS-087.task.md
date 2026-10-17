PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-087
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-086
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-TABS
Mode: BOUNDED_ONE_LEVEL_PACKAGING_AND_FREIGHT_TAB
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: db3849895f2f8611ba49970ba836b7e13ee12dff
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 30 minutes HARD

ARCHITECT RULING
086 is ACCEPTED for its bounded tab-2 integration despite the worker's appropriately cautious PARTIAL commercial/runtime verdict; not a claim of live operational readiness. This is task SEVEN of ELEVEN (081-091). Implement tab 3 «بسته‌بندی و کرایه» for the selected independently named one-level method. Reuse existing Shipping-owned packaging policies, freight payer mode and existing authorized APIs. Do not present a provider/option hierarchy, infer binding by splitting methodCode, enable any gate, or cut over checkout. Stop promptly at 30 minutes and return PARTIAL if genuinely incomplete; no retries against locked Host outputs.

PREFLIGHT — STOP ON CONFLICT
Claim exactly one Pending task on tooba-main using BRIDGE-WAKE-V1. Persist received task to docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-087.task.md as UNSTAGED receipt. Read AGENTS.md, docs/ai/TOOBA-PIPELINE-PROTOCOL.md, planning 081-086, exact tab-2 implementation, one-level binding resolver, existing shipping-packaging-dialog.tsx / shipping-packaging-model.ts and real API clients/contracts, one-level-tabs.tsx, and current recovery. Fetch origin/main. If HEAD differs from Expected-Base-SHA, accept only documented strictly nonoverlapping evidence/result bookkeeping drift; stop RECOVERY_CONFLICT on overlapping source drift. Preserve all existing dirty/untracked files; no reset/stash/rebase/force-push or unrelated commits. No Host/Next restart or DB mutation.

MISSION A — SINGLE-LEVEL BINDING AND OPERATIONS

Reuse the server-authoritative one-level method binding endpoint from 086, which validates canonical <=32-character methodCode and returns the immutable legacy serviceCode + nullable optionCode. Do not parse/split a method code on the browser, accept a client-provided arbitrary binding, or expose providerKind as merchant configuration.
Replace ONLY tab-3 placeholder with working read/create/update/activation/deactivation capabilities supported by EXISTING Shipping packaging-policy admin APIs, reusing the existing packaging model's semantics (destination scope, option-specific versus service fallback, flat packaging fee, currency, payer mode Prepaid/CollectOnDelivery, waiver/priority only insofar as already supported). Do not invent new capabilities or introduce a new Shipping endpoint unless a demonstrated minimal gap makes tab integration impossible; report a blocker before widening the backend scope.
Scope EVERY read/write to the selected one-level method and its verified binding. Policies for a service-level fallback must not appear as an independently selectable second-level method or silently be edited as a method-specific policy. Keep option-specific vs fallback semantics intact; preserve nullable optionCode where legitimately required. Reuse existing validation and policy-code/error localization; no financial ledger, charge collection or Checkout pricing modification.
On method switches, discard obsolete in-flight read results and prevent old-method writes from being submitted as if they belonged to the new selection. Consider already-in-flight writes carefully: do not claim cancellation of a committed network operation; keep any completion notices tied to the original identity. No duplicate writes on retry. Do not let a missing/unresolved/disabled gate binding fall back to a guessed pair.

MISSION B — MERCHANT UX, HONEST STATUS
5. Tab 3 must show selected method context, existing policies in an intelligible one-level list, create/edit/toggle operations where existing API supports them, Persian labels, RTL-friendly controls, explicit freight payer and amount/currency, validation, confirmation for destructive/deactivation actions if established UX requires it, save/cancel feedback, and no option dropdown or nested hierarchy. Preserve translations and form conversions from the proven legacy packaging editor rather than reimplementing business calculations.
6. Required states: no selected method; loading; empty; 401/403 unauthorized; unavailable/not-ready binding; unexpected API error; busy/save confirmation; success. Reject mismatched service/option read responses before presenting or mutating them. Preserve tabs 1/2 and placeholders 4/5; do not remove legacy packaging dialog or route/actions while the gate is OFF.
7. Gate Tooba:ShippingOneLevelCatalog is still OFF by absence, Dev one-level migration/backfill unapplied and running Host may predate new routes. Accordingly, distinguish source/test integration from actual live browser CRUD; do not claim tab 3 is deployed or the system commercially ready. The new shell remains default-OFF and legacy /admin/shipping-services must remain usable.

MISSION C — NARROW VERIFICATION & RECOVERY
8. Add focused tests for post:standard / tipax:express / optionless store_courier, inactive post:express, stale selection, unresolved/broken binding, policy-option filtering and nullable fallback, Prepaid vs CollectOnDelivery display/payload behavior, no provider hierarchy, and gate-OFF parity. Test no ledger settlement or checkout modification is implied. Prefer narrow frontend test files + typecheck. Only run backend tests if backend source genuinely changed. Do NOT rerun whole module/suite or FE-SIZE existing failing guard. If Host DLLs are locked, record once, do not loop or restart service.
9. Write docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-087.md and docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-087/ with exact path list, tests and real counts, any blocked checks, concrete single-level screenshots only IF safely obtainable (otherwise state source-only), gate and DB statuses, implementation boundaries, rollback and elapsed time. Commit/push ONLY task-owned paths; receipt remains untracked/unstaged. STOP_FOR_ARCHITECT; no next task automatically.

ALLOWED PATHS

src/frontend/app/admin/shipping-services/** (only tab-3, reuse helpers and focused regression tests)
src/frontend/app/admin/shipping-services/page.tsx (only if an absolutely necessary narrow bridge adjustment)
docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-087.md
docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-087/**
docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-087.task.md (receipt ONLY; never stage)
If a backend API defect makes implementation impossible, STOP/PARTIAL and document the exact missing contract rather than expanding the allowed paths autonomously.

FORBIDDEN
No new/mutated schema; no Development/staging/production DDL/DML, backfill, real commercial data seeding; no gate enablement, service activation, Host/Next restart, or new provider-option hierarchy; no Order/Checkout/Payment/Settlement changes, capacity reservation fiction, delivery promise or invented weight/item counts; no rearrangement of frozen Host/Shipping module authority; no unrelated edits or broad tests; no next Bridge task.

ACCEPTANCE / STOP
PASS only for a genuinely working tab-3 source implementation with verified authorized binding and focused green tests, while explicitly marking the live gate OFF/unmigrated database as NOT READY. PARTIAL if functional wiring or focused verification cannot be completed before 30-minute HARD stop. BLOCKED for missing required access/authority and RECOVERY_CONFLICT for source overlap. Record elapsed honestly. Do not chase broad tests beyond the timebox.

RESULT CONTRACT — BRIDGE-WAKE-V1
Return exactly one complete envelope with real fields and a concise evidence body; no text after END_TOOBA_TASK.
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-087
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-086
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-TABS
Mode: BOUNDED_ONE_LEVEL_PACKAGING_AND_FREIGHT_TAB
Bridge-Task-Row: <actual>
Status: PASS | PARTIAL | BLOCKED | RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: db3849895f2f8611ba49970ba836b7e13ee12dff
Base-SHA: <actual>
Code-SHA: <actual or NONE>
Evidence-SHA: <actual or NONE>
automaticNextImplementationTask: NONE
<changed paths; tests; method/binding safety; policy behavior; gate/DB truth; blockers; actual elapsed; rollback>
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK