PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-089
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-088
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-TABS
Mode: BOUNDED_ONE_LEVEL_OPERATIONAL_QUOTE_INSPECTOR_TAB
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 4dc0b1dacaeffbc6e5ac6e9113dfd2040f0919d5
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 30 minutes HARD

ARCHITECT RULING
088 ACCEPT for bounded source/test delivery: tab 4 schedule and configured-only capacity is wired, NOT live verified and NOT commercial-ready. This is task 9 of at most 11 (081-091). Complete fifth and last tab «بررسی هزینه» of the one-level Shipping admin shell, using the EXISTING read-only Shipping operational-quote endpoint and methodCode-to-legacy-binding resolver. This is a diagnostic, NEVER a customer charge, payment collection, COD settlement, delivery promise or Checkout cutover. No automatic next task.

PREFLIGHT — STOP ON CONFLICT
Claim exactly one Pending task from tooba-main under BRIDGE-WAKE-V1. Persist received task under docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-089.task.md as UNSTAGED receipt. Read AGENTS.md, docs/ai/TOOBA-PIPELINE-PROTOCOL.md, planning documents 081-088, the 078 operational inspector implementation/tests, 076 operational quote contract, 086 server-owned binding resolver, 084 shell, tabs 2-4 and their UI/API/test patterns. git fetch origin; require Expected-Base-SHA or verified ancestry with only unrelated bookkeeping drift; overlapping source drift => RECOVERY_CONFLICT/STOP. Preserve all dirty/untracked work; no reset/stash/rebase/force-push. No Host/Next restart, no staging or production access. Do not attempt locked Host test builds or repeated broad test suites.

MISSION A — REAL FIFTH TAB, NO SECOND LEVEL

Replace only tab 5 placeholder in shipping-one-level-tabs.tsx with working «بررسی هزینه» diagnostic for the SELECTED independent method. Show method code and localized label, and resolve the immutable legacy serviceCode/optionCode pair EXCLUSIVELY through GET /v1/admin/shipping-services/shipping-methods/one-level/{methodCode}/binding. Never infer it by splitting methodCode or accepting editable serviceCode/optionCode input. No provider level, option selector, grouping-by-option or hierarchy.
Reuse existing read-only POST /v1/admin/shipping-services/{serviceCode}/operational-quote and 078 admin inspector API/model, numeric conversion, error mapping, destination picker and quotation calculations. Confirm actual endpoint request/response shape from source before wiring; only use supported fields. OptionCode must be taken exclusively from resolved binding, including null for optionless methods. If the endpoint cannot safely scope a request to one method, stop that portion, report precise contract deficiency and do not invent an endpoint or silently broaden paths.
Inputs must use the existing semantics for geographic identity, explicit IANA timezone, local date/time, subtotal/weight grams/physical item count when supported, currency and preparation/lead terms. Never guess an actual packed weight/count, promise or timezone; distinguish manually supplied DIAGNOSTIC test inputs from proven order measurements. Respect grams vs kg, nonnegative amounts, boundaries and null money components. Preserve existing 078 refusal for ambiguous/nonexistent local times.
Display a truthful componentized quote: freight amount, packaging amount, combined total, checkout-due, COD-due, availability/schedule verdict and any specific failure codes actually returned by the existing API. Never label diagnostic checkoutDue as a Checkout charge or COD-due as collected/settled. Always show «فقط بررسی هزینه؛ مبلغ سفارش یا پرداخت تغییر نمی‌کند». Capacity is NotEnforced; if shown, use «ظرفیت تنظیم‌شده (هنوز اعمال نمی‌شود)». Pickup/dispatch windows are NOT promised delivery times. Do not manufacture a price, zero default or success when quote has no usable result.
UI states: no method, binding loading, gate OFF, not migrated/not ready, unresolved/malformed binding, 401/403 denied, invalid input, pending evaluation, valid result, business refusal/failure code, unexpected API failure. Never show previous method's quote after selection changes (generation/identity guard); avoid duplicate submits and do not claim cancellation of a committed server operation. Persian RTL and existing keyboard-accessible five-tab shell must remain intact. Existing legacy inspector must remain working while gate is OFF. Tabs 1-4 must not regress.

MISSION B — MINIMAL PROOF
6. Add focused tests for server-owned identity binding (post:standard, tipax:express, optionless store_courier, inactive/unknown), fail-closed/403, no methodCode splitting or option picker, stale async reply, strict target isolation, quote component math DISPLAY as returned (not recomputed into checkout pricing), null values, timezone invalidity and failure states. Verify the five tab labels and tabs 1-4 remain intact. Reuse proven 078 models rather than reimplementing business logic.
7. Run narrowly scoped shipping-services frontend tests and frontend typecheck. No backend test suite if backend source remains unchanged; no locked Host test retry. Report every executed test count and any failure truthfully. New shell remains OFF while OneLevelCatalog migration/backfill absent; DO NOT claim a live tab-5 CRUD or business acceptance. This tab should require NO write to database: evaluation is read-only semantics notwithstanding the existing POST verb.

ALLOWED PATHS

src/frontend/app/admin/shipping-services/shipping-one-level-quote-*.ts
src/frontend/app/admin/shipping-services/shipping-one-level-quote-*.tsx
src/frontend/app/admin/shipping-services/shipping-one-level-tabs.tsx
src/frontend/app/admin/shipping-services/shipping-one-level-tabs-model.ts
src/frontend/app/admin/shipping-services/shipping-one-level-tabs.test.ts
src/frontend/app/admin/shipping-services/operational-quote or quote-inspector ONLY if essential for safe existing-code reuse, with focused regression evidence
docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-089.md
docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-089/**
docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-089.task.md (receipt ONLY, do not stage)
Changes outside these paths require STOP and Architect review, not self-expanded scope.

FORBIDDEN
No Fulfillment, Shipping, Geography, Order, Checkout, Payment, Settlement, Host or backend source change; no migration, seed, backfill, Development DDL/DML, gate activation, Host/Next restart or new permission; no price authority cutover, paid snapshot mutation, invented packed measurement, COD ledger, guaranteed delivery or capacity reservation. No manual live login or credential publication. Tooba:ShippingOneLevelCatalog, Tooba:ShippingRateAuthority and Tooba:ShippingOperationalShadow remain OFF. No second-level UX. Preserve unrelated source and user data.

ACCEPTANCE / STOP
PASS only for functional, source-and-focused-test-proven fifth-tab inspector with server-resolved method identity, honest read-only quote semantics, explicit no-Checkout/no-settlement/no-reservation claims and no regression to existing four tabs. The OFF gate and unmigrated Development are known rollout blockers: state NOT LIVE VERIFIED and NOT COMMERCIALLY READY. PARTIAL if a real endpoint contract prevents safe per-method evaluation or proof is incomplete. BLOCKED on unsafe access. STOP after at most 30 minutes; report actual elapsed, files, tests, Code/Evidence SHAs, known rollout blockers and rollback. Automatic next task NONE. Architect must separately decide 090 scope for controlled Development migration/backfill and actual UI verification rather than silently progressing.

RESULT CONTRACT — BRIDGE-WAKE-V1
Return exactly one complete worker result using the field order below:
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-089
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-088
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-TABS
Mode: BOUNDED_ONE_LEVEL_OPERATIONAL_QUOTE_INSPECTOR_TAB
Bridge-Task-Row: <actual>
Status: PASS | PARTIAL | BLOCKED | RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 4dc0b1dacaeffbc6e5ac6e9113dfd2040f0919d5
Base-SHA: <actual>
Code-SHA: <actual or NONE>
Evidence-SHA: <actual or NONE>
automaticNextImplementationTask: NONE
<scope and proof, tests, gate and DB truth, exact elapsed, changed paths, blockers, rollback and live-vs-source verdict>
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK