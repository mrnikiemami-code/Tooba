PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_REPAIR_TASK

Task-ID:
TB-TMAR-NEXT-MODULE-BATCH-004-R3

Parent-Task:
TB-TMAR-NEXT-MODULE-BATCH-004-R2

Channel:
tooba-main

WorkerId:
tooba-worker-01

AgentType:
cursor

Program:
TMAR — Tooba Microservice-Ready Architecture Recovery

Mode:
FAST-SAFE

Track:
REFERENCE_BATCH_REPAIR

Title:
Fulfillment Final Host Adapter / Shipping Helper Closure

Backend-Only:
YES

Architect verdict

R2 is NOT fully accepted yet.

Direct repository verification confirms many R2 claims:

ShippingService admin CRUD now uses ISender + ApiResponseFactory.

Work-queue bulk logic moved into Fulfillment.Application.

AdminFulfillmentWorkQueueComposer deleted.

Localization Contracts seam exists.

Returns remains clean.

However, two concrete Host-authority seams remain and violate the task's own constraints.

Do NOT start BATCH-005.
Do NOT modify Tax/Pricing.
Checkout remains PAUSED_AT_SAFE_W5_CHECKPOINT.
Frontend remains frozen.

1. Remaining defect A — HostAdminOrderFulfillmentOperations

Verified file:
src/backend/Host/Tooba.Host/Admin/HostAdminOrderFulfillmentOperations.cs

Current behavior:

Host implements IAdminOrderFulfillmentOperations

wraps Host AdminOrderOperationsComposer

catches PlatformHttpException

catches InvalidOperationException

converts Host behavior into an Order.Contracts outcome

This means Fulfillment Application still depends operationally on a Host-owned business implementation.

That violates:

Host transport/composition only

task R2 "Fulfillment Application must not depend on Host"

"no Host delegate/callback workaround"

public Order contract implementation should be owned by Order side

Required repair A

Implement IAdminOrderFulfillmentOperations on the Order side:

Order.Application or Order.Infrastructure as appropriate

no Host dependency

no Host types

no AdminOrderOperationsComposer

Use existing Order-owned services/ports/domain logic to perform exactly the required fulfillment admin operations.

Then:

delete HostAdminOrderFulfillmentOperations.cs

register Order-owned implementation in Order module DI

Fulfillment continues consuming only Order.Contracts

Do NOT broadly recover Order.
Do NOT resume Checkout.
Do NOT refactor unrelated admin order operations.

Preserve exact operation semantics for the fulfillment bulk actions.

2. Remaining defect B — shipping methods tree helper in Host

Verified inside:
src/backend/Host/Tooba.Host/Admin/ShippingServiceEndpoints.cs

Public helper:
ListEnabledMethodsTreeAsync(...)

Still owns Fulfillment-specific projection/business behavior:

reads IShippingCatalogReader

resolves language

filters enabled shipping methods

fallback to ShippingMethodRegistry

builds provider/icon/color/options projection

hardcoded default option projection

hardcoded color mapping

This is Fulfillment-specific application/query behavior and should not remain in Host.

Required repair B

Move this use case into Fulfillment.Application behind a query/use-case boundary, e.g.:

ListEnabledShippingMethodsTreeQuery

Result/query handler

module response DTO/model

Host caller(s) should only:

send query / call module-owned application boundary

return/use result

Move:

enabled-code filtering

language fallback

catalog fallback

default colors/options projection

DTO shaping

out of Host.

Delete Host DefaultColor / DefaultOptions and module-specific tree projection logic if no longer used.

Preserve exact output shape consumed by current order/shipment UI.

3. Scope

ONLY:

Order-side implementation of IAdminOrderFulfillmentOperations

Fulfillment shipping-methods tree query ownership

required DI/compile updates

focused tests/guards

Do NOT:

broadly recover Order

touch Returns behavior

modify Tax/Pricing

resume Checkout

touch frontend

4. Behavior preservation
Order fulfillment operation seam

Preserve exact semantics currently reached through:
AdminOrderOperationsComposer.ExecuteAsync

for only the action codes used by Fulfillment bulk.

At minimum preserve:

mark processing

mark packed

create shipment

assign tracking

dispatch

deliver

cancel shipment if supported

same missing/stale/invalid-state outcomes

same side effects

same fulfillment/order linkage

If the existing Host composer delegates to lower-level Order/Fulfillment-owned components, reuse those lower-level owned components rather than copy logic.

No duplicated business rules.

Shipping methods tree

Preserve:

enabled method filtering

language selection/fallback

catalog fallback when empty

code/label/name/providerKind/iconKey/colorKey

active options only

express/standard fallback semantics

sort order behavior

No output contract drift.

5. Order boundary constraints

Order-owned implementation may depend only on legitimate Order-owned dependencies / public Contracts.

Forbidden:

Order module -> Host

service locator

reflection invocation of Host composer

duplicated Host model types

TypeForwardedTo

callback delegate registered from Host

If the current Host composer contains logic that belongs to Order but is too broad to move safely:
extract only the specific fulfillment-operation core needed by IAdminOrderFulfillmentOperations into an Order-owned service.
Keep broader Host endpoints untouched.

6. Result semantics

IAdminOrderFulfillmentOperations.TryExecuteAsync outcome semantics must remain stable for Fulfillment bulk:

success true + null error

failure false + stable semantic error code

unexpected failures propagate only where appropriate

Do not swallow arbitrary exceptions.

Do not reduce all errors to null/unknown if a stable code exists.

7. Shipping query architecture

Target:

Host / order caller
→ ISender
→ Fulfillment Application query
→ IShippingCatalogReader + ILanguageLookup
→ response DTO

No Fulfillment-specific projection logic in Host.

Host may keep wire/consumer adaptation only if truly transport-shaped.

8. Architecture guards

Add/strengthen guards:

Must fail if:

Host implements IAdminOrderFulfillmentOperations

Host contains HostAdminOrderFulfillmentOperations

Fulfillment work-queue application path requires Host registration

Order implementation references Tooba.Host

ShippingServiceEndpoints.cs contains:

ListEnabledMethodsTreeAsync

DefaultColor

DefaultOptions

direct IShippingCatalogReader use for the methods-tree projection

shipping tree fallback/projection logic

Add guard proving:

IAdminOrderFulfillmentOperations implementation assembly is Order-owned

shipping tree query lives under Fulfillment.Application

path↔namespace aligned for any new files

9. Focused tests

Required:

Order fulfillment operations:

one successful bulk operation path through Order-owned adapter

one semantic failure path with preserved error code

no Host dependency test/guard

Shipping methods tree:

catalog-backed result

empty-catalog fallback

language fallback

inactive options filtered

stable output shape

Reuse existing tests when possible.

10. Evidence

Create under:
docs/evidence/TB-TMAR-NEXT-MODULE-BATCH-004-R3/

Required:

recovery-start.md

order-fulfillment-operation-boundary.md

shipping-tree-ownership-audit.md

behavior-preservation-audit.md

host-authority-scan.md

recovery-sot.md

host-authority-scan.md must prove:

HostAdminOrderFulfillmentOperations = absent

Host implementation of IAdminOrderFulfillmentOperations = 0

Host shipping methods tree business/projection logic = 0

11. Validation — FAST-SAFE

Required:

Fulfillment focused tests/guards

Order focused build/tests for this contract seam

focused Host compile/tests for changed callers

final:
dotnet build src/backend/Tooba.slnx

Do NOT run:

broad Host suite

broad Order suite unrelated to changed seam

Returns full suite

Checkout workflow

Tax/Pricing

frontend

No retries/sleeps.

12. Success state

Only if true:

Fulfillment-State:
COMPLETE_REFERENCE_PATTERN

Fulfillment-Physical-State:
VERIFIED_ON_DISK_AND_NAMESPACE

Fulfillment-CrossModule-Boundary:
CONTRACTS_ONLY

Fulfillment-Endpoint-State:
HOST_THIN_TRANSPORT

Fulfillment-Host-DbAuthority:
NONE

Fulfillment-Shipping-Presentation:
CENTRALIZED

Fulfillment-WorkQueue-Authority:
APPLICATION_OWNED

Fulfillment-ShippingTree-Authority:
APPLICATION_OWNED

Order-Fulfillment-Operations-Implementation:
ORDER_OWNED

Host-Fulfillment-Business-Adapter:
NONE

Returns-State:
COMPLETE_REFERENCE_PATTERN

Returns-Endpoint-State:
HOST_THIN_TRANSPORT

Returns-Host-DbAuthority:
NONE

Returns-Error-Presentation:
CENTRALIZED

Behavior-Preservation:
VERIFIED

Notification-State:
COMPLETE_REFERENCE_PATTERN
Support-State:
COMPLETE_REFERENCE_PATTERN
Wallet-State:
COMPLETE_REFERENCE_PATTERN
Payment-State:
COMPLETE_REFERENCE_PATTERN
Inventory-State:
COMPLETE_REFERENCE_PATTERN
Promotion-State:
COMPLETE_REFERENCE_PATTERN
Offer-State:
COMPLETE_REFERENCE_PATTERN
Foundation-State:
RESULT_PATTERN_FOUNDATION_COMPLETE

Tax-State:
DEFERRED_PHYSICAL_REVIEW_BY_USER
Pricing-State:
DEFERRED_PHYSICAL_REVIEW_BY_USER
Checkout-State:
PAUSED_AT_SAFE_W5_CHECKPOINT
Frontend-Production-Changes:
NONE

Batch-State:
COMPLETE
Module-Recovery-State:
NEXT_REFERENCE_BATCH_004_COMPLETE
Next-Recommended-Task:
TB-TMAR-NEXT-MODULE-BATCH-005

If either Host seam remains:
return INCOMPLETE with exact path.
Do NOT claim COMPLETE.

13. Canonical Result

Return ONLY BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Track
Recovery-Start
Order-Fulfillment-Operation-Boundary
Order-Fulfillment-Operations-Implementation
Shipping-Tree-Ownership-Audit
Fulfillment-ShippingTree-Authority
Host-Authority-Scan
Behavior-Preservation-Audit
Architecture-Guards
Fulfillment-Validation
Order-Validation
Focused-Validation
Skipped-Validation
Full-Validation
AntiPattern-Gate
Residual-Defects
Fulfillment-State
Fulfillment-Physical-State
Fulfillment-CrossModule-Boundary
Fulfillment-Endpoint-State
Fulfillment-Host-DbAuthority
Fulfillment-Shipping-Presentation
Fulfillment-WorkQueue-Authority
Host-Fulfillment-Business-Adapter
Returns-State
Returns-Endpoint-State
Returns-Host-DbAuthority
Returns-Error-Presentation
Behavior-Preservation
Notification-State
Support-State
Wallet-State
Payment-State
Inventory-State
Promotion-State
Offer-State
Foundation-State
Tax-State
Pricing-State
Checkout-State
Frontend-Production-Changes
Batch-State
Git
Blockers
User-Work-Preserved
Module-Recovery-State
Next-Recommended-Task

After Result:
STOP.
Do NOT poll.
Do NOT fetch next task.
Do NOT write Worker IDLE.

END_TOOBA_REPAIR_TASK