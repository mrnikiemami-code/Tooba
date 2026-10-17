PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-FULL-OPERATIONS-078
Parent-Task: TB-SHIPPING-FULL-OPERATIONS-077
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-FULL-OPERATIONS
Mode: BOUNDED_ADMIN_OPERATIONAL_QUOTE_INSPECTOR_AND_REAL_DEV_GATES
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: ecd9b91ab502dc574845e3c72257343011663d65
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 45 minutes HARD

ARCHITECT RULING
This is task 8 of the user's strict MAXIMUM TEN implementation tasks (071–080). 077 core packaging
admin is ACCEPT; its optional diagnostic inspector was NOT STARTED. The next task must build a real,
human-readable operational quote inspector on the existing shipping-services admin page using the 076
backend route. The goal is to let a merchant actually see how service/option, destination, measured
weight/count, carrier schedule, freight, packaging, and prepaid/collect-on-delivery explain the final
quote; do not make claims of Checkout cutover, customer delivery promises, or enforced capacity. A
narrow live Development compatibility matrix must accompany the UI. Protect the two future remaining
tasks 079/080 for actual safe integration gating/final readiness. No automatic followup.

PREFLIGHT
On BRIDGE-WAKE claim ONE pending tooba-main task; preserve docs/ai/tasks/TB-SHIPPING-FULL-OPERATIONS-078.task.md
verbatim UNSTAGED. Read AGENTS.md and docs/ai/TOOBA-PIPELINE-PROTOCOL.md, 075–077 planning/evidence,
current existing admin screen/grid actions, shipping destination picker, 073 tier model and shipping
packaging/schedule editors, backend ShippingOperationalQuoteEndpoints.cs, OperationalQuote Contracts
and validator. Fetch origin/main, prove Expected-Base-SHA match or ONLY path-by-path verified
docs/bookkeeping non-overlap. Source drift => RECOVERY_CONFLICT/STOP. Preserve unrelated
dirty/untracked paths, no stash/reset/rebase/force push. Verify Development provenance GET
/__platform-commerce before any writes and confirm existing frontend/Host rather than assume old PID.
Never write staging/production or create scratch DB.

MISSION A — WORKING ADMIN INSPECTOR, PRIMARY

Add one discoverable per-service action «بررسی هزینه و شرایط ارسال» next to existing
rate/schedule/packaging actions in /admin/shipping-services, opening one bounded RTL dialog. No new
route/page/mega-form. Service code must originate from the existing row, not manually provided.
Reuse actual Fulfillment service/option tree and Geography destination picker. Offer explicit inputs:
currency (default IRR, validated against response), order subtotal in IRR, measured shipment weight in
kg converted EXACTLY to integer grams via existing 074 model; physical shipment item count (not cart
lines), and OPTIONAL explicit carrier pickup datetime with timezone made understandable. State
timezone interpretation: input is local time in an explicit IANA zone, converted to UTC with correct
ambiguity/nonexistent-time handling or choose an unambiguous UTC input with clear label; NEVER
silently assume browser timezone or wrong DST. For no supplied datetime, clearly show NotEvaluated and
not eligible when active schedule exists. Do not label pickup window as customer delivery promise.
Call ONLY the existing authorized diagnostic POST
/v1/admin/shipping-services/{serviceCode}/operational-quote using its exact body DTO. Inspect backend
source for proper response envelope, enum encoding and nullable amount behavior; never invent wire
fields or calculate prices client-side. Show separate result lines: carrier freight, packaging, total
service expense, payable-at-checkout, collect-on-delivery; payer mode; tier/rate scope if returned;
schedule availability and matched weekday/window if returned; configured capacity clearly
«فقط تنظیم شده؛ رزرو/اجرای ظرفیت وجود ندارد». State today's Checkout remains under legacy quote
authority. Unquoted/ambiguous/currency mismatch => visible failure state, with nullable amounts NOT
rendered as 0. For NotEvaluated/Unavailable, do not show 'can book' or promise.
Distinguish 401/403, 400 validation child code, host unreachable, load/submit states, no fake success.
Disable Submit while request outstanding. Display clear Persian explanations for Prepaid and
CollectOnDelivery without implying real Payment/COD accounting exists. Keep pure formatting and
validation in small separate model, API in own file, existing design-system dialog, no source file >
applicable architecture threshold.
Reuse existing real Geography/option identities. Never create or edit rates/coverage/packaging/schedules
through the inspector; no paid-order writes and no new API route or backend change in Mission A.

MISSION B — ACTUAL DEVELOPMENT MATRIX + INTEGRITY, SECONDARY BUT REQUIRED
6. With positively verified dev-singlestore -> tenant-alpha/tooba_alpha, execute real admin browser
quote evaluation using EXISTING active approved-for-test fixtures without mutating them. Cover a
configured schedule available and outside-window if currently active (existing tipax/standard Sunday
13:30 versus 14:00), an absent schedule identity, with/without packaging (zero policy fallback), a
valid weight/count combination, a missing required measurement only where an active tier exists,
invalid destination, invalid option and 401. If proving a condition requires new synthetic
configuration, prefer a unit/API fixture and label clearly; no mutation for the sake of UI cosmetics.
Capture HTTP request/response envelopes and screenshots or a concise UI transcript. Quote is an
internal diagnostic, not customer price authority. Verify admin view output matches backend response
exactly; don't claim 1999/2000 live tier boundary unless real active tiers tested.
7. Targeted frontend model/API tests and tsc only. Existing frontend size guard has five pre-existing
unrelated failures; do not change those files. No backend build unless a demonstrable backend defect
blocks the real UI and a separately justified scoped fix is permitted by Architect; otherwise report
blocker and stop. No repetitive proof loops; 45-minute HARD cutoff, PARTIAL if necessary.

ACCEPTANCE / STOP
PASS only with a discoverable, real, authorized admin inspector, correct exact input unit conversion,
faithful nullable monetary parts and payer split, timezone/schedule meaning, no capacity or promise
overclaim, focused tests, a real Development browser/API proof and no data mutation. PARTIAL if any
primary or live-proof gate missing. Report exact elapsed and stop at 45 minutes even if incomplete. No
customer Checkout cutover. Worker PASS does not constitute Architect ACCEPT.

ALLOWED PATHS
src/frontend/app/admin/shipping-services-screen.tsx (minimal action/dialog composition only)
src/frontend/app/admin/shipping-services/shipping-operational-quote-api.ts (new)
src/frontend/app/admin/shipping-services/shipping-operational-quote-model.ts (new)
src/frontend/app/admin/shipping-services/shipping-operational-quote-dialog.tsx (new)
src/frontend/app/admin/shipping-services/shipping-operational-quote-api.test.ts (new)
src/frontend/app/admin/shipping-services/shipping-operational-quote-model.test.ts (new)
docs/planning/shipping/SHIPPING-FULL-OPERATIONS-078.md
docs/architecture/evidence/TB-SHIPPING-FULL-OPERATIONS-078/**
docs/ai/tasks/TB-SHIPPING-FULL-OPERATIONS-078.task.md (receipt only; never stage)

FORBIDDEN
No backend/Order/Payment/Checkout/Settlement/Host edits, no migration, no broad permissions, no
Results.Json, no price authority change, no CapacityEnforced claim, no client-authored pricing
calculation, no new page, no new fixtures unless essential and bounded, no Production/staging, no
parallel DB, no unrelated dirty edits, no broad tests/restarts, no automatic next implementation.

RESULT CONTRACT
Return ONE canonical Bridge result with actual fields and no text after END_TOOBA_TASK. Complete
detailed evidence body after automaticNextImplementationTask and before END_TOOBA_WORKER_RESULT.
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-FULL-OPERATIONS-078
Parent-Task: TB-SHIPPING-FULL-OPERATIONS-077
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-FULL-OPERATIONS
Mode: BOUNDED_ADMIN_OPERATIONAL_QUOTE_INSPECTOR_AND_REAL_DEV_GATES
Bridge-Task-Row: <actual Bridge row>
Status: PASS | PARTIAL | BLOCKED | RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: ecd9b91ab502dc574845e3c72257343011663d65
Base-SHA: <actual>
Code-SHA: <actual or NONE>
Evidence-SHA: <actual or NONE>
automaticNextImplementationTask: NONE
<actual UI result; wire DTOs; input conversion/timezone; monetary output; schedule/capacity honesty;
browser matrix; tests; Development provenance; no-write proof; elapsed; changes; git push; blockers>
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK
