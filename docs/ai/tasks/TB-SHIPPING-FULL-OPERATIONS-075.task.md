PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-FULL-OPERATIONS-075
Parent-Task: TB-SHIPPING-FULL-OPERATIONS-074
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-FULL-OPERATIONS
Mode: BOUNDED_PACKAGING_POLICY_AND_FREIGHT_PAYER_CONTRACT
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: d2c9680f8b7b1fba2b5ca656b6a1334061142bcd
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 45 minutes HARD

ARCHITECT RULING
Task 074 Mission A (weight/item-count tier editor) ACCEPT; its packaging Mission B was NOT STARTED and the worker correctly reported PARTIAL. This is implementation task 5 of a maximum 10 (071-080), not a fresh budget. Close the concrete packaging gap now; do not repeat the tier editor tests. The user wants separate packaging fee plus prepaid/collect-on-delivery freight. Distinguish a policy for WHO pays freight and WHEN from actually collecting money: Order and Payment/Settlement must retain their own financial authority. No cutover, no commercial data invention. Current tooba_alpha is disposable Development/test data. No scratch/parallel database, unnecessary Host restart, repetitive testing or source drift.

PREFLIGHT - STOP ON CONFLICT
On BRIDGE-WAKE claim exactly one Pending task on tooba-main. Preserve received docs/ai/tasks/TB-SHIPPING-FULL-OPERATIONS-075.task.md verbatim and UNSTAGED. Read AGENTS.md, docs/ai/TOOBA-PIPELINE-PROTOCOL.md, 069-074 Shipping planning/evidence, current Shipping rate/coverage aggregate, contracts, quote engine, module migration, Shipping admin endpoints, and the existing Order/Payment freight-money boundaries WITHOUT modifying those modules. Fetch origin/main; Expected-Base-SHA d2c9680f8b7b1fba2b5ca656b6a1334061142bcd must be ancestor of HEAD. Only individually proved nonoverlapping evidence/bookkeeping drift is acceptable; overlapping source drift => RECOVERY_CONFLICT/STOP. Preserve all unrelated dirty/untracked files; no reset/stash/rebase/force push. Identify Development host and tooba_alpha positively before writes. Never access staging or production.

MISSION A - SHIP WORKING SHIPPING-OWNED PACKAGING POLICY, PRIMARY ACCEPTANCE

Define packaging separately from freight chargeAmount: a Shipping-owned packaging fee policy keyed by real Fulfillment serviceCode, optional optionCode and valid Geography destination scope (reuse the existing read-only validation seams). Currency required (ISO 3-letter like IRR); fixed fee >=0; explicitly active/inactive; no matching policy means packaging fee zero, never a hidden extra charge.
Specify deterministic precedence consistent with existing Shipping policy: option-specific before service-level, then deepest destination scope, ambiguity fail-closed with a stable code; never first-row-wins. Explicitly decide and document the FREE-SHIPPING behavior: carrier freight waived by its existing threshold does NOT automatically waive packaging unless a separately configured rule says so. For this bounded slice, packaging is independent; do not invent configurable exceptions not needed by user.
Implement Contracts + Domain + Application CQRS ISender handlers + EF directory and additive Shipping-scoped migration + authorized admin GET/PUT/PATCH (or existing CRUD convention) under Shipping routes, no Results.Json. Validate duplicates, negative amounts, invalid currency, unknown service/option, wrong geography, authorization 401/403, and read-after-write. Preserve every existing rate/coverage/tier/schedule row. No Host-owned DbContext/business authority, no new global privileges, no cross-module EF/FK.
Provide a Shipping-owned read-only quote/component result contract usable by later orchestration: explicit carrier freight component, packaging component, currency and final combined amount, without overwriting existing IShippingRateQuoteLookup semantics or the customer amount. IMPORTANT: never claim an extra charge in today's Checkout. If a separate read seam cannot be safely built in time, report this precisely and still finish the persisted/admin slice. Avoid duplicating rate-engine logic in the application layer.

MISSION B - PAYER MODE SEMANTICS, SECONDARY ACCEPTANCE
5. Define explicit freight payer/collection terminology through Shipping-owned Contracts and domain rules: Prepaid means freight is payable at checkout by the customer; CollectOnDelivery means freight collection by the carrier/recipient at delivery; shipping fee ownership is a quote fact, not a Payment transaction. Consider packaging payer separately (default charged at checkout only if a later authorized Order pricing seam consumes it); do not assume packaging is collectible by carrier. The commercial policy may be service/option and scope scoped, but never persist any mode with no responsible amount party. Use a stable enum/code contract; avoid free-form Persian strings in database.
6. If within the same 45-minute bound, add persisted, admin-configurable collection mode attached to packaging/operational commercial policy with clear default Prepaid that maintains old legacy checkout behavior. If safe ownership/handoff is unresolved, provide Contracts-only expression and an explicit integration blocker, mark overall PARTIAL. Do NOT touch Order, Checkout, Payment, Settlement or real payments; those handoffs are for a later Architect-reviewed task.

MISSION C - FOCUSED LIVE DEV PROOF
7. Use existing Development tooba_alpha only; apply ONLY the new Shipping migration through the canonical design-time context if necessary. Prefer one existing real service and option plus a narrowly scoped actual Geography destination. Create a clearly marked synthetic packaging policy (e.g. 12,000 IRR) through authorized admin HTTP, read back, toggle inactive/active, and show zero/default behavior on a nonconfigured identity; prove that a free-freight quote does not silently remove packaging. Capture exact synthetic IDs and deactivate task-owned fixtures afterwards. A built-only slice is not a runtime PASS; if live write is blocked, mark PARTIAL.
8. Run only focused Shipping tests touching new code, a Shipping module build, and small HTTP probes; skip Host.Tests locking loops and broad guards. Avoid large logs; no credentials/tokens in evidence. No new frontend in this task: existing dialog UI extension comes after API semantics are accepted. No unchecked green claim.

ALLOWED PATHS

src/backend/Modules/Shipping/Tooba.Shipping.Contracts/**
src/backend/Modules/Shipping/Tooba.Shipping.Domain/**
src/backend/Modules/Shipping/Tooba.Shipping.Application/**
src/backend/Modules/Shipping/Tooba.Shipping.Infrastructure/**
src/backend/Modules/Shipping/Tooba.Shipping.Endpoints/**
src/backend/Modules/Shipping/Tooba.Shipping.Tests/**
docs/planning/shipping/SHIPPING-FULL-OPERATIONS-075.md
docs/architecture/evidence/TB-SHIPPING-FULL-OPERATIONS-075/**
docs/ai/tasks/TB-SHIPPING-FULL-OPERATIONS-075.task.md (received Bridge receipt ONLY, never stage)
Only if essential for route composition: src/backend/Host/Tooba.Host/Program.cs (one composition registration, no other edits).

FORBIDDEN
No customer pricing cutover; no Order/Checkout/Payment/Settlement writes or source edits; no generalized global price engine; no inventing approved commercial tariff; no broad country coverage grant; no existing row deletion; no unauthorized SQL bypass; no separate database; no unapproved Host/Admin/Security edits; no unrelated dirty-file edits; no invented capability; no Results.Json; no force push/rebase/stash/reset; no automated next task; no test/restart loops.

ACCEPTANCE / STOP
PASS only if packaging is distinctly modeled, persisted with safe default and deterministic precedence, has authorized working HTTP admin CRUD and focused real read-after-write/validation, additive migration, backwards compatibility and documented rollback; and collection mode contract semantics are unambiguous. Say explicitly whether payer mode is PERSISTED, CONTRACT-ONLY or NOT STARTED and whether packaging quote integration is operational or future-facing. PARTIAL if any primary gate is missing or secondary claimed implementation incomplete. Stop HARD at 45 minutes. Report actual elapsed, tests, exact changes, Code/Evidence SHA, baseline/provenance, fixture cleanup, and explicit next integration gaps. Worker PASS never means Architect ACCEPT.

RESULT CONTRACT - CANONICAL BRIDGE-WAKE-V1
Return one actual Worker Result string via Bridge with all fields filled. The body follows automaticNextImplementationTask and precedes END_TOOBA_WORKER_RESULT; nothing follows END_TOOBA_TASK. No unresolved placeholders in the transmitted result.
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-FULL-OPERATIONS-075
Parent-Task: TB-SHIPPING-FULL-OPERATIONS-074
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-FULL-OPERATIONS
Mode: BOUNDED_PACKAGING_POLICY_AND_FREIGHT_PAYER_CONTRACT
Bridge-Task-Row: <actual Bridge row>
Status: PASS | PARTIAL | BLOCKED | RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: d2c9680f8b7b1fba2b5ca656b6a1334061142bcd
Base-SHA: <actual SHA>
Code-SHA: <actual or NONE>
Evidence-SHA: <actual or NONE>
automaticNextImplementationTask: NONE
<packaging ownership and data contract; policy precedence; free freight behavior; payer-mode status; migration; validated admin HTTP evidence; Development target; fixture ledger; compatibility; focused tests; elapsed; exact changed paths; git push; remaining blockers>
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK
