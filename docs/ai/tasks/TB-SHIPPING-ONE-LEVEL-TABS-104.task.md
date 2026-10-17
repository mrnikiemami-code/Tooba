PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-104
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-103
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: BOUNDED_CATALOG_SEED_LATENCY_AND_NARROW_SELLABLE_OFFER_PATH
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 5e85522ddb454da8b13cbcc33c4662f2cb1c05b9
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 30 minutes HARD, including teardown, evidence and push. Stop work by minute 24 and reserve 6 minutes for cleanup/report. A single seed invocation may consume at most 8 minutes; no retry of a long-running seed.

ARCHITECT RULING
103 PARTIAL accepted as honest evidence, NOT as a repaired Catalog seed. Scratch reset-and-seed reached 545 Draft products, 116 categories and 41 attributes, but zero offers/prices/stock before a five-minute client cancellation. It has NOT proven that increasing the HTTP timeout would yield any sellable offer. Repeating the same large reset-and-seed is prohibited. This task is a SMALL DIAGNOSTIC AND NARROW PATH decision: inspect source first and find an official bounded Catalog Development path to exactly one sellable offer. If no such path exists, STOP with a minimal, evidence-based proposed repair; do not consume the timebox generating hundreds of products again.

PREFLIGHT — STOP ON CONFLICT

Claim exactly one Pending task on tooba-main. Read AGENTS.md, pipeline/controller, 102/103 reports and the official Catalog demo seed endpoint, command/handler and configuration. Persist task receipt UNSTAGED. Fetch origin/main and verify Expected-Base-SHA ancestor; tolerate only non-overlapping result-bookkeeping drift. STOP RECOVERY_CONFLICT on source overlap. Preserve ALL dirty/untracked work; no reset/stash/rebase/force push.
Verify :5088/:3000 provenance and shared tooba_alpha parity READ-ONLY before work. Never touch shared data or change shared Host process/gates.

MISSION A — SOURCE-FIRST, MAXIMUM 10 MINUTES

Trace EXACT routes and implementations of reset-and-seed and possible seed-only (DO NOT assume the latter exists); identify whether seed-only avoids the 545-product expansion, the cancel token semantics, the first offer generation step and existing bounded/idempotent seed options. Inspect official Catalog Application/Domain commands needed for one sellable offer: product publication, offer/price/inventory/eligibility. Report relevant file paths and precise dependency/latency risk.
Choose ONE of two paths:
A. An EXISTING official Development-only route or supported parameter can create a single/very small sellable set and is demonstrably bounded; use it ONCE in task-owned scratch.
B. Otherwise NO SEED CALL: produce a precise minimal Catalog-owned proposal for an isolated one-off sellable fixture builder USING EXISTING PUBLIC APPLICATION COMMANDS, not EF/raw inserts, without implementing a framework or changing production logic. Return BLOCKED_NARROW_SELLABLE_SEED_PATH and STOP.
Absolutely no raw SQL DML to construct products/offers/prices/stock, no bypassing publish/readiness/inventory/auth rules, no fabricated authorization, no production Catalog behavior changes. A full 545-product demo run is disallowed even with a longer timeout.

MISSION B — CONDITIONAL REAL PROOF, ONLY IF PATH A AND ENOUGH TIME

Create isolated disposable loopback Postgres/Host exactly as guarded in 099/102/103: offline effective config has only scratch references, DB-side barrier, /__platform-commerce provenance isolated, secrets ephemeral/untracked, real Development admin authentication. Run the chosen existing BOUNDED official seed path ONCE, <=8 minutes, no indefinite HTTP wait. If not complete, cancel once, record exact phase and STOP; no retry.
Prove sellability from the real API, not merely product counts: real customer credential login, cart create, one line add for the legitimately created offer. Record HTTP outcomes and read-back. Do not create Checkout/payment/order or change customer production data. If cart line succeeds and >=8 minutes safely remain, optionally test one gate-OFF vs gate-ON read-only shipping projection pair on the same scratch cart; do not synthesize shipping identities with SQL or broaden to a separate shipping seed. Otherwise leave projection to 105.
No task-owned code implementation unless a provable tiny fix is clearly within Catalog Development seed ownership and can be tested and cleaned up inside the timebox; prefer report-only. Never modify Order/Fulfillment/Shipping/Checkout/Payment/Settlement or frontend for this task.

MISSION C — CLEANUP & EVIDENCE

At minute 24 or earlier STOP tests, tear down scratch Host/container and ephemeral credentials, verify ports freed and shared stack/database unchanged. Do not sacrifice cleanup to a seed call.
If source code changed, run only appropriate narrow Catalog tests. Report real elapsed minutes, exact result PASS/PARTIAL/BLOCKED, code/evidence SHA. Do not assert any successful offer/cart without real HTTP response. Report timeouts/cancellation and unexpected failures accurately.
Commit/push only task-owned changes and docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-104.md, docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-104/**. Receipt docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-104.task.md remains UNSTAGED. Master recovery and unrelated files untouched.

ACCEPTANCE
PASS: bounded official seed path plus real authorized sellable offer cart-line add on isolated scratch, with cleanup and parity. PARTIAL: official bounded path or narrow source fix identified/proven but cart-line not complete. BLOCKED: no correct narrow existing path; precise Catalog-owned proposal and file/route evidence, no long seed launched. STOP_FOR_ARCHITECT in every case. NEVER claim ready for Checkout cutover or commercial shipping.

RESULT CONTRACT — BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-104
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-103
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE
Mode: BOUNDED_CATALOG_SEED_LATENCY_AND_NARROW_SELLABLE_OFFER_PATH
Bridge-Task-Row: <assigned at claim>
Status: PASS|PARTIAL|BLOCKED
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 5e85522ddb454da8b13cbcc33c4662f2cb1c05b9
Base-SHA: <actual>
Code-SHA: <commit or NONE>
Evidence-SHA: <commit>
automaticNextImplementationTask: NONE
Report source-confirmed seed path, invocation count, duration, actual offer/cart-line HTTP facts if any, shared parity, teardown, exact changed files, measured minutes, unresolved prerequisites.
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK