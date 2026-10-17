TB-SHIPPING-ADMIN-METHOD-SCHEDULE-175

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-METHOD-SCHEDULE-175
Parent-Task: TB-SHIPPING-METHOD-COVERAGE-BROWSER-174
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: f0159086ebd42fd125b51ca5f3cfe5261102ba14
Program: TOOBA-SHIPPING-FLAT-METHOD-COMMERCIAL-READINESS
Mode: BOUNDED_METHOD_OWNED_TRANSPORT_SCHEDULE_REPAIR
Timebox: 45 minutes HARD
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

Mission

Make the existing «برنامه کاری حمل» tab for ONE canonical flat shipping method genuinely useful and method-isolated, with real persisted changes where the Shipping owner supports them. Inspect current code and owner contracts FIRST, then implement the smallest production slice. Do not invent carrier calendar/capacity/timezone, confirmed delivery slots, or a new provider-wide inheritance mechanism. This task is an implementation slice, not another wireframe/report-only task.

Mandatory architecture and source inspection

Read docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-16.md, docs/architecture/shipping/SHIPPING-ADMIN-FLAT-METHOD-CONTRACT.md including its LOCKED addendum, docs/architecture/shipping/SHIPPING-GROUP-INDEPENDENT-DELIVERY-COMMITMENT.md, and TB-173/174 evidence. Inspect actual General/Rates/Schedule/Packaging tabs, Fulfillment one-level canonical binding, Shipping operating-schedule models/endpoints, current storefront group horizons, and their existing tests. Distinguish (a) method-owned admin restrictions from (b) actual carrier capacity/time-slot promises and (c) static generic customer windows. Never infer method identity by splitting methodCode in browser.

Required bounded work
Trace current read/write of the Transport Schedule tab, the scope of every persisted operating-day/hour/cutoff rule, current legacy service-level policy, and the actual storefront consumer. Produce a concise ownership/provenance matrix in task evidence. If no method-specific write/read authority exists, introduce only the minimum additive Shipping-owned method identity/read/write contract needed for safe persistence; avoid destructive schema guesses. A method has no extra Admin day/window restriction by default; this does not assert real carrier availability.
Ensure method-specific policy is keyed by authoritative (serviceCode, optionCode) binding; reject unknown/mismatched binding; never mutate or inherit a provider/service-level legacy row or a sibling tipax:express while editing tipax:standard. Existing legacy rows must remain auditable and not masquerade as method-owned settings. Existing one-level list/three actions/four non-nested tabs, RTL/responsive UI and styling must remain intact.
Make the existing Schedule tab able to show the honest current state, edit applicable method-owned restrictions, save with a real authorized endpoint, refetch/reopen from server, and show invalid/denied/unverified/read-only states. No fake success, fabricated working days, carrier reservations, or implicit enabled/financial-ready claims. Use explicit unrestricted vs restricted vs restricted-empty semantics where relevant; if model does not support a requested dimension, visibly say it is unsupported rather than storing a fake value.
Critically verify the real downstream effect: trace whether saved schedule restrictions are enforced in the customer ShippingGroupHorizons, per-group selected dates/windows and SaveSelectionAsync. If this is not owner-correctly enforceable within scope, report a precise, concrete blocker and do not claim that editing Admin modifies customer promises. Do not silently broaden a default-OFF gate or make admin-only weekdays equal guaranteed carrier slots. Do not disturb TB-149–154 seller-specific date independence.
Add narrow tests for per-method exact identity, absent/unrestricted state, restricting only A not sibling B, invalid writes and View-mode read-only, save/refetch, and legacy behaviour. Prefer an isolated scratch HTTP write/read proof if feasible within 45 minutes; if no authenticated browser proof, report PARTIAL when that is a mandatory acceptance criterion. Use TB-157/166/174 existing scratch harness; do not repeatedly bootstrap blindly.
Hard safety / scope

Only one active task. No changes or restarts to shared :3000/:5088/:5432, tooba_alpha, store-alpha, RabbitMQ/Redis; only independently guarded disposable loopback scratch DB/Host/browser. Preserve unrelated dirty admin-fulfillment-work-queue-screen.tsx, TB-075/079 logs and other task artifacts. No reset, stash, force push, schema rebaseline, broad suite loops, fake customer data in production, real SMS/PSP, payout, carrier quotation or shipping share allocation. Use contracts-only module crossings, CQRS/ISender, existing Result conventions; no Host business logic or Results.Json. Keep existing CSS and Persian RTL. If owner semantics are genuinely blocked, stop and report source paths, missing contract and smallest next step rather than shipping a misleading UI.

PASS / PARTIAL / BLOCKED

PASS only if a genuine method-owned Admin schedule configuration is persisted and read back through authorized owner paths, sibling and legacy isolation is proven, frontend is correct and tests pass, AND customer impact is either demonstrated or explicitly identified as not currently authoritative without suggesting a successful delivery-promise change. Report remaining carrier calendar/capacity/timezone support as OPEN even with PASS. PARTIAL if code ships but required save/read or user-facing proof is absent; BLOCKED if safe ownership cannot be established. Keep full worker evidence with exact SHA, changes, tests, environment, timing and remaining blockers. Push fast-forward only, then STOP_FOR_ARCHITECT; do not automatically claim another task.
END_TOOBA_TASK