PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-CART-MULTICURRENCY-LINES-001-R1
Parent-Task: TB-TMAR-CART-MULTICURRENCY-LINES-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: CART_MULTICURRENCY_ORDER_COMPAT_REPAIR
Title: Repair unsafe Order compatibility after Cart multi-currency slice
Backend-Only: YES

Architect verdict

Parent is NOT YET ACCEPTED.

Verified defect on main:

StorefrontShippingService currently sums cart.TotalsByCurrency.Sum(...); this can add unlike currencies.

Order checkout paths use cart.DefaultCurrency as transaction/order/pricing currency even when the sole CartLine currency differs.
These are semantic defects, not mechanical compile edits.

Closed decision

Order multi-currency remains deferred.

Until its dedicated wave:

Order shipping/checkout may proceed ONLY when all Cart lines have exactly one distinct non-empty QuotedCurrency.

that sole line currency is the effective Order/Checkout currency.

Cart.DefaultCurrency is NEVER Order transaction authority.

mixed-currency Cart fails closed BEFORE shipping arithmetic, repricing, reservation/order persistence or payment-facing flow.

no FX, split-order or payment-group design here.

Stable error:
checkout.multicurrency.not_supported

Use existing typed StorefrontOrderException / StorefrontOrderErrors.

Exact files

Primary:

src/backend/Modules/Order/Tooba.Order.Application/Storefront/StorefrontOrderErrors.cs

src/backend/Modules/Order/Tooba.Order.Application/Storefront/Services/StorefrontShippingService.cs

src/backend/Modules/Order/Tooba.Order.Application/Storefront/Services/StorefrontCheckoutService.cs

src/backend/Modules/Order/Tooba.Order.Infrastructure/Checkout/Persistence/CheckoutDirectory.cs

src/backend/Modules/Order/Tooba.Order.Infrastructure/Checkout/Persistence/CheckoutSubmitHost.cs

Tests/guards only as required.

Do NOT touch Cart production code unless unavoidable compile fallout.
Do NOT redesign Order.
Do NOT resume Checkout W6.
Do NOT modify Payment or Pricing contracts.
Do NOT touch frontend.

Required repair
A. Typed error

Add:
StorefrontOrderErrors.CheckoutMultiCurrencyNotSupported = "checkout.multicurrency.not_supported"

No message parsing. No localized prose in Domain/Application.

B. Small Order-owned compatibility helper

Add one cohesive helper under existing Order Storefront capability, e.g.
StorefrontCartCurrencyCompatibility.cs

Responsibilities only:

inspect CartPage/CartSnapshot line currencies

require non-empty line currency

require exactly one distinct currency using ordinal comparison

return sole currency

mixed/missing => typed StorefrontOrderException

NEVER fall back to DefaultCurrency

No shipping/pricing/payment policy inside helper.

C. Shipping

REMOVE any cross-currency sum such as:
cart.TotalsByCurrency.Sum(...)

Before shipping subtotal/rate arithmetic:

require single currency

take only matching TotalsByCurrency entry

if totals inconsistent, fail closed

StorefrontShippingProjection may remain single-currency only because mixed carts are rejected.
Its currency must be sole line currency, not DefaultCurrency.

D. Storefront checkout

Before Preview/Submit enters Order checkout:

require single line currency

reject mixed currencies with stable error

never treat DefaultCurrency as transaction currency

Persisted checkout stub may use persisted Order snapshot currency.

E. CheckoutDirectory

For CartSnapshot:

derive sole line currency from CartLineSnapshot.QuotedCurrency

use it for:

CheckoutGroup currency

SellerOrder currency

FinancialRounder.MoneyPlaces

campaign/base Pricing selector

REMOVE transaction use of cart.DefaultCurrency

mixed/missing line currency => fail closed before quote/persist

F. CheckoutSubmitHost

Persist using same effective sole line currency from Cart lines.
Do NOT use cart.DefaultCurrency.
Reuse same compatibility logic; no duplication.

Preserve Cart

Do NOT change:

optional requested AddLine currency

ShoppingCart.DefaultCurrency

CartLine.QuotedCurrency authority

CartPage.TotalsByCurrency

CartSnapshot.DefaultCurrency

cart DB column/schema/migrations

Pricing contracts

Cart remains mixed-currency capable; only Order boundary rejects mixed carts for now.

Tests

Prove:

USD-only cart shipping uses USD total, never DefaultCurrency.

IRR-only cart uses IRR.

USD+IRR cart fails checkout.multicurrency.not_supported.

checkout preview/submit mixed cart fails before Order persistence.

single-currency cart with DefaultCurrency != line currency uses line currency.

CheckoutDirectory reprices using line currency.

SellerOrder/CheckoutGroup currency comes from sole line currency.

missing line currency fails closed.

no TotalsByCurrency.Sum remains.

no Order transaction path uses cart.DefaultCurrency as currency authority.

Cart tests remain green.

Cart/Order/StoreContext structure certifications remain green.

Guards

Add/strengthen focused guard proving:

no TotalsByCurrency.Sum in Order shipping

no Order pricing/order currency authority from cart.DefaultCurrency

typed mixed-currency fail-closed exists

no Host business implementation added

Preserve all Golden locks including ARCH-COMPLETE-001/002, ARCH-CQRS-001/002, ARCH-VAL-001, ARCH-CONTRACT-001, ARCH-HOST-001, ARCH-DB/DATA/READ, ARCH-SIZE, ARCH-NOWORKAROUND, ARCH-TX, ARCH-FE-FREEZE, ARCH-USERWORK, ARCH-BASELINE.

Recovery high-level closure

On PASS update:

docs/architecture/tmar-current-state.json

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

durable guard assertions

Record:

Cart multi-currency line slice = ACCEPTED_WITH_ORDER_SINGLE_CURRENCY_COMPATIBILITY_GUARD

Cart = LINE_LEVEL_CURRENCY_AUTHORITY_WITH_DEFAULT_SELECTION_AND_TOTALS_BY_CURRENCY

Order multi-currency = DEFERRED

Order boundary = SINGLE_CURRENCY_ONLY_FAIL_CLOSED_UNTIL_DEDICATED_WAVE

Checkout = PAUSED_AT_SAFE_W5_CHECKPOINT

Cart/Order/StoreContext certifications unchanged

frontendFrozen = true

nextTask = USER_REVIEW_CART_MULTICURRENCY_LINES_001_R1

Do not start another module automatically.

Validation

Run only:

focused Order shipping compatibility tests

focused Order checkout compatibility tests

full Tooba.Cart.Tests

relevant Order focused tests only

HostCartResidualGuardTests

TmarCompleteReferenceStructureGateTests

TmarDurableGuardTests

one final dotnet build src/backend/Tooba.slnx

PASS criteria

PASS only if:

no cross-currency subtotal computed in Order shipping

mixed-currency Cart fails closed at Order boundary

single-currency Cart uses sole line currency, not DefaultCurrency

Order quote/reprice/persist uses line truth

no FX/payment-group/split-order scope

Cart behavior intact

certifications preserved

Checkout remains paused

frontend untouched

recovery updated at high level

tests/build pass

Canonical Result

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-CART-MULTICURRENCY-LINES-001-R1
Parent-Task: TB-TMAR-CART-MULTICURRENCY-LINES-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-Verdict-State:
CrossCurrency-Shipping-Sum-State:
Order-Boundary-MixedCurrency-State:
SingleCurrency-EffectiveCurrency-State:
DefaultCurrency-Transaction-Authority-State:
CheckoutDirectory-Currency-State:
CheckoutSubmitHost-Currency-State:
Typed-Error-State:
Cart-MultiCurrency-State:
Order-MultiCurrency-State:
Checkout-State:
Architecture-Guards:
Focused-Validation:
Cart-Tests:
Full-Build:
Cart-Certification-State:
Order-Certification-State:
StoreContext-Certification-State:
Frontend-Production-Changes:
Recovery-HighLevel-Closure:
Residual-Defects:
Git:
User-Work-Preserved:
Recovery-Next-Task:
Next-Recommended-Task:
END_TOOBA_WORKER_RESULT

STOP

After Result:
STOP completely.
Do not start Offer/Payment/Settlement/etc.
Do not resume Checkout.
Do not implement Order multi-currency.
Do not touch Payment.
Do not poll.
Wait for Architect verification.

END_TOOBA_TASK