PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-ORDER-GOLDEN-001-R2A
Parent-Task: TB-TMAR-ORDER-GOLDEN-001-R2
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: FAST-SAFE
Track: ORDER_GOLDEN_IMPLEMENTATION_SLICE_1_PARITY_REPAIR
Title: Admin Order Completeness Full Behavior Parity + Contract-Safe Projection Repair
Backend-Only: YES

Architect verdict on R2

R2 Result = INCOMPLETE.

Architect independently inspected repository commit:
6ef22c8f862a98dabf886c7ed17d00c388fe5aaf

R2 made real production progress and that progress MUST be preserved:

Tooba.Order.Endpoints exists.

AdminOrderCompleteness Host endpoint implementation was removed.

AdminOrderCompleteness Host composer was removed.

six module-owned MediatR routes exist.

module-owned ISender dispatch exists.

HostOrderAdminAuthorizer is a small presentation adapter.

Order persistence for this slice moved behind Order Infrastructure.

Checkout remains PAUSED_AT_SAFE_W5_CHECKPOINT.

protected Golden modules remain complete.

Do NOT roll this migration back.

However R2 did NOT preserve the complete pre-migration behavior.

The worker reported operational-history loss, but Architect direct comparison found additional behavior/security regressions that R2A MUST address.

1. Authoritative behavior baseline

For THIS slice only, behavior baseline is the production implementation immediately before R2:

Commit:
113a017a81b893caefcbde63ee0d4a4ebb8d613c

Reference file:
src/backend/Host/Tooba.Host/Admin/AdminOrderCompletenessComposer.cs
at that commit.

Use this file only as a behavioral specification/reference.

DO NOT restore the old Host composer.
DO NOT restore Host DbContext access.
DO NOT restore foreign Application/Infrastructure coupling.

R2A must reproduce supported behavior through:
Order.Endpoints
→ ISender
→ Order.Application
→ Order.Infrastructure + stable foreign Contracts

2. Architect direct findings — mandatory

Architect directly confirmed the following regressions/residuals.

A. Fine-grained permission semantics were lost

Old behavior explicitly required:

order.view
for:

list notes

operational history

invoice

receipt

order.handle
for:

add note

delete note

Old implementation called:
EnsurePermissionAsync(actorUserId, permissionId, ...)

New HostOrderAdminAuthorizer only calls:
AdminPanelAccess.RequireAuthorizedAsync(...)

That proves general admin authorization, but does NOT prove the old order.view / order.handle permission semantics.

R2A must restore them without importing AccessControl.Application into Order.Application.

B. Actor labels were lost

Old note/history projection resolved actor labels from:

OperatorProfile

Identity contact lookup

and exposed human-readable actor semantics such as:

system

user

display name

localized actor display

Current R2 AdminOrderNoteView contains only:

CreatedByUserId
and lost the prior actor display projection.

Operational history also lost actor-label resolution.

R2A must restore public behavior through stable Contracts/read gates.

C. Operational history is substantially incomplete

Current R2 history contains only:

order_created

order_cancelled

operational_note

Old behavior included at minimum:

Order:

order_created

order_cancelled

inventory_released

order_restored

Payment:

payment_created

payment_pending

payment_deposit_restored

payment_succeeded

payment_failed / payment_deposit_rejected

payment_cancelled

payment_expired

payment_refund_pending

payment_refunded

payment_refund_failed

Fulfillment:

fulfillment_processing

fulfillment_packed

shipment_created

tracking_assigned

tracking_corrected

shipment_cancelled

allocation_released

shipment_dispatched

shipment_delivered

Consolidated packages:

consolidated_package_created

consolidated_package_member_added

consolidated_package_cancelled

consolidated_package_members_released

consolidated_package_dispatched

consolidated_package_members_dispatched

consolidated_package_delivered

consolidated_package_members_delivered

Returns/refunds:

return_requested

return_approved

return_rejected

refund_completed

refund_failed

refund_retried

Settlement:

settlement_cancel_adjustment

settlement_adjustment

settlement_accrual

Inventory recovery note mapping:

inventory_recovery_requested

inventory_recovery_succeeded

inventory_recovery_failed_insufficient

inventory_recovery_manual_review

Plus:

seller display names

product/variant titles

actor labels

quantity-aware summaries

deterministic ordering

Restore supported parity.

D. Invoice output was simplified

Current R2 invoice renderer is NOT behaviorally equivalent to the prior renderer.

Old invoice included substantially richer persisted snapshot output, including:

order reference

timestamp

recipient information

mobile

province/city/address

line rows

quantities

unit prices

line totals

subtotal

net before tax

tax

duty

discount

total tax+duty

grand total

item count

shared-unit total quantity behavior

payment status

print-ready HTML semantics

R2A must preserve the prior observable invoice behavior unless a field can be proven dead/unreachable.

Do not reconstruct business truth from foreign modules if Order already owns the necessary snapshots.

E. Receipt parity must be verified, not assumed

Current receipt is simplified.

Compare it against pre-R2 behavior and restore all observable fields/semantics that existed.

Use Payment.Contracts only.

F. Test adaptation may not hide regression

R2 added:
AdminOrderCompletenessLegacyTestHelpers.cs

and modified existing tests.

Audit these changes carefully.

Tests MUST characterize production behavior.
Do not use helper shims to make old tests compile while silently dropping behavior.

If a legacy helper bypasses the new production path or encodes reduced expectations, repair/remove it.

3. R2A exact objective

Complete the R2 vertical slice with REAL behavior parity.

After R2A:

AdminOrderCompleteness is still owned by:
Tooba.Order.Endpoints
→ ISender
→ Order.Application handlers

Host composer remains deleted.

Host endpoint implementation remains deleted.

No Host DbContext authority returns.

All relevant pre-R2 externally observable behavior is preserved through clean module boundaries.

4. Permission architecture — mandatory

Restore exact use-case permission requirements.

Required semantic rules:

order.view

ListAdminOrderNotesQuery

GetAdminOrderOperationalHistoryQuery

GetAdminOrderInvoiceQuery

GetAdminOrderReceiptQuery

order.handle

AddAdminOrderNoteCommand

DeleteAdminOrderNoteCommand

Do NOT:

inject HttpContext into Application

reference AccessControl.Application from Order.Application

let Application call Host

duplicate authorization truth in multiple handlers

Preferred pattern:
Order.Endpoints / presentation authorization adapter obtains an actor context capable of proving required Order permission, OR a narrow stable authorization contract/gate is consumed.

Choose the smallest architecture-consistent design.

Authorization must remain deny-by-default.

Add focused tests:

authenticated admin lacking order.view → denied

authenticated admin lacking order.handle → denied

view permission does not imply handle unless policy says so

authorized actor succeeds

5. Actor-label contract closure

Restore actor label behavior without:

OperatorProfile.Application dependency from Order

Identity.Application dependency from Order

Host composer

foreign DbContext

Use stable Contracts/read interfaces.

If Contracts do not exist:
add minimal read-only contracts in owning modules.

Required data semantics:

display name if usable

first+last name fallback if previously supported

email fallback

mobile fallback

system actor

missing-user actor

avoid broken/placeholder encoding-only values

Do NOT expose full user/profile aggregates.

Return a compact projection contract only.

6. Party/seller label contract closure

Operational history needs seller display labels.

Use:
Party.Contracts

Do not reference:

Party.Application

Party.Infrastructure

PartyDbContext

If current IPartyLookup is sufficient, use it.
If not, extend it minimally and compatibly.

7. Catalog product/variant title closure

Old history looked up localized product names from CatalogDbContext.

R2A must NOT restore CatalogDbContext.

Use Catalog.Contracts.

Required semantics:

variant → product/title lookup sufficient for order-history display

locale preference equivalent to previous behavior where practical

deterministic fallback to "ordered item" presentation semantics

Do not make Order own Catalog localization truth.

8. Fulfillment history contract

Fulfillment is protected COMPLETE_REFERENCE_PATTERN.

Do NOT reference Fulfillment.Application or Infrastructure.

Use existing Fulfillment.Contracts if adequate.

If history-specific read projection is missing:
add the smallest stable read contract in Fulfillment.Contracts and implement it inside Fulfillment module.

Required facts are only those needed to reproduce prior history entries:

fulfillment status/timestamps

item quantities/order-line ids

shipment metadata/status/timestamps

tracking/current+previous references

consolidated package lifecycle/member facts

seller party ids

created-by actor when relevant

Do not expose Fulfillment Domain aggregates.

Run Fulfillment architecture guards if Contracts change.

9. Returns/refund history contract

Returns is protected COMPLETE_REFERENCE_PATTERN.

Do NOT consume Returns.Application/Domain/Infrastructure from Order.

Use/extend Returns.Contracts minimally.

Projection must support prior history semantics:

return request lifecycle

request actor

returned order-line quantities

refund attempt status/amount/currency/timestamps

Do not duplicate Return business logic in Order.

Run Returns architecture guards if changed.

10. Settlement history contract

Settlement is protected COMPLETE_REFERENCE_PATTERN.

Do NOT consume Settlement.Application/Domain/Infrastructure.

Use/extend Settlement.Contracts minimally.

Projection needs only prior history facts:

sellerOrderId

source type

entry type semantic

postedAt

If debit/credit classification is needed, expose stable contract enum/value.
Do not leak Settlement Domain enum.

Run Settlement architecture guards if changed.

11. Payment history and receipt

Current R2 already uses:
Payment.Contracts.Admin.IPaymentAdminGateway

Verify that this contract provides all needed prior history/receipt facts.

If sufficient:
do not add another contract.

If insufficient:
extend Payment.Contracts minimally.

No Payment.Application/Domain/Infrastructure usage may be introduced.

Run Payment guards if contract changes.

12. Inventory recovery history semantics

Do NOT make Order history depend on Host OrderInventoryRecoveryComposer constants.

Current old behavior recognized note prefixes owned historically by Host.

Move the stable semantic prefixes/codes to an appropriate Order-owned semantic location OR replace note-text inference with already persisted stable semantics if available.

No new StartsWith/message classification debt in Application.

If persisted historical notes only contain prefixes and compatibility requires parsing:

isolate it as an explicit legacy compatibility parser

use stable named constants

document it as historical-data compatibility, NOT exception/error classification

add focused tests

do not spread parsing elsewhere

Do not touch Inventory HTTP applicability.

13. Operational-history projection design

Do NOT put a 1000+ line god composer back into Infrastructure.

Decompose by source responsibility.

Suggested architecture:

Application:

GetAdminOrderOperationalHistoryQuery/Handler

orchestration over narrow read ports

projection DTOs

deterministic merge/sort/paging

Infrastructure adapters:

Order-owned snapshot reader

implementations of Order-owned ports when backed by Order DB

Foreign module facts:

Contracts interfaces implemented by owning modules

Pure projection helpers:

event-to-history mapping

quantity formatting

actor display mapping

No Host code.

No cross-module DbContext.

No foreign Application/Infrastructure references.

14. Determinism requirements

Preserve deterministic history ordering:

Primary:
OccurredAt descending

Tie-break:
Kind ordinal or equivalent stable deterministic rule matching old behavior.

Paging applies AFTER complete history composition, as before.

Do not page each source independently and then merge incorrectly.

Cap behavior such as returns limit should match prior production behavior unless explicitly justified by a test/evidence note.

15. Note behavior parity

Restore note projection parity.

Compare old AdminOrderNoteView with new model.

Preserve:

note id

checkout id

body

creator id

created timestamp

actor kind/display fields that existed

CanDelete semantics

Do not silently shrink the API contract.

If wire JSON names changed in R2, restore compatibility.

16. Invoice parity

Use pre-R2 renderer as behavioral reference.

Move rendering to a clean module location:

Endpoint presentation renderer, or

pure deterministic renderer fed by Application projection

No persistence calls inside renderer.

No foreign DbContext.

Preserve:

HTML content type

RTL/printability

escaping

persisted financial snapshots

recipient/shipping snapshot

line table

totals

payment status

quantity formatting

Do not calculate new tax/pricing business truth.

Only display Order-owned committed snapshots.

17. Receipt parity

Compare exact pre-R2 observable behavior.

Use Payment.Contracts projection plus Order snapshot where needed.

Preserve:

amount/currency

payment status

payment/provider references

timestamps or other previously emitted metadata

escaping/RTL printable HTML

Do not query Payment DB directly.

18. Error semantics

R2A must preserve R2's semantic Result improvement.

No:

PlatformHttpException in Order module

ex.Message HTTP mapping

exception text classification

localized exception identity in Application/Domain

However do not flatten all InvalidOperationException into one error.

Current DeleteNoteAsync catches ANY InvalidOperationException and returns false.
Audit this.

Use typed/stable semantic outcome where necessary so unrelated programming/infrastructure exceptions are not misreported as delete-forbidden.

Unknown failures must propagate.

19. Clock/ID

No new direct system clock/UUID bypass.

Use canonical:

IClock

IIdGenerator where needed

Do not add unused now parameters merely for appearance.
Current AdminOrderCompletenessStore.AddNoteAsync(... DateTimeOffset now ...) passes now but implementation does not use it.
Either make ownership meaningful or remove the redundant seam without changing note timestamp semantics.

20. Architecture dependency hard gates for this slice

After R2A, AdminOrderCompleteness path must have:

Order.Endpoints
↛ Infrastructure

Order.Application
↛ foreign Application
↛ foreign Infrastructure

Order.Infrastructure implementation for this slice
↛ foreign Application
↛ foreign Infrastructure

Foreign facts:
Contracts only.

Host:
auth adapter + registration/mapping only.

No foreign DbContext.

21. Tests — behavior parity, not compile parity

Create/repair tests that compare the NEW path against the old supported behavior.

At minimum cover:

Permissions:

view allowed/denied

handle allowed/denied

Notes:

list

add

delete

CanDelete

actor labels

missing/system actor fallback

History:

Order created/cancelled/restored/inventory release

Payment states

Fulfillment processing/packed

shipment create/tracking/correction/cancel/dispatch/deliver

consolidated package lifecycle

return lifecycle

refund lifecycle

settlement entries

inventory-recovery legacy notes

seller labels

product titles

actor labels

deterministic ordering

paging after merge

Invoice:

recipient/shipping fields

lines

financial totals

quantities

payment status

HTML escaping

Receipt:

prior observable fields/semantics

Architecture:

no Host composer resurrection

no Host endpoint resurrection

no Host OrderDbContext for this slice

no foreign DbContexts

no foreign App/Infra dependency introduced by this slice

Endpoints → ISender

Do not use AdminOrderCompletenessLegacyTestHelpers as a compatibility facade that bypasses production code.
Tests must exercise real production path/helpers.

Delete or narrow that helper if it masks regression.

22. Focused validation

Run at minimum:

AdminOrderCompletenessTests

new permission tests

new operational history parity tests

new invoice parity tests

new receipt parity tests

Order endpoint architecture tests

HostModuleEndpointOwnershipTests

TmarDurableGuardTests

CheckoutOrderFoundationTests

protected module architecture guards for any Contracts modules changed

dotnet build src/backend/Tooba.slnx

Do not run frontend.

23. Protected state

Remain COMPLETE:

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

No Checkout W6.

24. Git/user-work safety

Protected user-work ancestor:
18ca10c9

Forbidden:

reset

clean

force push

unsafe checkout

unsafe restore

unsafe rebase

blind stash manipulation

broad git add .

If user work conflicts:
RECOVERY_CONFLICT

25. Recovery SoT

If R2A PASS:

Order remains:
INCOMPLETE_REFERENCE_REPAIR

completedSlices must include:

ORDER_ENDPOINTS_FOUNDATION

ADMIN_ORDER_COMPLETENESS_CQRS

ADMIN_ORDER_COMPLETENESS_BEHAVIOR_PARITY

Set:
nextTask = TB-TMAR-ORDER-GOLDEN-001-R3
nextTaskGate = ORDER_GOLDEN_REPAIR_REQUIRED

Do NOT mark Order COMPLETE yet.
Do NOT add Order to completeReferenceModules yet.
Do NOT start R3.

If R2A remains incomplete:
nextTask = TB-TMAR-ORDER-GOLDEN-001-R2B

26. Evidence

Create:

docs/evidence/TB-TMAR-ORDER-GOLDEN-001-R2A/

Required:

recovery-start.md

r2-repository-verification.md

pre-r2-behavior-baseline.md

permission-parity.md

actor-label-contract.md

operational-history-parity-matrix.md

foreign-source-contract-map.md

invoice-parity.md

receipt-parity.md

notes-parity.md

legacy-helper-audit.md

error-semantics-audit.md

dependency-boundary-audit.md

architecture-guard-audit.md

focused-validation.md

recovery-state-sync.md

recovery-sot.md

The operational-history parity matrix must list every old history kind and show:

source owner

new contract

new implementation path

test covering it

No generic "restored" statements.

27. R2A PASS criteria

PASS only if ALL:

Order-Endpoints-Foundation:
PRESERVED

AdminOrderCompleteness-Host-Composer:
ABSENT

AdminOrderCompleteness-Host-Endpoints:
ABSENT

AdminOrderCompleteness-Permission-Parity:
PRESERVED_ORDER_VIEW_AND_HANDLE

AdminOrderCompleteness-Note-Parity:
PRESERVED

AdminOrderCompleteness-Actor-Labels:
PRESERVED

AdminOrderCompleteness-History-Parity:
FULL_SUPPORTED_PARITY

AdminOrderCompleteness-History-Ordering:
DETERMINISTIC

AdminOrderCompleteness-Invoice-Parity:
PRESERVED

AdminOrderCompleteness-Receipt-Parity:
PRESERVED

AdminOrderCompleteness-Foreign-Boundary:
CONTRACTS_ONLY

AdminOrderCompleteness-Foreign-DbContext:
NONE

AdminOrderCompleteness-Host-DbAuthority:
NONE

AdminOrderCompleteness-Result-Semantics:
STABLE

AdminOrderCompleteness-Unknown-Exception-Handling:
PROPAGATES

AdminOrderCompleteness-Architecture-Guards:
ENFORCED

AdminOrderCompleteness-Behavior-Regression:
NONE_FOUND

Checkout-State:
PAUSED_AT_SAFE_W5_CHECKPOINT

Protected-Golden-Modules:
UNCHANGED_COMPLETE

Order-Overall-State:
INCOMPLETE_REFERENCE_REPAIR

Recovery-Next-Task:
TB-TMAR-ORDER-GOLDEN-001-R3

Full-Build:
PASS

If ANY R2A criterion is false:
Status = INCOMPLETE
Next-Recommended-Task = TB-TMAR-ORDER-GOLDEN-001-R2B

28. Canonical Result

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT

Task-ID: TB-TMAR-ORDER-GOLDEN-001-R2A
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT

Summary:
Program-Name:
Track:
Recovery-Start:
R2-Repository-State:
Behavior-Baseline:
Order-Endpoints-Foundation:
AdminOrderCompleteness-Host-Composer:
AdminOrderCompleteness-Host-Endpoints:
AdminOrderCompleteness-Permission-Parity:
AdminOrderCompleteness-Note-Parity:
AdminOrderCompleteness-Actor-Labels:
AdminOrderCompleteness-History-Parity:
AdminOrderCompleteness-History-Kinds:
AdminOrderCompleteness-History-Ordering:
AdminOrderCompleteness-Invoice-Parity:
AdminOrderCompleteness-Receipt-Parity:
AdminOrderCompleteness-Foreign-Boundary:
AdminOrderCompleteness-Foreign-DbContext:
AdminOrderCompleteness-Host-DbAuthority:
AdminOrderCompleteness-Result-Semantics:
AdminOrderCompleteness-Unknown-Exception-Handling:
Legacy-Test-Helper-State:
Contracts-Changed:
Protected-Module-Guard-Validation:
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
Next-Recommended-Task: TB-TMAR-ORDER-GOLDEN-001-R2B

END_TOOBA_WORKER_RESULT

29. STOP rule

After Result:
STOP completely.

Do NOT:

start R3

start R2B

migrate AdminOrderOperations

migrate AdminOrdersGrid

remove unrelated foreign Application edges

touch StorefrontCheckout

start Checkout W6

select another module

poll

fetch next task

write Worker IDLE

Architect will independently inspect repository before issuing any next task.

END_TOOBA_TASK