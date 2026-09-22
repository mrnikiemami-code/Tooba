PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-ORDER-GOLDEN-001-R2B
Parent-Task: TB-TMAR-ORDER-GOLDEN-001-R2A
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: FAST-SAFE
Track: ORDER_GOLDEN_IMPLEMENTATION_SLICE_1_PARITY_CQRS_CLOSURE
Title: Admin Order Completeness Full Parity + Offer-Style CQRS/MediatR Closure
Backend-Only: YES

0. Architect verdict

Parent:
TB-TMAR-ORDER-GOLDEN-001-R2A = INCOMPLETE

Architect independently verified repository state through the latest pushed R2A commits.

R2A production progress is real and MUST be preserved:

Tooba.Order.Endpoints exists

Admin Order Completeness routes are module-owned

endpoints use ISender

Application has IRequest/IRequestHandler implementations

Host completeness endpoint/composer remain deleted

Host DbContext authority for this slice remains removed

explicit order.view / order.handle checks were restored at presentation boundary

invoice/receipt output was substantially expanded

Checkout remains PAUSED_AT_SAFE_W5_CHECKPOINT

But R2A is NOT complete.

Architect directly verified remaining defects:

actor labels missing

note actor fields missing

full operational history sources missing

seller/product labels missing

deterministic kind tie-break missing

typed delete outcome missing

legacy test helper remains

invoice shared-unit quantity parity not closed

dedicated parity tests incomplete

CQRS physical structure does NOT yet match the accepted Offer reference pattern

1. GOLDEN CQRS REFERENCE LOCK — OFFER IS AUTHORITATIVE

For CQRS/MediatR architecture in this task, use the CURRENT Offer module as the reference pattern.

Architect directly verified these canonical files:

src/backend/Modules/Offer/Tooba.Offer.Endpoints/OfferEndpointModule.cs

src/backend/Modules/Offer/Tooba.Offer.Endpoints/Seller/OfferSellerEndpoints.cs

src/backend/Modules/Offer/Tooba.Offer.Application/Commands/CreateOffer/CreateOfferCommand.cs

src/backend/Modules/Offer/Tooba.Offer.Application/Queries/GetOffer/GetOfferQuery.cs

src/backend/Modules/Offer/Tooba.Offer.Application/Ports/IOfferStore.cs

src/backend/Modules/Offer/Tooba.Offer.Tests/Application/OfferHandlerTests.cs

src/backend/Modules/Offer/Tooba.Offer.Tests/Architecture/OfferArchitectureGuardTests.cs

src/backend/Modules/Offer/Tooba.Offer.Tests/Endpoints/OfferEndpointModuleTests.cs

Required architectural shape:

Endpoint
→ ISender
→ one explicit Command or Query
→ one IRequestHandler
→ Result / Result<T>
→ Application-owned Port / read model service
→ Infrastructure implementation
→ foreign modules through Contracts only

This is NOT optional style guidance.
It is the structural reference for Order CQRS closure.

2. Current Order CQRS structural defect

Current file:

src/backend/Modules/Order/Tooba.Order.Application/Admin/Completeness/AdminOrderCompleteness.cs

currently contains:

error codes

actor model

note view models

history models

printable document model

IAdminOrderCompletenessStore

6 Requests

6 Handlers

This is functionally MediatR, but NOT yet the accepted physical CQRS structure.

R2B must split it into explicit per-use-case files/folders similar to Offer.

Target example:

Tooba.Order.Application/
Admin/
Completeness/
Errors/
AdminOrderCompletenessErrorCodes.cs
Models/
AdminOrderActor.cs
AdminOrderNoteView.cs
AdminOrderHistoryEntry.cs
AdminOrderOperationalHistoryPage.cs
AdminOrderPrintableDocument.cs
Ports/
IAdminOrderCompletenessStore.cs
[narrow foreign projection ports if Order-owned]
Commands/
AddAdminOrderNote/
AddAdminOrderNoteCommand.cs
DeleteAdminOrderNote/
DeleteAdminOrderNoteCommand.cs
Queries/
ListAdminOrderNotes/
ListAdminOrderNotesQuery.cs
GetAdminOrderOperationalHistory/
GetAdminOrderOperationalHistoryQuery.cs
GetAdminOrderInvoice/
GetAdminOrderInvoiceQuery.cs
GetAdminOrderReceipt/
GetAdminOrderReceiptQuery.cs

Exact naming may vary slightly if repository conventions demand it, but:

one use case per file/folder

Request + Handler colocated like Offer

no mega AdminOrderCompleteness.cs

no generic handler switch

no mediator facade wrapping a directory

no endpoint direct directory calls

3. MediatR registration

Order Application assembly must continue to be registered through canonical:

AddToobaCqrsFoundation(typeof(<Order command/query>).Assembly)

Do not manually register individual handlers.

Do not create a second Mediator container.

Do not bypass MediatR with direct service calls from endpoints.

Add a guard proving:

Order completeness endpoints contain ISender

each endpoint constructs/sends its specific Command/Query

each Request has an IRequestHandler<...>

no endpoint references Infrastructure

no endpoint calls IAdminOrderCompletenessStore directly

4. R2B exact scope

Close ONLY the Admin Order Completeness slice completely.

Required closure areas:

A) Offer-style CQRS physical structure
B) full behavior parity against pre-R2 baseline
C) actor labels
D) notes parity
E) operational-history parity
F) invoice parity
G) receipt parity
H) typed error/outcome semantics
I) real parity/architecture tests
J) Recovery SoT progression to R3

Do NOT migrate:

AdminOrderOperations

AdminOrdersGrid

OrderInventoryRecoveryComposer itself

OrderSupplyComposer itself

StorefrontCheckoutComposer

global Order foreign Application edges unrelated to this slice

Those are later slices.

5. Authoritative behavior baseline

Baseline commit:
113a017a81b893caefcbde63ee0d4a4ebb8d613c

Behavior reference:
src/backend/Host/Tooba.Host/Admin/AdminOrderCompletenessComposer.cs
at that commit.

This baseline is behavioral only.

Forbidden:

restoring that Host composer

restoring its foreign DbContexts

restoring its foreign Application/Infrastructure dependencies

restoring PlatformHttpException business flow

Reimplement equivalent observable behavior cleanly through module boundaries.

6. Permission parity

Preserve exact current restored rules:

order.view
for:

ListAdminOrderNotesQuery

GetAdminOrderOperationalHistoryQuery

GetAdminOrderInvoiceQuery

GetAdminOrderReceiptQuery

order.handle
for:

AddAdminOrderNoteCommand

DeleteAdminOrderNoteCommand

Host may keep:
HostOrderAdminAuthorizer

only as a thin Host security adapter.

It may depend on Host AccessControl integration because auth is a Host/platform concern.

Order Application must NOT reference:

AccessControl.Application

HttpContext

Host types

Add explicit tests for:

authenticated admin without order.view => denied

authenticated admin without order.handle => denied

order.view success

order.handle success

deny-by-default behavior

7. Actor-label parity

Restore actor projection behavior equivalent to baseline.

Required fields/semantics for notes/history:

actor kind

display name

Persian display phrase

English display phrase

system actor

missing/unknown user actor

Baseline fallback semantics:

usable profile DisplayName

usable FirstName + LastName

usable email

usable mobile

unknown-user fallback

Do NOT use:

OperatorProfile.Application from Order

Identity.Application from Order

foreign DbContext

Host composer

Use/extend the smallest stable Contracts read seams in owning modules.

If Contract seams are added:

implement them in owning module Infrastructure

keep protected module architecture intact

add/execute focused guards

Do NOT expose entire aggregates.

8. Notes parity

Restore the full prior note wire/read behavior.

Required:

NoteId

CheckoutId

Body

CreatedByUserId

CreatedAt

actor Kind

actor DisplayName

actor DisplayFa

actor DisplayEn

CanDelete

Do not shrink response shape relative to baseline.

Typed delete outcome

Current:
AdminOrderCompletenessStore.DeleteNoteAsync
catches ANY InvalidOperationException and returns false.

This is forbidden.

Replace with stable typed/result semantics.

Expected delete-forbidden condition must be identifiable without exception-message inference and without catching every InvalidOperationException.

Unknown/programming/infrastructure exceptions MUST propagate.

Preferred:

Order-owned Result/typed outcome from note delete path
or

specific semantic exception type only if unavoidable internally, converted to Result in handler

Handler must return:
Result
with stable:
order.note.delete.forbidden

No ex.Message classification.

9. Operational-history full parity

R2B must restore ALL supported baseline history kinds.

Required matrix:

Order

order_created

order_cancelled

inventory_released

order_restored

Payment

payment_created

payment_pending

payment_deposit_restored

payment_succeeded

payment_failed

payment_deposit_rejected

payment_cancelled

payment_expired

payment_refund_pending

payment_refunded

payment_refund_failed

Fulfillment

fulfillment_processing

fulfillment_packed

shipment_created

tracking_assigned

tracking_corrected

shipment_cancelled

allocation_released

shipment_dispatched

shipment_delivered

Consolidated package

consolidated_package_created

consolidated_package_member_added

consolidated_package_cancelled

consolidated_package_members_released

consolidated_package_dispatched

consolidated_package_members_dispatched

consolidated_package_delivered

consolidated_package_members_delivered

Returns/refunds

return_requested

return_approved

return_rejected

refund_completed

refund_failed

refund_retried

Settlement

settlement_cancel_adjustment

settlement_adjustment

settlement_accrual

Inventory recovery compatibility

inventory_recovery_requested

inventory_recovery_succeeded

inventory_recovery_failed_insufficient

inventory_recovery_manual_review

Notes

operational_note

No kind may be silently dropped.

10. Foreign history sources — CONTRACTS ONLY

For this slice, foreign projection dependencies must be Contracts-only.

Payment

Prefer existing:
Payment.Contracts.Admin.IPaymentAdminGateway

Extend only if required.

Fulfillment

Use/extend:
Fulfillment.Contracts

No:

Fulfillment.Application

Fulfillment.Domain

Fulfillment.Infrastructure

Expose only DTO facts needed for history.

Returns

Use/extend:
Returns.Contracts

No:

Returns.Application

Returns.Domain

Returns.Infrastructure

Expose only:

request lifecycle

actor

lines/quantities

refund attempts

Settlement

Use/extend:
Settlement.Contracts

No:

Settlement.Application

Settlement.Domain

Settlement.Infrastructure

Expose stable DTO enum/string semantics, not Domain enums.

Party

Use:
Party.Contracts

for seller display names.

Catalog

Use:
Catalog.Contracts

for variant/product display title projection.

Identity / OperatorProfile

Use/extend their stable Contracts/read seams for actor labels.

If a module currently lacks a Contracts project and adding one is clearly required:
make the smallest architecture-safe contract addition.
Do NOT perform a broad module refactor in this task.

11. No foreign Domain leakage

R2B must not introduce foreign Domain types into:

Order.Application

Order.Endpoints

History source DTOs must be contract DTOs.

Do not compare foreign Domain enums directly in Order Application.

Map them to stable Contracts enum/code in owning module adapter.

12. History projection design

Do NOT rebuild the old 1000+ LOC composer.

Use decomposition.

Preferred Order Application shape:

Admin/Completeness/History/

history source ports/interfaces

source-neutral history fact DTOs

pure projection/mapping helpers

merge/sort/paging logic

Infrastructure:

Order-owned history facts from OrderDbContext

no foreign DbContext

Foreign modules:

contract-based readers

The MediatR Handler remains the use-case owner and coordinates the projection through Application abstractions.

Do not place the entire business/read composition in Endpoint.

Do not make Infrastructure a god composer bypassing Application logic.

13. Deterministic history ordering

Must match baseline:

OccurredAt descending

Kind ordinal ascending as stable tie-break

Paging occurs AFTER the full cross-source merge.

Add deterministic tests with equal timestamps.

14. Seller/product labels

Restore quantity-aware display summaries.

Seller:
Party.Contracts

Product title:
Catalog.Contracts

Preserve baseline fallback semantics:

unknown seller => stable seller fallback

unknown title => stable ordered-item fallback

No technical GUID should replace a human-readable title when baseline provided a label.

15. Inventory-recovery legacy compatibility

Do not import Host:
OrderInventoryRecoveryComposer

For historical persisted notes, if stable prefixes are the only available data:

Create an Order-owned compatibility parser with named constants.

This is allowed only as:
HISTORICAL_PERSISTED_NOTE_COMPATIBILITY

It must NOT be used for:

exception classification

general error routing

new write behavior

Add focused tests for all four recognized historical prefixes.

Unknown notes remain:
operational_note

16. Invoice parity — FULL closure

Current R2A renderer is closer but still incomplete.

Restore all baseline supported invoice behavior, including:

reference

timestamp

recipient first/last/name fallback

contact mobile

province

city

postal address

line table

quantity formatting

unit price

line total

item count

subtotal

discount

net before tax

tax

duty

tax+duty

grand total

payment status

HTML escaping

RTL print layout

Shared-unit total quantity

Baseline used:
InvoiceHeaderSemantics.HasSharedUnit(...)
and conditionally displayed total quantity.

Preserve equivalent semantics WITHOUT moving Host helper dependency into module.

If this logic is Order-owned:
move/extract a pure Order-owned helper to the appropriate module layer.

Do not reference Host InvoiceHeaderSemantics.

Add dedicated test for:

all lines compatible shared unit => total quantity displayed

mixed/non-compatible units => aggregate quantity not misleadingly displayed

No new pricing/tax calculation.
Display only committed Order snapshots.

17. Receipt parity

Fully characterize and preserve baseline output.

Required:

amount

currency

status

provider display semantics

completion/update timestamp

provider transaction/request reference behavior

masking behavior if baseline did so

order reference

HTML escaping

RTL printable layout

Use Payment.Contracts only.

Add dedicated receipt tests.

18. Error semantics

Follow Offer reference:

Commands/Queries:
IRequest<Result<T>> or IRequest<Result>

Handlers:
return Result.Failure(...) with SemanticError(stableCode) for expected semantic failures.

Endpoints:
ApiResponseFactory api
→ api.From(result)

No local catch blocks for expected semantic errors.

No:

PlatformHttpException in Order Application/Domain

ex.Message classification

Results.Json local ProblemDetails builders

Accept-Language branching in Order endpoint

custom local semantic mapper

Add architecture tests patterned after Offer guards.

19. Error catalog/localization

If Order completeness error codes are already globally catalogued, use them.

If not:
add module-owned error catalog resources following Offer pattern:

Order.Endpoints/
Errors/
Resources/

with:

stable error definitions

FA/EN resources

IErrorCatalogContributor

IErrorResourceSet

Register via:
AddOrderEndpointPresentation()

Do not hardcode localized error prose in Application handlers.

20. CQRS tests patterned after Offer

Create a dedicated Order test project if none exists, or use the proper existing Order module test location.

Required handler tests must construct real ISender through:
AddToobaCqrsFoundation(typeof(<Order request>).Assembly)

Then call:
sender.Send(...)

Do not directly invoke Handler.Handle in all tests as the only proof.

Required tests:

List notes through ISender

Add note through ISender

Delete note through ISender

History through ISender

Invoice through ISender

Receipt through ISender

Use fakes for Application ports.

This proves real MediatR registration/discovery.

21. CQRS architecture guards patterned after Offer

Add Order guards equivalent in spirit to Offer:

Endpoints

contain ISender sender

contain specific new ...Command/Query

use ApiResponseFactory

no direct IAdminOrderCompletenessStore

no direct DbContext

no direct Infrastructure

no local exception mapping

Application

contains IRequest / IRequestHandler

one use case per physical folder/file

no Host refs

no Endpoints refs

no foreign Application refs for this slice

no foreign Infrastructure refs

expected failures use Result

Infrastructure

implements Application ports

no Host references

no foreign DbContext

foreign source refs Contracts only

Physical

mega AdminOrderCompleteness.cs removed

no TypeForwardedTo

path/namespace alignment

22. Legacy test helper

Current:
AdminOrderCompletenessLegacyTestHelpers.cs

Audit it.

If it exists only to preserve old static helper access and bypasses production architecture:
DELETE it and migrate tests to real production units.

If a tiny pure test helper remains:
it must not emulate missing production behavior.

R2B PASS requires:
Legacy-Test-Helper-State = REMOVED_OR_NON_MASKING

23. MediatR package/version lock

Use existing canonical:
MediatR 12.5.0

Do not add another version.

Do not use legacy:
IMediator service locator patterns
or custom mediator abstraction.

Use:
ISender

24. Focused validation

Run at minimum:

new Order completeness handler tests via ISender

new Order completeness endpoint tests

new Order completeness architecture guard tests

permission tests

actor label tests

note typed delete tests

every operational history category test

deterministic ordering test

paging-after-merge test

invoice shared-unit tests

receipt tests

AdminOrderCompletenessTests

InvoiceHeaderAggregateTests / InvoiceHeaderSemanticsTests if still relevant

HostModuleEndpointOwnershipTests

TmarDurableGuardTests

CheckoutOrderFoundationTests

architecture guards for every protected Contracts module changed

dotnet build src/backend/Tooba.slnx

If the previously reported Checkout Postgres fixture failure is environmental/pre-existing:
document exact failure separately.
Do not call it PASS if caused by this change.

25. Protected state

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

Checkout:
PAUSED_AT_SAFE_W5_CHECKPOINT

Tax:
UNCHANGED

Pricing:
UNCHANGED

Frontend:
FROZEN

Do NOT start Checkout W6.

26. Recovery SoT

If R2B PASS:

Order remains overall:
INCOMPLETE_REFERENCE_REPAIR

But completedSlices must include:

ORDER_ENDPOINTS_FOUNDATION

ADMIN_ORDER_COMPLETENESS_CQRS

ADMIN_ORDER_COMPLETENESS_BEHAVIOR_PARITY

ADMIN_ORDER_COMPLETENESS_OFFER_STYLE_CQRS

Set:
nextTask = TB-TMAR-ORDER-GOLDEN-001-R3
nextTaskGate = ORDER_GOLDEN_REPAIR_REQUIRED

Do NOT mark Order COMPLETE.
Do NOT add Order to completeReferenceModules.
Do NOT start R3 automatically.

If R2B incomplete:
nextTask = TB-TMAR-ORDER-GOLDEN-001-R2C

27. Evidence

Create:

docs/evidence/TB-TMAR-ORDER-GOLDEN-001-R2B/

Required:

recovery-start.md

r2a-repository-verification.md

offer-cqrs-reference-map.md

cqrs-physical-structure.md

cqrs-handler-map.md

mediatr-registration-proof.md

permission-parity.md

actor-label-parity.md

notes-parity.md

typed-delete-outcome.md

operational-history-parity-matrix.md

foreign-contract-delta.md

seller-product-label-parity.md

inventory-recovery-compatibility.md

invoice-parity.md

receipt-parity.md

legacy-helper-audit.md

result-error-semantics.md

architecture-guard-audit.md

focused-validation.md

recovery-state-sync.md

recovery-sot.md

offer-cqrs-reference-map.md must explicitly map:
Offer reference file/pattern → corresponding Order implementation.

28. R2B PASS criteria

PASS only if ALL:

Order-CQRS-Reference:
OFFER_PATTERN_CONFORMANT

Order-MediatR:
12_5_REAL_ISENDER_HANDLERS

Order-CQRS-Physical-Structure:
ONE_USE_CASE_PER_FOLDER_FILE

Order-Endpoints-Foundation:
PRESERVED

AdminOrderCompleteness-Host-Composer:
ABSENT

AdminOrderCompleteness-Host-Endpoints:
ABSENT

AdminOrderCompleteness-Permission-Parity:
PRESERVED

AdminOrderCompleteness-Note-Parity:
PRESERVED

AdminOrderCompleteness-Typed-Delete:
STABLE_SEMANTIC_OUTCOME

AdminOrderCompleteness-Actor-Labels:
PRESERVED

AdminOrderCompleteness-History-Parity:
FULL_SUPPORTED_PARITY

AdminOrderCompleteness-History-Kinds:
ALL_BASELINE_SUPPORTED

AdminOrderCompleteness-History-Ordering:
DETERMINISTIC

AdminOrderCompleteness-Seller-Labels:
PRESERVED

AdminOrderCompleteness-Product-Labels:
PRESERVED

AdminOrderCompleteness-Invoice-Parity:
FULL_SUPPORTED_PARITY

AdminOrderCompleteness-Shared-Unit-Invoice:
TESTED

AdminOrderCompleteness-Receipt-Parity:
FULL_SUPPORTED_PARITY

AdminOrderCompleteness-Foreign-Boundary:
CONTRACTS_ONLY

AdminOrderCompleteness-Foreign-DbContext:
NONE

AdminOrderCompleteness-Result-Semantics:
OFFER_PATTERN_STABLE_RESULTS

AdminOrderCompleteness-Unknown-Exception-Handling:
PROPAGATES

Legacy-Test-Helper-State:
REMOVED_OR_NON_MASKING

Order-Completeness-Architecture-Guards:
ENFORCED

Protected-Golden-Modules:
UNCHANGED_COMPLETE

Checkout-State:
PAUSED_AT_SAFE_W5_CHECKPOINT

Order-Overall-State:
INCOMPLETE_REFERENCE_REPAIR

Recovery-Next-Task:
TB-TMAR-ORDER-GOLDEN-001-R3

Full-Build:
PASS

If any criterion is false:
Status = INCOMPLETE
Next-Recommended-Task = TB-TMAR-ORDER-GOLDEN-001-R2C

29. Canonical Result

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT

Task-ID: TB-TMAR-ORDER-GOLDEN-001-R2B
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT

Summary:
Program-Name:
Track:
Recovery-Start:
R2A-Repository-State:
Offer-CQRS-Reference:
Order-CQRS-Reference:
Order-MediatR:
Order-CQRS-Physical-Structure:
Order-Endpoints-Foundation:
AdminOrderCompleteness-Host-Composer:
AdminOrderCompleteness-Host-Endpoints:
AdminOrderCompleteness-Permission-Parity:
AdminOrderCompleteness-Note-Parity:
AdminOrderCompleteness-Typed-Delete:
AdminOrderCompleteness-Actor-Labels:
AdminOrderCompleteness-History-Parity:
AdminOrderCompleteness-History-Kinds:
AdminOrderCompleteness-History-Ordering:
AdminOrderCompleteness-Seller-Labels:
AdminOrderCompleteness-Product-Labels:
AdminOrderCompleteness-Invoice-Parity:
AdminOrderCompleteness-Shared-Unit-Invoice:
AdminOrderCompleteness-Receipt-Parity:
AdminOrderCompleteness-Foreign-Boundary:
AdminOrderCompleteness-Foreign-DbContext:
AdminOrderCompleteness-Result-Semantics:
AdminOrderCompleteness-Unknown-Exception-Handling:
Legacy-Test-Helper-State:
Contracts-Changed:
Protected-Module-Guard-Validation:
MediatR-Handler-Validation:
Endpoint-Architecture-Validation:
Architecture-Guard-Validation:
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
Next-Recommended-Task: TB-TMAR-ORDER-GOLDEN-001-R2C

END_TOOBA_WORKER_RESULT

30. STOP rule

After Result:
STOP completely.

Do NOT:

start R3

start R2C

migrate AdminOrderOperations

migrate AdminOrdersGrid

remove unrelated Order foreign Application refs

touch StorefrontCheckout

start Checkout W6

select next module

poll

fetch next task

write Worker IDLE

Architect will independently inspect repository before any next task.

END_TOOBA_TASK