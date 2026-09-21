PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_REPAIR_TASK

Task-ID:
TB-TMAR-NEXT-MODULE-BATCH-004-R2

Parent-Task:
TB-TMAR-NEXT-MODULE-BATCH-004-R1

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
Fulfillment Shipping Admin + Work Queue Host Closure

Backend-Only:
YES

Architect verdict

R1 is only PARTIALLY accepted.

Direct repository verification confirms:

Fulfillment/Returns cross-module boundaries are Contracts-only.

Fulfillment/Returns Host DbContext query authority was removed.

Returns presentation is centralized and ReturnErrorMapper is deleted.

But Fulfillment is still NOT COMPLETE_REFERENCE_PATTERN because two Fulfillment-owned Host surfaces retain business/presentation authority.

Returns is accepted. Do not reopen Returns except compile-only fallout.

Do NOT start BATCH-005.
Do NOT modify Tax/Pricing.
Checkout remains PAUSED_AT_SAFE_W5_CHECKPOINT.
Frontend remains frozen.

Concrete defects verified
ShippingServiceEndpoints

File:
src/backend/Host/Tooba.Host/Admin/ShippingServiceEndpoints.cs

Still contains:

manual Results.Json(...)

manual { title, errorCode } envelopes

PlatformHttpException mapping

catch (InvalidOperationException ex) exposing ex.Message

Host-owned list/detail projection logic

Host-owned LoadDetailAsync

direct ILanguageDirectory Application dependency

This is a real Fulfillment HTTP surface:
/v1/admin/shipping-services/...

AdminFulfillmentWorkQueueComposer

File:
src/backend/Host/Tooba.Host/Admin/AdminFulfillmentWorkQueueComposer.cs

Still owns:

supported-action validation

empty-selection validation

cross-seller validation

row identity validation

action compatibility

shipment resolution policy

orchestration loop

attempted/succeeded partial-success policy

catches PlatformHttpException/InvalidOperationException

invokes Host AdminOrderOperationsComposer

This is application/business logic and must not remain in Host.

Objective

Close only:

Shipping Service admin HTTP/presentation/application boundary.

Fulfillment work-queue bulk application ownership.

Preserve all already accepted Fulfillment/Returns behavior.

Shipping Service target

Keep existing routes exactly:

GET /v1/admin/shipping-services/

GET /v1/admin/shipping-services/{serviceId}

POST /v1/admin/shipping-services/

PUT /v1/admin/shipping-services/{serviceId}

POST /v1/admin/shipping-services/{serviceId}/deactivate

POST /v1/admin/shipping-services/ensure-seed

Target:
Host thin auth/transport
→ ISender
→ Fulfillment Application Command/Query Handler
→ ports/infrastructure
→ Result / Result<T>
→ ApiResponseFactory

Host must not:

map Fulfillment semantic errors

expose ex.Message

own shipping list/detail projection

call Fulfillment directory directly for these routes

Existing write commands/handlers may be evolved; do not duplicate them.

Add/complete Application queries/use cases for:

list

get

create

update

deactivate

ensure-seed

Expected failures use SemanticError + Result:

shipping_service.not_found

invalid/duplicate code

invalid provider

invalid language

invalid option/translation

existing stable shipping_service.* codes

Unexpected failures remain exceptions.

Localization boundary

Do not make Fulfillment depend on Localization.Application.

If language resolution is needed:

reuse existing Localization.Contracts public port if available;

otherwise extract the minimum stable language lookup contract into Localization.Contracts;

Fulfillment consumes Contracts only.

Preserve current language fallback semantics.

Do not broadly recover Localization.

Shipping DTO / JSON compatibility

Wire-only request DTOs may remain in Host.

Module response projection belongs to Fulfillment Application.

Preserve successful JSON shape.

Work Queue target

Move business logic out of:
AdminFulfillmentWorkQueueComposer

Target:
Host auth/binding
→ ISender
→ ExecuteAdminFulfillmentBulkCommand
→ Fulfillment Application handler/service
→ module ports

Fulfillment Application owns:

action validation

empty selection

cross-seller rule

row mismatch rule

action compatibility

shipment resolution

bulk loop

attempted/succeeded counts

semantic failure codes

Do not move the same logic into another Host wrapper.

Order operation seam

Fulfillment Application must not depend on Host.

Current Host composer calls AdminOrderOperationsComposer.

Use an existing public Order contract if available; otherwise extract the smallest stable Order.Contracts port required for the specific fulfillment operations.

Forbidden:

Fulfillment.Application -> Order.Application

Fulfillment.Infrastructure -> Order.Application

Host delegate/service-locator workaround

duplicated contract types

Preserve exact operation behavior.

Partial-success behavior preservation

Preserve:

whole-set prevalidation where currently done

attempted count

succeeded count

stop/report behavior after downstream failure

shipment-id resolution

supported action codes

cross-seller restriction

row mismatch behavior

Add focused characterization tests.

Presentation closure

After repair ShippingServiceEndpoints.cs must have:

no manual expected-error envelope

no errorCode = ex.Message

no catch (InvalidOperationException ex) for business mapping

no Fulfillment-business PlatformHttpException mapping

central ApiResponseFactory for semantic failures

Fulfillment work queue uses Result/Result<T> + SemanticError.

Use existing Fulfillment error catalog/resources.

Endpoint state

After repair ALL Fulfillment-owned HTTP surfaces must satisfy:

Fulfillment-Endpoint-State: HOST_THIN_TRANSPORT

This includes:

fulfillment routes

shipping-service CRUD

fulfillment work queue

Architecture guards

Add/strengthen guards so they fail on regressions.

Shipping Host surface:

no raw ex.Message

no manual { title, errorCode } business envelopes

no module-business PlatformHttpException mapping

no direct Fulfillment directory business calls

no Localization.Application dependency for module-specific resolution

Work queue:

Host contains no bulk validation/orchestration policy

no Host AdminFulfillmentWorkQueueComposer business implementation

no call chain requiring Host AdminOrderOperationsComposer from Fulfillment Application

Preferred:
delete AdminFulfillmentWorkQueueComposer.cs
or reduce it to a tiny transport-only adapter with zero policy/orchestration.

Behavior tests — focused only

Shipping:

list shape

get not-found central mapping

create shape

update shape

deactivate

ensure-seed idempotency

language fallback

invalid write central semantic error

Work queue:

unsupported action

empty items

cross seller

row mismatch

incompatible action

missing shipment

partial success after downstream failure

successful bulk

Reuse existing tests where possible.

Evidence

Create:
docs/evidence/TB-TMAR-NEXT-MODULE-BATCH-004-R2/recovery-start.md
shipping-endpoint-audit.md
workqueue-host-authority-audit.md
localization-boundary-audit.md
behavior-preservation-audit.md
presentation-scan.md
recovery-sot.md

presentation-scan.md must prove across Fulfillment-owned Host HTTP surfaces:

raw ex.Message error exposure = 0

manual expected-error title/errorCode envelopes = 0

Fulfillment-business PlatformHttpException mapping = 0

Validation — FAST-SAFE

Required:

Fulfillment focused tests/guards

focused Host shipping/workqueue tests

Order contract build/tests only if contract changed

Localization contract build/tests only if contract changed

final dotnet build src/backend/Tooba.slnx

Do NOT run:

broad Host suite

Returns full suite unless compile-only seam requires

Checkout workflow

Tax/Pricing

frontend

No retry/sleep.

Forbidden

Do NOT:

reopen Returns behavior

resume Checkout

modify Tax/Pricing

touch frontend

move business logic into another Host composer

use service locator

expose ex.Message

keep manual shipping error envelopes

use TypeForwardedTo

duplicate public contract types

broadly rewrite Order or Localization

Success state

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

Returns-State:
COMPLETE_REFERENCE_PATTERN

Returns-Physical-State:
VERIFIED_ON_DISK_AND_NAMESPACE

Returns-CrossModule-Boundary:
CONTRACTS_ONLY

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

If shipping or work-queue Host authority remains:
return INCOMPLETE with exact path.
Do NOT claim COMPLETE.

Canonical Result

Return ONLY BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Track
Recovery-Start
Shipping-Endpoint-Audit
Shipping-Application-UseCases
Shipping-Localization-Boundary
Shipping-Presentation
WorkQueue-Host-Authority-Audit
WorkQueue-Application-Ownership
Order-Contract-Seam
Behavior-Preservation-Audit
Presentation-Scan
Architecture-Guards
Fulfillment-Validation
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
Returns-State
Returns-Physical-State
Returns-CrossModule-Boundary
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
STOP completely.
Do NOT poll.
Do NOT fetch next task.
Do NOT write Worker IDLE.

END_TOOBA_REPAIR_TASK