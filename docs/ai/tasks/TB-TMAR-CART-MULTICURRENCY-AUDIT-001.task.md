PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-CART-MULTICURRENCY-AUDIT-001
Parent-Task: TB-TMAR-STORECONTEXT-GOLDEN-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: CART_MULTICURRENCY_BOUNDED_AUDIT
Title: Audit Cart single-currency assumptions before implementation
Backend-Only: YES

Architect verdict on parent

TB-TMAR-STORECONTEXT-GOLDEN-001 is ARCHITECT-ACCEPTED.

Verified on main:

commit f2667a249d43fb542903a08b429cd1ea8e219704 exists

StoreCommerceContext now uses DefaultCurrency semantics

StoreContext is present in ARCH-COMPLETE-002 structure manifest

StoreContext is classified INTERNAL_ONLY / PLATFORM_CONTEXT_REFERENCE_PATTERN

no ceremonial Application/Endpoints/MediatR was added

BuildingBlocks remains free of StoreCommerceContext

Cart/Order certification state preserved

known Cart single-currency debt is real and remains open

One objective only

AUDIT ONLY.

Produce the exact implementation map for removing Cart-level single-currency assumptions while preserving line-level quoted currency.

Do NOT modify production code.
Do NOT modify DB schema.
Do NOT implement multi-currency.
Do NOT touch Order/Checkout/Payment.
Do NOT perform repository-wide discovery.

This audit exists so the next implementation task is deterministic and Cursor does not have to architect while editing.

Parent acceptance SoT bookkeeping

Because the parent task deliberately left lastAcceptedTask/lastAcceptedCommit unchanged pending Architect review, update ONLY recovery SoT bookkeeping to mark the accepted parent:

lastAcceptedTask = TB-TMAR-STORECONTEXT-GOLDEN-001

lastAcceptedCommit = f2667a249d43fb542903a08b429cd1ea8e219704

nextTask = USER_REVIEW_CART_MULTICURRENCY_AUDIT_001

Do not alter certification meaning beyond the already-recorded accepted StoreContext state.

Exact files to inspect

Read ONLY these production files unless one direct type definition is required to understand a referenced member:

src/backend/Modules/Cart/Tooba.Cart.Domain/Aggregates/ShoppingCart.cs

src/backend/Modules/Cart/Tooba.Cart.Domain/Entities/CartLine.cs

src/backend/Modules/Cart/Tooba.Cart.Application/Ports/ICartDirectory.cs

src/backend/Modules/Cart/Tooba.Cart.Application/Ports/ICartCommerceContextResolver.cs

src/backend/Modules/Cart/Tooba.Cart.Application/Commands/CreateGuestCart/CreateGuestCartCommand.cs

src/backend/Modules/Cart/Tooba.Cart.Infrastructure/Directories/CartDirectory.cs

src/backend/Modules/Cart/Tooba.Cart.Application/Presentation/CartPresentationComposer.cs

src/backend/Modules/Cart/Tooba.Cart.Contracts/Checkout/CartContracts.cs

src/backend/Modules/Cart/Tooba.Cart.Contracts/Presentation/CartPresentationContracts.cs

src/backend/Modules/Pricing/Tooba.Pricing.Contracts/Ports/PriceLookupContracts.cs

src/backend/Modules/Pricing/Tooba.Pricing.Contracts/Ports/CampaignCartPriceAuthority.cs

src/backend/Modules/Pricing/Tooba.Pricing.Contracts/Dtos/CurrencyCode.cs

Do NOT inspect Order/Payment/Checkout implementation in this task.

Required audit questions

Answer each with exact file/member evidence.

A. Cart aggregate assumptions

Determine every place where ShoppingCart.Currency currently acts as:

cart creation input

price-resolution input

merge fallback

presentation currency

persistence field/invariant

Classify each occurrence as:

REMOVE_CART_LEVEL_CURRENCY

KEEP_DEFAULT_SELECTION_ONLY

KEEP_LINE_LEVEL_CURRENCY

REQUIRES_LATER_CHECKOUT_DECISION

B. CartLine truth

Confirm whether CartLine.QuotedCurrency is already sufficient as the authoritative currency of an individual quoted line.

Document:

creation path

replacement/requote path

snapshot path

presentation path

Do not change it.

C. Pricing contract capability

Determine whether Pricing already supports resolving different currencies per Offer by issuing independent:
PriceResolutionQuery(... Currency ...)

Do NOT redesign Pricing.
State whether Cart can become multi-currency without changing Pricing Contracts.

D. Add/increase line behavior

Identify the exact current path that resolves a new line price.

For next implementation, decide the deterministic rule:

initial cart creation receives StoreContext.DefaultCurrency only as an initial/default selection input;

when adding/requoting a line, line currency must come from the resolved PriceQuote.Currency;

Cart must not reject a second line merely because its quote currency differs from another line.

If current Pricing API requires a currency input before it can return a quote, identify that circular dependency explicitly as:
PRICING_CURRENCY_SELECTION_BLOCKER

Do NOT invent a fix in this audit.

E. Presentation totals

Identify all places where Cart currently computes one scalar subtotal across lines without currency grouping.

Classify them as:
MULTICURRENCY_INVALID_TOTAL

The next design must not sum:
10 USD + 500000 IRR

into one number.

Recommend only one of these two presentation shapes, based on current contracts:

TotalsByCurrency

no aggregate subtotal when more than one currency

Do NOT implement yet.

F. Contract compatibility

List exact public contract members that would need changing in a multi-currency Cart slice:

CartSnapshot

CartPage

CartLineView

ICartDirectory create methods

any other exact member in the listed files

For each classify:

BREAKING_PRE_RELEASE_SAFE

CAN_ADD_COMPATIBLY

KEEP_UNCHANGED

G. Persistence impact

From ShoppingCart and Cart persistence mapping referenced directly by CartDirectory, determine whether removing cart-level Currency requires:

DB column removal

temporary nullable/deprecated column

no DB change

If the mapping file is required to answer this, inspect only the exact CartDbContext/configuration file that maps ShoppingCart.Currency.

No migration in this task.

Golden architecture requirements for the future implementation

The audit recommendation MUST preserve:

Cart remains ARCH-COMPLETE-002 certified

capability-first foldering

path↔namespace alignment

no god-files

no Host business logic

cross-module only through Contracts

existing CQRS/MediatR 12.5 endpoint flow remains unchanged

no ceremonial new handlers

FluentValidation remains transport/input validation only

no cross-module DB access

no cross-bounded-context ACID

no frontend changes

Checkout remains paused

Output artifact

Create ONLY:

docs/evidence/TB-TMAR-CART-MULTICURRENCY-AUDIT-001/cart-multicurrency-audit.md

The document MUST contain:

current single-currency assumptions table

line-level currency truth

Pricing capability/blocker verdict

exact next implementation file list

exact contract changes

persistence impact

presentation total strategy

explicitly deferred Order/Checkout/Payment work

proposed next implementation task scope limited to ONE Cart slice

parent acceptance SoT bookkeeping evidence

Do not edit production files.

Validation

Run only:

verify no production .cs/.csproj/config file changed

TmarDurableGuardTests

TmarCompleteReferenceStructureGateTests

No full build is required for an audit-only task unless SoT guard compilation requires it.
Do not run broad suites.

PASS criteria

PASS only if:

parent StoreContext golden task is stamped accepted in SoT

no production code changed

audit is limited to the exact files above

every cart-level Currency use is classified

Pricing currency-selection blocker, if any, is explicitly identified

presentation invalid cross-currency summing is explicitly identified

next implementation scope is deterministic and small

Order/Checkout/Payment are deferred

Cart/Order/StoreContext certifications remain unchanged

frontend remains frozen

Canonical Result

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-CART-MULTICURRENCY-AUDIT-001
Parent-Task: TB-TMAR-STORECONTEXT-GOLDEN-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-Acceptance-Stamped:
Production-Code-Changes:
Cart-Level-Currency-Assumptions:
CartLine-Currency-Truth:
Pricing-Contract-State:
Pricing-Currency-Selection-Blocker:
Presentation-Subtotal-State:
Recommended-Presentation-Shape:
Contract-Change-Map:
Persistence-Impact:
Next-Implementation-Files:
Deferred-Order-Checkout-Payment:
Architecture-Locks-State:
Focused-Validation:
Cart-Certification-State:
Order-Certification-State:
StoreContext-Certification-State:
Checkout-State:
Frontend-Production-Changes:
Residual-Defects:
Git:
User-Work-Preserved:
Recovery-Next-Task:
Next-Recommended-Task:
END_TOOBA_WORKER_RESULT

STOP

After Result:
STOP completely.
Do not implement multi-currency.
Do not modify Pricing.
Do not touch Order/Checkout/Payment.
Do not poll.
Wait for Architect verification.

END_TOOBA_TASK
