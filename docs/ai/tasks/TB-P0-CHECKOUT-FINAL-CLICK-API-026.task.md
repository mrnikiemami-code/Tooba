TB-P0-CHECKOUT-FINAL-CLICK-API-026

Task-ID: TB-P0-CHECKOUT-FINAL-CLICK-API-026
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-CHECKOUT-PAYMENT-COMMIT-BOUNDARY
Parent-Task: TB-P0-PAYMENT-FRESH-GATE-WIRING-025
Mode: BOUNDED_IMPLEMENTATION_OR_BLOCKED
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 27ff280320ec5b510e3934953538f28164bf06d4
Repository: mrnikiemami-code/Tooba
Branch: main
Timebox: 20 minutes HARD
automaticNextImplementationTask: NONE

Mandatory preflight
Verify HEAD == origin/main == Expected-Base-SHA, else STOP; no rebase. Read AGENTS.md, protocol, task results 019/021/024/025, existing Order storefront endpoint conventions and the composer. Preserve all unrelated dirty files. Host CLOSED: no changes to src/backend/Host/** (including Host.Tests), no Host build/tests, no Program.cs/config, no stop/restart/deploy of :5088. No live DB write, migration, payment, PSP/wallet call. Do not change Inventory/Payment business rules, TTLs, retry quotas or product policy.

Mission — make the final-click backend API reachable safely using the already ACCEPTED Order StorefrontStartPaymentComposer from 019, while keeping existing routes untouched. Prefer new endpoint under Order.Endpoints: POST /v1/storefront/cart/{cartId:guid}/start-payment with request body selected provider/useWallet, expectedCartVersion, coupon and stable idempotency key from existing header convention. Identity and guest proof MUST be derived from the canonical storefront actor/guest pattern; never trust a client-provided AuthenticatedUserId, CheckoutId, amount, ReservationId, deadline, or actor. Respect canonical Order endpoint auth/rate-limit/anti-abuse and guest proof. The composer must receive the authenticated actor from trusted context only. Return checkout/payment/redirect with safe errors; never leak guest secrets or internal ids beyond existing accepted public DTO patterns.

Safety decisions

Final click only commits Cart; merely visiting Shipping or Payment or selecting a method must not commit (frontend change explicitly OUT OF SCOPE here).
Call existing composer exactly once. Never create another checkout/reservation algorithm or Payment bypass. Idempotency key must be stable, bounded and compatible with the existing composer.
Map composer InvalidRequest, CheckoutCommitFailed, HoldPreparationFailed to meaningful, existing registered, localized 4xx error codes using existing catalog conventions. Do not forge unknown code, emit generic 500, or repurpose semantically unrelated code. If a new error is necessary, STOP with precise minimal file list; never expose half-safe endpoint.
Preserve already committed CheckoutId when a subsequent hold preparation fails; clients must be able to retry the SAME persisted order via the established key. Don't auto-cancel or recreate. Payment errors stay Payment-owned. Avoid publishing a partially operational route.
Defend double-click, foreign cart/guest proof, method-change under same key (021), invalid/expired hold (025), and absent method. Do not invent a universal fixed 3-cycle limit. No automatic reacquisition here; report why renewal must be a separate canonical existing action.
The composer 019 currently may resolve the selector 'gateway' policy before Payment resolves the concrete provider; verify this policy consistency and do not allow initiation with an undersized hold versus selected provider's policy. If unsafe, STOP with pinpoint evidence rather than enabling an endpoint.
Existing public POST /v1/storefront/checkout/{checkoutId}/payments stays guarded by 025. No Payment code edits or endpoint removal in this task.

Budget: Prefer <=3 production files in Order.Endpoints/Order.Application. Only focused Order module tests and endpoint route/source guards; no Host.Tests. Before publishing, verify DI composition with module-level tests, absence of Host edits, and no unknown error-code mapping. If correctness cannot be established in 20 minutes, STOP, commit only safe inert groundwork and report precise blocker. Do not start the frontend task.

Worker result
BEGIN_TOOBA_WORKER_RESULT / END_TOOBA_WORKER_RESULT. Include initial/final SHA, exact diff, test counts, whether endpoint publicly registered, identity source, method policy consistency, failure codes and statuses, lost-response replay, residual risk, Host CLOSED. STOP_FOR_ARCHITECT.
