TB-SHIPPING-NEW-METHOD-BROWSER-PAYMENT-172

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-NEW-METHOD-BROWSER-PAYMENT-172
Parent-Task: TB-SHIPPING-NEW-METHOD-CUSTOMER-ACCEPTANCE-171
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: c36d2f8004f74cea00eae119f60514ca83ca9178
Program: TOOBA-SHIPPING-FLAT-METHOD-SELLABLE-CLOSURE
Mode: BOUNDED_AUTHENTICATED_PRIVATE_BROWSER_AND_PAYMENT_DRAFT_ACCEPTANCE
Status-Required: PASS | PARTIAL | BLOCKED
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE
Hard-Timebox: 45 minutes, stop at boundary; no overrun

Mission

Close the precise remaining customer-visible proof for a newly created canonical-only method (TB-170/171): real authorized Admin creation/activation and owned price; real authenticated storefront browser selection and independent shipping card; persisted choice after full reload; read-only payment draft with the same authoritative method, group identity, and amount. This is an execution/acceptance task, not another wireframe or broad rewrite.

Preparation / truth sources
Verify HEAD == origin/main == Expected-Base-SHA before work. Stop on divergence. Read locked docs/architecture/shipping/SHIPPING-ADMIN-FLAT-METHOD-CONTRACT.md and docs/architecture/shipping/SHIPPING-GROUP-INDEPENDENT-DELIVERY-COMMITMENT.md plus TB-166 and TB-171 seed recipes and evidence. Reuse the tested scratch fixture; do not rediscover or rebuild ad hoc.
Work only with task-owned loopback PostgreSQL, private Development Host and private Next frontend, and scratch one-active-tenant; fail-closed isolation guard before any write. The committed Tooba:ShippingOwnedRateAuthority:Enabled remains false everywhere; set true only in disposable overlay. No shared :3000/:5088/:5432, tooba_alpha, actual store-alpha, Redis/RabbitMQ, or worker runtime modification. Credentials/tokens never enter evidence.
Use the exact approved canonical product flows: Admin authorization bootstrap, live language registry, create a fresh tipax:<unique-task-identifier> method inactive, activate via supported seam, configure legitimate owned fixed IRR rate and explicit method coverage, Development customer OTP fixture, real seeded seller/offer/stock/cart and tax rules. Do not create a legacy option row, invent FREE/carrier slots, or bypass eligibility.
Required live acceptance

A. REAL BROWSER: Start private frontend wired only to private Host. Authenticate real customer through sanctioned Development fixture. Navigate /fa/shipping with a real cart and demonstrate the newly-created method as a selectable option showing the server-owned 185000 IRR owned rate (or actual configured amount if explicitly set differently); do not confuse it with existing tipax:standard/express. Select its server-provided date/window and click Continue. Capture genuine screenshot(s) and DOM assertions for method label/code, eligibility, date and window, fee and Continue state. On fresh full document reload/back from /fa/payment, the same selection, group ID, date/window and fee must reappear. Never fake screenshots or claim live browser proof without executing it.
B. REAL HTTP/DB: From the same authenticated customer/cart, GET shipping projection, PUT selection, GET reread and GET read-only /v1/storefront/cart/{cartId}/payment-draft; assert method code exact, customer shipping charge exactly once per group, payment-draft ShippingAmount equals sum of group amounts, payable arithmetic matches subtotal-discount+tax+shipping, no payment initiation or checkout creation. SQL readback must show membership exactly once and stable ShippingGroupId; no paid order/payment rows.
C. SAFETY/NEGATIVES: With new method active but owned rate absent, or with method-specific coverage Restricted and zero own grants, it must disappear/reject selection (400 stable error), no missing-rate=FREE, no provider/sibling inheritance. Rejection must not corrupt previously valid draft. Restore the valid owned rate/coverage inside scratch only; other methods remain unchanged. If reproducing all negatives would break safe timing, prioritize A+B and mark unfinished negatives explicitly.
D. REGRESSION: OFF control on same scratch data must hide canonical-only method, retain legacy quote amounts and omit UnavailableMethods; no change in committed flags. Do not interpret Admin activation as financial readiness; this method is selectable only after valid owner quote.

Scope and discipline
Prefer no production code changes. If a real product defect blocks A/B, make only a minimal owner-correct repair with focused tests, no Host business authority, no invented external rate, no schema guesses. Stop and report PARTIAL/BLOCKED if unsafe.
Keep the user's existing storefront visual style, Persian RTL, and one-level Admin UX intact. No global date/window, no pricing arithmetic in React, no provider policy inherited across options.
Execute only essential focused checks if production code changes; otherwise the browser+HTTP+Postgres proof is the test. Avoid repeated full suites.
Do not broaden to same-seller incompatible origins, carrier working-day/capacity/timezone, settlement share/payout, or release performance topics in this task.
Artifacts and verdict

Commit sanitized evidence under docs/architecture/evidence/TB-SHIPPING-NEW-METHOD-BROWSER-PAYMENT-172/: RESULT.md, isolation guard, actual scratch seed/harness commands, genuine browser screenshot(s) and DOM assertions, sanitized HTTP projections/save/reread/payment draft, DB readback, negative and OFF control matrices, cleanup/port checks. Persist this task in docs/ai/tasks/ when possible. Fast-forward push to main only; record code/evidence/final SHAs and HEAD-origin parity. Preserve unrelated dirty work; no stash/reset/rebase/force-push.

PASS only if real private authenticated browser choice/reload AND read-only payment-draft arithmetic/identity AND fail-closed no-fake-rate conditions are proven, plus cleanup. Otherwise PARTIAL/BLOCKED with concrete observed blocker; do not substitute source inference or mock UI for acceptance. No final purchase, PSP, SMS, real payout or shared-environment write. STOP_FOR_ARCHITECT; never auto-start next task.
END_TOOBA_TASK