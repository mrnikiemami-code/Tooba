TB-P0-CHECKOUT-PAYABILITY-ATOMIC-014

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Mode: BOUNDED_ANALYZE_AND_SAFE_IMPLEMENT — STOP_FOR_ARCHITECT
Parent-Task: TB-P0-CHECKOUT-PAYABILITY-GATE-013
Program: TOOBA-CHECKOUT-PAYMENT-COMMIT-BOUNDARY
Expected-Base-SHA: d6ce094c0cfe344cf0e26ac3138b564058163938
Timebox: 15–20 minutes; no looping/repeated tests

Architect decision

Choose atomic coordination design A for Order-owned reservation-cycle payability and Payment-owned money initiation. Do not ship a check-then-initiate TOCTOU gap. Do not weaken or update locked error/architecture guards merely to pass tests. Reuse existing correctly semantic errors only; if a new typed error requires Host.Tests changes, STOP and report an exact governance proposal instead of editing Host.Tests.

Hard protections

NO files under src/backend/Host/ (including Host.Tests), NO Program.cs/Host DI or configuration edits, NO Host build/restart/kill/deploy, NO live DB writes/migrations, NO frontend, NO change to hold settings or quotas, NO live payment/order API mutation. Preserve existing operational checkout endpoints. Do not fabricate PASS or claim ACID based only on source text. Host is running and must remain running. No broad test suite. Existing dirty files untouched.

Bounded mission
Inspect exact current HEAD and trace Inventory reservation expiration/release locking, Order reservation-cycle expiry/correlation, PaymentDirectory initiation/replay/provider call sequence, current Contracts and error catalog. Distinguish active cycle from a genuinely held Inventory reservation; ensure the proposed gate does not authorize money on stale released stock.
Define a single atomic linearization point for initiation vs expiry/release using current PostgreSQL transaction/lock/CAS mechanisms, including cross-DbContext enlistment and transaction scope. Ensure no provider money movement occurs inside a DB transaction if that would create long locks or unrollbackable side effects. If that cannot be enforced safely inside allowed files, STOP with exact gap and minimal split.
If feasible inside allowed module files and guards: implement the smallest contract/implementation slice that enforces the atomic payability/correlation boundary. Do NOT add final-click endpoint until race is actually closed and approved; do not add dead or optimistic boolean guards misrepresented as race-free.
If new Payment error code is required and locked Host.Tests would fail, do NOT edit Host.Tests; return BLOCKED_ARCHITECT_DECISION with a concrete semantic error/lock-governance change request.
At most one focused module test run if code changed. Tests must cover expired/released vs valid reserve and simultaneous worker expiry; if behavioral PostgreSQL proof is not safely runnable, explicitly mark UNVERIFIED without killing Host. Do not retry a locked Host test.
At end: commit/push only approved, complete production changes plus targeted tests, or docs-only blocker evidence. Output exact SHAs, changed paths, test command/result, invariants proven vs unknown, and STOP_FOR_ARCHITECT. automaticNextImplementationTask=NONE.
Acceptance

No Host modification; no payment initiation against an invalid reservation; no unproven claim of race-free coordination; no duplicate charges or extra reserves; no changes to configurable durations. If atomicity, typed errors, or module ownership cannot be upheld, BLOCKED without partial unsafe endpoint.
