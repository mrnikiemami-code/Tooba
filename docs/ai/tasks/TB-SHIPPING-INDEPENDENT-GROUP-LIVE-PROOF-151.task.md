PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-INDEPENDENT-GROUP-LIVE-PROOF-151
Parent-Task: TB-SHIPPING-INDEPENDENT-GROUP-FRONTEND-150
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: e56a0e67b92aa3421017aa69c7ee67d342b0e1d2
Program: TOOBA-SHIPPING-INDEPENDENT-SELLER-COMMITMENTS
Mode: BOUNDED_ISOLATED_REAL_HTTP_WRITE_READ_AND_PAYMENT_HANDOFF_PROOF
Timebox: 45 MINUTES HARD
Status-Target: PASS_OR_PARTIAL_OR_BLOCKED
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

USER AUTHORITY
Continue autonomously to a usable Shipping journey. Do not request another routine approval and do not launch concurrent tasks. Treat 149 and 150 as implemented but PARTIAL until actual authorized persistence is proven. Do NOT fabricate browser evidence.

READ FIRST

docs/architecture/shipping/SHIPPING-GROUP-INDEPENDENT-DELIVERY-COMMITMENT.md
docs/planning/shipping/SHIPPING-GROUP-SCHEDULE-IMPLEMENTATION-145.md
docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-16.md
TB-149 and TB-150 evidence/results.

GOAL
Demonstrate and, only if a focused actual defect is found, repair the real saved two-seller, SAME method but distinct promise flow using the current code and a completely disposable private environment. Example: seller A tipax:standard, preparation 2 days; seller B tipax:standard, preparation 3 days, same method lead days. Two independent customer-visible cards, different authoritative earliest dates, independent dates/time windows, no accidental cart/order mutation before final checkout.

SCOPE — 45 MINUTES

Independently inspect the available Development/scratch bootstrap scripts and identify an already-supported ephemeral isolated Host + PostgreSQL + frontend run. Use unique private ports and temporary database, sanctioned dev identities and development fixtures ONLY. Do not reconfigure, touch, stop or write shared localhost:3000, :5088, tooba_alpha/store-alpha or another worker's process/database. No real external SMS/PSP or payments. If private environment is unavailable within 10 minutes, stop with BLOCKED and exact missing prerequisites; do not spend rest of time on hypothetical architecture.
Use a sanctioned authenticated development CUSTOMER identity, with a real two-seller cart whose lines select the SAME canonical method tipax:standard. Seller-specific preparation = 2 and 3 (plus same lead-days); server IClock determines actual date. No invented shipping costs/origins. Capture authoritative GET group horizons, UI screenshots, POST/PUT save selection, subsequent GET re-read of each ShippingGroupId/date/window, back/refresh, and Payment draft read if its supported read-only seam exists. Never call final payment, create a real order, or bypass authentication/eligibility.
Mandatory assertions: two distinct seller ShippingGroupIds after save; before-save distinct group keys; each valid independent earliest date; different selected windows/dates retained; editing A leaves B unchanged; one invalid date/slot fails closed without overwriting B; line membership unchanged; shipping fee charged per group exactly once; no universal date-time picker; summary and payment draft match accepted selections. Different seller IDs and same method code must remain distinguishable in both UI and API.
If there is a small real bug in 149/150 blocking these assertions, fix ONLY its owner-correct narrow path with essential tests; no speculative migration, no policy bypass, no schema rewrite. Respect Contracts-only, MediatR/Result, no Host business logic. If the bug exceeds remaining time or requires trusted calendar/origin authority, PARTIAL/BLOCKED with exact reproduction and safe next step.
The frontend currently takes a special scalar path for ONE-LINE carts and multiple lines for group cards; test whether two lines from SAME seller sharing method are safely one group and two sellers sharing a method are necessarily two groups. Do not substitute a fake screenshot for an authenticated multi-seller live journey.
Preserve existing production storefront styles/RTL and Admin. Do not create or update Settlement payouts, carrier cost policy, shipping provider policy, method creation or Admin forms; no N+1 investigation here. Case D (same seller+same method+incompatible origins) remains documented BLOCKED; never fake origin. No invented carrier timezone/slot promise: disclose current static generic windows and seller-prep + lead-days authority.

PROOF AND EXIT

One concise evidence document in docs/architecture/evidence/TB-SHIPPING-INDEPENDENT-GROUP-LIVE-PROOF-151/ containing actual private-environment identity (without secrets), used scripts/private ports, request identifiers and sanitized response examples, two clear browser screenshots if available, per-group DB/API row proof, price reconciliation, and PASS/FAIL matrix.
Essential focused tests only; do not run broad slow suites or repeat prior 149/150 green tests unless modified code demands it.
If true private scratch setup is not available or customer auth can't be established safely, never write shared data; report BLOCKED with exact blocker, and propose a bounded test harness/fixture seam as next step. Never call read-only smoke a live roundtrip.
Commit only task-owned changes/evidence, fast-forward push after remote HEAD confirmation, preserve all preexisting dirty work and stashes. If main moves, handle safely without reset/rebase of dirty workspace.
PASS only with genuine authenticated private HTTP save/read + UI proof, otherwise PARTIAL/BLOCKED. End STOP_FOR_ARCHITECT. No automatic follow-up.
END_TOOBA_TASK
