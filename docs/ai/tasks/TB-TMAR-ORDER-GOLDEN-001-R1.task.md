PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-ORDER-GOLDEN-001-R1
Parent-Task: TB-TMAR-ORDER-GOLDEN-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: FAST-SAFE
Track: ORDER_GOLDEN_REFERENCE_REPAIR_R1
Title: Order Endpoints + CQRS + Host Authority + Contracts Boundary Closure
Backend-Only: YES

Architect verdict on parent task

Parent result:
TB-TMAR-ORDER-GOLDEN-001 = INCOMPLETE

Architect independently verified the repository after the worker result.

The INCOMPLETE verdict is ACCEPTED.

Do NOT repeat the parent audit as if starting from zero.
Continue from the current repository state.

Current recovery SoT correctly says:

nextTask = TB-TMAR-ORDER-GOLDEN-001-R1

nextTaskGate = ORDER_GOLDEN_REPAIR_REQUIRED

activeModuleRecovery.module = Order

activeModuleRecovery.state = INCOMPLETE_REFERENCE_REPAIR

checkoutState = PAUSED_AT_SAFE_W5_CHECKPOINT

goldenWaveUserReview = USER_ACCEPTED

Previous Golden Wave remains protected COMPLETE.

1. Architect direct repository verification — authoritative R1 findings

Architect directly confirmed all of the following after the parent task:

Order physical state

Current module still has only:

Tooba.Order.Domain

Tooba.Order.Application

Tooba.Order.Contracts

Tooba.Order.Infrastructure

Missing:

Tooba.Order.Endpoints

Therefore:
Order-Endpoint-Ownership = INCOMPLETE

Host Order authority still exists

Production Host still owns Order HTTP/business surfaces including at minimum:

Admin/AdminOrderOperationsEndpoints.cs

Admin/AdminOrderCompletenessEndpoints.cs

Admin/AdminOrderOperationsComposer.cs

Admin/AdminOrderCompletenessComposer.cs

Admin/OrderInventoryRecoveryComposer.cs

Admin/OrderSupplyComposer.cs

Grid/AdminOrdersGridQueryEngine.cs

Storefront/StorefrontCheckoutComposer.cs

These are not all harmless transport shells.

Architect directly confirmed:

AdminOrderOperationsComposer directly consumes OrderDbContext

AdminOrderOperationsComposer orchestrates multiple business modules

AdminOrderCompletenessComposer directly consumes OrderDbContext

AdminOrderCompletenessComposer also reaches foreign module Application/Infrastructure/Domain surfaces

AdminOrdersGridQueryEngine directly consumes OrderDbContext

AdminOrdersGridQueryEngine directly consumes PartyDbContext

This is real Host business/data authority and must not remain in the COMPLETE target.

Foreign Application project-reference residuals in Order.Infrastructure

Architect directly confirmed current
Tooba.Order.Infrastructure.csproj
references foreign Application projects:

Cart/Tooba.Cart.Application

Catalog/Tooba.Catalog.Application

Payment/Tooba.Payment.Application

Fulfillment/Tooba.Fulfillment.Application

AccessControl/Tooba.AccessControl.Application

This violates target CONTRACTS_ONLY extraction readiness.

Do not limit R1 to the subset named by the parent Result.
Audit and eliminate ALL foreign Application references from Order Infrastructure/Application unless an explicit architecture exception already exists and is documented by a current lock. No such exception should be invented.

OrderModule direct foreign Application usage

Architect directly confirmed OrderModule.cs imports:

Tooba.Payment.Application.Models

Tooba.Payment.Application.Ports

and registers ports using foreign Application types.

This must converge to Contracts-owned seams.

Parent anti-pattern repair was partial only

CheckoutProcessManager now correctly uses:

IClock

IIdGenerator

CheckoutConflictException

logging for failed reservation release

But residual message classification remains:

inventory.reservation.conflict still classified through ex.Message

Residual direct time/id calls also remain in Order Infrastructure including CheckoutDirectory.

Current CheckoutDirectory still contains direct examples such as:

DateTimeOffset.UtcNow

UuidV7.New()

and localized InvalidOperationException messages.

These must be handled according to semantic ownership and without changing frozen Checkout W1-W5 behavior.

Conflict policy

Parent task bounded the conflict-winner retry but unit-policy closure was not completed.

R1 must prove:

explicit policy

deterministic test coverage

cancellation

bounded maximum wait

no hidden/unbounded polling

no magic-number business dependency

Do not introduce background polling.

2. R1 primary objective

Close Order to a REAL:

COMPLETE_REFERENCE_PATTERN

Required target:

Order.Endpoints
→ ISender
→ Order.Application MediatR 12.5 Commands/Queries
→ Order Domain / Infrastructure
→ foreign Contracts only

Host:
composition/security/global platform only

Order R1 must not merely make tests green.
It must remove the verified ownership and dependency leaks.

3. Create Tooba.Order.Endpoints — mandatory

Create the real project:

src/backend/Modules/Order/Tooba.Order.Endpoints/Tooba.Order.Endpoints.csproj

Add it to:
src/backend/Tooba.slnx

Endpoints must own Order HTTP routes and wire DTO/presentation behavior belonging to Order.

Host must only map the module and supply tiny host-level adapters for:

authentication/session

tenant

authorization integration

global platform concerns

Pattern:

MapOrderEndpoints()
→ module endpoint handlers
→ ISender
→ Order Application Command/Query handlers

Do not put OrderDbContext in Endpoints.
Do not inject Order Infrastructure implementation types into endpoint methods.

4. Mandatory HTTP migration matrix

Use the parent ownership audit and migrate all confirmed ORDER_OWNED_HTTP.

At minimum resolve ownership for:

Admin Order Operations

Current:
Host/Admin/AdminOrderOperationsEndpoints.cs
Host/Admin/AdminOrderOperationsComposer.cs

Move Order-owned operations into:
Order.Endpoints
→ Order.Application Commands/Queries

Host may retain only narrowly defined auth adapter functionality.

Admin Order Completeness

Current:
Host/Admin/AdminOrderCompletenessEndpoints.cs
Host/Admin/AdminOrderCompletenessComposer.cs

Order-owned capabilities include at minimum:

notes

operational history

invoice projection

receipt projection

Do NOT keep these as Host business composers.

If invoice/receipt require cross-module presentation data:
implement Application read composition through stable Contracts/Gates.
Do not use foreign DbContexts.

Admin Orders Grid

Current:
Host/Grid/AdminOrdersGridQueryEngine.cs

Architect verified it directly consumes:

OrderDbContext

PartyDbContext

This is forbidden target state.

Move Order-owned grid query policy/execution behind Order Application query handling.

Party lookup must be through Party.Contracts / stable read gate.
No cross-module DbContext.

Grid infrastructure helpers from BuildingBlocks may be reused if platform-level.

Inventory Recovery / Supply

Audit:

OrderInventoryRecoveryComposer

OrderSupplyComposer

If capability is Order-owned:
move behind Order Application.
If capability belongs to Inventory/Fulfillment:
consume the already accepted module Contracts and keep Order composition within Order Application.

Do not re-open Inventory/Fulfillment internals.

Storefront Checkout

Current:
Host/Storefront/StorefrontCheckoutComposer.cs

This is sensitive because Checkout is frozen at W5.

Separate:
A) Order/Checkout business use cases
from
B) genuine Storefront BFF/presentation composition.

Order writes/business rules must move behind Order Application CQRS.
A thin Storefront BFF may remain only if it:

performs no Order business calculation

performs no Order write

consumes stable module contracts/query interfaces

does not reference Order Infrastructure/DbContext

does not classify business errors by message

Document exact retained BFF responsibilities.

5. Real CQRS, not wrapper theater

For each migrated HTTP use case create real:

Commands/
Queries/
Handlers/

Use MediatR 12.5.0.

Do not implement handlers that simply call back into Host composers.

Allowed migration strategy:

reuse cohesive Order internal services

place orchestration ownership in Application handlers

narrow old directories gradually

Required:
Endpoint → ISender

No endpoint → Directory directly.

No endpoint → DbContext.

No endpoint → Host composer.

6. Remove ALL foreign Application edges from Order

This is a hard R1 gate.

Audit:

Tooba.Order.Application.csproj

Tooba.Order.Infrastructure.csproj

all production using statements/source usage

Target:
Order.Application ↛ foreign Application
Order.Infrastructure ↛ foreign Application

Architect-confirmed current foreign Application project references to eliminate:

Cart.Application

Catalog.Application

Payment.Application

Fulfillment.Application

AccessControl.Application

Also search for any additional foreign Application reference not listed above.

Use existing Contracts where sufficient.

Where no stable Contracts seam exists:
create the SMALLEST necessary Contracts interface/DTO in the owning module.

Rules:

do not expose Domain entities

do not expose EF types

do not expose Infrastructure types

do not move implementation ownership into Order

do not reopen protected modules broadly

tiny contract-only additions to protected modules are allowed only when necessary and guarded

If a protected COMPLETE module requires a contract addition:
preserve its COMPLETE architecture and run its relevant guard tests.

7. Specific foreign edge closure expectations
Cart

Order must consume Cart.Contracts only.
No:

ICartDirectory from Cart.Application

Cart.Application.Conversion

Cart.Application.Ports

Replace required mutation/query seams with existing or narrowly added Cart.Contracts ports.

Catalog

No Catalog.Application dependency.
Use Catalog.Contracts read interfaces or add a minimal Catalog.Contracts seam if genuinely required.

Payment

No Payment.Application Models/Ports in Order.
Use Payment.Contracts.

OrderModule registrations involving Payment Application types must be replaced by Contracts-owned ports/events.

Fulfillment

No Fulfillment.Application dependency from Order.
Use Fulfillment.Contracts.

Protected Fulfillment module must stay COMPLETE.

AccessControl

Order must not reference AccessControl.Application.
Authorization capability should be provided through a stable contract/gate or Host adapter at endpoint boundary, depending on semantic ownership.

Do not embed Host auth types into Order Application.

8. Host DbContext authority removal — mandatory

After R1, production Host must not directly inject/use:

OrderDbContext
for Order business/query paths.

Also remove:

PartyDbContext usage from AdminOrdersGridQueryEngine

Do not replace direct DbContext with another module's Infrastructure service.

Use module-owned Application Queries and Contracts.

Required final scans:

Host → OrderDbContext = NONE for business paths

Host → PartyDbContext in Order grid = NONE

Host → Order.Infrastructure = no business authority

Explicit development/bootstrap exceptions must be separately allowlisted and justified.

9. Result/error semantic closure

R1 must remove text-based business classification.

Mandatory:

remove ex.Message == "inventory.reservation.conflict" classification

use stable Inventory Contracts semantic error/result

Because Inventory is protected COMPLETE_REFERENCE_PATTERN and INTERNAL_ONLY:
add only the minimal typed/stable Contracts seam necessary.
Do NOT create Inventory HTTP.

Replace Order business failures with:

Result / semantic error
or

typed internal semantic exception only where the canonical pattern requires it

HTTP errors must flow through canonical:
ApiResponseFactory / ProblemDetails / central localization

Remove Host response creation based on:

ex.Message

localized business exception text

Unknown exceptions must not be swallowed.

10. Order direct clock / ID audit and repair

Repo-wide production scan under Order.

Repair orchestration-level:

DateTimeOffset.UtcNow

DateTime.UtcNow

Guid.NewGuid()

UuidV7.New()

Use:

IClock

IIdGenerator

For Domain:
pass now / generated IDs explicitly where behavior requires them.

Do NOT inject services into entities merely for style.

Known CheckoutDirectory direct calls must be repaired.

No fallback constructors.

11. Localized exception debt

Audit Order.Application/Infrastructure/Domain for user-facing Persian/English exception identity.

Stable error identity must be machine semantic.

Do not use:
"order.cancel.forbidden: <localized prose>"
as mixed identity/presentation.

Domain/Application must not own locale presentation.

Preserve user-visible behavior at HTTP boundary through centralized localization.

12. Conflict winner policy closure

The parent bounded retry remains under review.

Extract an explicit policy if not already done.

Requirements:

named attempts configuration/policy

named delay policy

deterministic bounded maximum

cancellation-aware

no unbounded polling

no Thread.Sleep

no background loop

no arbitrary retry spread to unrelated paths

focused unit tests proving attempt count and delay schedule

tests for immediate winner

tests for delayed winner

tests for no winner

cancellation test

If current algorithm cannot be justified, replace with a deterministic transaction/idempotency mechanism without widening cross-context ACID.

Record evidence.

13. Checkout W5 freeze — hard lock

Checkout remains:
PAUSED_AT_SAFE_W5_CHECKPOINT

R1 may refactor architecture but MUST NOT:

add W6 behavior

introduce distributed Saga runtime

change PONR

change checkout business semantics

expand TransactionScope across new contexts

add new compensation behavior

change pricing/tax semantics

Preserve all existing W1-W5 characterization tests.

If an architectural migration cannot be completed without workflow semantic change:
return INCOMPLETE with exact blocker.

14. Order module DI/composition

Order.Application handlers must be registered canonically.

Order.Endpoints presentation services must have module-owned registration method, e.g.:
AddOrderEndpointPresentation()

Host composition should reduce to:
services.Add...
app.MapOrderEndpoints()

Do not register a large graph of Order business composers in Host.

No service locator.

No static mutable state.

15. Physical structure

Target:

src/backend/Modules/Order/
Tooba.Order.Domain/
Tooba.Order.Application/
Commands/
Queries/
[real responsibility folders]
Tooba.Order.Contracts/
Tooba.Order.Infrastructure/
Tooba.Order.Endpoints/
Tooba.Order.Tests/ if module-owned focused tests are appropriate

No root dump.

Path ↔ namespace aligned.

No TypeForwardedTo.

Do not create empty ceremonial folders.

16. Durable guards — mandatory

Create/extend Order architecture guards so future regressions fail.

Required checks:

Tooba.Order.Endpoints exists

Order is in canonical HTTP-owned module manifest only after complete

Host does not own Order HTTP business routes

Host does not use OrderDbContext for business paths

Order Endpoints use ISender

Order Endpoints do not reference Infrastructure

Order Application/Infrastructure have no foreign Application project refs

Order Application/Infrastructure have no foreign Infrastructure refs

no foreign DbContext

no direct time/id bypass in protected Order orchestration

no ex.Message classification

no PlatformHttpException in Order module business layers

no hardcoded localized Domain/Application business identity

no silent catch

no TypeForwardedTo

physical namespace/folder alignment

Checkout remains W5 paused in recovery state

Update HostModuleEndpointOwnershipTests only when Order is truly complete.

Do not fake manifest membership before closure.

17. Microservice extraction proof

Update:
docs/evidence/TB-TMAR-ORDER-GOLDEN-001-R1/order-microservice-extraction.md

Final proof must identify:

Inbound HTTP:
Order.Endpoints

Internal dispatch:
MediatR Application handlers

Database:
Order-owned OrderDbContext/schema/migrations

Outbound sync:
foreign Contracts only

Outbound async:
stable integration events/outbox

Host dependency:
platform/security/composition only

Checkout:
W5 process manager remains behaviorally unchanged

PASS requires:
READY_WITHOUT_BUSINESS_REWRITE

18. Focused validation

Run at minimum:

Order module tests / new Order architecture tests

HostModuleEndpointOwnershipTests

TmarDurableGuardTests

CheckoutOrderFoundationTests

AtomicCheckoutCommitTests

CheckoutProcessFoundationTests

CheckoutImplW4InventoryLifecycleTests

CheckoutImplW5PromotionContractTests

OrderInventoryRecoveryTests

OrderOfferContractsCharacterizationTests

relevant Cart/Inventory/Fulfillment/Payment architecture guards if Contracts touched

conflict policy tests

endpoint/CQRS tests

dotnet build src/backend/Tooba.slnx

If changed behavior touches a focused characterization suite, run it.

Do NOT run frontend.

Do not run broad unrelated suites only for ceremony.

19. Protected COMPLETE state

Must remain COMPLETE_REFERENCE_PATTERN:

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
INTERNAL_ONLY / NOT_APPLICABLE / INTERNAL_USE_CASE_BOUNDARIES

Tax:
UNCHANGED

Pricing:
UNCHANGED

Checkout:
PAUSED_AT_SAFE_W5_CHECKPOINT

Frontend:
FROZEN

20. Git/user-work safety

Protected user-work ancestor:
18ca10c9

Forbidden:

git reset

git clean

force push

unsafe checkout

unsafe restore

unsafe rebase

blind stash manipulation

broad git add .

If conflict with user work:
RECOVERY_CONFLICT

Preserve unrelated files/stashes.

21. Recovery SoT

If R1 PASS:

Update:

docs/architecture/tmar-current-state.json

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

R1 recovery-sot

Set Order:

module: Order
state: COMPLETE_REFERENCE_PATTERN
httpApplicability: HTTP_OWNING
endpointOwnership: MODULE_ENDPOINTS
cqrs: MEDIATR_12_5
lastAcceptedTask: TB-TMAR-ORDER-GOLDEN-001-R1
lastAcceptedCommit: accepted implementation commit

Remove:
activeModuleRecovery for Order

Set:
nextTask = USER_REVIEW_ORDER_GOLDEN
nextTaskGate = USER_REVIEW_REQUIRED_BEFORE_NEXT_TMAR_MODULE

Do NOT select Catalog/Checkout/another module automatically.

If not complete:

state remains:
INCOMPLETE_REFERENCE_REPAIR

Set exact:
nextTask = TB-TMAR-ORDER-GOLDEN-001-R2

22. Evidence

Create:

docs/evidence/TB-TMAR-ORDER-GOLDEN-001-R1/

Required:

recovery-start.md

parent-result-verification.md

order-http-migration-matrix.md

order-endpoints-audit.md

order-cqrs-handler-map.md

order-host-authority-closure.md

order-host-dbcontext-closure.md

order-foreign-application-edge-closure.md

order-contract-delta.md

order-result-error-closure.md

order-time-id-closure.md

order-conflict-policy-closure.md

order-checkout-w5-regression-proof.md

order-physical-tree.md

order-microservice-extraction.md

architecture-guard-audit.md

focused-validation.md

recovery-state-sync.md

recovery-sot.md

Evidence must name concrete files/types.
No generic PASS prose.

23. R1 success criteria

PASS only if ALL:

Order-State:
COMPLETE_REFERENCE_PATTERN

Order-HTTP-Applicability:
HTTP_OWNING

Order-Endpoint-Ownership:
MODULE_ENDPOINTS

Order-EndPoints:
REAL_AND_MODULE_OWNED

Order-CQRS:
MEDIATR_12_5_REAL_HANDLERS

Order-Host-HTTP-Authority:
NONE

Order-Host-Business-Authority:
NONE

Order-Host-DbAuthority:
NONE

Order-Host-PartyDbAuthority-For-Order:
NONE

Order-Foreign-Application-Refs:
ZERO

Order-Foreign-Infrastructure-Refs:
ZERO

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

Order-Message-Classification:
NONE

Order-Time-Id-Tracing:
COMPLIANT

Order-Conflict-Resolution:
DETERMINISTIC_BOUNDED_TESTED

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

Full-Validation:
PASS

Residual-Defects:
NONE

If any success criterion is false:
DO NOT return PASS.

Return INCOMPLETE with exact residuals and:
Next-Recommended-Task: TB-TMAR-ORDER-GOLDEN-001-R2

24. Canonical Result

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT

Task-ID: TB-TMAR-ORDER-GOLDEN-001-R1
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT

Summary:
Program-Name:
Track:
Recovery-Start:
Parent-Task-State:
Architect-Verified-Starting-State:
Order-HTTP-Applicability:
Order-Endpoint-Ownership:
Order-Endpoints:
Order-HTTP-Migration:
Order-Host-HTTP-Authority:
Order-Host-Business-Authority:
Order-Host-DbAuthority:
Order-Host-PartyDbAuthority-For-Order:
Order-CQRS:
Order-Foreign-Application-Refs:
Order-Foreign-Infrastructure-Refs:
Order-CrossModule-Boundary:
Order-Contracts-Surface:
Order-Data-Ownership:
Order-Event-Boundary:
Order-Result-Semantics:
Order-Message-Classification:
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

If incomplete:
Next-Recommended-Task: TB-TMAR-ORDER-GOLDEN-001-R2

END_TOOBA_WORKER_RESULT

25. STOP rule

After Result:
STOP completely.

Do NOT:

start R2 automatically

start Checkout W6

select the next module

touch frontend

poll

fetch next task

write Worker IDLE

Architect will independently inspect the repository before any next task.

END_TOOBA_TASK