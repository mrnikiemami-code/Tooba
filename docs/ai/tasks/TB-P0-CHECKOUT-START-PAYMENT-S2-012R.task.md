TB-P0-CHECKOUT-START-PAYMENT-S2-012R

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Repo: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-CHECKOUT-PAYMENT-COMMIT-BOUNDARY
Parent-Task: TB-P0-CHECKOUT-START-PAYMENT-012
Mode: BOUNDED_IMPLEMENTATION — STOP_FOR_ARCHITECT
Timebox: 20 minutes HARD STOP

Architect decision (approved)

S2: Order owns the cart-to-checkout final-click command; Payment owns initiation/charge, exposed to Order ONLY through a minimal interface and DTO(s) in Tooba.Payment.Contracts. Explicit authorization for this limited Contracts change. No direct Order -> Payment.Application/Infrastructure references, no Host composer changes. Cart/Inventory/Payment domain authority stays unchanged.

Preflight / STOP gates
Read AGENTS.md, pipeline protocol, recovery context and the 011/012 evidence, inspect current HEAD and diff. The observed GitHub HEAD at issuing time is 77c28e62fcf01251ae15ea36eb5f369aea4518a4 (docs-only follow-up to 012). Do not hard reset to it if repo has since advanced; compare and STOP on unexpected source drift or substantive dirty tracked changes. Do not overwrite existing work.
Locate real runtime call graph and determine actual type signatures. Never invent interface calls, bypass actor ownership or loosen validators.
Confirm the one-call orchestration is feasible under 20 min. If not, implement ONLY the minimal Payment.Contracts initiation port + Payment adapter and its focused tests (no published mutation endpoint), report remaining work and STOP; no half-safe route.
Implementation target
Create a narrow IPaymentInitiationPort in Payment.Contracts with strictly necessary request/result DTO(s), implemented by Payment infrastructure/application adapter invoking existing StorefrontPaymentOrchestrator initiation. Preserve online, wallet, manual behavior, provider eligibility, actor and idempotency checks. Use existing module DI registration; do not touch Host.
Order-side final-click POST /v1/storefront/cart/{cartId:guid}/start-payment ONLY if it can be completed safely: validate Cart ownership, guest secret, shipping draft and expected version; commit via existing canonical shipping/checkout process once; verify Order-owned reservation-cycle Payability before starting money movement; initiate chosen method via Payment.Contracts port.
Request replay for a Converted Cart MUST recover its OWN persisted Checkout via immutable cartId + original actor/guest proof and same cart-scoped key, not blindly call LoadActiveCartAsync; different actor must fail closed. Multiple clicks/concurrent requests must not create multiple checkouts, holds or charges. Do not treat SQL unique-key conflict as successful replay without verifying original request identity.
If order commit succeeds but initiation fails, preserve the same PendingPayment checkout with a safe, discoverable retry; never repeat reservation or quota. If commit fails, Cart stays Active and no orphan reservations. No silent compensation, no invented cross-DbContext ACID guarantee.
Do not conflate the 1-minute dev override and two-hour payment timeout. All durations are configurable via existing Store/Offer/Category/Method policy precedence; do not add hardcoded values, cross-duration floor or settings changes.
The existing /shipping/commit and checkoutId-based endpoints remain backward-compatible. No Frontend cutover in this task.
Guard ordering: Payment initiation must not occur until successful checkout commit AND active-cycle Payability; account for race between checking cycle and beginning payment. If code cannot enforce the invariant with existing seams, STOP with a precise proposed remedy rather than asserting safety.
Tests (minimal, no repeats)
One focused Order/Payment module test command only, not full solution or Host.Tests. Verify actor rejection; successful one-shot; same-key replay after commit; initiation failure then retry same checkout; fail-closed expired cycle. If scope limits, test only the port you actually implement and clearly mark uncovered cases.
Real PostgreSQL atomicity test already exists in CheckoutOrderFoundationTests but could not run because the Development Host locks output DLLs. Do NOT stop Host or retry this build in a loop. Explicitly report this as UNVERIFIED runtime evidence. Do not claim cross-module TransactionScope proof based merely on source.
Constraints

No Host edits, no database writes/migrations, no service restart, no frontend changes, no actual payment/order calls, no AMSC sweep, no unrelated fixes, no repeated tests. Preserve baseline and automaticNextImplementationTask=NONE.

Deliverable

Report exact HEAD before/after, files/diff, Contracts seam, idempotency and ownership proof, how expired-hold race is handled, exact tests run and results, and explicit ACCEPTANCE_REQUESTED or BLOCKED_ARCHITECT_DECISION. Commit/push only completed safely-scoped changes; STOP for Architect review. Do NOT autonomously start next task.
