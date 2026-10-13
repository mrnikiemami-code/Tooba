TB-P0-CHECKOUT-FINAL-CLICK-API-028

Task-ID: TB-P0-CHECKOUT-FINAL-CLICK-API-028
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-CHECKOUT-PAYMENT-COMMIT-BOUNDARY
Parent-Task: TB-P0-CHECKOUT-PROVIDER-ALIGNMENT-027
Mode: BOUNDED_IMPLEMENTATION_OR_BLOCKED
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 20b7c0627972dd27337ec1e1091a5508f7308db2
Repository: mrnikiemami-code/Tooba
Branch: main
Timebox: 20 minutes HARD
automaticNextImplementationTask: NONE

Mandatory preflight
Verify HEAD == origin/main == Expected-Base-SHA. On drift STOP; no rebase/stash. Read AGENTS.md, BRIDGE-WAKE-V1 and accepted 019, 025, 026, 027 outputs. Read existing Order storefront endpoint organization/route tests before modifying. Preserve all unrelated dirty files.

Mission — ONE public final-click endpoint, Order-owned
Expose POST /v1/storefront/cart/{cartId:guid}/start-payment under Tooba.Order.Endpoints, delegating to the accepted StorefrontStartPaymentComposer (019, 027). No frontend changes in this task. The cart remains Active across Shipping -> Payment entry; final click alone invokes commit. Never implement reservation/order/cart/Payment internals again.

Security and API contract

Derive authenticated actor identity ONLY from the canonical storefront checkout identity gate / trusted HttpContext, not caller JSON. Match existing Order guest proof convention (X-Tooba-Guest-Secret or cookie) and canonical cart authorization. Enforce CartId route match and never accept a caller CheckoutId/ReservationId/Amount/Deadline or identity in body.
Require stable Idempotency-Key and selected payment method, ExpectedCartVersion and optional CouponCode using existing endpoint conventions. Handle replay of Converted cart through Composer canonical process tracker and existing ownership; do not require active Cart on replay.
Use 027 provider resolver BEFORE commit, preserve selected original selector into Payment, canonical concrete provider into 018 hold preparation; do not recreate catalog/policy.
Only a successful Composer Initiated is HTTP success. For InvalidRequest and HoldPreparationFailed select genuine domain-owned, localized, registered 4xx mappings; never invent raw error code, mask 409 as 200, or leak platform.unexpected 500. Producer-owned CheckoutCommitFailed codes and Payment exceptions preserve existing typed boundary.
A committed Order must remain recoverable if hold preparation or initiation fails, and API response should retain authorized checkout reference when safe. Never auto cancel or re-commit. Confirm two-click same key does not duplicate checkout/payment.
Do not claim that read-payability removes TOCTOU; 025 enforces the current money gate. Do not alter retry/renewal counts, expired-stock reacquisition, hold durations or platform/store/provider settings.

Explicit authorization to transition TEMPORARY 019 guard (NOT a blanket weakening)
Change ONLY StorefrontStartPaymentComposerGuardTests.No_module_or_host_route_publishes_start_payment from an inert-phase prohibition to a durable assertion that exactly ONE approved final-click route exists in Order.Endpoints and ZERO such routes or composer references are in Host. Keep Composer_publishes_no_http_endpoint_and_stays_inert and its other architecture guards intact. Update any Order storefront route organization/ISender guard narrowly for the new Order-owned service handler; preserve prior route counts and invariants, make the new route explicitly and positively asserted; do not delete, skip or loosen unrelated guards. If existing certification cannot be transitioned with only Order-module tests, STOP and report exact blocker.

STRICT Host CLOSED
ABSOLUTELY NO changes/build/test/restart/stop/deploy under src/backend/Host/** including Host.Tests; no Program.cs, no live Host :5088 contact, no live DB writes, no migration/schema/config/worker/PSP/wallet action. No frontend edits. No changes to Inventory/Payment money logic. Do not run Host.Tests or solution-wide build. New endpoint MUST be discoverable through existing Order endpoint registration, not through an edit to Host.

Focused verification
Run only focused Order module endpoint tests plus Order architecture guards, minimal builds if required. Verify authenticated/guest identity, malicious identity/body rejection, invalid selector causes no commit, success returns checkout+payment reference, replay uses same keys and same ownership, all non-Initiated outcomes return correct 4xx, and no Host edits. No real PSP/database mutation. If robust response mapping cannot be completed in time, DO NOT publish a partially safe HTTP endpoint: STOP with a precise blocker and changed-file list.

Deliverable and stop
Commit/push only permitted files, capture exact initial/final SHAs, changed paths, actual test counts, route registration evidence, guard changes, residual risks. Return BEGIN_TOOBA_WORKER_RESULT / END_TOOBA_WORKER_RESULT. STOP_FOR_ARCHITECT; never auto-run next task.
