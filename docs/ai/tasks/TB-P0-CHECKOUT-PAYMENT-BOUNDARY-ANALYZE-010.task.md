TB-P0-CHECKOUT-PAYMENT-BOUNDARY-ANALYZE-010

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-CHECKOUT-PAYMENT-COMMIT-BOUNDARY
Parent: TB-P0-DEV-PACKAGE-ONE-SHOT-RETRY-009
Mode: AMSC_ANALYZE_ONLY — READ_ONLY — STOP_FOR_ARCHITECT
Timebox: 15 minutes hard cap; no long/repeated tests.

Product contract (source of truth)
Cart remains Active, owned, populated, and resumable through Cart → Shipping → viewing Payment → selecting payment method (bank, wallet, card-to-card, any future provider). These steps may save selection/draft only: they MUST NOT create an Order/CheckoutGroup, convert/detach Cart, reserve/decrement stock, or consume unpaid-order/churn quotas.
Only the final user action explicitly starting payment with the selected method starts one checkout commit: validate final cart/prices/shipping/method, atomically check and reserve inventory, persist Order/CheckoutGroup as PendingPayment and convert/detach Cart on successful completion; then initiate chosen payment method. Payment success is not the Order creation boundary. If commit fails, Cart remains Active; no dangling reservation/order and show error. If payment initiation/transaction fails after successful commit, keep Order with safe retry/expiry policy.
Inventory availability check and reservation MUST be a single atomic, concurrency-safe operation within Inventory-owned transaction, not separate query-then-command. Competing requests cannot oversell or double reserve. Verify DB conditional update/locking, available quantity formula, reservation creation and stock accounting; distinct Cart/Order DB contexts cannot be assumed one ACID transaction. If an existing TransactionScope claims atomic cross-module behavior, prove enlisted connections/transaction behavior, and identify any outbox/saga/compensation/retry needs. Do NOT claim distributed atomicity without evidence.
Final payment click must be idempotent (double-click, timeout/retry, multiple tabs) and method-independent, with explicit handling of guest/session checkout access and correct cart detachment only after proven commit success.
Host is CLOSED; no Host edit. Existing Fulfillment/Inventory journal invariants preserved.
Investigation, repository- and evidence-grounded
Re-check main HEAD and governance, inspect actual frontend persistAndContinue, commitShippingToPayment, payment page's checkoutId dependency, payment-method selection and final-pay handlers, storage/session and navigation.
Trace API /shipping/selection, /shipping/commit, checkout GET and payment initiation; find responsible Order/Cart/Inventory/Payment contracts, DTOs, transaction coordinator, and live policy limits. Check docs/architecture/47-checkout-order-foundation.md and recovery/context docs, but distinguish old documented behavior from the new product contract above.
Inspect ReserveForCheckoutAsync and DB implementation for stock availability + reservation atomicity under concurrent commits, and any transaction boundaries between Inventory, Order, Cart. Explicitly report race/oversell/partial-commit failure scenarios, concurrency token/SQL lock or atomic predicate, unique constraints and idempotency proof.
Design minimum safe migration path so payment screen can load with an Active cart / pre-order payment draft without fake checkoutId or eagerly creating Order. State exactly which UI/API contracts need change, which module owns each transition, reservation expiry and anti-abuse quota timing. Consider wallet and offline payment paths.
Provide a short, implementable ordered sequence of bounded follow-up tasks, each ~15–20 min max, minimal changed files, single targeted test run, with PASS/FAIL acceptance and rollback; do not start them.
Mandatory deliverable (NO implementation)
Return: (a) verified current call graph with source file:line refs, (b) exact violations of desired contract, (c) evidence-backed assessment of inventory check+reserve atomicity and cross-module transaction boundaries, (d) concrete smallest safe design + migration/compatibility risk, (e) next ONE recommended implementation task with scope and tests, (f) unresolved facts marked UNKNOWN. No changes to production code, test code, DB, migration, runtime, or Host. No order/cart/payment mutations. No build/test suite needed unless a truly essential tiny read-only verification; do not loop. Stop for Architect ACCEPT.
Report protocol: BEGIN_TOOBA_WORKER_RESULT / END_TOOBA_WORKER_RESULT, Task-ID, repo SHA, changes=0, evidence, STOP.
