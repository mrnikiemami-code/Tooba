Persisted claim artifact — full Architect body received via Bridge
`GET /api/tasks/next?channelId=tooba-main&workerId=tooba-worker-01&agentType=cursor`
at Bridge task UUID `2e3799f6-102b-4b02-b690-bbff801fa9ec` (createdAtUtc 2026-10-16T19:50:18.5798776Z).


PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-METHOD-FINANCIAL-AUTHORITY-155
Parent-Task: TB-SHIPPING-GROUP-UX-ACCEPTANCE-154
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 3134141cff468b37401a11100ce1bf2606c7f4e6
Program: TOOBA-SHIPPING-FLAT-METHOD-COMMERCIAL-READINESS
Mode: BOUNDED_IMPLEMENT_PER_METHOD_AUTHORITATIVE_MONEY_STATUS
Status-Target: PASS_OR_PARTIAL_OR_BLOCKED
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 45 MINUTES HARD. By minute 35, stop expanding scope and reserve time for focused tests, evidence, commit/push and report. No parallel task or automatic continuation.

READ FIRST — LOCKED AUTHORITIES

docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-16.md
docs/architecture/shipping/SHIPPING-ADMIN-FLAT-METHOD-CONTRACT.md (especially LOCKED ADDENDUM: explicit financial status, no implicit free, zero sibling policy inheritance)
docs/architecture/shipping/SHIPPING-GROUP-INDEPENDENT-DELIVERY-COMMITMENT.md
docs/planning/MUST-FIX-BEFORE-RELEASE.md
docs/architecture/checkout/SHIPPING-GROUP-FEE-ALLOCATION-POLICY-DECISION.md

PROVEN BASELINE / DO NOT REPEAT
TB-154 passed genuine isolated two-seller same-method Shipping UI, per-group schedule, Continue gating, payment preview and reload. TB-153 stabilized ShippingGroupId. Do not alter storefront group UI, date schedules or accepted order/payment/fulfillment behavior. TB-148 implemented multilingual Admin General but successful write/reopen remains unverified. Seven canonical Admin methods are independently listed (post:express, post:standard, tipax:express, tipax:standard, snapp_courier, store_courier, in_person). Current General tab honestly says financial state is unreadable. User requires clear per-method IRR policy and no sibling inheritance.

SINGLE DELIVERABLE
Implement the SMALLEST OWNER-CORRECT SERVER-AUTHORITATIVE READ MODEL and Admin representation of the financial configuration/readiness status of ONE CANONICAL SHIPPING METHOD. Use actual current persisted rate/coverage/fee data; do NOT invent policy or treat empty data as free. Make General tab show a meaningful verified status for a chosen method, independently from activation, along with sourced currency/fee information ONLY when actually authoritative. The UI must distinguish Active from FinanciallyConfigured/QuoteReady/NeedsConfiguration and explicitly show uncertainty or unsupported modes. Preserve the four existing top-level tabs, dynamic languages, compact RTL design and canonical method binding.

BOUNDARIES / REQUIREMENTS

Inspect present Shipping module rate authority, admin one-level method binding, rates/policies, Storefront quoting and the exact Admin General state. First write a concise table of which existing monetary modes are truly supported; never claim FREE, FIXED, RULE_BASED, COLLECT_ON_DELIVERY or PROVIDER_QUOTE if the current schema/authoritative owner cannot prove it. Do NOT manufacture a generic 5-mode write API just to match the locked vocabulary.
Scope any readiness/rate evaluation by the authoritative canonical method binding (methodCode + resolved serviceCode/optionCode), tenant/edition and policy ownership. A provider-level legacy tariff with optionCode null is NOT by default the tariff of post:express or post:standard. Do not leak sibling rates; if ambiguous report LEGACY_SHARED_UNRESOLVED/UNCONFIGURED, not a positive amount or free.
Prefer one minimal additive read seam under owning module (or enrich an existing authenticated one-level method-detail read) exposing typed per-method financial status and reason. Preserve API authorization/feature gating, localization and backwards compatibility. Admin consumption must use its existing client auth transport; 401/403 fail closed. Do not infer financial readiness from isActive or label.
If an existing explicit approved method-specific fee policy can be computed authoritatively, show its IRR money representation and mode; if not, show 'نیازمند تنظیم مالی' with a precise explanation. Never quote a made-up customer shipping price. No client-side split of 'post:express' to claim ownership.
Do not alter production price calculations, existing customer quotes, financial ledger, carrier payable, seller/platform shares, payout, checkout tax, order snapshots, schema or migrations in this task without a necessary reviewed migration and explicit stop. Do not activate new quote modes. Do not implement create-method API or unrelated optional-restriction editor in this task. The long-term financial mode authoring API and rules are a SEPARATE later increment.
Prove method isolation for tipax:standard vs tipax:express and post:standard vs post:express, including missing/ambiguous policy, active-but-not-ready and independently configured vs unconfigured. Assert no implicit zero-charge/free behavior. Tests MUST target real server evaluation and Admin mapping, not fake readiness badges.
Source-owner architectural rules remain: Contracts-only cross-module access; no Host DbContext/business logic; existing CQRS/MediatR/Result conventions; no broad redesign. Keep task-owned file sizes and canonical paths compliant.

VERIFICATION / EVIDENCE

Focused backend tests of readiness mapping, 2 sibling-isolation cases, absence of tariff != free, invalid binding/authorization fail-closed; focused frontend mapping/render tests and TypeScript check if UI edited.
Prefer isolated read-only scratch HTTP/browser proof of a real method's status; shared :3000/:5088, tooba_alpha/store-alpha are READ-ONLY, must not restart or write. If unavailable inside timebox state exactly what is unverified; never mark it PASS merely due to mocks.
No duplicate long test suite, no rerunning TB-154 journey. No real SMS/PSP/payment or carrier API, no secrets in artifacts, no unsafe cache cleanup.
Record changed files, actual HTTP sample/status without credentials, scoped proof, focused test counts, blocked capability gaps and realistic next step. Commit/push fast-forward, confirm HEAD==origin/main; preserve unrelated dirty files and stashes, no reset/stash/rebase/force-push.
PASS only for a genuinely implemented, authoritative per-method status which Admin displays without sibling leakage; PARTIAL if only read model or UI is complete, BLOCKED if no safe canonical ownership exists. All results STOP_FOR_ARCHITECT.

FOLLOW-UP BACKLOG — NOT AUTHORIZED BY THIS TASK
A. Next: method-specific financial policy EDIT/WRITE and optional restriction defaults/isolated per-method enforcement, after owner-correct read seam.
B. Add a real canonical create-method API and functional Add Service form; do not use old nested provider creator.
C. Multilingual Admin successful write/reopen and full rate/coverage/schedule/packaging persistence in disposable environment.
D. Shipping storefront independent-group release acceptance is proven in TB-154 scratch, but live production rollout/customer acceptance remains separate; carrier authoritative calendar/origin and same-seller same-method incompatible origins remain open.
E. PERF-CRIT-001 EF Debug log flood, bounded scaling proof; FIN-POL-001 commercial allocation decision remain release blockers.

END_TOOBA_TASK
