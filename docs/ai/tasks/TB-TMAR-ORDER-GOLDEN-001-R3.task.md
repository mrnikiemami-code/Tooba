PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID:
TB-TMAR-ORDER-GOLDEN-001-R3

Parent-Task:
TB-TMAR-ORDER-GOLDEN-001-R2C

Channel:
tooba-main

WorkerId:
tooba-worker-01

AgentType:
cursor

Program:
TMAR — Tooba Microservice-Ready Architecture Recovery

Mode:
IMPLEMENTATION + REPAIR

Track:
ORDER_GOLDEN_IMPLEMENTATION_SLICE_2

Title:
Order Golden R3 — Remove Remaining AdminOrderOperations + OrdersGrid Host Authority

Architect-Decision:
TB-TMAR-ORDER-GOLDEN-001-R2C is ACCEPTED.
R2C closed the AdminOrderCompleteness slice with full parity and stable architecture guards, but Order remains INCOMPLETE_REFERENCE_REPAIR.
R3 MUST continue the Order recovery as a bounded next slice.
Do NOT mark Order COMPLETE_REFERENCE_PATTERN in this task unless every acceptance criterion below is proven and there are no remaining Order-hosted business/data authorities in this R3 scope.

Reference Architecture:
Use Fulfillment as the reference for capability-owned endpoint placement.
Use Offer as the reference for CQRS/MediatR physical structure and Result/SemanticError boundary style.

Mandatory Existing Locks:

Module-owned endpoint folders/projects.

Endpoint -> ISender only for application use-cases.

Offer-style per-use-case Commands/Queries/Handlers with path/namespace alignment.

MediatR 12.5.0.

Result/SemanticError + centralized localized error catalog.

ApiResponseFactory at HTTP boundary.

No Endpoint -> Infrastructure.

No Application -> foreign Application.

No Application -> foreign Infrastructure.

Foreign module access through Contracts only.

Host limited to transport/auth/composition/bootstrap responsibilities.

No Host business DbContext authority.

No manual semantic Results.Json.

No raw ex.Message business presentation.

No TypeForwardedTo.

No hidden DI fallback.

No silent catch/workaround.

No hardcoded localized business exception prose.

No Big Bang rewrite.

Recovery Safety:
Before modification verify:

repository root = D:\Users\User\source\repos\SarvNewVer

branch = main

current HEAD is synchronized with origin/main

R2C commit a98aca6eef3530c432b32eaa0cc99048b3169dd7 is an ancestor of HEAD

user work is preserved

unrelated stashes remain untouched

no destructive git operation

no git reset

no git clean

no force push

no rebase

no unsafe restore/checkout over user work

no blind stash manipulation

no broad git add .

Evidence:
docs/evidence/TB-TMAR-ORDER-GOLDEN-001-R3/recovery-start.md

R3 Primary Scope:
Recover the remaining Admin Order operational and grid/query surfaces that are still owned by Host after R2C.

Mandatory Audit Before Editing:
Find and classify ALL remaining Order-related Host files and routes for:

AdminOrderOperations

OrdersGrid

direct OrderDbContext reads

direct OrderDbContext writes

SaveChanges

transactions

business validation

admin order mutation orchestration

status/state transition orchestration

order grid filtering/sorting/paging

order detail/query composition directly owned by Host

direct foreign-module DbContext access in those paths

direct foreign Application/Infrastructure dependencies in those paths

Do not classify by filename alone.
Trace runtime registration and endpoint mapping.

Evidence:
docs/evidence/TB-TMAR-ORDER-GOLDEN-001-R3/order-remaining-host-audit.md

Implementation Scope A — AdminOrderOperations:
Move the remaining AdminOrderOperations business/application authority out of Host.

Target:
HTTP endpoint
-> ISender
-> Order.Application Command/Query Handler
-> Order Domain/Repositories/Gates/Contracts
-> Result/SemanticError
-> ApiResponseFactory

Requirements:

Preserve existing routes.

Preserve authentication/authorization semantics.

Preserve request/response contracts unless a documented architectural necessity requires otherwise.

Preserve status transition rules.

Preserve note/audit/history behavior.

Preserve actor attribution.

Preserve idempotency/concurrency behavior where already present.

Preserve transaction boundaries.

Preserve existing side effects/events/outbox behavior.

Host must not own Order business decisions after migration.

Host must not call OrderDbContext for migrated operational writes.

No temporary facade that leaves real business authority in Host.

Evidence:
docs/evidence/TB-TMAR-ORDER-GOLDEN-001-R3/admin-order-operations.md

Implementation Scope B — OrdersGrid:
Move OrdersGrid query authority out of Host.

Requirements:

Preserve all supported filters.

Preserve sorting.

Preserve paging.

Preserve projection shape.

Preserve deterministic ordering.

Preserve seller/customer/product labels where currently supported.

Preserve authorization/visibility rules.

Do not introduce cross-schema SQL JOIN.

Do not make Host authoritative for order query logic.

Foreign data required by the grid must come through declared Contracts/read boundaries.

Do not move business truth decisions into the composer.

Avoid N+1 regressions; document the chosen projection strategy.

Evidence:
docs/evidence/TB-TMAR-ORDER-GOLDEN-001-R3/orders-grid.md

Contracts Boundary:
For every foreign dependency touched by AdminOrderOperations or OrdersGrid:

prefer existing Tooba.<Module>.Contracts

extend Contracts only when required by existing behavior

do not reference foreign Application/Infrastructure

do not leak foreign Domain entities or DbContexts

do not add convenience abstractions without a real boundary need

Document every added/changed contract and why.

Evidence:
docs/evidence/TB-TMAR-ORDER-GOLDEN-001-R3/contracts-boundary.md

CQRS Physical Structure:
All new/migrated Order use-cases must follow the established Offer-style structure:

one use-case folder

Command or Query

Handler

validator when needed

result/outcome type when needed

namespace aligned with physical path

no CQRS mega-file

no fake ISender wrapper around old Host business code

Evidence:
docs/evidence/TB-TMAR-ORDER-GOLDEN-001-R3/cqrs-structure.md

Host Boundary:
At completion, for the R3 migrated paths Host may contain only:

route registration / HTTP transport

authentication/session boundary

authorization adapter where appropriate

serialization

composition/DI

ApiResponseFactory mapping

Forbidden in migrated paths:

OrderDbContext business reads/writes

SaveChanges

BeginTransaction

business validation

state-transition decisions

grid business/query authority

foreign DbContext access

business exception-to-HTTP mapping

manual semantic Results.Json

Evidence:
docs/evidence/TB-TMAR-ORDER-GOLDEN-001-R3/host-boundary.md

Architecture Guards:
Add or strengthen durable Order architecture tests so future drift fails CI.

At minimum enforce for the migrated R3 surfaces:

no Host OrderDbContext business authority

no migrated AdminOrderOperations implementation in Host

no migrated OrdersGrid implementation in Host

Order endpoints use ISender

Order.Application has no foreign Application reference

Order.Application has no foreign Infrastructure reference

Order endpoint layer does not reference Infrastructure

CQRS path/namespace alignment

no CQRS mega-file

stable Result/SemanticError boundary

no raw localized exception prose in touched Order paths

Do not weaken existing tests to pass.

Evidence:
docs/evidence/TB-TMAR-ORDER-GOLDEN-001-R3/architecture-guards.md

Behavior Parity:
Characterize before replacing when behavior is not already locked by tests.

Must prove parity for all migrated AdminOrderOperations and OrdersGrid behavior, including where applicable:

permissions

state transitions

notes

actor labels

history effects

seller/product/customer labels

filters

sort

paging

deterministic ordering

response shapes

error semantics

concurrency/idempotency

transaction/outbox side effects

Evidence:
docs/evidence/TB-TMAR-ORDER-GOLDEN-001-R3/behavior-parity.md

Protected State:
The following modules are protected and must not be redesigned or regressed:

Cart = COMPLETE_REFERENCE_PATTERN

Settlement = COMPLETE_REFERENCE_PATTERN

Fulfillment = COMPLETE_REFERENCE_PATTERN

Returns = COMPLETE_REFERENCE_PATTERN

Notification = COMPLETE_REFERENCE_PATTERN

Support = COMPLETE_REFERENCE_PATTERN

Wallet = COMPLETE_REFERENCE_PATTERN

Payment = COMPLETE_REFERENCE_PATTERN

Promotion = COMPLETE_REFERENCE_PATTERN

Offer = COMPLETE_REFERENCE_PATTERN

Inventory = COMPLETE_REFERENCE_PATTERN

Additional locks:

Checkout = PAUSED_AT_SAFE_W5_CHECKPOINT

Tax = UNCHANGED

Pricing = UNCHANGED

Frontend production changes = NONE

Do not start Checkout W6.
Do not redesign Inventory.
Do not touch Tax/Pricing/frontend unless compilation requires a minimal mechanical compatibility change; any such change must be explicitly reported and justified.

Explicitly Deferred From R3 Unless Directly Required:

broader inventory/supply/storefront Order composers outside AdminOrderOperations/OrdersGrid

repository-wide foreign Application-reference cleanup outside touched Order paths

physical folder reorganization unrelated to touched CQRS use cases

Checkout W6

Tax

Pricing

frontend

unrelated module cleanup

If any deferred item blocks correct R3 completion:
STOP and return BLOCKED with exact evidence.
Do not silently expand scope.

Focused Validation:
Run at minimum:

dedicated Order.Tests

Order architecture guards

focused Host tests covering AdminOrderOperations and OrdersGrid

existing R2C Order completeness/foundation regression tests

protected Fulfillment/Returns/Settlement architecture guards if their Contracts are touched

full backend solution build

Use the actual test project/filter names found in repository.
Do not invent test names.

Full Validation:

dotnet build src/backend/Tooba.slnx

zero build errors

AntiPattern Audit:
Before PASS verify:

no Host Order business/query authority remains for migrated R3 paths

no foreign Application/Infrastructure references introduced

no foreign DbContext access introduced

no manual semantic Results.Json

no raw ex.Message business mapping

no localized business exception prose added

no TypeForwardedTo

no hidden DI fallback

no polling workaround

no timeout/interval magic workaround

no first-item/first-seller shortcut

no silent catch

no broad architecture suppression

no test-only behavior patch

no protected module redesign

Recovery SoT:
Synchronize:
docs/architecture/tmar-current-state.json

Required state on PASS:

R2C remains accepted

R3 recorded with exact commit

Order remains INCOMPLETE_REFERENCE_REPAIR unless repository evidence proves no remaining Order recovery work beyond deliberately deferred future scope

Next task must be evidence-driven

protected module states unchanged

Checkout remains PAUSED_AT_SAFE_W5_CHECKPOINT

Tax/Pricing unchanged

frontend unchanged

Git Discipline:

branch main

preserve user work

no destructive operations

commit only R3 scope

push to origin/main

report exact commit SHA

verify HEAD == origin/main after push

Success Criteria:
PASS only if ALL are true:

AdminOrderOperations R3 scope is removed from Host business authority.

OrdersGrid R3 scope is removed from Host query authority.

Migrated endpoints use ISender.

Migrated use-cases are physically structured Offer-style CQRS.

Behavior parity is evidenced.

Foreign boundaries are Contracts-only.

No touched-path foreign DbContext authority remains.

Stable Result/SemanticError + centralized localized presentation is used.

Durable Order architecture guards cover the R3 boundaries.

Existing R2C behavior remains green.

Protected module guards remain green when touched.

Full backend build succeeds.

Checkout W6 not started.

Tax/Pricing unchanged.

Frontend production changes = NONE.

User work preserved.

Recovery SoT synchronized.

Canonical Result delivered through Bridge.

Result Contract:
Return ONLY canonical BRIDGE-WAKE-V1 Result with at least:

Task-ID
Parent-Task
Channel
WorkerId
AgentType
Status
Summary
Program-Name
Track
Recovery-Start
R2C-Repository-State
Order-Remaining-Host-Audit
AdminOrderOperations-State
OrdersGrid-State
Order-CQRS-State
Order-CQRS-Physical-Structure
Order-Endpoint-State
Order-Host-DbAuthority
Order-Foreign-Boundary
Order-Result-Semantics
Order-Error-Presentation
Contracts-Changed
Behavior-Parity
Architecture-Guard-Validation
Focused-Validation
Skipped-Validation
Full-Validation
AntiPattern-Gate
Residual-Defects
Order-Overall-State
Checkout-State
Cart-State
Settlement-State
Fulfillment-State
Returns-State
Notification-State
Support-State
Wallet-State
Payment-State
Promotion-State
Offer-State
Inventory-State
Tax-State
Pricing-State
Frontend-Production-Changes
Recovery-State
Recovery-Next-Task
Current-State-Manifest
Git
Blockers
User-Work-Preserved
Next-Recommended-Task

Final Chat Token Rule:
After the canonical Result is sent through Bridge, do NOT write a detailed final report in Cursor chat.
Final chat response must be only:
DONE
or, if blocked:
BLOCKED: <short reason>

After sending canonical Result through Bridge:
STOP completely.

Do NOT:

poll

fetch next task

continue implementation

start R4

write Worker IDLE

END_TOOBA_TASK