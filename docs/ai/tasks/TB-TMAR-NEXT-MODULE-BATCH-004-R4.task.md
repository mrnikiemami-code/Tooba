PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_REPAIR_TASK

Task-ID:
TB-TMAR-NEXT-MODULE-BATCH-004-R4

Parent-Task:
TB-TMAR-NEXT-MODULE-BATCH-004-R3

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
Fulfillment Final Host Language Gate + Order Adapter Anti-Pattern Closure

Backend-Only:
YES

Architect verdict

R3 is still NOT fully accepted.

Direct repository verification confirms:

HostAdminOrderFulfillmentOperations is deleted.

IAdminOrderFulfillmentOperations is now Order-owned.

shipping tree projection is now Fulfillment.Application-owned.

previous Host work-queue/shipping tree logic is removed.

But two concrete anti-patterns remain from the R3 implementation itself.

Do NOT start BATCH-005.
Do NOT modify Tax/Pricing.
Checkout remains PAUSED_AT_SAFE_W5_CHECKPOINT.
Frontend remains frozen.

1. Remaining defect A — HostShippingServiceLanguageGate

Verified file:
src/backend/Host/Tooba.Host/Admin/ShippingServiceEndpoints.cs

Still contains:

HostShippingServiceLanguageGate : IShippingServiceLanguageGate

This means Host still implements a Fulfillment Application port for module-specific shipping behavior.

It:

validates shipping language IDs

maps Localization.Contracts snapshots into Fulfillment seed language models

This is module adapter/application support logic and belongs in Fulfillment.Infrastructure, not Host.

Required repair A

Move implementation of IShippingServiceLanguageGate out of Host.

Target:
Fulfillment.Application
→ IShippingServiceLanguageGate
→ Fulfillment.Infrastructure implementation
→ Localization.Contracts ILanguageLookup

Delete:
HostShippingServiceLanguageGate

Host shipping endpoints must contain only:

wire DTOs

auth

ISender

ApiResponseFactory

minimal wire→command mapping

No module port implementation classes in Host.

Register implementation in Fulfillment module DI.

2. Remaining defect B — newly introduced Order adapter uses prohibited exception/prose pattern

Verified file:
src/backend/Modules/Order/Tooba.Order.Infrastructure/Fulfillment/AdminOrderFulfillmentOperations.cs

New R3 code currently:

throws PlatformHttpException

contains Persian user-facing prose in Infrastructure

catches InvalidOperationException

maps message text through MapFulfillmentException

has message-based localized switch mapping

This is a newly introduced anti-pattern and must not be accepted simply because Order is not yet broadly recovered.

Required repair B

Keep the public contract:
IAdminOrderFulfillmentOperations

But implement its expected business outcomes without module-local HTTP exceptions.

Preferred:

internal stable semantic/error-code flow

return AdminOrderFulfillmentOperationOutcome(false, stableCode) for expected business failures

unexpected failures may propagate

no PlatformHttpException in this adapter

no localized prose

no message→localized-text switch

When downstream Fulfillment operations currently throw stable machine codes in InvalidOperationException.Message, map only known stable machine codes to stable outcome codes where unavoidable during transition.

Do NOT map Persian/English prose strings.

Do NOT catch arbitrary unexpected InvalidOperationException and silently turn it into unknown failure unless the exception is a known expected stable-code case.

3. Preserve behavior

Order fulfillment-operation behavior must remain exactly as R3 characterized:

cancelled checkout blocking

permission gate

seller-order linkage

mark processing

mark packed

create shipment

cancel shipment

assign tracking

dispatch

deliver

shipping method enablement

shipment/fulfillment linkage

Preserve existing stable error codes expected by Fulfillment bulk.

No Checkout process semantics changes.

4. Host thinness invariant

After repair, Host must have ZERO implementations of Fulfillment Application ports.

Specifically scan for Host classes implementing:

IShippingServiceLanguageGate

IAdminOrderFulfillmentOperations

other newly introduced Fulfillment application ports from BATCH-004

Any implementation found in Host is a blocker unless it is purely HTTP transport abstraction explicitly approved.

5. Architecture guards

Add/strengthen guards:

Must fail if:

HostShippingServiceLanguageGate exists

Host implements IShippingServiceLanguageGate

Host implements IAdminOrderFulfillmentOperations

AdminOrderFulfillmentOperations.cs contains PlatformHttpException

AdminOrderFulfillmentOperations.cs contains localized Persian prose in thrown exceptions

AdminOrderFulfillmentOperations.cs contains message-switch mapping on localized prose

new Order fulfillment adapter depends on Host

Also ensure:

Fulfillment.Infrastructure implementation of IShippingServiceLanguageGate

depends only on Localization.Contracts

path↔namespace aligned

no hidden service locator

6. Focused tests

Required:

Language gate:

valid known language IDs succeed

unknown language ID yields stable shipping_service.language_invalid behavior

seed language mapping preserved

no Host implementation

Order adapter:

success path unchanged

denied permission preserves same stable code

cancelled checkout preserves same stable code

one downstream stable Fulfillment code preserves mapping

unexpected exception is not silently converted to generic expected failure

Reuse existing tests where possible.

7. Evidence

Create under:
docs/evidence/TB-TMAR-NEXT-MODULE-BATCH-004-R4/

Required:

recovery-start.md

host-language-gate-audit.md

order-adapter-antipattern-audit.md

behavior-preservation-audit.md

antipattern-scan.md

recovery-sot.md

antipattern-scan.md must prove:

Host IShippingServiceLanguageGate implementation = 0

Host IAdminOrderFulfillmentOperations implementation = 0

PlatformHttpException in AdminOrderFulfillmentOperations = 0

localized exception prose in AdminOrderFulfillmentOperations = 0

localized message-switch mapping = 0

8. Validation — FAST-SAFE

Required:

Fulfillment focused tests/guards

Order focused tests for adapter

focused Host compile/tests only

final:
dotnet build src/backend/Tooba.slnx

Do NOT run:

broad Host suite

broad Order suite

Returns full suite

Checkout workflow

Tax/Pricing

frontend

No retries/sleeps.

9. Success state

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

Fulfillment-LanguageGate-Implementation:
INFRASTRUCTURE_OWNED

Order-Fulfillment-Operations-Implementation:
ORDER_OWNED

Order-Fulfillment-Adapter-AntiPattern:
CLEAN

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

If either anti-pattern remains:
return INCOMPLETE with exact path.
Do NOT claim COMPLETE.

10. Canonical Result

Return ONLY BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Track
Recovery-Start
Host-Language-Gate-Audit
Fulfillment-LanguageGate-Implementation
Order-Adapter-AntiPattern-Audit
Order-Fulfillment-Operations-Implementation
Order-Fulfillment-Adapter-AntiPattern
Behavior-Preservation-Audit
AntiPattern-Scan
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
Fulfillment-ShippingTree-Authority
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