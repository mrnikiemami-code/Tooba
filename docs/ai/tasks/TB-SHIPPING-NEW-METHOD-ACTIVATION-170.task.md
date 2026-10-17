PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-NEW-METHOD-ACTIVATION-170
Parent-Task: TB-SHIPPING-ADMIN-MULTILINGUAL-ACTIVATION-169
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-FLAT-METHOD-COMMERCIAL-READINESS
Mode: BOUNDED_OWNER_CORRECT_NEW_METHOD_OPERATIONAL_ACTIVATION
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 1671349312ee0ce75759bbbabda324b7fc91bb95
Hard-Timebox: 45 minutes; stop at boundary, report PARTIAL honestly.
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

MISSION
Close the concrete TB-169 gap: a newly created, canonical flat Shipping method can be named but cannot be activated (fulfillment.shipping_method.unsupported / LegacyOptionMissing) and cannot enter customer eligibility even with valid method-owned rate and coverage. Deliver an owner-correct, real activation pathway while refusing to fabricate provider capabilities, rates, coverage, or financial readiness. Preserve existing methods and accepted orders.

REQUIRED INITIAL INSPECTION
Read docs/architecture/shipping/SHIPPING-ADMIN-FLAT-METHOD-CONTRACT.md (including locked addendum), docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-16.md, TB-167/168/169 evidence and the real source of the one-level Fulfillment catalog, activation handler/ports, Shipping-owned policy/quote and Order storefront LoadEligibleMethodsAsync. Confirm exact current HEAD == origin/main == Expected-Base-SHA, otherwise STOP and report mismatch; never overwrite another worker.

IMPLEMENTATION SCOPE / ORDER

Map and document precisely the canonical owner and truth conditions for a newly created method: Fulfillment owns shipping_methods + translations/activation; Shipping owns rates, coverage and restrictions; Order consumes Contracts-only eligibility and owned quote. Differentiate legacy-bound existing entries from genuinely new entries that have no legacy service/option record.
Implement smallest bounded additive activation/eligibility integration at the proper module owner. A new method's activation MUST NOT require creating fake legacy shipping_services/shipping_service_options records merely to pass a guard. Do not arbitrarily bypass established tenant/edition/seller/capability/active checks. If legitimate carrier/provider registration is required, report that precise prerequisite and fail closed; do not invent it. Existing method activation must remain behaviorally unchanged.
Strict customer availability: enabling a newly created method is NOT monetary readiness. When ShippingOwnedRateAuthority is OFF, newly created methods without valid legacy quote authority MUST remain unquotable/non-selectable; NEVER appear with an invented fallback/free/zero price. When ON in private scratch, require exact owned-option Shipping quote, appropriate restriction/measurement validation, trustworthy method identity and real eligible seller/cart context before exposing the new method. Existing default-OFF customer behavior and seven original methods must remain unchanged. If safely integrating the NEW method into the existing catalog requires a larger contract redesign, STOP with exact blocker rather than broaden architecture.
Keep Admin flat table (three row actions), four non-nested tabs, multilingual RTL editor, server-derived immutable method binding; show activation denial/reason and readiness transparently. No parent-child UI resurrection.
Tests: focused owner activation/identity cases for new vs legacy entries, unauthorized/unknown/duplicate fail-closed, independent siblings, customer OFF absence vs private ON eligible case if implemented, missing owned price/coverage/measurements never silently quoted, no snapshot drift. Build relevant projects. Do not rerun broad suites repeatedly.
If feasible inside timebox, use reusable TB-166/TB-168/TB-169 isolated scratch harness to demonstrate real Admin create -> name -> legitimate activation -> independent valid owner rate -> authenticated customer projection; compare OFF behavior. Do not write, migrate, restart or purge shared :3000/:5088/:5432, tooba_alpha/store-alpha, shared RabbitMQ/Redis. Use task-owned resources and cleaned-up processes only. If browser/HTTP acceptance cannot be completed, report PARTIAL, not PASS.

NON-NEGOTIABLE SAFETY
One active task only. No Host business logic or Host DbContext, no Results.Json, MediatR/ISender and Contracts-only boundaries. No synthetic rate, legacy option, free shipping, carrier slot, delivery promise, shipping fee share or settlement payout. Do not switch any production feature flag ON. No unrelated dirty files, caches or stashes touched; no reset/rebase/autostash/force-push. No final PSP/SMS/checkout mutation in acceptance.

ACCEPTANCE / OUTPUT
PASS only if an actual newly created method can be legitimately activated with server-owned identity and, when independently quote-ready, becomes available to a real customer in the appropriate gated runtime; missing pricing must stay unavailable and legacy methods must retain parity. Include explicit evidence for HTTP/DB and a live private browser if feasible, all focused tests/build logs, source ownership map, SHA, cleanup proof. If authority prerequisites are missing or HTTP/browser cannot be proven: PARTIAL/BLOCKED with specific failing call and exact subsequent smallest step, not a ceremonial PASS. Commit scoped code/evidence and fast-forward push only; verify HEAD/origin parity. STOP_FOR_ARCHITECT, no automatic task.
END_TOOBA_TASK