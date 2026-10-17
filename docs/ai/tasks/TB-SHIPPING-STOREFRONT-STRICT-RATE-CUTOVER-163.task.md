TB-SHIPPING-STOREFRONT-STRICT-RATE-CUTOVER-163

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-STOREFRONT-STRICT-RATE-CUTOVER-163
Parent-Task: TB-SHIPPING-OPTION-COVERAGE-MIGRATION-162
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 727cdb46111446cf1a15364de73ebacb03660e5f
Program: TOOBA-SHIPPING-FLAT-METHOD-COMMERCIAL-READINESS
Mode: BOUNDED_OWNER_CORRECT_DEFAULT_OFF_STOREFRONT_RATE_INTEGRATION
Timebox: HARD 45 minutes; STOP_FOR_ARCHITECT on expiry, even if PARTIAL.

Mission

Connect the customer-facing shipping quote used by Order/Storefront to the existing Shipping-owned quote port with ShippingOptionMatchMode.StrictOwnedOption, under a new, explicit default-OFF production gate. Do not enable the gate in shared/staging/production or modify the legacy behavior when OFF. Price for tipax:standard must originate from an active exact-option Shipping-owned rule, not from Tooba:ShippingMethods, tipax:express, or service-level provider tariff. Preserve server-owned per-group dates, IDs, quantities, fees and Payment draft consistency from TB-149..154. This is a working software increment, not another report-only task.

Before modifying
Verify HEAD == origin/main == Expected-Base-SHA and preserve existing unrelated dirty files/stashes. Read docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-16.md, locked SHIPPING-ADMIN-FLAT-METHOD-CONTRACT.md, SHIPPING-GROUP-INDEPENDENT-DELIVERY-COMMITMENT.md, TB-159 owner blocker and TB-160..162 actual source/evidence.
Trace existing StorefrontShippingService -> calculator -> Order cart draft/payment handoff; inspect IShippingRateQuoteLookup, ShippingQuoteRequest, ShippingOptionMatchMode, actual result/outcome fields, coverage declarations and the ShippingRatePolicyEngine. Verify existing DI and permitted Contracts-only dependency. Do not duplicate Shipping business decisions in Order.
Implementation, strictly bounded

A. Add a dedicated typed config switch for Order's authoritative Shipping-owned customer rate (default false in every appsettings / absent-is-false), separate from the existing observe-only Tooba:ShippingRateAuthority and operational shadow. OFF: exact existing rates, errors, DTOs and runtime paths; no hidden traffic or quote lookup if OFF.
B. ON: use existing Shipping Contracts quote lookup for each actual canonical method + seller group + authoritative destination, request StrictOwnedOption, and map only an IsQuoted successful authoritative amount/currency into the existing quote and draft selection paths. Do not let fee calculated by the old options override an authoritative quote downstream. Preserve method/seller and line isolation and accepted-order/payment aggregates. Keep discount/tax/settlement unchanged.
C. Make fail-closed mapping explicit for missing owned rule, Restricted coverage denied, ambiguous/conflicting rule, currency mismatch, stale/inactive row, and conditioned rule without trusted measurement (MeasurementRequired). Never guess parcel weight/dimensions from customer text, never treat zero/missing quote as FREE, never silently fall back to generic or provider rate when ON. If caller cannot provide required trusted measurement, expose the stable unavailable reason and prohibit choosing/continuing with that method rather than crash or fabricate a fee.
D. Implement no new Shipping rate engine, no new Shipping Admin writer, no new schema/migration and no Host DbContext/business pricing. Prefer existing interfaces and capability wiring. If an actual contract cannot safely expose the method option identity/quote scope or if eligible method filtering would leak invalid choices, STOP with a precise partial and minimal owner-first fix; don't bypass.
E. Add narrowly focused tests proving: OFF parity and zero calls to owner; ON exact tipax:standard amount (example 205000 IRR, fixture-only) never inherited by tipax:express; legacy shared-only row unquotable; Unrestricted absent declaration allowed only with owned rate; Restricted grants enforced; MeasurementRequired fails closed; two sellers/same method remain independent with group charges exactly once; invalid rate cannot enter draft/payment; failure does not corrupt prior saved shipping selection.

Real acceptance (only if safe in remaining time)

Use the TB-157/158 reusable private harness and TB-154 authenticated customer fixture on a disposable loopback Host/DB/frontend, with Shipping migrations applied through the product migration runner. Seed exact option-owned active rate plus applicable explicit destination coverage/restriction and tax fixture as needed, not fake service-wide fallback. In this scratch ONLY enable the new gate. Show authenticated customer shipping projection -> selection save -> GET reread -> read-only payment draft using real price and group fees; verify sibling no-quote, fail-closed negative cases and no checkout/order/payment before final click. Capture sanitized JSON/DB readback (browser screenshots only if genuine). Compare OFF behavior in a separate private run or deterministic test. Never call PSP/SMS or make a final purchase.

Invariants / exclusions
No changes or restart to shared :3000, :5088, :5432, tooba_alpha, shared tenant store-alpha, RabbitMQ, Redis, or unrelated workers; no shared migrations, cleanup by explicit task-owned PID/container only.
No bypass of customer auth or Admin authorization, no invented customer quote/discount/payer, no seller/platform/carrier allocation policy, no Case-D origin workaround, no unconditional feature enablement.
Contracts-only cross-module; MediatR/ISender and Result conventions, no Results.Json, no Host business authority. Source-size/architecture guards respected. Avoid lengthy unrelated suites and repeated retries.
Do not change the TB-160 LegacyFallback default semantics or TB-161 scoped-coverage semantics.
Evidence and finish

Record implementation SHA, focused tests/build, exact mode flag and OFF default, quote provenance matrix, isolated HTTP results if achieved, exact unverified items, cleanup, and PASS/PARTIAL/BLOCKED with honest reasons in docs/architecture/evidence/TB-SHIPPING-STOREFRONT-STRICT-RATE-CUTOVER-163/. Update planning blocker only for capabilities actually proven. Commit/push only scoped work, verify HEAD == origin/main, emit BEGIN_TOOBA_WORKER_RESULT ... END_TOOBA_WORKER_RESULT, STOP_FOR_ARCHITECT, Automatic-Next-Task: NONE.
PASS requires default-OFF parity + focused fail-closed owner quote tests + genuinely persisted correct customer quote into selected shipping fee/payment draft in isolated authenticated runtime. Otherwise PARTIAL, explicitly distinguishing tested from unverified. No automatic follow-up.
END_TOOBA_TASK
