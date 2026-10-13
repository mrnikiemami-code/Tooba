TB-P0-PAYMENT-FRESH-ENTRY-GUARD-023

Task-ID: TB-P0-PAYMENT-FRESH-ENTRY-GUARD-023
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-CHECKOUT-PAYMENT-COMMIT-BOUNDARY
Parent-Task: TB-P0-PAYMENT-ENTRY-RESERVATION-GATE-022
Mode: BOUNDED_IMPLEMENTATION_OR_BLOCKED
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 8187ddde120918f22e31bac6bc80ea89dfe0cbc5
Repository: mrnikiemami-code/Tooba
Branch: main
Timebox: 20 minutes HARD; no automatic next task.

MANDATORY PREFLIGHT
Confirm HEAD == origin/main == Expected-Base-SHA; otherwise STOP with exact drift. Read AGENTS.md, pipeline protocol, current recovery, and 017-022 evidence. Do not re-run analysis already recorded in 022. Do not modify/rebuild/restart/deploy Host production, Program.cs, active :5088, live DB, frontend, settings, migrations, or payment providers. Host.Tests should not be built or executed. Preserve unrelated dirty files. No PSP/wallet calls and no actual orders.

ARCHITECT DECISION
The old POST /v1/storefront/checkout/{checkoutId}/payments is a real money entry and MUST fail closed for fresh money attempts without active, supply-backed reservation payability. No blind check based on Order cycle.status only. Preserve exact existing-key replay when a payment already exists (including succeeded/failed semantics), authorization first, and 021 cross-checkout/cross-provider key conflict. A payment RETRY is not the same as a reservation RENEWAL. Existing Order unpaid-retry path and configurable renewal/quota rules are authoritative; do not invent fixed retry caps, prices or TTLs. Do not call guest-dependent 018 PrepareAsync from PaymentDirectory without guest proof. Do not initiate a PSP under database row locks or multi-DbContext TransactionScope.

IMPLEMENTATION SLICE
Implement the smallest safe, strongly typed fresh-money gate by extending an EXISTING Order->Payment read projection/port, preferably IPayableCheckoutReader/GetPayableAsync and PayableCheckoutSnapshot (avoid required PaymentDirectory constructor parameter and eight Host.Tests positional call sites). Order-owned implementation must derive authoritative persisted checkout reservations and check Order cycle using canonical payability policy, and Inventory held/unexpired supply via its owner-provided read port. Preserve payment verification/read authorization after cycle expiry: expose payability AS DATA and enforce at fresh initiation, never throw from GetPayableAsync solely because cycle expired. Also consider the existing FAILED-payment replay/retry branch, which can initiate again and MUST NOT bypass the guard; same-key succeeded/pending benign replay remains readable. All wallet/manual/online must pass the same true new-money check. If the existing supply read cannot provide a truthful all-lines snapshot without a race or if a safe retry/renewal contract needs more scope, STOP with exact implementation blocker; NEVER publish an optimistic gate.

ERROR SURFACE DECISION
Authorize an additive dedicated Payment-owned not-payable 409 code if necessary, with en/fa resources and catalog descriptor; no generic/platform.unexpected 500 and no semantic repurposing of payment.unpaid.supply_unavailable. Authorize count-only alignment in ONLY the two already-permitted Host.Tests architecture guard files, WITHOUT deleting/weakening tests. Do not edit any other Host file. Historical tmar R1/R2/R3 evidence must remain immutable; if recovery current truth needs an additive R4, document for separate governance follow-up rather than altering history.

SAFETY/VERIFICATION
No new public endpoint and no frontend change in this task. Tests focused in Order.Tests/Payment.Tests using fakes, no Host builds. Prove: valid Held active cycle -> fresh initiation permitted; expired cycle -> no PSP, no Payment/Attempt row; cycle Active but hold Released -> rejected; no cycle -> explicit policy-safe outcome, never silent bypass; wallet/manual same gate; existing paid replay and 021 conflict unchanged; failed-prior retry can't bypass fresh-money rule; legitimate configured supply reacquisition remains available where canonical retry already supports it. Declare TOCTOU limits and verify whether Order can fetch actual Inventory row state without misrepresenting it as atomic. If tests cannot prove these claims in time/scope, implement no publicly reachable change and report BLOCKED with exact minimal next files.

OUTPUT
Commit only clean bounded changes, push, report SHA, actual diff, focused tests, remaining risks, Host untouched, STOP_FOR_ARCHITECT and automaticNextImplementationTask=NONE. 20-minute stop is strict; if incomplete, leave no unsafe partial call path.
