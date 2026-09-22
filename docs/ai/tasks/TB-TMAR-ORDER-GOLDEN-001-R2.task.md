PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-ORDER-GOLDEN-001-R2
Parent-Task: TB-TMAR-ORDER-GOLDEN-001-R1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: FAST-SAFE
Track: ORDER_GOLDEN_IMPLEMENTATION_SLICE_1
Title: Order Endpoints Foundation + Admin Order Completeness CQRS Migration
Backend-Only: YES

Architect verdict on R1

R1 Result = INCOMPLETE and is accepted as an accurate diagnosis.

Architect independently verified repository state after commit:
83a0581a74e0d02d5be927bd66225051f73a101c

Critical finding:
R1 made NO production-code implementation changes.
The commit contains task/evidence/recovery metadata only.

Therefore R2 MUST be an implementation task, not another audit-only pass.

Do not attempt to close all Order debt in one pass.

This task is intentionally bounded to one real vertical slice so the Order migration proceeds safely and measurably.

1. Current Recovery SoT

Current state is authoritative:

Golden Wave = COMPLETE + USER_ACCEPTED

Order = INCOMPLETE_REFERENCE_REPAIR

nextTask = TB-TMAR-ORDER-GOLDEN-001-R2

Checkout = PAUSED_AT_SAFE_W5_CHECKPOINT

Frontend = FROZEN

Protected COMPLETE modules:

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

Do not reopen them broadly.

2. R2 exact scope

R2 has TWO implementation objectives only:

A) Create the real Order Endpoints/MediatR module foundation.

B) Fully migrate the AdminOrderCompleteness HTTP vertical slice out of Host business ownership.

Do NOT attempt the following in this task:

AdminOrderOperations migration

AdminOrdersGrid migration

OrderInventoryRecoveryComposer migration

OrderSupplyComposer migration

StorefrontCheckoutComposer migration

removal of all foreign Application project references

Checkout W6

Tax/Pricing work

frontend work

Those belong to later repair slices.

R2 PASS means this slice is complete and architecture-correct.
It does NOT mean Order as a whole is COMPLETE_REFERENCE_PATTERN.

3. Create Tooba.Order.Endpoints — mandatory

Create:

src/backend/Modules/Order/Tooba.Order.Endpoints/
Tooba.Order.Endpoints.csproj

Add to:
src/backend/Tooba.slnx

Project responsibilities:

Order-owned HTTP routes

Order wire request/response DTOs where needed

endpoint presentation composition

ISender dispatch only

canonical Result/ApiResponseFactory presentation

Forbidden:

OrderDbContext

direct Infrastructure references

foreign DbContexts

direct business directories

Host composers

business orchestration

Required DI/mapping surface:

AddOrderEndpointPresentation(...) or equivalent canonical module registration

MapOrderEndpoints(...)

Host should only invoke those module surfaces.

4. MediatR 12.5 foundation — mandatory

Ensure Order.Application is wired for MediatR 12.5 following the accepted Golden pattern.

Create real use-case folders for this R2 slice.

Suggested shape:

Tooba.Order.Application/
Admin/
Completeness/
Commands/
Queries/
Models/
Errors/

Use exact responsibility-oriented structure if repository conventions suggest a better equivalent.

Do not create empty ceremonial folders.

Endpoint flow must be:

Order.Endpoints
→ ISender
→ Command / Query
→ Handler
→ Order-owned persistence/services + stable foreign Contracts

No endpoint may call:

ICheckoutDirectory directly

OrderDbContext directly

Host composer

foreign Application service directly

5. Migrate AdminOrderCompletenessEndpoints completely

Current Host file:
src/backend/Host/Tooba.Host/Admin/AdminOrderCompletenessEndpoints.cs

Current Host composer:
src/backend/Host/Tooba.Host/Admin/AdminOrderCompletenessComposer.cs

Current routes include:

GET /v1/admin/orders/{checkoutId}/notes

POST /v1/admin/orders/{checkoutId}/notes

DELETE /v1/admin/orders/{checkoutId}/notes/{noteId}

GET /v1/admin/orders/{checkoutId}/operational-history

GET /v1/admin/orders/{checkoutId}/invoice.html

GET /v1/admin/orders/{checkoutId}/receipt.html

After R2:
these routes must be owned by Tooba.Order.Endpoints.

Host must no longer contain their business endpoint implementation.

Preserve route URLs and external behavior unless canonical error presentation intentionally changes representation.

6. Required CQRS use cases

Implement real MediatR handlers for at least:

Queries:

ListAdminOrderNotesQuery

GetAdminOrderOperationalHistoryQuery

GetAdminOrderInvoiceQuery

GetAdminOrderReceiptQuery

Commands:

AddAdminOrderNoteCommand

DeleteAdminOrderNoteCommand

Names may vary only if the semantics remain explicit and one-use-case-per-handler.

Do not build one generic "AdminOrderCompletenessCommand".

7. Authorization boundary

Current Host uses:
AdminPanelAccess.RequireAuthorizedAsync(...)

Do NOT move Host authentication/session types into Order.Application.

Use the Golden pattern:

Host provides a tiny Order admin authorizer adapter if required

Order.Endpoints asks a narrow module presentation authorization abstraction

Application receives an authenticated/authorized actor identity, not HttpRequest/Host session types

The adapter may live in Host because global auth/session/tenant is Host responsibility.

It must NOT perform Order business logic.

Document the exact retained adapter.

8. Remove AdminOrderCompletenessComposer business authority

AdminOrderCompletenessComposer must not remain the business implementation behind new handlers.

Migrate its responsibilities to appropriate Order Application/Infrastructure boundaries.

Do not simply inject the Host composer into handlers.

After R2:
production Host must not need AdminOrderCompletenessComposer.

Delete it if fully obsolete.

If a tiny pure HTML renderer remains genuinely presentation-only:

move it to Order.Endpoints or Order.Application presentation model as appropriate

ensure it has no DbContext/module orchestration responsibility

document why

9. OrderDbContext ownership for this slice

Current Host composer directly uses:
OrderDbContext

After R2:
Admin Order Completeness paths must access Order persistence only from Order.Infrastructure through Order-owned Application ports/repositories/query services.

Forbidden:
Host → OrderDbContext

for these migrated routes.

If other Order Host features still use OrderDbContext, do NOT claim global Host Db authority is closed yet.

R2 only closes it for AdminOrderCompleteness.

10. Foreign module access for this slice

Current composer reaches multiple foreign modules for invoice/history/labels/payment context.

For THIS migrated slice:
Order Application may not add new foreign Application/Infrastructure dependencies.

Use existing Contracts where available.

Where a required read has no stable contract:
create the smallest stable Contracts seam in the owning module.

Potentially involved:

Payment

Party

Identity

OperatorProfile

Fulfillment

Returns

Settlement

Catalog

Rules:

no foreign Domain leakage

no DbContext leakage

no Host type leakage

no broad protected-module refactor

tiny Contracts-only additions allowed

keep protected COMPLETE modules structurally COMPLETE

If a capability is purely presentation metadata and genuinely belongs outside Order, use a narrow read contract rather than importing the implementation.

11. Result/error closure for this slice

Current Host implementation contains:

PlatformHttpException

ex.Message mapping

localized messages used as business classification/presentation

For migrated AdminOrderCompleteness use cases:

business failures use stable semantic Result/ErrorDescriptor

no ex.Message classification

no PlatformHttpException in Order module layers

no hardcoded localized business identity in Application/Domain

HTTP maps through canonical ApiResponseFactory / ProblemDetails/localization

Unknown exceptions must flow to central exception handling.

Do not create duplicate local error mapper logic.

12. Invoice / receipt output

Preserve current functional capability:

invoice HTML

receipt HTML

But business/data retrieval belongs behind Application queries.

Rendering must not justify Host DbContext or foreign Infrastructure access.

If HTML generation is considered endpoint presentation:
keep renderer in Order.Endpoints, fed by an Application-owned projection DTO.

If rendering is pure deterministic output, keep it separate from persistence/orchestration.

No business write inside renderer.

13. Operational history

Current history composition mixes Order + payment/fulfillment/etc information.

For R2:

preserve externally visible history behavior required by existing tests

use stable read contracts for foreign data

do not query foreign DbContexts

do not reference foreign Infrastructure

do not move foreign business truth into Order

Order may compose a read projection, but authoritative facts remain owned by source modules.

Document each foreign source contract used.

14. Notes

For note list/add/delete:

persistence remains Order-owned

actor/authorization semantics preserved

stable semantic errors

use IClock where needed

no direct system time

no Host business authority

Do not change note lifecycle semantics.

15. Host cleanup for R2

After migration:

Host should remove:

mapping of AdminOrderCompletenessEndpoints implementation

AdminOrderCompletenessComposer registration

direct business dependencies needed only by that composer

Host should add only:

Order endpoint project reference

module endpoint presentation registration

MapOrderEndpoints()

tiny admin authorizer adapter if needed

Do not yet remove unrelated Order Host registrations used by future slices.

16. Program/composition safety

Update Program/composition minimally.

No giant Program.cs refactor.

No new service locator.
No static service resolution.
No duplicate MediatR registration graph.

Follow patterns already proven by:

Settlement

Support

Payment

Promotion

Offer

Use those modules as architectural reference.

17. Physical/namespace rules

Verify:

path ↔ namespace

project references point in correct direction

Endpoints ↛ Infrastructure

Application ↛ Endpoints

Domain ↛ Application/Infrastructure/Host

no TypeForwardedTo

no root dump

18. Durable R2 guards

Add focused guards proving this slice remains migrated.

Required:

Tooba.Order.Endpoints project exists

Host no longer defines AdminOrderCompletenessEndpoints business implementation

Host no longer contains AdminOrderCompletenessComposer

Admin completeness routes are declared in Order.Endpoints

those endpoints dispatch through ISender

handlers live in Order.Application

no OrderDbContext usage from migrated Host path

no PlatformHttpException in migrated module use cases

no ex.Message classification in migrated module use cases

Endpoints does not reference Infrastructure

checkoutState still PAUSED_AT_SAFE_W5_CHECKPOINT

Do NOT add Order to the COMPLETE HTTP manifest yet.
Order is still incomplete after this slice.

19. Tests

Migrate/update existing Admin Order Completeness tests so they validate the module-owned route path.

Run at minimum:

AdminOrderCompletenessTests or equivalent existing focused tests

relevant invoice/receipt/history/note tests

new Order Endpoints architecture tests

HostModuleEndpointOwnershipTests only to prove existing COMPLETE modules unaffected

TmarDurableGuardTests

CheckoutOrderFoundationTests

dotnet build src/backend/Tooba.slnx

Do not run frontend.

If a focused test reveals a real regression, fix it within this slice.

20. Checkout freeze

Hard lock:
Checkout = PAUSED_AT_SAFE_W5_CHECKPOINT

Do not modify:

Checkout process stage

TransactionScope topology

reservation semantics

payment transition semantics

cart conversion semantics

PONR

Saga design

Admin completeness migration must not alter checkout flow.

21. Protected modules

Must remain COMPLETE:

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

If Contracts are added to a protected module:
run that module's architecture guard and prove no regression.

22. Git/user-work safety

Protected user-work ancestor:
18ca10c9

Forbidden:

git reset

git clean

force push

unsafe checkout/restore/rebase

blind stash manipulation

broad git add .

If conflict:
RECOVERY_CONFLICT

23. Recovery SoT after R2

R2 is NOT expected to close the whole Order module.

If this slice is successfully implemented:

Order state remains:
INCOMPLETE_REFERENCE_REPAIR

Record progress such as:
completedSlices:

ORDER_ENDPOINTS_FOUNDATION

ADMIN_ORDER_COMPLETENESS_CQRS

Set:
nextTask = TB-TMAR-ORDER-GOLDEN-001-R3
nextTaskGate = ORDER_GOLDEN_REPAIR_REQUIRED

Do NOT set USER_REVIEW_ORDER_GOLDEN.
Do NOT add Order to completeReferenceModules.

If R2 itself is incomplete:
nextTask = TB-TMAR-ORDER-GOLDEN-001-R2A

Use R2A only for failure to complete THIS exact slice.

24. Evidence

Create:

docs/evidence/TB-TMAR-ORDER-GOLDEN-001-R2/

Required:

recovery-start.md

r1-repository-verification.md

order-endpoints-foundation.md

admin-order-completeness-route-migration.md

admin-order-completeness-cqrs-map.md

admin-order-completeness-host-removal.md

admin-order-completeness-data-boundary.md

admin-order-completeness-contract-boundary.md

admin-order-completeness-result-errors.md

invoice-receipt-projection-boundary.md

operational-history-source-map.md

architecture-guard-audit.md

focused-validation.md

recovery-state-sync.md

recovery-sot.md

Evidence must identify concrete files and types.

25. R2 PASS criteria

PASS for R2 means THIS SLICE is complete.

All must be true:

Order-Endpoints-Project:
CREATED

Order-Endpoints-Foundation:
MODULE_OWNED_ISENDER

AdminOrderCompleteness-Routes:
MIGRATED_TO_ORDER_ENDPOINTS

AdminOrderCompleteness-CQRS:
REAL_MEDIATR_HANDLERS

AdminOrderCompleteness-Host-Endpoints:
REMOVED

AdminOrderCompleteness-Host-Composer:
REMOVED

AdminOrderCompleteness-Host-DbAuthority:
NONE

AdminOrderCompleteness-Foreign-DbContext:
NONE

AdminOrderCompleteness-Foreign-Boundary:
CONTRACTS_ONLY_FOR_NEW_MIGRATION

AdminOrderCompleteness-Result-Semantics:
STABLE

AdminOrderCompleteness-Message-Classification:
NONE

AdminOrderCompleteness-Behavior:
PRESERVED

Order-Overall-State:
INCOMPLETE_REFERENCE_REPAIR

Checkout-State:
PAUSED_AT_SAFE_W5_CHECKPOINT

Protected-Golden-Modules:
UNCHANGED_COMPLETE

Frontend-Production-Changes:
NONE

Recovery-Next-Task:
TB-TMAR-ORDER-GOLDEN-001-R3

Full-Build:
PASS

If any R2 criterion is false:
Status = INCOMPLETE
Next-Recommended-Task = TB-TMAR-ORDER-GOLDEN-001-R2A

26. Canonical Result

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT

Task-ID: TB-TMAR-ORDER-GOLDEN-001-R2
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT

Summary:
Program-Name:
Track:
Recovery-Start:
R1-Repository-State:
Order-Endpoints-Project:
Order-Endpoints-Foundation:
AdminOrderCompleteness-Routes:
AdminOrderCompleteness-CQRS:
AdminOrderCompleteness-Host-Endpoints:
AdminOrderCompleteness-Host-Composer:
AdminOrderCompleteness-Host-DbAuthority:
AdminOrderCompleteness-Foreign-DbContext:
AdminOrderCompleteness-Foreign-Boundary:
AdminOrderCompleteness-Result-Semantics:
AdminOrderCompleteness-Message-Classification:
AdminOrderCompleteness-Behavior:
Invoice-Receipt-Boundary:
Operational-History-Boundary:
Focused-Validation:
Skipped-Validation:
Full-Validation:
AntiPattern-Gate:
Residual-Defects:
Order-Overall-State:
Checkout-State:
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
Next-Recommended-Task: TB-TMAR-ORDER-GOLDEN-001-R3

If incomplete:
Next-Recommended-Task: TB-TMAR-ORDER-GOLDEN-001-R2A

END_TOOBA_WORKER_RESULT

27. STOP rule

After Result:
STOP completely.

Do NOT:

start R3

start R2A

migrate AdminOrderOperations

migrate Grid

touch StorefrontCheckout

start Checkout W6

poll

fetch next task

write Worker IDLE

Architect will inspect repository before issuing the next task.

END_TOOBA_TASK