PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_REPAIR_TASK

Task-ID:
TB-TMAR-NEXT-MODULE-BATCH-004-R1

Parent-Task:
TB-TMAR-NEXT-MODULE-BATCH-004

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
Fulfillment + Returns Host Authority / Endpoint / Presentation Closure

Backend-Only:
YES

Architect verdict

Parent PASS is REOPENED.

Direct repository verification found concrete remaining Host authority and presentation leaks that are incompatible with:

Host transport/composition only

centralized Result/ProblemDetails/localization

COMPLETE_REFERENCE_PATTERN

Do NOT start BATCH-005.

Tax/Pricing/Checkout/frontend remain untouched.

1. Concrete defects verified by Architect
Fulfillment Host authority

src/backend/Host/Tooba.Host/Fulfillment/FulfillmentEndpoints.cs

currently contains business/authorization orchestration including:

direct service-location via RequestServices.GetRequiredService

direct OrderDbContext

catalog lookups

seller-order category-scope authorization logic

manual PlatformHttpException

manual Results.Json

raw InvalidOperationException message exposed as HTTP detail

This is not thin transport.

FulfillmentPanelComposer.cs constructor still receives:

FulfillmentDbContext

PartyDbContext

OrderDbContext

and constructs AdminFulfillmentWorkQueueQueryEngine in Host.

This is Host query/business authority, not composition-only.

Returns Host authority / presentation

ReturnPanelComposer / Host return grid path still owns DB-backed query composition.

ReturnEndpoints.cs manually:

catches PlatformHttpException

catches InvalidOperationException

maps errors with raw/manual Results.Json

owns expected-flow presentation

ReturnErrorMapper.cs is a Host-local error taxonomy/localization mapper and duplicates central error presentation.

ReturnPanelComposer.ParseDestination throws localized Persian prose:
"مقصد بازگشت وجه نامعتبر است."

This violates central SemanticError / descriptor / localization architecture.

Guard weakness

Current Fulfillment/Returns architecture guards allowlist many Host production files containing module DbContexts, including endpoints/composers/query engines.

That allowlist hides authority leakage instead of preventing it.

Only migration/bootstrap/seed may remain justified DbContext Host usage.

2. Objective

Close:

Host direct Fulfillment/Returns DbContext business/query authority

Host endpoint business orchestration

manual error mapping / raw expected-flow HTTP presentation

service locator in Fulfillment endpoint

architecture guards that currently bless these leaks

Preserve routes and externally visible success behavior.

No broad redesign of Checkout/Order/AccessControl/Catalog.

3. Ownership target

Target:

HTTP Host mapping
→ module-owned Application use case/query port
→ module Infrastructure implementation
→ module DbContext

Host may:

authenticate

establish transport identity/context

call a module-owned use case/query boundary

map module endpoint registration/composition

Host must NOT:

query FulfillmentDbContext/ReturnsDbContext

query OrderDbContext for Fulfillment business authorization

construct DB-native query engines for module data

contain business eligibility/scope logic

service-locate module collaborators

manually translate semantic error strings to HTTP

4. Fulfillment seller authorization seam

Preserve current semantics exactly:

seller must own fulfillment

order.handle permission required

GlobalWithinOwner accepted

category-scoped permission requires every line authorized

category snapshot fallback lookup preserved

missing order -> not found

denied scope -> forbidden

Move this bounded policy/orchestration behind a module-owned Application port/use case.

Do NOT weaken authorization.

If required cross-module data is needed:

consume public Contracts ports only

Order.Contracts for seller-order/category snapshot

Catalog public contract if one exists; otherwise extract minimum stable public lookup contract

AccessControl public contract if one exists; otherwise keep transport actor/permission snapshot input from Host rather than adding foreign implementation dependency

Do NOT create foreign Application/Domain references in Fulfillment.

No direct OrderDbContext in Host endpoint after repair.

5. Fulfillment query ownership

Move Fulfillment DB-native query/work-queue authority out of Host.

Current Host items using FulfillmentDbContext for actual query behavior must become:

Fulfillment Application query port/model

Fulfillment Infrastructure implementation

Host may normalize generic HTTP grid input only if truly transport-generic, but module-specific filtering/sorting/querying belongs to module.

Do not break existing grid response shape.

6. Returns query ownership

Move Returns DB-native grid/query authority out of Host similarly.

Host must not use ReturnsDbContext for production query behavior.

Keep route and response compatibility.

7. Endpoint/CQRS decision

These modules demonstrably own real HTTP use cases.

Therefore, for the changed owned HTTP flows, establish a proper module application boundary.

Preferred:
Endpoint/Host thin transport
→ ISender
→ module Command/Query Handler
→ ports/domain

MediatR version exactly 12.5.0.

Do NOT mechanically convert every single legacy method if that would balloon scope.
But all endpoint paths touched to remove Host authority must no longer call business composers that hide Host DB logic.

If a dedicated module Endpoints project is the cleanest bounded implementation, create it with real routes and no ceremonial empty project.

If Host remains physical route registration for compatibility, document:
Endpoint-State: HOST_THIN_TRANSPORT
not NOT_APPLICABLE.

8. Result / semantic error closure

Expected business failures touched in this repair must use:

Result / Result<T>

SemanticError

ApiResponseFactory / central presentation

Examples:

fulfillment missing

seller order permission denied

category scope denied

return missing

invalid refund destination

invalid transition

return eligibility rejection

Do NOT:

expose ex.Message

manually build {title,errorCode,detail}

use Host-local switch mapper

use PlatformHttpException for module business outcomes

catch arbitrary Exception into Result

Delete ReturnErrorMapper.cs if it becomes unused.

Localized strings belong in central descriptor/resource layer.

9. Preserve HTTP compatibility

Preserve existing route URLs and successful response shapes as much as possible.

Required route families include existing:

seller fulfillment

admin fulfillment

customer fulfillment

customer/seller/admin returns

No frontend changes.

If status code behavior changes only because central semantic mapping corrects an obvious defect, document it.

10. Host DbContext rule after repair

For FulfillmentDbContext / ReturnsDbContext in Host:

Allowed:

Program.cs only if needed for migration startup composition

ModuleMigrationRegistry.cs

explicit development seed/bootstrap file, only if truly seed-only

Forbidden:

Endpoints

PanelComposer

GridQueryEngine

Admin composer

storefront composer

order-operation composer

business services

Do not retain giant filename allowlists.

Guard should enforce semantic minimal allowlist.

11. Service locator ban

Remove endpoint use of:
request.HttpContext.RequestServices.GetRequiredService(...)

Dependencies must be explicit constructor/handler parameters/DI.

No static service locator.

12. Behavior preservation

Compare against BATCH-004 baseline:
247865f73d498d6fe3d1857e2007644412524cdc

Audit at minimum:

Fulfillment:

seller list/get

seller mutate authorization

order.handle global scope

category-scoped authorization

admin list/grid/work queue

customer fulfillment list

shipment transitions

Returns:

customer create/list/get

seller list/get/approve/reject

admin list/grid/get/retry

refund destination parsing

error status/code mapping

success response shape

Accidental behavior change target: 0.

13. Focused tests

Required high-value tests:

Fulfillment:

unauthorized seller mutation denied

category-scoped seller all-lines authorization preserved

global owner permission preserved

endpoint/business boundary no direct DbContext

semantic failure produces central mapped status/code without raw ex.Message

Returns:

invalid refund destination central semantic failure

return missing central mapping

seller approve/reject expected failure mapping

admin grid uses module query port, not ReturnsDbContext in Host

Architecture:

Host production scan: no FulfillmentDbContext/ReturnsDbContext outside minimal bootstrap allowlist

no RequestServices.GetRequiredService in Fulfillment/Returns endpoint paths

no ReturnErrorMapper

no manual Results.Json expected-error mapping for module semantic failures

Reuse existing tests where possible.

14. Evidence

Create under:
docs/evidence/TB-TMAR-NEXT-MODULE-BATCH-004-R1/

Required:

recovery-start.md

host-authority-audit.md

fulfillment-endpoint-boundary.md

returns-endpoint-boundary.md

presentation-error-audit.md

behavior-preservation-audit.md

host-dbcontext-scan.md

recovery-sot.md

host-dbcontext-scan.md must list every remaining Host hit for:

FulfillmentDbContext

ReturnsDbContext
and classify each as bootstrap-only or defect.

Target production business/query hits: 0.

15. Fast-Safe validation

Required:

Fulfillment focused tests/guards

Returns focused tests/guards

focused Host endpoint tests for changed seams

one final:
dotnet build src/backend/Tooba.slnx

Conditional:

AccessControl/Order/Catalog focused compile/tests only if public contracts change

BuildingBlocks tests only if production BuildingBlocks changed

Do NOT run:

broad unrelated Host suite

Checkout workflow suite

Tax/Pricing

frontend

No retry/sleep workaround.

16. Forbidden

DO NOT:

resume Checkout W6

change CheckoutProcessManager semantics

modify Tax/Pricing

touch frontend

add new Host business composer wrappers

hide DbContext behind another Host class

widen Host allowlist

use service locator

use PlatformHttpException for expected module business failures

return raw ex.Message

add duplicate error taxonomy

create ceremonial Endpoints/MediatR structures

17. Success state

Only if true:

Fulfillment-State:
COMPLETE_REFERENCE_PATTERN

Fulfillment-Physical-State:
VERIFIED_ON_DISK_AND_NAMESPACE

Fulfillment-CrossModule-Boundary:
CONTRACTS_ONLY

Fulfillment-Endpoint-State:
HOST_THIN_TRANSPORT_OR_MODULE_ENDPOINTS

Fulfillment-Host-DbAuthority:
NONE

Returns-State:
COMPLETE_REFERENCE_PATTERN

Returns-Physical-State:
VERIFIED_ON_DISK_AND_NAMESPACE

Returns-CrossModule-Boundary:
CONTRACTS_ONLY

Returns-Endpoint-State:
HOST_THIN_TRANSPORT_OR_MODULE_ENDPOINTS

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

If Host business/query authority remains:
return INCOMPLETE with exact path.
Do NOT claim COMPLETE.

18. Canonical Result

Return ONLY BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Track
Recovery-Start
Host-Authority-Audit
Fulfillment-Endpoint-Boundary
Fulfillment-Host-DbAuthority
Fulfillment-Result-Adoption
Fulfillment-Validation
Fulfillment-State
Fulfillment-Physical-State
Returns-Endpoint-Boundary
Returns-Host-DbAuthority
Returns-Error-Presentation
Returns-Result-Adoption
Returns-Validation
Presentation-Error-Audit
Behavior-Preservation-Audit
Host-DbContext-Scan
Architecture-Guards
Focused-Validation
Skipped-Validation
Full-Validation
AntiPattern-Gate
Residual-Defects
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