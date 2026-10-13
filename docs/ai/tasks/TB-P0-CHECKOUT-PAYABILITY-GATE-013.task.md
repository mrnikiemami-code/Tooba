TB-P0-CHECKOUT-PAYABILITY-GATE-013

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-CHECKOUT-PAYMENT-COMMIT-BOUNDARY
Parent-Task: TB-P0-CHECKOUT-START-PAYMENT-S2-012R
Mode: BOUNDED_IMPLEMENTATION — STOP_FOR_ARCHITECT
Expected-Base-SHA: f1c050013406ce3f8c9f3bbaadbda0c26f71d0f9
Timebox: <= 20 min; one focused test invocation, no test loops

Architect decision

S2 APPROVED. Order owns checkout commit and reservation-cycle payability; Inventory owns atomic reservation; Payment owns actual payment initiation and all online/wallet/manual methods. The delivered IPaymentInitiationPort from 012R is accepted. This task ONLY closes the missing payability guard at the Payment money-entry boundary; it MUST NOT yet add or expose POST /v1/storefront/cart/{cartId}/start-payment.

Objective

Use Order-owned, canonical reservation-cycle payability inside the Payment initiation path. An expired/released/not-active reservation cycle must fail closed with a typed actionable error before any payment attempt, wallet debit, manual payment creation, or provider request. Reuse existing Order/Payment Contracts readers when safe; if not sufficient, make the smallest additive read-only Contracts seam or extension, implemented by Order. Do not duplicate the policy rules in Payment. Validate current checkout ownership, payable amount and current reservation-cycle state, including max cycle/retry constraints. Preserve existing pending-order and payment methods behavior.

Mandatory investigation before changes
Confirm HEAD==origin/main==Expected-Base-SHA (or STOP and report drift). Inspect StorefrontPaymentOrchestrator.InitiateAsync, PaymentDirectory.InitiateAsync, IPayableCheckoutReader, ReservationCyclePayabilityPolicy, StorefrontPendingPaymentProjector and expiry paths.
Establish precisely where all online, wallet, manual initiation routes pass; prove no unguarded alternative path.
Identify canonical Order owner of the decision and the transaction/locking seams. A read immediately before initiation may reduce but NOT eliminate a concurrent expiry race. Never call this atomic or race-free without a single transaction/lock/CAS proof.
Implementation limits
Max 3 production files preferred. If a correct, fully guarded change needs more, STOP and submit exact minimal proposed file set; no partial unsafe path.
Only Order/Payment module and their Contracts/tests may change. NO Host/Program/appsettings/Frontend/Cart/Inventory source, no migrations, no DB mutations, no deployment/restart, no service kill.
Do not change configurable hold durations or add an artificial minimum/floor between reservation and payment timeouts.
Preserve typed errors, existing actor/guest ownership, existing idempotency, the S2 initiation contract, and legacy shipping/commit routes.
Do NOT weaken locked guards, duplicate payability logic, or start a payment based only on a frontend-provided flag.
If payability cannot be enforced correctly across all methods without an unresolved race, implement no unsafe endpoint and report BLOCKED_ARCHITECT_DECISION with a specific design for atomic coordination. A simple check-then-initiate MUST be characterized as residual race, not an invariant proof.
Minimal verification

One focused project test run, using isolated output if needed; do not stop or overwrite the running Development Host (5088). Include cases: active cycle accepted, expired/released cycle rejected with no Payment-side side effects, manual/wallet/online covered, replay of existing initiation remains semantically correct. If unavailable, explain exact block; do not claim tests passed.

Report / stop

Provide exact base/new SHA, commit & push only scoped accepted complete changes, exact file diff, guards, executed tests and observed results, known remaining races and whether mutation endpoint can now safely follow. If blocked, give precise file:line evidence and no speculative code edits. Update only narrowly necessary evidence; automaticNextImplementationTask=NONE. STOP_FOR_ARCHITECT. Do not start next task automatically.
