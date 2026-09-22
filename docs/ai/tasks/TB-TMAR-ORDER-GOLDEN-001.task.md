PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-ORDER-GOLDEN-001
Parent-Task: TB-TMAR-GOLDEN-WAVE-FINAL-CLOSURE-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: FAST-SAFE
Track: ORDER_GOLDEN_REFERENCE
Title: Order Complete Reference Pattern + Host Authority Removal + Checkout-Safe Architecture Closure
Backend-Only: YES

Architect decision

The previous Golden Wave is COMPLETE and explicitly ACCEPTED_BY_USER.

Accepted closure:
TB-TMAR-GOLDEN-WAVE-FINAL-CLOSURE-001

Protected COMPLETE_REFERENCE_PATTERN modules:

Cart

Settlement

Fulfillment

Returns

Notification

Support

Wallet

Payment

Promotion

Offer

Inventory

Inventory remains:

state = COMPLETE_REFERENCE_PATTERN

httpApplicability = INTERNAL_ONLY

endpointOwnership = NOT_APPLICABLE

cqrs = INTERNAL_USE_CASE_BOUNDARIES

The previous recovery gate:
USER_REVIEW_GOLDEN_WAVE
has been satisfied by explicit user acceptance.

This task starts the next TMAR module wave.

Architect selected:
Order

This task is Order-only except for tiny bounded changes required to preserve existing contracts/guards/recovery metadata.

Checkout remains:
PAUSED_AT_SAFE_W5_CHECKPOINT

Do NOT start Checkout W6.

Frontend remains frozen.

1. Direct repository findings — mandatory starting facts

Architect directly inspected the current repository and confirmed:

Current Order physical module:

Tooba.Order.Domain

Tooba.Order.Application

Tooba.Order.Contracts

Tooba.Order.Infrastructure

There is currently NO:

Tooba.Order.Endpoints

Order-owned HTTP/business presentation still exists in Host, including at minimum:

Host/Tooba.Host/Admin/AdminOrderOperationsEndpoints.cs

Host/Tooba.Host/Admin/AdminOrderCompletenessEndpoints.cs

Host currently contains Order-related composers including at minimum:

AdminOrderOperationsComposer

AdminOrderCompletenessComposer

OrderInventoryRecoveryComposer

OrderSupplyComposer

StorefrontCheckoutComposer

Current Order Application contains:

CheckoutProcessManager

checkout/process contracts

reservation-cycle contracts

seller cancellation policy

Order contracts/use-case seams

Current Order Infrastructure contains:

OrderDbContext

CheckoutSubmitHost

CheckoutDirectory

CheckoutProcessTracker

ReservationCycleDirectory

payment/fulfillment/returns/notification bridges

order module registration

persistence/migrations

Architect directly found current anti-pattern candidates in Order checkout orchestration:

DateTimeOffset.UtcNow

UuidV7.New()

exception classification through ex.Message

hardcoded localized exception text

swallowed InvalidOperationException during reservation release

retry loop in conflict resolution using:
for attempt < 10
Task.Delay(20 * attempt)

These are findings to investigate and repair correctly.
Do NOT mechanically edit code only to satisfy string scans.
Preserve business behavior.

2. Primary objective

Converge Order to a real COMPLETE_REFERENCE_PATTERN consistent with the accepted Golden modules.

Target HTTP-owning architecture:

Order.Endpoints
→ ISender
→ Order.Application Commands / Queries
→ Domain + Order Infrastructure + foreign Contracts only

Host must converge to:

composition root

middleware/platform concerns

global auth/session/tenant/correlation

tiny security adapters when required

explicit Development bootstrap allowlists

Host must NOT remain the Order application layer.

3. HTTP ownership audit — mandatory

Search the entire production repository for all Order-owned HTTP routes and presentation responsibilities.

At minimum inspect:

/orders

/order

/checkout

admin order routes

seller order routes

customer/storefront order routes

pending-payment/order-result routes

invoice/receipt endpoints

order operational-history

order notes

cancellation

inventory-recovery views/actions

order supply status

reservation policy/order lifecycle routes

MapOrder

checkout endpoints

Host order composers used directly by HTTP handlers

Classify every relevant HTTP hit as:

ORDER_OWNED_HTTP

CHECKOUT_ORDER_HTTP

FOREIGN_MODULE_HTTP_USING_ORDER_CONTRACT

PLATFORM_ONLY

DEVELOPMENT_ONLY

TEST_ONLY

Do not infer ownership from folder names alone.
Ownership follows business capability.

Create:
docs/evidence/TB-TMAR-ORDER-GOLDEN-001/order-http-ownership-audit.md

4. Create real Tooba.Order.Endpoints

If the audit confirms Order owns HTTP surfaces, create:

src/backend/Modules/Order/Tooba.Order.Endpoints/

The Endpoints project must own Order HTTP routes/wire DTOs/presentation composition that belong to Order.

Required flow:
Endpoint
→ ISender
→ Command/Query
→ Handler

Do not create ceremonial handlers that merely hide Host composers without establishing Application ownership.

Host may retain only tiny security/session/tenant adapters that cannot reasonably belong to the module.

No Order business decisions in Host.

5. CQRS / MediatR 12.5 — mandatory

Order Application must converge to real MediatR 12.5 use-case boundaries.

Audit all Order HTTP use cases.

For each real route:

define Command or Query

define Handler

move orchestration to Order.Application where appropriate

preserve domain ownership

use repositories/ports/contracts behind the handler

use FluentValidation through the canonical pipeline where validation is required

Do not Big-Bang rewrite unrelated internal services.

Existing cohesive internal services may remain behind handlers where safe.

No HTTP endpoint may directly invoke Order Infrastructure or OrderDbContext.

6. Host authority removal — critical

Remove Order business authority from Host.

At minimum audit and migrate/retire as appropriate:

AdminOrderOperationsEndpoints

AdminOrderCompletenessEndpoints

AdminOrderOperationsComposer

AdminOrderCompletenessComposer

OrderInventoryRecoveryComposer

OrderSupplyComposer

StorefrontCheckoutComposer

AdminOrdersGridQueryEngine if it performs Order-owned business query logic

any Seller/Customer/Storefront direct Order Application orchestration

Do NOT delete legitimate BFF/presentation composition blindly.

If a composer is truly cross-module presentation-only:

prove it

keep it thin

ensure it consumes stable Contracts/Gateways

ensure it does not calculate business truth

ensure it does not perform Order writes

Order-owned HTTP/business logic must move into Order.Endpoints/Application.

7. Checkout freeze boundary

Checkout is still:
PAUSED_AT_SAFE_W5_CHECKPOINT

This task may repair Order architecture used by Checkout, but must NOT implement Checkout W6.

Allowed:

move Order-owned HTTP into Order.Endpoints

wrap existing Order use cases with MediatR

replace architecture anti-patterns

remove Host Order business authority

improve Order contracts/boundaries

fix semantic errors/time/id/tracing

repair bounded retry behavior where clearly safe

preserve current Checkout W5 behavior

Forbidden:

distributed Saga implementation

new checkout workflow stage

new compensation design

new cross-context ACID expansion

changing checkout business semantics

frontend changes

If completing Order requires changing Checkout workflow semantics:
return INCOMPLETE with exact blocker.
Do not silently advance Checkout.

8. Cross-module boundaries

Audit all Order project references and source usage.

Order may depend on foreign modules only through stable Contracts/Gates where required.

Specifically inspect:

Cart

Inventory

Payment

Fulfillment

Returns

Notification

Offer

Promotion

Pricing

Tax

Catalog

Party

Identity

AddressBook

Forbidden target state:

Order.Application → foreign Application

Order.Infrastructure → foreign Application

Order → foreign Infrastructure

foreign Domain entities crossing Order boundary

direct foreign DbContext

Host callbacks acting as business ports

Existing approved Contracts edges must remain stable.

Do not reopen COMPLETE modules broadly.
Only tiny bounded compatibility changes are allowed if absolutely required.

Create:
docs/evidence/TB-TMAR-ORDER-GOLDEN-001/order-cross-module-boundary-audit.md

9. Order.Contracts extraction readiness

Audit all Order.Contracts surfaces.

Verify:

DTOs/interfaces contain no EF/internal implementation types

no ASP.NET types

no Host types

no foreign Domain entities

stable synchronous contract semantics

stable async integration semantics where applicable

no persistence leakage

version/extraction feasibility

Explicitly inspect current folders:

Fulfillment

Notifications

Payments

Returns

Add/move contract surfaces only when needed by real Order capabilities.

Do not create broad generic contracts.

10. Persistence/data ownership

Verify:

OrderDbContext exists only in Order.Infrastructure

migrations remain Order-owned

Host has no Order DbContext authority

no foreign module directly uses OrderDbContext

no cross-schema SQL JOIN/FK introduced by current production code

Order writes occur through Order-owned Application/Infrastructure boundaries

Search repo-wide, not only project references.

If direct cross-module SQL exists:
repair if bounded and safe;
otherwise return INCOMPLETE.

Create:
docs/evidence/TB-TMAR-ORDER-GOLDEN-001/order-data-ownership-audit.md

11. Result / error semantics — mandatory

Order must converge to canonical semantic Result/error handling.

Repair:

ex.Message classification

Contains/StartsWith message classification

hardcoded Persian/English business error identity

PlatformHttpException inside module business layers

leaking ex.Message across HTTP/contract boundaries

swallowed expected failures

Known direct finding to eliminate or justify:
catch (InvalidOperationException ex)
when ex.Message is "checkout.conflict" or "inventory.reservation.conflict"

Replace with stable machine-semantic errors/results.

HTTP boundary must use the canonical error presentation / ApiResponseFactory / ProblemDetails infrastructure already accepted by TMAR.

Do not duplicate localization logic.

Unknown exceptions must propagate to central handling unless explicitly and safely transformed.

12. Time / ID / tracing — mandatory

Known direct findings:

DateTimeOffset.UtcNow

UuidV7.New()

Replace production orchestration bypasses with canonical:

IClock

IIdGenerator

IModuleCallTracer where cross-module synchronous calls apply

Pure Domain methods may receive now explicitly.

Do not inject abstractions into deterministic pure helpers only for style.

No hidden fallback:
?? new SystemUtcClock()

No raw activity creation where canonical tracing exists.

13. Conflict/retry logic — critical audit

Direct repository inspection found conflict-winner resolution in Order Infrastructure with approximately:

up to 10 attempts

increasing Task.Delay based on attempt

Audit this carefully.

Determine whether this is:
A) a justified bounded concurrency-resolution algorithm with explicit policy, cancellation, metrics/tests, and no polling smell
or
B) a workaround/magic retry that should be replaced by a deterministic concurrency/idempotency mechanism.

Do NOT merely rename constants.

If retained:

encapsulate policy

eliminate magic numbers

document reason

add deterministic tests

ensure cancellation

ensure no unbounded polling

ensure no hidden latency growth

If replaced:

preserve behavior and idempotency

do not broaden transaction scope

do not introduce new distributed architecture

Create:
docs/evidence/TB-TMAR-ORDER-GOLDEN-001/order-conflict-resolution-audit.md

14. CheckoutProcessManager quality

Audit CheckoutProcessManager in depth.

Verify:

orchestration responsibility remains cohesive

no direct system clock/UUID bypass

no text-based error classification

no silent catch

reservation release failure handling is explicit and observable

process milestones remain durable

idempotency semantics remain intact

TransactionScope behavior remains unchanged unless a proven bug requires bounded repair

no new cross-context ACID dependency

foreign interactions use Contracts

tracing/correlation is preserved

no business truth moves to Host

Do not split the class merely to reduce line count.
Split only by actual responsibility/use-case boundary.

15. Order event/message boundaries

Audit Order integration events and message interactions.

Verify:

stable contract/event semantics

no direct foreign Application handler coupling

Outbox/Inbox behavior preserved

transport replacement remains possible for future microservice extraction

no process-local assumption leaks into public contracts

Do not redesign broker topology.

Create:
docs/evidence/TB-TMAR-ORDER-GOLDEN-001/order-event-boundary-audit.md

16. Physical structure

Target module should converge to:

src/backend/Modules/Order/
Tooba.Order.Domain/
Tooba.Order.Application/
Commands/
Queries/
...
Tooba.Order.Contracts/
Tooba.Order.Infrastructure/
Tooba.Order.Endpoints/
Tooba.Order.Tests/ if existing test ownership warrants dedicated module tests

Do not create empty ceremonial folders.

Path ↔ namespace must align.

No root dump.

No TypeForwardedTo.

Do not move files only for cosmetic architecture.
Ownership/dependency correctness comes first.

17. Durable architecture guards

Extend/create durable guards for Order.

Required protections:

Order is present in canonical COMPLETE HTTP module ownership manifest after successful closure

Tooba.Order.Endpoints exists

Host has no Order-owned business endpoints

module endpoint routing is mapped through Host composition only

Endpoints dispatch through ISender

Order Application uses MediatR 12.5

no foreign Application references from Order Application/Infrastructure

no foreign Infrastructure references

no foreign DbContext use

no Host Order DbContext authority

no direct system clock/id bypass in protected Order layers

no ex.Message business classification

no localized Domain/Application exception identity

no silent catch

no TypeForwardedTo

physical layout/namespace alignment

recovery state matches architecture reality

Guards must be explicit and ownership-aware.
Do not use brittle global regexes.

18. Microservice extraction proof

Create:
docs/evidence/TB-TMAR-ORDER-GOLDEN-001/order-microservice-extraction.md

Prove concrete future extraction.

Document:

Today:
HTTP
→ Order.Endpoints
→ Order.Application
→ Order.Domain/Infrastructure
→ Order DB

Foreign modules:
→ Order.Contracts / integration events

Future:
external HTTP adapter
→ Order service Application
→ Order Domain/Infrastructure
→ Order DB

Foreign services:
→ API/message adapters preserving current contract semantics

Identify:

synchronous inbound surfaces

synchronous outbound contract dependencies

async inbound/outbound messages

database ownership

transaction boundary

checkout process-manager implications

Host dependencies that must be zero/business-neutral

whether extraction requires business rewrite

PASS requires:
READY_WITHOUT_BUSINESS_REWRITE

19. Development/bootstrap handling

Development seeds/bootstrap may remain in Host only through explicit allowlist when they are platform bootstrap concerns.

Do not use Development code as justification for production Host authority.

Any Order development bootstrap exception must be explicit and guarded.

20. Focused validation

Run focused Order/architecture validation including at minimum:

Order-specific tests

Checkout W1-W5 characterization/architecture tests necessary to prove no regression

HostModuleEndpointOwnershipTests

TmarDurableGuardTests

Order architecture guards added by this task

relevant payment/fulfillment/returns/inventory contract characterization where Order edges changed

final:
dotnet build src/backend/Tooba.slnx

Do NOT run:

frontend

unrelated broad UI suites

Tax/Pricing behavioral work unless directly required for compile characterization

Run broader backend tests only if a change makes them necessary.

21. Protected state

Do NOT regress or reopen accepted Golden modules:

Cart

Settlement

Fulfillment

Returns

Notification

Support

Wallet

Payment

Promotion

Offer

Inventory

Checkout:
PAUSED_AT_SAFE_W5_CHECKPOINT

Tax:
UNCHANGED

Pricing:
UNCHANGED

Frontend:
FROZEN

No production frontend changes.

No broad refactor of unrelated modules.

22. Git / user-work safety

Repository safety is mandatory.

Protected user-work ancestor remains:
18ca10c9

Forbidden:

git reset

git clean

force push

destructive checkout

unsafe restore

unsafe rebase

blind stash manipulation

broad git add .

If pre-existing user work conflicts:
return RECOVERY_CONFLICT.

Preserve unrelated untracked/user files.

23. Recovery SoT — mandatory same cycle

Because USER_REVIEW_GOLDEN_WAVE has been explicitly accepted, synchronize Recovery SoT in this task.

Update as appropriate:

docs/architecture/tmar-current-state.json

TOOBA-TMAR-MASTER-RECOVERY.md

TOOBA-ARCHITECT-BOOTSTRAP.md

task recovery-sot

Record previous Golden Wave as:
COMPLETE + USER_ACCEPTED

If Order reaches COMPLETE_REFERENCE_PATTERN:

preferred state:
module: Order
state: COMPLETE_REFERENCE_PATTERN
httpApplicability: HTTP_OWNING
endpointOwnership: MODULE_ENDPOINTS
cqrs: MEDIATR_12_5
lastAcceptedTask: TB-TMAR-ORDER-GOLDEN-001
lastAcceptedCommit: accepted implementation commit

Do NOT guess the next module.

Set:
nextTask = USER_REVIEW_ORDER_GOLDEN

Gate:
USER_REVIEW_REQUIRED_BEFORE_NEXT_TMAR_MODULE

If Order remains incomplete:
nextTask must be exact repair Task-ID:
TB-TMAR-ORDER-GOLDEN-001-R1

Do not mark COMPLETE optimistically.

24. Evidence

Create:

docs/evidence/TB-TMAR-ORDER-GOLDEN-001/

Required evidence:

recovery-start.md

order-http-ownership-audit.md

order-host-authority-audit.md

order-cqrs-use-case-map.md

order-cross-module-boundary-audit.md

order-contract-surface-audit.md

order-data-ownership-audit.md

order-event-boundary-audit.md

order-result-error-audit.md

order-time-id-tracing-audit.md

order-conflict-resolution-audit.md

order-checkout-process-manager-audit.md

order-physical-tree.md

order-microservice-extraction.md

architecture-guard-audit.md

focused-validation.md

recovery-state-sync.md

recovery-sot.md

Evidence must reflect actual repository state after implementation.
Do not generate ceremonial evidence disconnected from code.

25. Success criteria

PASS only if ALL are true:

Order-State:
COMPLETE_REFERENCE_PATTERN

Order-HTTP-Applicability:
HTTP_OWNING

Order-Endpoint-Ownership:
MODULE_ENDPOINTS

Order-CQRS:
MEDIATR_12_5

Order-Host-HTTP-Authority:
NONE

Order-Host-Business-Authority:
NONE

Order-Host-DbAuthority:
NONE

Order-CrossModule-Boundary:
CONTRACTS_ONLY

Order-Contracts-Surface:
EXTRACTION_SAFE

Order-Data-Ownership:
MODULE_OWNED

Order-Event-Boundary:
EXTRACTION_SAFE

Order-Result-Semantics:
STABLE

Order-Time-Id-Tracing:
COMPLIANT

Order-Conflict-Resolution:
DETERMINISTIC_AND_BOUNDED

Order-Checkout-W5-Behavior:
PRESERVED

Order-Physical-State:
VERIFIED_ON_DISK_AND_NAMESPACE

Order-Architecture-Guards:
ENFORCED

Order-Microservice-Extraction:
READY_WITHOUT_BUSINESS_REWRITE

Protected-Golden-Modules:
UNCHANGED_COMPLETE

Checkout-State:
PAUSED_AT_SAFE_W5_CHECKPOINT

Frontend-Production-Changes:
NONE

Recovery-State:
CURRENT_AND_MACHINE_READABLE

Recovery-Next-Task:
USER_REVIEW_ORDER_GOLDEN

Full-Build:
PASS

Residual-Defects:
NONE

If any criterion is not true:
Status must NOT be PASS.
Return INCOMPLETE and exact repair path.

26. Canonical Result

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT

Task-ID: TB-TMAR-ORDER-GOLDEN-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT

Summary:
Program-Name:
Track:
Recovery-Start:
Previous-Golden-Wave-State:
Previous-Golden-Wave-User-Review:
Order-HTTP-Applicability:
Order-Endpoint-Ownership:
Order-HTTP-Ownership-Audit:
Order-Host-HTTP-Authority:
Order-Host-Business-Authority:
Order-Host-DbAuthority:
Order-CQRS:
Order-CrossModule-Boundary:
Order-Contracts-Surface:
Order-Data-Ownership:
Order-Event-Boundary:
Order-Result-Semantics:
Order-Time-Id-Tracing:
Order-Conflict-Resolution:
Order-Checkout-Process-Manager:
Order-Checkout-W5-Behavior:
Order-Physical-State:
Order-Architecture-Guards:
Order-Microservice-Extraction:
Focused-Validation:
Skipped-Validation:
Full-Validation:
AntiPattern-Gate:
Residual-Defects:
Order-State:
Cart-State:
Settlement-State:
Fulfillment-State:
Returns-State:
Notification-State:
Support-State:
Wallet-State:
Payment-State:
Promotion-State:
Offer-State:
Inventory-State:
Checkout-State:
Tax-State:
Pricing-State:
Frontend-Production-Changes:
Recovery-State:
Recovery-Next-Task:
Current-State-Manifest:
Git:
Blockers:
User-Work-Preserved:
Next-Recommended-Task:

If PASS:
Next-Recommended-Task: USER_REVIEW_ORDER_GOLDEN

If repair required:
Next-Recommended-Task: TB-TMAR-ORDER-GOLDEN-001-R1

END_TOOBA_WORKER_RESULT

27. STOP rule

After returning the Result:

STOP completely.

Do NOT:

start Order repair automatically

start Checkout W6

select another module

poll

fetch the next task

continue implementation

write Worker IDLE

The Architect will inspect the Result AND independently verify the repository before issuing any next task.

END_TOOBA_TASK