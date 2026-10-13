TB-P0-CHECKOUT-PAYMENT-DRAFT-011

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-CHECKOUT-PAYMENT-COMMIT-BOUNDARY
Parent-Task: TB-P0-CHECKOUT-PAYMENT-BOUNDARY-ANALYZE-010
Mode: BOUNDED_IMPLEMENTATION — STOP_FOR_ARCHITECT
Timebox: 15–20 minutes, one focused test run
Repository: mrnikiemami-code/Tooba
Expected starting SHA: 207dae32fd899584d8057405901593aecf222e45

Task

Implement the smallest read-only, pre-order payment-draft projection for an ACTIVE cart with saved shipping selection, without requiring a checkoutId. Use established module contracts and endpoints registration patterns; make no Host-root changes. Preserve all existing settings-derived hold durations and their scope/method overrides. Compose only data actually available from existing read contracts (cart/version/currency, persisted shipping selection and authoritative pricing, eligible payment methods and wallet quote if safe to compute before an Order). If any field requires order/checkout creation or new cross-module authority, OMIT it or report BLOCKED with exact file:line evidence; never fabricate a quote. Keep frontend and existing payment/checkout flows unchanged in this task.

Safety invariants
GET causes zero writes to Cart, Order, Inventory, Payment and quota tables; no Reservation, CheckoutGroup, SellerOrder, Cart Converted or Payment Attempt may be created.
Guest/auth ownership and tenant boundaries are enforced using existing read contracts; no loose cartId enumeration.
No static hold times, no cross-duration floor, no changes to Catalog/Store/Offer/Payment overrides.
No edits to InventoryDirectory, CheckoutProcessManager, PaymentDirectory, existing /shipping/commit endpoint, Host Program/root, DB schema/migrations.
Do not assert TransactionScope guarantees cross-module ACID without a behavioral proof; defer that question explicitly.
Preserve baseline and existing untracked evidence. No mass formatting, resets or unrelated cleanup.
Execution
Verify SHA and working-tree content diff; STOP on mismatch.
Identify minimal existing Cart/Shipping/Payment read contracts with file:line citations.
Add minimal CartId-addressed GET payment-draft query + endpoint in owning feature/module only. Validate actor/guest access; use effective settings read-only. Return clear typed errors for missing/expired cart or shipping draft.
Run one focused test proving Active cart + shipping draft yields a draft without checkoutId, and absence of Checkout/Reservation/Conversion/quota writes. If proving no-writes requires an integration DB test, use one focused test and no broad suites.
Report actual changed files, SHA, test command/result, endpoint shape, limitations, and explicit STOP. Do not begin follow-up task or trigger checkout/payment operations.
Acceptance

A read-only payment draft exists for an active cart without first creating an Order; existing checkout behavior is unchanged; no Host changes, no schema changes, no live data mutations; no settings regressions. If this cannot be achieved with minimal current contracts, report BLOCKED and suggest narrowest missing seam rather than expanding scope.

automaticNextImplementationTask=NONE
STOP: USER_REVIEW / ARCHITECT_ACCEPTANCE
