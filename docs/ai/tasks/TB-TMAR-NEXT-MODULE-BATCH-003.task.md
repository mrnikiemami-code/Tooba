PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID:
TB-TMAR-NEXT-MODULE-BATCH-003

Parent-Task:
TB-TMAR-NEXT-MODULE-BATCH-002-R1

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
NEXT_REFERENCE_MODULE_BATCH

Title:
Notification + Support Golden Recovery Batch

Backend-Only:
YES

TMAR-Execution-Mode:
BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE

Accepted baseline

Architect directly verified BATCH-002-R1.

Accepted:

Wallet COMPLETE_REFERENCE_PATTERN

Wallet physical VERIFIED

Wallet→Notification CONTRACTS_ONLY

Payment COMPLETE_REFERENCE_PATTERN

Payment physical VERIFIED

Financial behavior preservation VERIFIED

Foundation RESULT pattern complete

Offer/Inventory/Promotion complete

Checkout paused W5

Frontend frozen

Deferred by USER:

Tax physical review

Pricing physical review

DO NOT modify Tax/Pricing.

Why Notification + Support now

Direct repository inspection found:

Notification:

Domain references Notification.Contracts

Domain imports Contracts.Dtos

Domain still uses UuidV7.New()

Domain still has localized exception prose

Infrastructure still uses DateTimeOffset.UtcNow

Infrastructure references Payment.Application, Fulfillment.Application, Returns.Application

Application/Infrastructure/Domain still have root-dumped source files

Support:

Infrastructure references Notification.Application

Application/Domain/Infrastructure contain root-dumped files

SupportDirectory is oversized

SupportTicket is broad

Goal:
Recover Notification + Support without behavior loss.

Scope

Recover ONLY:

Notification

Support

Allowed compile-only consumer updates:

Wallet

Payment

Fulfillment

Returns

Host

Only when public Contracts changes require them.

DO NOT:

reopen Wallet/Payment logic

modify Tax/Pricing

resume Checkout

touch frontend

Behavior-preservation rule

Before changing code, characterize current behavior.

Moves/splits/extractions must preserve:

branches

idempotency

recipient routing

notification semantic type strings

SourceEventId / SourceType

support ticket transitions

persistence behavior

notification side effects

outbox/event behavior

Any accidental behavior change is a blocker.

Notification ownership

Notification owns:

transactional notification persistence

recipient targeting

read/unread

soft delete

localized copy/rendering

creation idempotency

integration-event projection

Notification schema/DbContext/migrations

Cross-module callers may depend only on:
Tooba.Notification.Contracts

No external module may require:

Notification.Application

Notification.Domain

Notification.Infrastructure

Fix Notification Domain -> Contracts inversion

Current defect:
Tooba.Notification.Domain.csproj -> Tooba.Notification.Contracts

Remove it.

Target:

Domain has zero Contracts ref

Contracts has zero Domain ref

boundary mapping is explicit

Recommended:

Domain owns its own recipient-kind domain type

Contracts owns public recipient DTO/enum

Application/Infrastructure maps explicitly

Equivalent clean solution allowed.

Preserve persisted numeric semantics:
Customer=1
Seller=2

Forbidden:

TypeForwardedTo

namespace alias trick

duplicate compatibility wrappers

service locator

Notification.Contracts

Keep as cross-module surface.

Only genuinely public cross-module types belong here.

Contracts may contain meaningful folders such as:

Commands

Dtos

Ports

Copy

Routes

Events (only if true public integration contracts)

Contracts must reference no:

Domain

Application

Infrastructure

Host

Do not move local read models to Contracts without need.

Remove Notification foreign Application dependencies

Current Notification.Infrastructure references:

Payment.Application

Fulfillment.Application

Returns.Application

Remove these implementation-layer references.

Consume stable public integration contracts/events only.

If event contracts currently exist only in foreign Application:

extract the smallest public contract to the owning module Contracts boundary

update producer/consumer atomically

preserve event names/payloads

do not duplicate event types

do not broadly reopen those modules

No foreign DbContext or cross-module SQL.

Support -> Notification boundary

Support must not reference:

Notification.Application

Notification.Domain

Notification.Infrastructure

Support may use:

Notification.Contracts

Preserve support notification behavior:

type

recipient

route

SourceEventId

SourceType

payload semantics

Do not remove notifications to satisfy architecture.

Mandatory Visual Studio physical structure

Definition of Done includes real physical folders + matching namespaces.

Suggested:

Notification.Domain/

Aggregates

ValueObjects

Events

Policies

Notification.Application/

Ports

Models

Queries

Rendering/Services

Notification.Contracts/

Commands

Dtos

Ports

Copy

Routes

Events (if public)

Notification.Infrastructure/

Persistence

Directories

Projectors

Handlers

Observability

Messaging

DependencyInjection

Support.Domain/

Aggregates

Entities

ValueObjects

Events

Policies

Support.Application/

Ports

Models

Commands

Queries

Support.Infrastructure/

Persistence

Directories

Adapters

Seeds

Messaging

DependencyInjection

No empty ceremonial folders.
No root dump where responsibility is identifiable.
Namespaces must match path/responsibility.

Generated EF migrations may remain in migration folders if coherent.

Clock / IDs

Notification:

replace UuidV7.New with explicit caller-provided id / IIdGenerator boundary

replace DateTimeOffset.UtcNow with IClock in orchestration

Support:
audit and repair same patterns.

Forbidden:

DateTime.UtcNow

DateTimeOffset.UtcNow

Guid.NewGuid

UuidV7.New

No hidden fallbacks:

?? new SystemUtcClock()

?? new UuidV7IdGenerator()

Error/localization

Domain/Infrastructure exception messages:

stable machine-safe codes only

no user-facing Persian/English prose

Notification user-facing copy belongs in copy/localization layer.

Use existing Result/SemanticError only where an actual expected-flow boundary exists.
Do not create a second error model.

CQRS / MediatR / Endpoints

Audit real ownership first.

Do NOT add MediatR ceremonially.

If a module owns real HTTP application use cases:
Endpoint → ISender → Command/Query → Handler
MediatR exactly 12.5.0
Result/ApiResponseFactory for expected failures

If Host owns transport and module exposes internal directory/ports:

document Endpoint-State: NOT_APPLICABLE

do not create empty Endpoints

do not create fake Commands/Handlers

No raw Results.Json introduction.

Observability

Preserve metrics/tracing behavior.
No raw new StartActivity.
Place Notification instrumentation under meaningful Observability folder.
No duplicate spans.

Notification behavior characterization

Focused coverage required:

CreateIfAbsent duplicate suppression

Customer recipient filter

Seller recipient filter

MarkRead idempotency

MarkAllRead

SoftDelete idempotency

route allowlist behavior

Wallet notification semantic types unchanged

at least one integration-event projection path unchanged

Reuse existing tests when available.

Support behavior characterization

Focused coverage required:

ticket creation

reply/message append

status transition(s)

ownership/authorization seam if present

Support→Notification semantic behavior

idempotency/concurrency if present

Preserve current behavior.

Architecture guards

Notification:

Domain no Contracts/Application/Infrastructure/Host

Contracts no Domain/Application/Infrastructure/Host

Application no foreign Application/Infrastructure

Infrastructure no foreign Application/Domain implementation refs

no foreign DbContext

root dump = 0

path↔namespace aligned

no TypeForwardedTo

no direct clock/id

no hidden fallback implementations

no localized exception prose

no silent catch

no raw StartActivity

Host NotificationDbContext authority absent except explicit bootstrap if justified

Support:

no Notification.Application/Domain/Infrastructure

Notification.Contracts only

Domain purity

Application no foreign Application/Infrastructure

root dump = 0

path↔namespace aligned

clock/id clean

no TypeForwardedTo

no localized exception prose

no silent catch

Host SupportDbContext authority absent except explicit bootstrap if justified

Evidence

Create under:
docs/evidence/TB-TMAR-NEXT-MODULE-BATCH-003/

Files:

recovery-start.md

notification-audit.md

support-audit.md

notification-physical-tree.md

support-physical-tree.md

cross-module-boundary-audit.md

behavior-preservation-audit.md

foundation-adoption-scan.md

host-leak-scan.md

recovery-sot.md

Physical tree files must list EVERY handwritten production .cs:

relative path

namespace

responsibility

Behavior audit classifications:

structural-only

intentional architecture abstraction

intentional behavior change

accidental behavior change

Target:
accidental behavior changes = 0

Fast-Safe validation

Required:

Notification focused tests/guards

Support focused tests/guards

focused tests for changed cross-module contracts

one final:
dotnet build src/backend/Tooba.slnx

Conditional:

Wallet tests only if Notification Contracts behavior changes

Payment/Fulfillment/Returns tests only if public event contracts change

Host focused tests only if DI/composition signatures change

DO NOT run:

broad Host integration suite

unrelated module suites

Tax/Pricing

frontend

No retry/sleep workaround.

AntiPattern Gate

Must be CLEAN for:

Domain -> Contracts inversion

foreign Application/Domain dependencies

root dumping

namespace masquerading

TypeForwardedTo

direct clock/id bypass

hidden DI fallbacks

localized exception prose

silent catch

Host DbContext authority

cross-module SQL/FK

deleted notification side effects

changed notification semantic strings

ceremonial Endpoints/MediatR

frontend changes

Checkout resume

Success state

Notification-State:
COMPLETE_REFERENCE_PATTERN

Notification-Physical-State:
VERIFIED_ON_DISK_AND_NAMESPACE

Notification-Public-Boundary:
CONTRACTS_ONLY

Support-State:
COMPLETE_REFERENCE_PATTERN

Support-Physical-State:
VERIFIED_ON_DISK_AND_NAMESPACE

Support-Notification-Boundary:
CONTRACTS_ONLY

Behavior-Preservation:
VERIFIED

Wallet-State:
COMPLETE_REFERENCE_PATTERN

Payment-State:
COMPLETE_REFERENCE_PATTERN

Foundation-State:
RESULT_PATTERN_FOUNDATION_COMPLETE

Offer-State:
COMPLETE_REFERENCE_PATTERN

Inventory-State:
COMPLETE_REFERENCE_PATTERN

Promotion-State:
COMPLETE_REFERENCE_PATTERN

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
NEXT_REFERENCE_BATCH_003_COMPLETE

Next-Recommended-Task:
TB-TMAR-NEXT-MODULE-BATCH-004

If one module remains blocked:

preserve completed module

mark blocked module INCOMPLETE

recommend focused R1 only

Canonical Result

Return ONLY BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Track
Recovery-Start
Notification-Audit
Notification-Physical-Tree
Notification-Repairs
Notification-Public-Boundary
Notification-CQRS-State
Notification-Endpoint-State
Notification-Foundation-Adoption
Notification-Behavior-Parity
Notification-Guards
Notification-Validation
Notification-State
Notification-Physical-State
Support-Audit
Support-Physical-Tree
Support-Repairs
Support-Notification-Boundary
Support-CQRS-State
Support-Endpoint-State
Support-Foundation-Adoption
Support-Behavior-Parity
Support-Guards
Support-Validation
Cross-Module-Boundary-Audit
Host-Leak-Scan
Foundation-Adoption-Scan
Focused-Validation
Skipped-Validation
Full-Validation
AntiPattern-Gate
Residual-Defects
Behavior-Preservation
Wallet-State
Payment-State
Foundation-State
Offer-State
Inventory-State
Promotion-State
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

END_TOOBA_TASK