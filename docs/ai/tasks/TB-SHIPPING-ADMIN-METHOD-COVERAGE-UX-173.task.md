PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-METHOD-COVERAGE-UX-173
Parent-Task: TB-SHIPPING-NEW-METHOD-BROWSER-PAYMENT-172
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 1f5edc29db4a5ba0a7ac523e6b920981186a7103
Program: TOOBA-SHIPPING-FLAT-METHOD-COMMERCIAL-READINESS
Mode: BOUNDED_REAL_METHOD_SCOPED_OPTIONAL_COVERAGE_ADMIN_UI
Hard-Timebox: 45 minutes; STOP at boundary, report PARTIAL rather than overrun

ARCHITECTURAL AUTHORITY
Read docs/architecture/shipping/SHIPPING-ADMIN-FLAT-METHOD-CONTRACT.md (LOCKED ADDENDUM), docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-16.md, TB-161/162 coverage implementation, TB-172 evidence. ONE selected canonical method owns its optional restriction; default = Unrestricted; Restricted with zero active own grants = deny all; never inherit sibling/provider grants in strict mode. Finance readiness and method activation are separate. User wants real compact RTL Admin UI, NOT wireframe or a new nested configuration surface.

MISSION — ONE SHIPPABLE SLICE
Make the existing method-specific تعرفه و پوشش tab genuinely able to READ, EDIT, SAVE and REOPEN the authoritative per-method geographic coverage mode introduced in TB-161: Unrestricted versus Restricted, and its method-owned allowed destinations/grants using existing Shipping owner endpoints and existing geography selector. A new method like tipax:scratch172 and tipax:standard must be independently editable. Do not invent a coverage grant, city, availability, price or carrier capability. If the existing grant write path cannot safely scope a grant to the canonical method, implement the SMALLEST owner-correct Shipping change; do not send a provider-level grant as an approximation. Use the server-authoritative one-level binding; never split a method code in browser to choose provider/option.

CONCRETE ACCEPTANCE

Before editing: one method without an explicit declaration displays بدون محدودیت مدیریتی (Unrestricted by default), without promising carrier coverage or a guaranteed slot. Clear distinction from financial NeedsConfiguration and disabled activation.
In Edit, choose محدود به مناطق منتخب, add an existing authoritative destination by the existing lookup, save via the Shipping-owned method-coverage declaration and option-scoped coverage-grant writer; reload shows the same exact choice and destination, all from server. Show actionable errors/denied state; no optimistic success.
On strict customer quote with the method-owned fixed rate, an allowed destination quotes the method's OWN amount; a disallowed destination fails closed. Sibling tipax:express remains unchanged in Admin and strict quote, including its own Unrestricted default. Restricted with no active grant denies everywhere, never silently turns back into Unrestricted.
Switch selected method back to Unrestricted and save; its restriction ceases to narrow it, with owner rate still REQUIRED to quote. Do NOT delete historical grants just to implement mode switch; keep declarations auditable.
Read-only View mode cannot write. Maintain existing four NON-NESTED method detail tabs, exactly three list-row actions, RTL, typography and design tokens. No extra top-level tab or parent-service screen.
Missing/unknown binding, missing language/permission, unknown destination, stale/malformed response -> fail closed. 401/403 denied, no cross-method writes. No assumption of FREE from absent price.

EXECUTION ORDER
A. Inspect exact existing read contracts: ShippingRatesAndCoverageView.MethodCoverages, IShippingMethodCoverageDirectory, PUT /v1/admin/shipping-services/{serviceCode}/method-coverage, option-scoped grant endpoints, and frontend shipping-one-level-rates-tab.tsx. Avoid duplicate APIs.
B. Implement smallest real API model/UI wiring with focused tests that verify distinct methods, missing declaration, Restricted empty, restore, sibling isolation, permission/identity and preserved existing rate editor.
C. Execute one safe isolated acceptance if 45-minute box permits: disposable PostgreSQL + authorized Admin Host/front-end (reuse TB-157/158/166/172 recipes), method A restricted and sibling unaffected, save/GET/reload. Shared :3000/:5088/:5432, tooba_alpha/store-alpha and shared RabbitMQ/Redis are READ-ONLY; never restart/modify. If runtime cannot be completed within box, report precisely PARTIAL and name smallest unblock, no fabricated screenshot.
D. Tests: focused Shipping owner and frontend tests, typecheck; no repeated full suites and no guard baseline repair.

STRICT LIMITS
No Checkout/Payment/Fulfillment/Settlement payout changes; no production feature-gate enabling, schema/migrations without demonstrated necessity, no new financial modes (FREE/COD/provider quote), no seller/carrier business promises. No Host DbContext/decision logic, cross-module Contracts only. No broad reformat, CSS redesign, unrelated dirty changes, stash/reset/rebase/autostash/force push. Commit and push only task-scoped work; preserve shared running services. Keep proof and sanitized evidence in docs/architecture/evidence/TB-SHIPPING-ADMIN-METHOD-COVERAGE-UX-173/ and task record docs/ai/tasks/TB-SHIPPING-ADMIN-METHOD-COVERAGE-UX-173.task.md.

RESULT CONTRACT
Return BEGIN_TOOBA_WORKER_RESULT / END_TOOBA_WORKER_RESULT with Task-ID, Parent-Task, Base/Code/Evidence/HEAD/origin SHA, exact changed paths, PASS/PARTIAL/BLOCKED, assertions/tests, server truth and real browser results if executed, outstanding gaps, isolation/cleanup and STOP_FOR_ARCHITECT. PASS only if method-scoped restriction edit/save/re-read, sibling isolation, fail-closed semantics and focused tests are PROVEN (otherwise PARTIAL). Automatic-Next-Task: NONE.
END_TOOBA_TASK
