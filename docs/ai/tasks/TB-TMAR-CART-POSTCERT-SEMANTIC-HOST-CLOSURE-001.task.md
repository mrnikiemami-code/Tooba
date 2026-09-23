PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-CART-POSTCERT-SEMANTIC-HOST-CLOSURE-001
Parent-Task: TB-TMAR-CART-ARCH-COMPLETE-002-REVERIFY-001-R1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: CART_POSTCERT_SEMANTIC_HOST_CLOSURE
Title: Remove Cart-Specific Host Implementation and Repair Cart Commerce/Locale Semantics
Backend-Only: YES

Architect decision

Cart remains:
COMPLETE_REFERENCE_PATTERN + ARCH-COMPLETE-002 STRUCTURE_CERTIFIED

This is a post-certification semantic/ownership repair.

Confirmed defects on current main:

Cart-specific implementation still exists in Tooba.Host:

HostCartPersistenceHoursResolver

CartExpiryHostedService

CartExpiryHostOptions

HostCartPersistenceHoursResolver blocks on an async Catalog contract with:

.GetAwaiter().GetResult()

CancellationToken.None

CreateGuestCartHandler hardcodes:

"IR"

"IRR"

SalesChannel.Marketplace

CartPresentationComposer hardcodes user-facing Persian fallbacks:

"کالا"

"فروشنده"

Architecture decision

Target:

HOST_HAS_ZERO_CART_SPECIFIC_IMPLEMENTATION

Host may retain only generic composition-root registration such as new CartModule().

Host must NOT own Cart-specific:

persistence adapters/policy

expiry worker implementation

Cart worker options

Cart business defaults

Cart localization fallbacks

Cart market/currency/channel decisions

Cart-specific runtime implementation belongs to Cart, primarily Tooba.Cart.Infrastructure.

Scope

Repair ONLY Cart semantic ownership / Host residue described here.

Do NOT modify:

frontend

unrelated modules

Checkout W6

Cart route contracts unless strictly required

database schema/migrations unless strictly required

Order production code

unrelated Host functionality

Mandatory audit before changes

Inspect and reuse existing canonical abstractions for:

current commerce context

tenant/store context

market

currency

sales channel

locale/culture

Catalog store Cart persistence settings

background-worker tenant iteration

Cart lifetime/expiry

Do NOT create duplicate tenant/commerce/locale abstractions if an existing canonical one already exists.

Create evidence of what was inspected and reused.

Required repair A — remove Cart implementation from Host

Audit all production Cart* types and Cart-specific registrations under Host.

At minimum:

remove HostCartPersistenceHoursResolver from Host

move the Cart-side Catalog persistence-hours adapter into Cart-owned infrastructure

remove Cart-specific DI implementation from Host

move CartExpiryHostedService to Cart-owned infrastructure

move/rename CartExpiryHostOptions to Cart-owned options

register the Cart worker/options from CartModule

Preferred physical ownership:

Tooba.Cart.Infrastructure/Lifetime/...

appropriate capability/integration folders consistent with ARCH-COMPLETE-002

Host must not retain Cart-specific implementation classes after this task.

Generic module composition is allowed.

Required repair B — remove sync-over-async

The Cart persistence-hours seam must become properly async if the foreign contract is async.

Forbidden in this path:

.Result

.Wait()

.GetAwaiter().GetResult()

CancellationToken.None

Use an async Cart-side port such as:
Task<int?> ResolveOverrideHoursAsync(CancellationToken cancellationToken)

or reuse an equivalent existing canonical async abstraction.

Propagate cancellation correctly through callers.

Required repair C — preserve expiry behavior

Moving the worker must preserve current behavior:

active tenant/poll-target iteration

commerce-context assignment per target

scoped ICartExpiryReconciler

per-tenant failure isolation

cancellation behavior

telemetry

background-worker registry state

existing configuration compatibility

Do not duplicate generic Host infrastructure.

If moving the worker would require a forbidden Cart -> Host reference, STOP with INCOMPLETE and report the exact blocking type. Do not create a reverse dependency.

Required invariant:
Tooba.Cart.* -> Tooba.Host = 0

Required repair D — CreateGuestCart commerce context

Remove hardcoded:

"IR"
"IRR"
SalesChannel.Marketplace

CreateGuestCartHandler must obtain effective:

Market

Currency

SalesChannel

from the authoritative current commerce/storefront context already present in the system.

Do NOT solve this by trusting arbitrary raw HTTP strings in the command.

Do NOT hardcode a finite currency list inside Cart.

The design must allow another valid effective currency such as USD/EUR when store/commerce configuration permits it.

Cart consumes effective commerce context; it does not become currency/market policy authority.

Required repair E — localization

Remove hardcoded user-facing:

"کالا"

"فروشنده"

from CartPresentationComposer.

Do not replace them with English hardcodes.
Do not add FA/EN branching.

Audit how LocalizedTitle is resolved and verify it respects the current canonical locale/culture mechanism.

If Catalog/Party already owns localized display fallback, use that boundary.
If no localized fallback is available, prefer a neutral non-localized absence/null-safe presentation contract over embedding language in Cart.

Validator decision

Do NOT add a validator merely because CreateGuestCartCommand exists.

If the command remains parameterless, classify it as:
NO_VALIDATOR_REQUIRED

Preserve the existing durable validator coverage convention.

If the repair legitimately introduces validated transport input, then add a concrete validator only for actual caller-provided fields.

Host residual audit

After changes, scan Host for Cart ownership residue.

Expected:

HOST_CART_SPECIFIC_IMPLEMENTATION = 0

Allowed:

generic CartModule registration/composition

generic platform infrastructure used by many modules

Forbidden:

Cart-specific classes implemented in Host

Host implementation of Cart Application ports

Cart-specific business policy/defaults in Host

Host-owned Cart background worker

Durable guards

Add/strengthen durable tests so regression cannot reappear.

Guard at minimum:

Host contains no Cart-specific implementation classes except explicit generic composition allowlist.

Host does not implement Cart Application ports.

Cart production projects do not reference Tooba.Host.

no hardcoded "IR" / "IRR" / SalesChannel.Marketplace remains in CreateGuestCartHandler.

no hardcoded "کالا" / "فروشنده" remains in CartPresentationComposer.

no sync-over-async pattern remains in Cart persistence-hours resolution.

Cart remains present in structureLock.certifiedModules.

Order remains structure certified.

Checkout remains paused.

Avoid fragile full-source string checks where a stronger structural test is practical.

Structure / namespace

Any moved/new files must obey ARCH-COMPLETE-002:

correct capability folder

path ↔ namespace alignment

project root allowlist

no new root dumping

no foreign Application/Domain/Infrastructure leakage

Contracts-only foreign module boundaries

Do not weaken existing Cart structure certification.

Evidence

Create:

docs/evidence/TB-TMAR-CART-POSTCERT-SEMANTIC-HOST-CLOSURE-001/cart-semantic-host-closure.md

Document:

Host Cart residue found

files moved/removed

final ownership

async persistence-hours path

current-commerce-context abstraction reused

Market/Currency/SalesChannel resolution path

localization resolution/fallback decision

validator classification

Host residual scan result

architecture guards added

no frontend changes

Checkout still paused

Recovery SoT

Update recovery SoT only after implementation is proven.

Preserve:

Golden wave COMPLETE + USER_ACCEPTED

Cart COMPLETE_REFERENCE_PATTERN

Cart ARCH-COMPLETE-002 STRUCTURE_CERTIFIED

Order ARCH-COMPLETE-002 STRUCTURE_CERTIFIED

Checkout PAUSED_AT_SAFE_W5_CHECKPOINT

frontendFrozen = true

Record this task as a post-certification Cart repair, not as a new structural certification.

Set next task:
USER_REVIEW_CART_POSTCERT_SEMANTIC_HOST_CLOSURE

Gate:
USER_REVIEW_REQUIRED_BEFORE_NEXT_TMAR_WAVE

Validation — focused only

Run the minimum necessary:

Cart tests relevant to lifetime/persistence/presentation/create-guest

Cart validator coverage guard

Cart structure gate

Host->Cart durable guard

TmarDurableGuardTests

TmarCompleteReferenceStructureGateTests

dotnet build src/backend/Tooba.slnx

Do NOT run broad unrelated suites.

PASS criteria

PASS only if:

Host owns zero Cart-specific implementation classes.

HostCartPersistenceHoursResolver no longer exists in Host.

Cart persistence-hours integration is Cart-owned and fully async.

no sync-over-async remains in that path.

Cart expiry worker/options are Cart-owned.

tenant-loop behavior and telemetry remain preserved.

no Cart -> Host dependency exists.

CreateGuestCartHandler has no hardcoded IR/IRR/Marketplace.

effective Market/Currency/SalesChannel come from canonical commerce context.

Cart supports valid non-IRR effective currencies without Cart hardcoding.

Cart presentation has no hardcoded Persian/English fallback strings.

localization follows canonical locale/context ownership.

validator classification remains correct.

Cart remains COMPLETE_REFERENCE_PATTERN + STRUCTURE_CERTIFIED.

Order certification remains preserved.

Checkout W6 not started.

frontend unchanged.

full build passes.

working tree contains only task-related changes.

evidence + SoT are consistent.

Canonical Result

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-CART-POSTCERT-SEMANTIC-HOST-CLOSURE-001
Parent-Task: TB-TMAR-CART-ARCH-COMPLETE-002-REVERIFY-001-R1
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Host-Cart-Implementation-State:
HostCartPersistenceResolver-State:
Persistence-Async-State:
CartExpiry-Ownership-State:
Tenant-Worker-Behavior-State:
Cart-To-Host-Dependency-State:
Commerce-Context-Audit:
CreateGuestCart-Market-State:
CreateGuestCart-Currency-State:
CreateGuestCart-SalesChannel-State:
Localization-Audit:
CartPresentation-Fallback-State:
Validator-State:
Structure-Certification-State:
Durable-Guard-State:
Focused-Validation:
Full-Validation:
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
Do not start another module.
Do not resume Checkout W6.
Do not poll.
Wait for Architect verification.

END_TOOBA_TASK