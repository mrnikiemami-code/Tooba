PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-STOREFRONT-RATE-AUTHORITY-159
Parent-Task: TB-SHIPPING-ADMIN-BROWSER-QUOTE-ACCEPTANCE-158
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA-SHIPPING-FLAT-METHOD-COMMERCIAL-READINESS
Mode: BOUNDED_OWNER_CORRECT_STOREFRONT_RATE_AUTHORITY_INTEGRATION
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: bfb9245bb6fce0f8a4b65b40c68090879413e667
Timebox: 45 minutes HARD
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

MISSION
Close the proven TB-158 commercial defect: Shipping-owned, method-specific fixed IRR rate saved by Admin does NOT currently price a customer shipping quote; Order storefront still uses Tooba:ShippingMethods configuration. Implement the smallest reviewed, owner-correct customer pricing seam for explicitly enabled Shipping-owned rate authority, and prove that the same tipax:standard owned rate visible in Admin is the amount quoted to an authenticated customer's corresponding shipping group. Preserve legacy default behavior when the authority feature is not enabled; do not silently convert missing/conflicting rates into zero/free or borrow a sibling's/provider's rule.

REQUIRED FIRST INSPECTION

Read docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-16.md, docs/architecture/shipping/SHIPPING-ADMIN-FLAT-METHOD-CONTRACT.md, docs/architecture/shipping/SHIPPING-GROUP-INDEPENDENT-DELIVERY-COMMITMENT.md and TB-155 through TB-158 result/evidence, especially storefront-quote-provenance-158.md. Confirm exact call chain, Shipping module owned query/quote contracts, current feature-gate semantics, Admin writer and Order adapter.
Confirm HEAD == origin/main == Expected-Base-SHA; preserve existing unrelated dirty paths and running shared services. If divergent or unsafe: BLOCKED; do not rebase/reset/stash/force-push.

IMPLEMENTATION BOUNDARY

Shipping module remains sole pricing/rate/coverage authority. Order may depend ONLY on a Shipping.Contracts read/quote abstraction; DI composition in Host only. No reference to Shipping.Infrastructure/Application/DbContext from Order; no business policy added to Host. Reuse a verified, supported existing contract and adapter if one exists; introduce the smallest additive Contracts-owned API otherwise, using existing CQRS/MediatR 12.5 ISender + Result patterns.
Evaluate the EXACT canonical method identity and seller/destination/weight/other validated applicable scope of the actual customer group. An Admin financial-status 'FinanciallyConfigured' flag or a single stored amount is NOT necessarily a customer quote: do not choose a country/weight/coverage row incorrectly or invent arbitrary matching. If selection semantics are not implementable safely in this timebox, stop PARTIAL with a precise blocked shape; do NOT ship an incorrect price.
If an authority gate already exists (Tooba:ShippingRateAuthority), inspect its meaning, defaults and shadow/observer topology. Explicitly enabled authoritative path must use owned Shipping pricing; disabled/default path must remain byte-/behavior-compatible with existing Tooba:ShippingMethods prices. Never silently enable authority globally. Keep observer-only shadows read-only; never confuse them with production authority.
Absent, disabled, ambiguous, mixed-currency, out-of-scope, zero-stored-but-unapproved-free, unauthorized or failed Shipping pricing MUST be fail-closed in the enabled authority mode (method unavailable or explicit stable quote failure, as existing contracts permit). No fallback to legacy configuration or sibling/service-level tariffs in that mode. Support legitimate zero only if approved contract explicitly permits charging zero; never label it FREE merely due to stored zero.
Preserve independent per-SellerPartyId + per-method group quote amounts in ShippingGroup snapshots and aggregate once in payment draft. Do not change per-group dates, identity, membership, save/restore, payment/settlement share policy, taxes or final-click/PSP behavior.
Avoid speculative schema or migration changes. No hardcoded seller ids, prices, fee formulas, method codes beyond named proof fixture, or false readiness indicators. Keep RTL/visual design unchanged.

PROOFS / ACCEPTANCE
A. Deterministic focused contract/owner tests: enabled authority with a real applicable tipax:standard owned 205000 IRR row quotes 205000 for that method, not legacy configuration; tipax:express cannot inherit it; nonexistent/conflicting/not-covered owned rate fails closed; disabled gate preserves legacy amount exactly; no cross-seller method leakage. Verify per-group sum is charged exactly once and read-only payment draft is consistent. Include authorization/tenant isolation where applicable.
B. Private authenticated end-to-end evidence if safely attainable within 45 minutes: reuse TB-157/TB-158 private harness recipes, scratch Shipping/Geography migration + fixture, true authorized Admin create/edit, authenticated customer two-seller cart shipping projection + save/read + payment draft. Observe owner rate amount in the applicable customer group(s); preserve group identities and the unrelated sibling; no real payment. If scratch customer auth/fixture cannot be established, mark PARTIAL, include precise request/response and fail reason; NEVER call it PASS solely from tests.
C. Build essential changed projects, run focused Order + Shipping tests and frontend typecheck ONLY if frontend touched. No broad slow guard loops. Report exact test commands and pass/fail counts and pre-existing failures separately.
D. Production source diff and evidence under docs/architecture/evidence/TB-SHIPPING-STOREFRONT-RATE-AUTHORITY-159/. Record default-off compatibility, explicit-on result, price provenance, and no premature checkout/payment mutation.

HARD SAFETY
Do not touch shared :3000/:5088/:5432, tooba_alpha/store-alpha live data, RabbitMQ/Redis, shared migrations/containers or unrelated dirty files. Scratch resource names/ports only; fail-closed target assertions; no secrets/tokens in evidence; path-matched cleanup. No real SMS, PSP, carrier booking or settlement payout. No fake screenshots, invented prices or fabricated PASS. No duplicate writer API, no provider-level tariff inherit, no Host DbContext/business logic, zero Results.Json.

STOP CONTRACT
PASS only if customer-facing authoritative pricing and default-off legacy compatibility are both tested and actual private customer quote/write/read/payment-draft proof succeeds. PARTIAL if source/tests are correct but runtime proof remains incomplete or semantic scope lacks authority. BLOCKED if safe pricing semantics cannot be established. Always commit/push safely if applicable, provide code/evidence SHA, HEAD/origin parity, scoped diff, tests, limitations and STOP_FOR_ARCHITECT. No automatic next implementation task.
END_TOOBA_TASK
