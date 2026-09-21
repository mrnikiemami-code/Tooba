PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_REPAIR_TASK

Task-ID:
TB-TMAR-NEXT-MODULE-BATCH-005-R1

Parent-Task:
TB-TMAR-NEXT-MODULE-BATCH-005

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
Settlement Host Thin Transport + Cart/Settlement Architecture Guard Closure

Backend-Only:
YES

Architect verdict

Parent INCOMPLETE is correct.

Direct repository verification confirms:

Cart

Delivered Cart structural/boundary work is acceptable:

physical folder/namespace split landed

cross-module refs are Contracts-only

IClock/IIdGenerator adopted

focused behavior parity evidence exists

Cart is not COMPLETE only because dedicated architecture guards are missing.

Settlement

Settlement remains blocking:

Host SettlementPanelComposer injects SettlementDbContext and PartyDbContext

Host constructs/owns AdminPayoutGridQueryEngine

Host AdminPayoutGridQueryEngine owns DB-native Settlement query behavior

Host SettlementEndpoints manually uses Results.Json

Host exposes InvalidOperationException.Message

Host maps expected failures manually instead of Result/ApiResponseFactory

Therefore:

Cart-State:
PROVISIONALLY_COMPLETE_WAITING_GUARDS

Settlement-State:
REOPENED_HOST_AUTHORITY_AND_PRESENTATION

Do NOT start BATCH-006.
Do NOT modify Tax/Pricing.
Do NOT resume Checkout.
Frontend remains frozen.

1. Objective

Close exactly:

Settlement Host Db/query authority

Settlement HTTP presentation/result boundary

dedicated Cart architecture guards

dedicated Settlement architecture guards

No broad redesign.
No unrelated module recovery.

2. Settlement target architecture

Target:

Host auth/binding
→ ISender or Settlement Application query/use-case boundary
→ Settlement Application ports/models
→ Settlement Infrastructure implementation
→ SettlementDbContext

Host may:

authenticate

bind route/query/body

call ISender/module boundary

use ApiResponseFactory

Host must NOT:

inject SettlementDbContext for business/query

inject PartyDbContext for seller names

construct Settlement query engines

own settlement grid filtering/sorting/query semantics

catch expected module failures into manual JSON envelopes

expose ex.Message

3. Move Admin payout grid authority into Settlement

Move AdminPayoutGridQueryEngine out of Host.

Preferred target:
Tooba.Settlement.Application:

query port/model

Tooba.Settlement.Infrastructure:

DB-native EF implementation

SettlementDbContext use

Party seller-name lookup through Party public contract, NOT PartyDbContext

Preserve:

Pending|Failed default scope

search behavior

filters

advanced filters

sort semantics

paging

seller display names

response shape

CreatedAt/UpdatedAt

amount/currency/status/idempotency fields

No cross-module SQL/JOIN.

4. Party boundary for seller names

Settlement must not use PartyDbContext outside Party module.

If Party already has a public Contracts lookup for party display names:

reuse it

Otherwise extract the smallest stable contract, e.g.:
IPartyDisplayNameLookup
with minimal batch lookup DTO/snapshot.

Implementation belongs to Party module.

Settlement Infrastructure consumes only Party.Contracts.

Do NOT broadly recover Party.

5. SettlementPanelComposer closure

After repair, Host SettlementPanelComposer must either:

be deleted, OR

be reduced to a thin transport/composition adapter with NO DbContext and NO module business/query logic

Prefer Application CQRS/use-cases over composer delegation.

Move module-owned use cases behind Settlement Application:

Seller:

get balance

list entries

list statements

list payout requests

request payout

Admin:

list balances

list payout queue

query payout grid

process payout

retry payout

Do not mechanically rewrite domain internals; establish clean application boundaries.

6. CQRS / MediatR

Settlement owns real HTTP use cases.

Use:
Host thin transport
→ ISender
→ Settlement Application Command/Query
→ Handler
→ ports/domain

MediatR exactly 12.5.0.

Only real use cases above.

7. Result pattern / semantic errors

Expected Settlement business failures must use:

Result / Result<T>

SemanticError

centralized ErrorDescriptor/catalog/localization

ApiResponseFactory in Host

At minimum centralize:

settlement.account.missing

payout rejected/invalid amount

invalid payout state

payout missing

retry/process invalid state

idempotency conflict if present

Do NOT:

expose InvalidOperationException.Message

catch arbitrary Exception into Result

use manual { title, errorCode, detail }

use PlatformHttpException for Settlement business outcomes

Unexpected system failures remain exceptions.

8. HTTP compatibility

Preserve routes:

Seller:

GET /v1/seller/settlement/balance

GET /v1/seller/settlement/entries

GET /v1/seller/settlement/statements

GET /v1/seller/settlement/payout-requests

POST /v1/seller/settlement/payout-requests

Admin:

GET /v1/admin/settlement/balances

GET /v1/admin/settlement/payout-queue

POST /v1/admin/settlement/payout-queue/query

POST /v1/admin/settlement/payout-requests/{payoutRequestId}/process

POST /v1/admin/settlement/payout-requests/{payoutRequestId}/retry

Preserve successful response shapes.

9. Host DbContext rule

After repair, Host references to SettlementDbContext allowed ONLY for:

migration/bootstrap

explicit development seed/bootstrap if truly seed-only

Forbidden:

SettlementPanelComposer

grid/query engines

endpoints

admin/seller presentation services

production business services

Host references to PartyDbContext for Settlement seller-name enrichment must be zero.

Do NOT replace these with another Host wrapper.

10. Cart dedicated architecture guards

Create/complete dedicated Cart test project if absent:
Tooba.Cart.Tests

At minimum:
Architecture/CartArchitectureGuardTests.cs

Enforce:

root dump = 0

path↔namespace alignment

no TypeForwardedTo

Domain no foreign Application/Domain/Infrastructure/Host

Application foreign modules only *.Contracts

Infrastructure no foreign Application/Domain/Infrastructure

no foreign DbContext

no DateTime/UtcNow/Guid.NewGuid/UuidV7 bypass

no hidden clock/id fallback

no silent catch

no localized exception prose in Domain/Infrastructure throws

no raw StartActivity

Host CartDbContext production authority absent except bootstrap allowlist

Do not duplicate existing behavioral tests unnecessarily.

11. Settlement dedicated architecture guards

Create/complete:
Tooba.Settlement.Tests

At minimum:
Architecture/SettlementArchitectureGuardTests.cs

Enforce:

root dump = 0

path↔namespace alignment

no TypeForwardedTo

Domain purity

Application no foreign Application/Infrastructure

Infrastructure foreign module refs Contracts-only

reject Payment.Application/Domain/Infrastructure

reject Order.Application/Domain/Infrastructure

reject Returns.Application/Domain/Infrastructure

reject Party Application/Domain/Infrastructure

no foreign DbContext

no clock/id bypass

no hidden DI fallback

no localized exception prose

no silent catch

no raw StartActivity

Host SettlementDbContext only minimal bootstrap allowlist

Host PartyDbContext Settlement-path usage = 0

Host Settlement endpoints no manual business-error Results.Json envelopes

no raw ex.Message exposure

12. Settlement behavior preservation

Preserve:

Seller:

balance missing/not-found semantics

entry list

statements list

payout list

request payout amount/idempotency/actor semantics

Admin:

balances list + seller display names

payout queue

payout grid filters/sort/search/paging

process payout

retry payout

Financial:

amount/currency precision

idempotency

payout status transitions

fake/fail-closed gateway behavior

event/outbox behavior

Accidental behavior change = 0.

13. Cart behavior preservation

No Cart redesign in R1.

Only architecture guard closure unless a guard catches a real defect.

If a guard catches a real Cart defect:

fix only that defect

preserve behavior

document exact delta

do not broaden scope

14. Evidence

Create under:
docs/evidence/TB-TMAR-NEXT-MODULE-BATCH-005-R1/

Required:

recovery-start.md

settlement-host-authority-audit.md

settlement-endpoint-boundary.md

settlement-presentation-audit.md

settlement-party-boundary.md

cart-architecture-guard-audit.md

settlement-architecture-guard-audit.md

behavior-preservation-audit.md

host-dbcontext-scan.md

recovery-sot.md

host-dbcontext-scan.md must list every remaining Host occurrence of:

SettlementDbContext

PartyDbContext on Settlement path

Classify each:

bootstrap-only

defect

Target Settlement production business/query hits:
0

15. Fast-Safe validation

Required:

Cart architecture guards

Settlement architecture guards

Settlement focused behavior tests

focused Host Settlement endpoint tests

Party focused contract build/test only if contract extracted

final dotnet build src/backend/Tooba.slnx

Do NOT run:

broad Host suite

full Checkout workflow

Tax/Pricing

frontend

unrelated module suites

No retry/sleep workaround.

16. AntiPattern Gate

Must be CLEAN:

Host SettlementDbContext business/query authority

Host PartyDbContext Settlement usage

Settlement manual semantic error envelopes

Settlement raw ex.Message

Settlement PlatformHttpException business mapping

foreign Application/Domain/Infrastructure refs

root dumping

namespace masquerading

TypeForwardedTo

direct clock/id bypass

hidden DI fallbacks

silent catch

localized exception prose

raw StartActivity

missing dedicated architecture guards

Checkout resume

Tax/Pricing touch

frontend changes

17. Success state

Cart-State:
COMPLETE_REFERENCE_PATTERN

Cart-Physical-State:
VERIFIED_ON_DISK_AND_NAMESPACE

Cart-CrossModule-Boundary:
CONTRACTS_ONLY

Cart-Architecture-Guards:
ENFORCED

Cart-Behavior-Preservation:
VERIFIED

Settlement-State:
COMPLETE_REFERENCE_PATTERN

Settlement-Physical-State:
VERIFIED_ON_DISK_AND_NAMESPACE

Settlement-CrossModule-Boundary:
CONTRACTS_ONLY

Settlement-Endpoint-State:
HOST_THIN_TRANSPORT

Settlement-Host-DbAuthority:
NONE

Settlement-Party-Boundary:
CONTRACTS_ONLY

Settlement-Error-Presentation:
CENTRALIZED

Settlement-Architecture-Guards:
ENFORCED

Settlement-Behavior-Preservation:
VERIFIED

Behavior-Preservation:
VERIFIED

Fulfillment-State:
COMPLETE_REFERENCE_PATTERN
Returns-State:
COMPLETE_REFERENCE_PATTERN
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
NEXT_REFERENCE_BATCH_005_COMPLETE

Next-Recommended-Task:
TB-TMAR-NEXT-MODULE-BATCH-006

If Host Settlement business/query/presentation authority remains:
return INCOMPLETE with exact path.
Do NOT claim COMPLETE.

18. Canonical Result

Return ONLY BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Track
Recovery-Start
Settlement-Host-Authority-Audit
Settlement-Endpoint-Boundary
Settlement-Party-Boundary
Settlement-Error-Presentation
Settlement-Result-Adoption
Settlement-CQRS-State
Settlement-Host-DbAuthority
Settlement-Behavior-Parity
Settlement-Architecture-Guards
Cart-Architecture-Guards
Cart-Behavior-Parity
Host-DbContext-Scan
Behavior-Preservation-Audit
Focused-Validation
Skipped-Validation
Full-Validation
AntiPattern-Gate
Residual-Defects
Cart-State
Cart-Physical-State
Cart-CrossModule-Boundary
Settlement-State
Settlement-Physical-State
Settlement-CrossModule-Boundary
Settlement-Endpoint-State
Settlement-Party-Boundary
Settlement-Host-DbAuthority
Settlement-Error-Presentation
Behavior-Preservation
Fulfillment-State
Returns-State
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

After canonical Result:
STOP completely.
Do NOT poll.
Do NOT fetch next task.
Do NOT write Worker IDLE.

END_TOOBA_REPAIR_TASK