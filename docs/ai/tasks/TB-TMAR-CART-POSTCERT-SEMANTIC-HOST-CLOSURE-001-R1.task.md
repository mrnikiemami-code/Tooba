PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-CART-POSTCERT-SEMANTIC-HOST-CLOSURE-001-R1
Parent-Task: TB-TMAR-CART-POSTCERT-SEMANTIC-HOST-CLOSURE-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: CART_POSTCERT_COMMERCE_AUTHORITY_REPAIR
Title: Repair Cart Commerce Context Authority
Backend-Only: YES

Architect verdict

Parent implementation is PARTIALLY ACCEPTED but parent PASS is REJECTED.

Accepted and must be preserved:

Host has zero Cart-specific implementation classes
Cart expiry worker/options are Cart-owned
Cart persistence-hours path is async
no Cart -> Host dependency
presentation hardcoded FA/EN fallbacks removed
Cart remains COMPLETE_REFERENCE_PATTERN + ARCH-COMPLETE-002 STRUCTURE_CERTIFIED
Order certification preserved
Checkout paused
frontend frozen

One semantic authority defect remains.

Current CartCommerceContextResolver resolves:
Market from ICurrentCommerceContext.Current.Tenant.DefaultMarketReference with Cart fallback
Currency from CartCommerceDefaultsOptions.DefaultCurrency
SalesChannel from CartCommerceDefaultsOptions.DefaultSalesChannel

CartCommerceDefaultsOptions.DefaultSalesChannel also defaults in code to SalesChannel.Direct.

Cart therefore still owns/invents commerce defaults instead of consuming effective storefront/store commerce authority.

Scope

Repair ONLY effective commerce authority for guest-cart creation and directly related cleanup.

Do NOT reopen:
Host Cart closure
Cart expiry worker ownership
persistence-hours async repair
presentation localization repair
routes
DB schema/migrations
Order
Checkout W6
frontend

Required repair

1. Remove Cart-owned commerce policy defaults
CartCommerceDefaultsOptions must not remain authority for market, currency, sales channel.
Remove code-level SalesChannel.Direct default.
Do not replace it with Marketplace or another hardcoded channel.
Do not leave a global Cart currency knob as effective store currency authority.

2. Audit existing authoritative sources first
Inspect repository for existing authoritative storefront/store sources or contracts for Market, Currency, SalesChannel.
Inspect at minimum: ICurrentCommerceContext, tenant/store configuration, Catalog Contracts, Pricing Contracts, Offer Contracts, storefront/market settings contracts.
Reuse an existing authoritative boundary if present. Do NOT create a duplicate source of truth.

3. Correct target behavior
CreateGuestCartHandler must receive an effective commerce context authoritative for the current storefront/store: Market, Currency, SalesChannel.
Required invariants: no hardcoded market; no hardcoded currency; no hardcoded sales channel; no Cart-owned policy default; no arbitrary raw HTTP authority; different stores/tenants can resolve different effective currencies; shared-DB storefront resolution must not collapse to one global Cart currency; Marketplace / SingleStore behavior remains compatible.
If no authoritative source exists for Currency or SalesChannel, do NOT invent one inside Cart. Introduce only the smallest correctly-owned contract/seam in the proper owner boundary, with evidence.
If ownership cannot be established safely, return INCOMPLETE rather than creating another Cart default.

4. Preserve fail-closed behavior
If effective Market/Currency/SalesChannel cannot be resolved, fail closed with stable semantic codes. Do not silently fall back to arbitrary deployment defaults inside Cart.

5. Small cleanup
Remove duplicate: using Tooba.Cart.Application; currently present twice in CartModule.cs.
No unrelated formatting churn.

Durable guard

Strengthen existing Cart commerce guard to verify:
CreateGuestCartHandler contains no hardcoded Market/Currency/SalesChannel values.
Cart has no code-level guest-cart default such as SalesChannel.Direct / Marketplace.
effective currency/channel are not sourced only from a global Cart options object.
Cart still has zero dependency on Host.
prior Host closure remains intact.
Prefer structural assertions where practical.

Evidence

Create: docs/evidence/TB-TMAR-CART-POSTCERT-SEMANTIC-HOST-CLOSURE-001-R1/commerce-authority-repair.md
Document: Architect defect; authoritative sources inspected; final Market source; final Currency source; final SalesChannel source; per-store/shared-DB behavior; removed Cart-owned defaults; guard added; prior Host closure preserved.

Recovery SoT

Do not revoke Cart structural certification.
On PASS: Cart remains COMPLETE_REFERENCE_PATTERN; Cart remains ARCH-COMPLETE-002 STRUCTURE_CERTIFIED; Order remains certified; Checkout remains PAUSED_AT_SAFE_W5_CHECKPOINT; frontendFrozen remains true; record this R1 as latest post-certification repair; nextTask = USER_REVIEW_CART_POSTCERT_SEMANTIC_HOST_CLOSURE_R1; preserve USER_REVIEW_REQUIRED_BEFORE_NEXT_TMAR_WAVE.

Validation — focused only
Run: Cart commerce/create-guest focused tests; HostCartResidualGuardTests as preservation check; TmarDurableGuardTests; Cart structure gate only if touched by guard; dotnet build src/backend/Tooba.slnx.
Do NOT run broad unrelated suites.

PASS criteria

PASS only if: Cart no longer owns effective Market/Currency/SalesChannel defaults; no code-level default channel is used for guest-cart authority; effective Currency can vary by authoritative store/storefront context; effective SalesChannel comes from authoritative context/config ownership; Market is authoritative and fail-closed; Shared-DB is not reduced to one global Cart currency; no raw HTTP authority introduced; prior Host closure remains intact; Cart remains structure certified; Checkout/frontend unchanged; build passes; SoT/evidence consistent.

END_TOOBA_TASK
