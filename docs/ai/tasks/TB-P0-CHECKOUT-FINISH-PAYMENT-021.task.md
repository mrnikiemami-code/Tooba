TB-P0-CHECKOUT-FINISH-PAYMENT-021

Task-ID: TB-P0-CHECKOUT-FINISH-PAYMENT-021
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-CHECKOUT-PAYMENT-COMMIT-BOUNDARY
Parent-Task: TB-P0-PAYMENT-IDEMPOTENCY-METHOD-BINDING-020
Mode: BOUNDED_IMPLEMENTATION_OR_BLOCKED
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: b8b2c33f79fc6952acc1adbf1a7dd66933328f6e
Repository: mrnikiemami-code/Tooba
Branch: main
Timebox: 25 minutes HARD
AutomaticNextImplementationTask: NONE

PREFLIGHT

Read AGENTS.md, protocol, current recovery, and 018/019/020 evidence. Verify HEAD == origin/main == Expected-Base-SHA; if drift, STOP with actual SHA and no edits.
Inspect working tree; never stage/revert unrelated dirty or untracked paths.
Host production CLOSED: absolutely NO edits/build/stop/restart/deploy under src/backend/Host except the narrowly specified architecture-test files below. NEVER run Host.Tests or build Host, never touch localhost:5088, live DB, schema, migration, settings, workers, frontend, Cart commit. No real PSP/wallet calls.

BUSINESS CONTRACT (DO NOT MODIFY)

Cart Active through Shipping/payment selection; only final Pay click commits PendingPayment checkout and converts Cart.
While an existing Order reservation cycle and Inventory hold are valid, allow payment attempts per canonical Payment state/idempotency.
When expired, use the EXISTING canonical unpaid-retry/reacquire path only if the effective configurable retry policy permits and real inventory is available. If exhausted/unavailable, fail closed with a user-recoverable way to reselect products. DO NOT hard-code 3 attempts or silently remove/raise the existing policy cap. Attempts to pay and reservation renewals are different counters.
Never charge on unproven supply; do not override merchant/store/method TTL, payment timeout, or anti-abuse configuration. No new TTL constant.

PRIORITY A — FIX PAYMENT REPLAY CROSS-METHOD/CROSS-CHECKOUT DEFECT (MANDATORY)

In PaymentDirectory.InitiateAsync existing-idempotency branch, BEFORE any gateway initiation, retry, or return: compare command.CheckoutId to persisted CheckoutId AND gateway-registry-canonicalized requested ProviderCode to stored ProviderCode. For conflicts reject with new registered/localized PaymentErrorCodes.IdempotencyConflict (payment.idempotency.conflict, HTTP 409) — never generic 500, never reuse wrong code, never mint a second method-scoped key.
Match identical-method replay including canonical default gateway alias; preserve actor/guest authorization, wallet/manual flows and same-key successful replay, reject failed-prior cross-method BEFORE provider call. If a key was reused across checkouts, reject even if same user.
Fix only the pinning guard expectations necessary to reflect the genuine new error: explicitly permitted exception to Host test boundary, LIMITED TO src/backend/Host/Tooba.Host.Tests/Architecture/PaymentModuleAmsc001W1MigrateGuardTests.cs and .../PaymentModuleAmsc001W3CertGuardTests.cs; update exact expected counts 28->29 and 24->25 and required en/fa parity; never remove/disable assertions. No Host production files whatsoever. If the necessary guard edit exceeds these two files, STOP.
Payment files allowed: PaymentErrorCodes.cs, PaymentErrorCatalogContributor.cs, PaymentErrors.resx, PaymentErrors.fa.resx, PaymentDirectory.cs, and focused Payment.Tests files. Test in module project only; no Host.Tests. Include missing-key replay cases, no provider call/rows on conflicts, legitimate replay, actor visibility. Avoid broad tests.

PRIORITY B — REVIEW THE EXISTING MONEY ENTRY AND RESERVATION RETRY (ONLY IF A IS VERIFIED AND TIME REMAINS)

Inspect existing POST /v1/storefront/checkout/{checkoutId}/payments and the canonical Order unpaid-retry hold reacquisition. Determine exactly whether an expired reservation can reach wallet debit, PSP initiation or manual initiation. If unsafe, implement the smallest fail-closed guard using the ALREADY existing Order-owned payability/retry boundary before money movement; never insert broad GetPayableAsync exceptions that would block verification/refunds after paid capture.
Do NOT quietly block legitimate retries within configured retry allowance; use existing policy and stock check. NEVER wire the 019 composer to a public endpoint while the old money-entry route remains unsafe. If fixing requires new cross-module seam, migration, large scope or independent Host update, STOP with exact blocker/next bounded patch proposal, no partial publicly reachable path.

PRIORITY C — API/FRONTEND CUTOFF (ONLY IF A+B PROVEN AND REMAINING TIME)

Only if both current money entry and hold lifecycle are proven safe with focused tests, wire final-click composer in Order endpoints and migrate frontend Shipping to read-only payment draft; Payment page selection must not commit. Final click does canonical commit+prepare+initiate; retry same checkout, show expired/out-of-stock recoverable path; preserve current design and checkout configurations. Never deploy/restart Host. If infeasible inside timebox, do NOT implement a half-cutover; report next exact files/tasks to do.

VERIFICATION / STOP

Correctness > breadth. At most one focused module test run for each touched module, plus a targeted build if essential. Test payment no-duplicate/cross-method/cross-checkout, ownership, no unintended money call. Never claim runtime Host or real PSP proof.
If A cannot be implemented safely, stop immediately with precise evidence. If A passes but B/C are not safe/timeboxed, commit/push A only and report B/C pending; DO NOT publish incomplete endpoint.
Commit and push ONLY intended files, report actual SHA, list, tests, limitations, new error catalog parity, Host untouched status. Update task/evidence/recovery only if within scope and time. STOP_FOR_ARCHITECT, no automatic next task.
