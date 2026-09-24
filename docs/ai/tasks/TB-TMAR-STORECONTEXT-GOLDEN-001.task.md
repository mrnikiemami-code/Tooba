PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-STORECONTEXT-GOLDEN-001
Parent-Task: TB-TMAR-STORECONTEXT-FOUNDATION-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: STORE_CONTEXT_GOLDEN_HARDENING
Title: Golden-certify StoreContext foundation and make currency semantics multi-currency-safe
Backend-Only: YES

Architect verdict on parent

TB-TMAR-STORECONTEXT-FOUNDATION-001 is ACCEPTED.

Verified on main:

implementation commit: 48720fd3ae5bc69ccb5ae6532a2000f7a2b9d4f8

SoT stamp: 95706f174cbfe63f32cc84641b77ace403ab190f

StoreCommerceContext removed from BuildingBlocks

StoreContext.Contracts + StoreContext.Infrastructure exist

request and worker assignment paths are separate

Cart consumes StoreContext.Contracts

parent production fail-fast behavior preserved

no Shared-DB implementation was introduced

One objective only

Harden StoreContext as a GOLDEN architecture foundation from the start and prevent its currency field from being misread as the transaction/line/order currency.

This task does NOT implement multi-currency Cart/Order.
It only makes StoreContext semantics explicitly DEFAULT-context semantics and structure-certifies the new module.

Do NOT perform a repository-wide relationship redesign.
Do NOT touch Pricing, Offer business behavior, Order, Checkout, DB schema, migrations, or frontend.

Closed architecture decision

StoreContext may define the store/storefront DEFAULT commerce context.

It must NOT define the currency of every:

Offer

PriceQuote

CartLine

OrderLine

PaymentGroup

Therefore:
StoreCommerceContext.Currency must become StoreCommerceContext.DefaultCurrency.

Meaning:

default/preferred currency used when a use-case needs an initial currency;

NOT an invariant that all lines in a cart/order must share one currency;

NOT a settlement currency;

NOT a payment-group currency.

No AllowedCurrencies model is introduced in this task.

Exact changes
1. StoreContext Contracts

File:
src/backend/Modules/StoreContext/Tooba.StoreContext.Contracts/Current/StoreCommerceContext.cs

Rename record property:

Currency -> DefaultCurrency

Update XML docs to explicitly state:

default storefront currency only

consumer transaction lines may carry their own currency

StoreContext does not impose single-currency Cart/Order semantics

Do NOT add another currency type.
Do NOT add SalesChannel enum duplication.

2. Host configuration naming

File:
src/backend/Host/Tooba.Host/Configuration/ToobaPlatformOptions.cs

Rename:

StoreCommerceOptions.Currency -> DefaultCurrency

Update ResolveStoreCommerce, production fail-fast validation, and error messages.

Canonical configuration key:
StoreCommerce:DefaultCurrency

SingleStore tenant key:
SingleStore:Tenants:<n>:StoreCommerce:DefaultCurrency

Do NOT retain a silent Currency fallback alias.
This is pre-release architecture cleanup; use one canonical key.

Preserve:

Market fallback behavior

SalesChannel validation against canonical Offer.Contracts enum

Disabled/Suspended skip behavior

no hardcoded currency values

3. Configuration/test data

Update only StoreCommerce configuration references needed for the rename in:

src/backend/Host/Tooba.Host/appsettings.Development.json

src/backend/Host/Tooba.Host.Tests/TenantResolutionTests.cs

Do not change unrelated config.
Production config has no StoreCommerce value today; do not invent production values.

4. Cart adapter wording only

Files:

src/backend/Modules/Cart/Tooba.Cart.Infrastructure/Lifetime/CartCommerceContextResolver.cs

src/backend/Modules/Cart/Tooba.Cart.Application/Ports/ICartCommerceContextResolver.cs

src/backend/Modules/Cart/Tooba.Cart.Application/Commands/CreateGuestCart/CreateGuestCartCommand.cs

The StoreContext source must read DefaultCurrency.

Rename Cart adapter record member:

Currency -> DefaultCurrency

Update CreateGuestCart usage to pass context.DefaultCurrency.

This is an initial/default selection only.

Do NOT change ShoppingCart schema/domain currency model in this task.
Do NOT claim Cart is multi-currency after this task.
Do NOT change price-resolution behavior.

Add a clear XML note:
current Cart may still have a single cart pricing currency; that is separate legacy/design debt and StoreContext must not encode it as global transaction policy.

5. StoreContext Golden physical structure certification

Update:
docs/architecture/tmar-module-structure-manifests.json

Add StoreContext manifest with:

structureCertified: true

lockVersion: ARCH-COMPLETE-002

Projects:

Tooba.StoreContext.Contracts

rootAllowlist: []

capability folder: Current

no root .cs

Tooba.StoreContext.Infrastructure

rootAllowlist: ["StoreContextModule.cs"]

capability folder: Current

no other root .cs

Apply:

PATH_NAMESPACE_ALIGNMENT

ROOT_ALLOWLIST

NO_NAMESPACE_ALIAS_WORKAROUND

no god-file

ARCH-SIZE locks

No Application project is required because StoreContext has no application use-case in this foundation.
No Endpoints project is required because StoreContext is INTERNAL_ONLY / PLATFORM_CONTEXT.
No MediatR handler is required because there is no Command/Query/use-case.
Do NOT add ceremonial MediatR.

6. SoT classification

Update:
docs/architecture/tmar-current-state.json

Record StoreContext separately from HTTP-owning business modules:

state: PLATFORM_CONTEXT_REFERENCE_PATTERN

httpApplicability: INTERNAL_ONLY

endpointOwnership: NOT_APPLICABLE

cqrs: NOT_APPLICABLE_NO_APPLICATION_USE_CASE

structureCertifiedUnderArchComplete002: true

Add StoreContext to:
structureLock.certifiedModules

Do NOT add StoreContext to the existing HTTP COMPLETE module list if that would misrepresent applicability.

Preserve Order and Cart certifications.

7. Guards

Strengthen existing architecture guards to prove:

StoreContext is listed in structure manifest and SoT certifiedModules.

Contracts root has zero .cs.

Infrastructure root contains only StoreContextModule.cs.

path ↔ namespace alignment for StoreContext.

no StoreContext Application/Endpoints project was created ceremonially.

no StoreCommerceContext.Currency remains.

StoreCommerceContext.DefaultCurrency exists.

no AllowedCurrencies, settlement currency, payment currency, or single-currency invariant is invented in StoreContext.

BuildingBlocks still contains no StoreCommerceContext.

Cart still has zero Host dependency.

Multi-currency protection statement

Evidence must explicitly state:

StoreContext.DefaultCurrency is ONLY a default selection input.

It MUST NOT be used as proof that:

all offers are priced in one currency,

all CartLines must share one currency,

all OrderLines must share one currency,

one payment can necessarily settle all currencies.

Existing Cart-level single-currency behavior is NOT repaired in this task and must be reported as residual architecture debt for the next dedicated wave.

Explicit non-goals

Do NOT:

modify ShoppingCart.Currency

modify CartLine quoted currency model

modify Pricing resolution behavior

modify Offer

modify Order

modify Payment

modify Checkout

implement currency conversion

implement payment groups

implement Shared-DB

add Application/Endpoints projects just to look complete

perform broad repo audit

If an unexpected architecture decision is required:
STOP and return INCOMPLETE.

Validation

Run only:

StoreContext structure/golden guards

PlatformOptionsValidatorTests

HostCartResidualGuardTests

Cart focused create/commerce tests

TmarCompleteReferenceStructureGateTests

TmarDurableGuardTests

one final dotnet build src/backend/Tooba.slnx

Do not run broad unrelated suites.

Evidence

Create:
docs/evidence/TB-TMAR-STORECONTEXT-GOLDEN-001/store-context-golden.md

Record:

parent accepted state

DefaultCurrency semantic decision

config rename

why MediatR is NOT_APPLICABLE

why Endpoints are NOT_APPLICABLE

structure manifest

multi-currency protection statement

known Cart single-currency residual debt

validation results

Recovery SoT

On PASS:

StoreContext = PLATFORM_CONTEXT_REFERENCE_PATTERN

StoreContext = ARCH-COMPLETE-002 STRUCTURE_CERTIFIED

Cart remains COMPLETE_REFERENCE_PATTERN + ARCH-COMPLETE-002 STRUCTURE_CERTIFIED

Order remains ARCH-COMPLETE-002 STRUCTURE_CERTIFIED

Checkout remains PAUSED_AT_SAFE_W5_CHECKPOINT

frontendFrozen = true

Set:
nextTask = USER_REVIEW_STORECONTEXT_GOLDEN_001

Do not start Cart multi-currency repair automatically.

PASS criteria

PASS only if:

StoreContext uses DefaultCurrency semantics

old StoreCommerceContext.Currency is gone

config uses DefaultCurrency canonically

StoreContext is structurally certified under ARCH-COMPLETE-002

no ceremonial Application/Endpoints/MediatR added

no single-currency transaction invariant introduced into StoreContext

Cart/Order/Checkout behavior not broadened

Cart and Order certifications preserved

frontend untouched

focused tests pass

full build passes

SoT/evidence consistent

Canonical Result

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-STORECONTEXT-GOLDEN-001
Parent-Task: TB-TMAR-STORECONTEXT-FOUNDATION-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-Foundation-State:
DefaultCurrency-Semantics:
Canonical-Config-Key:
StoreContext-Applicability:
MediatR-State:
Endpoints-State:
Structure-Certification-State:
Manifest-State:
SoT-State:
BuildingBlocks-State:
Cart-Adapter-State:
MultiCurrency-Protection-State:
Known-Cart-Currency-Debt:
Architecture-Guards:
Focused-Validation:
Full-Build:
Cart-Certification-State:
Order-Certification-State:
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
Do not start Cart multi-currency repair.
Do not start Shared-DB.
Do not touch Checkout.
Do not poll.
Wait for Architect verification.

END_TOOBA_TASK
