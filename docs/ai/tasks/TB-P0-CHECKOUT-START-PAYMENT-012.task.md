Cursor Task — TB-P0-CHECKOUT-START-PAYMENT-012

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Task-ID: TB-P0-CHECKOUT-START-PAYMENT-012
Parent-Task: TB-P0-CHECKOUT-PAYMENT-DRAFT-011
Program: TOOBA-CHECKOUT-PAYMENT-COMMIT-BOUNDARY
Mode: BOUNDED_IMPLEMENTATION — STOP_FOR_ARCHITECT
Expected base: latest origin/main containing 63b5b9a41a849359072f99670726980b279391de (011). Verify exact HEAD and report it; STOP on unexpected drift.
Timebox: 20 minutes hard stop. One focused test run, no loops.

Objective

Implement a Backend-only canonical final-click start-payment operation: active Cart with saved shipping draft and chosen payment method -> existing CheckoutProcessManager commit (atomic inventory check-and-reserve, Order PendingPayment, Cart Converted) -> existing Payment initiation. No order/reservation/conversion when merely viewing Shipping or Payment draft or selecting a payment method. Do NOT switch frontend/shipping yet. No Host modifications.

Required implementation gates
Inspect actual 011 code, Shipping commit, CheckoutProcessManager, StorefrontPaymentOrchestrator, all payment-method paths (online/wallet/manual), Cart guest identity, and existing Contracts. Choose smallest module-owned route/handler consistent with endpoint validation, CQRS, and Contracts-only boundaries. Do not invent Host-owned business orchestration or call localhost HTTP from module code.
Accept cartId, expectedCartVersion, selected method/provider, and stable cart-scoped idempotency key. Derive any other data from server-owned saved shipping draft/quote. No fake checkoutId, no client-supplied final price, no hard-coded hold duration. Preserve effective Store/Category/Offer and method hold settings.
Reuse existing commit/reserve path, never implement read-then-reserve. The Inventory conditional update is the authoritative availability guard. Multi-line all-or-nothing is mandatory; investigate actual transaction/enlistment semantics. Do not claim ACID merely because TransactionScope appears in source. If behavioral integration proof is unavailable, STOP and report BLOCKED before enabling a potentially unsafe mutation seam.
Idempotency across BOTH steps: same cart/key/method must return or recover the same Checkout and Payment attempt; do not recommit Converted cart, do not consume another abuse quota or reserve again. Different method/key after initial commit must follow explicit existing pending-payment policy, not invent a new Order. Concurrent double-submit must not produce duplicate Checkout/Order/stock holds, or uncontrolled 500. Inspect existing unique constraints and error mapping.
If commit fails, preserve Active Cart and ensure no orphan reserve/quota mutation. If commit succeeds but Payment initiation fails, preserve recoverable PendingPayment Order + ownership proof, allow safe retry of initiation without re-commit. Do NOT detach browser cart pointer in this backend-only task. Respect errors and idempotency/ownership for guests and authenticated users.
Reuse current payment initiation flows including wallet, online and manual as appropriate. Payability should use one Order-owned reservation-cycle policy, before beginning charge. If this requires new cross-module port, schema migration, changing locked semantics, or > bounded scope, STOP_FOR_ARCHITECT with precise blocker and minimal proposed split rather than shipping a partial unsafe mutation endpoint.
Preexisting GET payment-draft must remain read-only and must not be interpreted as persisted Order because quote contains transient sellerOrder identifiers. Existing Shipping commit remains untouched until later frontend cutover; no rollout or live mutation in this task.
Test / evidence (minimal)
One focused behavioral integration test with real Postgres (not grep/source-assertion/mocks) proving multi-item reservation+Order+Cart atomic rollback on a forced mid-commit failure, if feasible in timebox and environment. If impossible: mark transaction guarantee UNKNOWN and STOP rather than certify.
Targeted tests covering (a) one final-click creates 1 Checkout and correct reservations, (b) same-key replay after successful commit+initiate returns same identities and does not increase reservation/quota, (c) initiation failure after commit permits recovery with same Checkout, and (d) concurrent identical requests cannot double-charge/oversell (at least source-level constraints plus focused test if feasible). Limit to indispensable tests; do not run whole solution.
Produce exact commands/results, changed files, commit SHA, and succinct before/after guarantees. Be explicit FACT / RISK / UNKNOWN.
Strict safety and stop

No Host edits, no frontend cutover, no production, no service restart, no migrations/DDL, no live cart/order/payment mutation, no manual data repair, no bypassed auth; preserve existing baselines and locked architectural rules. No more than one bounded implementation commit, plus evidence if needed. Do not alter automaticNextImplementationTask=NONE or start follow-up tasks. If the complete safe final-click seam cannot fit 20 minutes, stop at ANALYSIS/BLOCKED (no half-functional endpoint). Output BEGIN_TOOBA_WORKER_RESULT ... END_TOOBA_WORKER_RESULT, then STOP_FOR_ARCHITECT.
