TB-P0-CHECKOUT-START-PAYMENT-COMPOSER-019

Task-ID: TB-P0-CHECKOUT-START-PAYMENT-COMPOSER-019
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-CHECKOUT-PAYMENT-COMMIT-BOUNDARY
Parent-Task: TB-P0-ORDER-PAYMENT-HOLD-COORDINATION-018
Mode: BOUNDED_IMPLEMENTATION_OR_BLOCKED
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 2992b7bb6dd423ff8fef21d8c987fb5ec29ee410
Repository: mrnikiemami-code/Tooba
Branch: main
Timebox: 20 minutes HARD. No automatic next task.

Mandatory preflight
Verify HEAD == origin/main == Expected-Base-SHA. On mismatch: STOP_FOR_ARCHITECT; do not auto-rebase or proceed.
Read AGENTS.md, TOOBA-PIPELINE-PROTOCOL, current recovery context, and 011/012R/017/018 results.
Keep Host CLOSED; do not stop, restart, build, or deploy the running Development Host.

Mission
Implement the Order-owned, non-HTTP backend composition seam for the user's final action to pay, reusing the accepted components without duplicating business decisions:
- 011: cart-addressed, read-only payment draft;
- existing Order canonical checkout submit (atomic reservation and cart conversion);
- 012R: IPaymentInitiationPort in Payment.Contracts;
- 017: Inventory atomic hold extension;
- 018: IOrderPaymentHoldPreparationPort — authoritative checkout/actor/reservation/policy preparation.
Sequence for a final-click request is (1) ownership + cart/draft + method validation; (2) canonical commit once or securely replay existing checkout; (3) Order hold preparation for selected effective method; (4) only on Prepared, invoke the Payment.Contracts initiation port once; (5) return persisted checkout/payment references. The caller supplies cart identity and chosen payment method, never reservation IDs, amounts, expiry or fabricated checkout IDs.
Keep the composer inert in this task: DO NOT expose/register an HTTP POST endpoint, do not touch Shipping/Payment frontend. The purpose is to make the orchestration behaviour testable in module tests before deployment. If an existing Order/Application composition seam can be reused, use it; no Host wiring.

Mandatory correctness gates
Reuse canonical Order submit; do not reproduce stock checking, TransactionScope, cart conversion, quota accounting or cycle state updates.
Exactly one checkout for the cart even after a lost response / converted-cart replay; confirm canonical replay and access checks. Repeated final clicks must not create a second order, consume new quota, or duplicate reservations. The same idempotency key must be stable across commit and Payment initiation, with explicit namespace/length compatibility.
If commit fails, cart remains Active and no money path runs. If commit succeeds but preparation or initiation fails, the existing checkout remains recoverable; never re-commit or auto-cancel it.
For Prepared only, call IPaymentInitiationPort. For AccessDenied, ReservationSetIncomplete, CycleNotPayable, PolicyUnavailable, HoldUnavailable, DeadlineRejected, AmbientTransactionUnsupported, CycleClosedAfterExtension, return typed, domain-owned failures. Do not invent a generic 500, forge Payment error codes, or repurpose an unrelated existing error code. If typed mapping requires a forbidden Host/Host.Tests edit or unapproved new contract, STOP with exact file-level blocker instead of publishing a partial/unsafe seam.
Wallet / gateway / manual all use the canonical Payment port, no special side channel. Do not call PSP or wallet in tests; use fakes.
All configured expiration/hold durations remain dynamic, with no new constants, floors, policy edits or OnlinePaymentHoldHours equality rule.
Disclose the inherent gap between extension and provider initiation; do not claim a cross-module ACID transaction or prevention of every late callback. The live direct Payment endpoint remains a separate guard concern for a follow-up.

Strict boundaries
ABSOLUTELY NO edits/build/restart/stop/deploy of src/backend/Host/** (including Host.Tests), Program.cs, running 127.0.0.1:5088, frontend, database/schema/migrations/settings, provider endpoints, Inventory reclaimer, 015/017 implementations, or unrelated files. No actual HTTP mutations, payments, orders or SQL writes. No broad test suites. Don't run Host.Tests because they can lock/collide with the live executable.
Prefer <=3 production files for the composer. If required changes substantially exceed the bounded slice or a safety gate cannot be met, report BLOCKED_ARCHITECT_DECISION and STOP without an externally callable partial feature.

Verification
Use Order module focused test project only. Cover: successful one-shot composition, lost-response same-key replay, commit failure leaves payment uncalled, post-commit preparation failure leaves existing checkout and no payment call, same chosen method flows through and a second click does not duplicate payment, actor/guest mismatch fail-closed. Source-guard: zero Host edits, zero endpoint publishing, zero fixed hold durations.
Do not claim real DB transaction enlistment proven by mocks. Reuse 017 PostgreSQL evidence and label it as separate proof. One focused run plus only essential guard tests.

Report / STOP
Report exact initial/final SHA, changed files, source line evidence, PASS/FAIL, every test command and counts, idempotency ownership, explicit limitations, and automaticNextImplementationTask=NONE. Commit/push only if safe and complete. STOP_FOR_ARCHITECT. Do not start a follow-up task.
