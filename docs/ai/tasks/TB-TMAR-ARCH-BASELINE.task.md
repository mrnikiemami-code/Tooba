PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID:
TB-TMAR-ARCH-BASELINE

Parent-Task:
TB-P10-T022-R21

Channel:
tooba-main

WorkerId:
tooba-worker-01

AgentType:
cursor

Status:
ISSUED

Program:
TMAR — Tooba Microservice-Ready Architecture Recovery

Persian Program Name:
برنامه بازیابی معماری توبا برای آمادگی مهاجرت بدون‌درد به میکروسرویس

Title:
TMAR Phase 1 — Full Architecture Recovery Audit, Ownership Map, Locks & Migration Roadmap

Task Type:
PLANNING + AUDIT ONLY

Absolute Rule

This task is the ONE complete planning task for TMAR.

DO NOT:

implement architecture changes

install packages

move projects/folders

rename namespaces

change runtime behavior

change database schema

refactor Host

add MediatR yet

migrate CQRS yet

rewrite existing features

perform cosmetic cleanup

change business behavior

touch unrelated code

The purpose is to establish the full, evidenced architecture recovery plan before implementation starts.

The audit scope is REPOSITORY-WIDE.

Any named examples in this task such as:
StoreAppearance*, StoreLandingPage*, StoreMenu*, StoreCheckout*
are EXAMPLES ONLY and MUST NOT limit the audit scope.

Do not ask the user to manually identify suspicious prefixes or classes.
The Worker must discover architecture drift across the whole repository.

0. Recovery Safety

Verify before any inspection work:

repository root:
D:\Users\User\source\repos\SarvNewVer

branch = main

HEAD == origin/main

commit 18ca10c9 is an ancestor of HEAD

user work is preserved

no destructive git operations

no git reset

no git clean

no checkout/restore over user work

no stash manipulation over user work

no unrelated cleanup

no .tmp-* cleanup unless explicitly requested

Evidence:
docs/evidence/TB-TMAR-ARCH-BASELINE/recovery-start.md

1. Target Architecture — Canonical Direction for Planning

The target architecture is:

Modular Monolith now

low-friction future migration to Microservices

strict bounded-context ownership

CQRS

MediatR for application use-cases

FluentValidation through pipeline

per-module Contracts boundary

per-module Domain/Application/Contracts/Infrastructure

per-module PostgreSQL schema

per-module DbContext

per-module migrations

no cross-schema FK

no cross-module SQL JOIN

no foreign-module DbContext in business write path

Host as HTTP/transport/composition root, not business owner

read composition through module read contracts/gateways

module-to-module communication through declared contracts/gates/events

IClock abstraction

IIdGenerator/IUuidGenerator abstraction

semantic error codes + localized ProblemDetails at boundary

centralized unlimited-locale policy

cache abstraction only

architecture tests/CI preventing new drift

gradual strangler migration, NEVER Big Bang rewrite

Important:
The current application may contain valid working behavior.
The objective is to preserve working behavior while recovering architectural boundaries.

2. Current Architecture Snapshot

Document the ACTUAL current state with exact project/file references.

At minimum verify:

Module topology

every *.Domain

every *.Application

every *.Infrastructure

any existing *.Contracts

Tooba.ModuleContracts

Host

BuildingBlocks/shared projects

Persistence

For every business module:

DbContext

PostgreSQL schema

migrations ownership

repository/persistence registration

Verify:

no global mega business DbContext

no cross-schema FK

no cross-module SQL JOIN pattern

current architecture tests enforcing these

Cross-module dependencies

Map:

Application → Application references

Application → Contracts references

Infrastructure → Infrastructure references

Domain → anything outside its own allowed boundary

Host → module Application

Host → module Infrastructure

Host → module DbContexts

Communication patterns

Map:

synchronous Gate/Directory/Gateway calls

Domain Events

Outbox

Integration Events

MassTransit SQL transport

transaction boundaries

Evidence:
docs/evidence/TB-TMAR-ARCH-BASELINE/current-state.md

3. CQRS + MediatR Recovery Plan

Current known issue:
MediatR is absent and application use-cases are mainly implemented as Directory/Application Service orchestration.

Target flow:

HTTP Endpoint
→ ISender
→ Command / Query
→ Handler
→ Domain + Repository/Gates/Contracts

Approved MediatR version for implementation planning:
MediatR 12.5.0

Do NOT install it in this task.

Validation target:
FluentValidation through MediatR pipeline.

Required pipeline responsibilities:

validation

authorization

logging/telemetry

transaction behavior where appropriate

idempotency where required

exception/error normalization boundary

Required migration strategy:

Stage A:
Endpoint
→ Handler
→ existing Directory

Stage B:
Handler
→ Domain + Repository + Gates/Contracts

Stage C:
Directory narrows to reusable internal service/query abstraction or is removed when truly redundant

Do NOT propose deleting all Directories immediately.
Do NOT propose an all-at-once CQRS rewrite.

Audit:

which Directories currently own validation

business rules

transactions

orchestration

cross-module calls

querying

persistence

authorization

Classify each major Directory:
Keep / Wrap First / Split / Replace Later

Evidence:
docs/evidence/TB-TMAR-ARCH-BASELINE/cqrs-mediatr-plan.md

4. Host Architecture Audit — Repository-Wide and Detailed

The Host is a high-priority recovery area.

Inventory ALL Host responsibilities.

Classify every meaningful Host file/group as one of:

HTTP transport

authentication/session boundary

middleware

DI/composition root

serialization

endpoint mapping

read composition

direct DbContext read

direct DbContext write

SaveChanges

transaction

business validation

pricing decision

inventory/availability decision

seller/buy-box decision

campaign eligibility decision

admin workflow orchestration

caching

localization/error mapping

business capability ownership

seed/dev-only infrastructure

unclear/needs-design

Do NOT limit this audit to known examples.

Provide exact paths and counts where practical.

Evidence:
docs/evidence/TB-TMAR-ARCH-BASELINE/host-audit.md

5. Immediate Host Freeze Policy

Design an enforceable freeze so architecture does not get worse while recovery is underway.

NO NEW:

Host business DbContext writes

Host SaveChanges

Host BeginTransaction

Host business rules

Host pricing decisions

Host inventory/availability decisions

Host seller/buy-box decisions

Host campaign/business eligibility decisions

Host domain ownership

Host cross-module business write orchestration

direct foreign-module DbContext reads that expand legacy coupling

Allowed:

HTTP transport

auth/session boundary

middleware

DI/composition root

serialization

endpoint mapping

temporary legacy read composition where already present, but it must not expand

Define exactly how architecture tests can prevent NEW violations before old violations are fully migrated.

Evidence:
docs/evidence/TB-TMAR-ARCH-BASELINE/host-freeze.md

6. Host Cleanup Waves

Create a safe, phased Host cleanup plan.

Wave 1 — Dangerous writes

Move out:

direct Add/Update/Delete

SaveChanges

BeginTransaction

business persistence orchestration

Wave 2 — Business decisions

Move out:

effective pricing decisions

stock/availability decisions

seller/buy-box choice

campaign eligibility

business validation

domain policies

Wave 3 — Read-side contracts

Replace direct foreign-module DbContext composition with:

I*ReadGateway

module query contracts

in-process adapters initially

Wave 4 — Host simplification

Host becomes:

transport

auth

middleware

DI

endpoint mapping

serialization

minimal view composition

For each wave return:

exact candidate files

destination owner/module

migration pattern

risk

S/M/L effort

required tests

rollback approach

Evidence:
docs/evidence/TB-TMAR-ARCH-BASELINE/host-cleanup-waves.md

7. Repository-Wide Domain Ownership Audit

THIS SECTION IS CRITICAL.

Audit ALL *.Domain projects and ALL domain types.

Do NOT inspect only Catalog.
Do NOT inspect only known suspicious names.
Do NOT classify by filename alone.

For every:

Aggregate

Entity

Value Object

Domain Service

Policy

Settings object

Registry

Domain enum

Domain rule

domain-specific helper with business semantics

Record:

Type
Namespace
Current Module
Business capability represented
Invariant/lifecycle owner
Persistence owner
Conceptual dependencies
Correct bounded-context owner
Keep / Move / Split / Needs-design
Reason
Migration risk
Move now / Defer

Known examples that MUST be inspected but are NOT the full scope:

StoreAppearance*

StoreLandingPage*

StoreMenu*

StoreCheckout*

StoreHoldPolicy*

ReservationPolicy*

IndustryBatchTemplate*

TemplateCatalog*

Principle:
A type belongs to the bounded context that owns its business invariant and lifecycle.

It MUST NOT live in a module merely because:

that module already had a DbContext

persistence was convenient there

UI currently consumes it there

another entity references its ID

the code historically started there

Cross-capability types must:

belong to one clear owner and expose Contracts, or

be split if they contain invariants from multiple bounded contexts

Evidence:
docs/evidence/TB-TMAR-ARCH-BASELINE/domain-ownership-map.md

8. Domain Purity Audit

Audit all Domain projects for:

direct system clock use

DateTime.UtcNow

DateTimeOffset.UtcNow

DateTime.Now

concrete clock implementation

Guid.NewGuid()

UuidV7.New()

random generation

hardcoded user-facing FA/EN error messages

references to Host

references to Infrastructure

references to foreign Application

references to foreign Domain entities

UI-specific concepts that do not belong to the bounded context

persistence-specific logic

transport-specific logic

Clarify valid pattern:
Pure domain methods may receive now, IDs or already-resolved policies as explicit inputs.

Do NOT require IClock injected into every entity.

Evidence:
docs/evidence/TB-TMAR-ARCH-BASELINE/domain-purity.md

9. Architecture Ownership Registry

Design a compact machine-readable architecture ownership registry, for example:

docs/architecture/TOOBA-DOMAIN-OWNERSHIP.yaml

The registry should define per module:

bounded-context name

owned business capabilities

owned aggregate/type namespaces or type families

public Contracts project

PostgreSQL schema

DbContext

migrations owner

allowed dependency direction

allowed public integration boundary

Important:
This is NOT a giant hand-maintained list of every class forever.

It defines module/capability ownership rules so structural drift can be detected.

Semantic ownership still requires architecture review.

Do NOT implement enforcement yet; design it.

Evidence:
docs/evidence/TB-TMAR-ARCH-BASELINE/ownership-registry-design.md

10. Module Packaging / Folder Structure Target

Current flat project layout must be evaluated separately from runtime architecture.

Target physical organization:

src/
Modules/
Catalog/
Tooba.Catalog.Domain/
Tooba.Catalog.Application/
Tooba.Catalog.Contracts/
Tooba.Catalog.Infrastructure/

Pricing/
  Tooba.Pricing.Domain/
  Tooba.Pricing.Application/
  Tooba.Pricing.Contracts/
  Tooba.Pricing.Infrastructure/

Inventory/
  ...

Cart/
  ...

src/
Host/
Tooba.Host/

Do NOT move folders in this task.

Rule:
dependency/ownership repair happens BEFORE physical reorganization.

No cosmetic folder move should hide incorrect boundaries.

Evidence:
docs/evidence/TB-TMAR-ARCH-BASELINE/module-layout-target.md

11. Contracts Boundary Recovery

Current known weakness:
cross-module interfaces/contracts frequently live inside *.Application.

Audit all cross-module references.

Target:
Tooba.<Module>.Contracts

Contracts may expose:

gateway interfaces

read/query interfaces

boundary DTOs/value contracts

stable cross-module commands only if architecturally justified

integration/public event contracts

Forbidden as cross-module public boundary:

Infrastructure

internal Application implementation classes

Domain entities

DbContext

EF models

internal repositories

Target example:

Cart.Application
→ Pricing.Contracts

NOT:

Cart.Application
→ Pricing.Application

Define:

which modules need Contracts projects

migration order

compatibility strategy

temporary adapters

architecture test rollout

Evidence:
docs/evidence/TB-TMAR-ARCH-BASELINE/contracts-plan.md

12. Read-Side Microservice Readiness

Audit all read composition paths that directly depend on multiple module DbContexts.

Target:

Composer / Query Handler
→ ICatalogReadGateway
→ IPricingReadGateway
→ IInventoryReadGateway
→ IOfferReadGateway
→ etc.

Today:
implementations may stay in-process.

Tomorrow:
same boundary can be backed by:

HTTP

gRPC

dedicated read API

replicated/read model where justified

Rules:

no cross-schema SQL JOIN

no business truth decision in Host

Pricing module resolves effective price

Inventory module resolves availability

Offer/Sales module resolves sellable seller/buy-box if it owns the invariant

Campaign/Promotion module resolves campaign eligibility if it owns the invariant

Composer only composes already-authoritative projections

Identify all current Host/composer sites that violate this target.

Evidence:
docs/evidence/TB-TMAR-ARCH-BASELINE/read-gateway-plan.md

13. Time Abstraction

Audit repository-wide:

DateTime.UtcNow

DateTimeOffset.UtcNow

DateTime.Now

direct clock implementations

ad-hoc time providers

Target:
IClock abstraction at Application/Infrastructure orchestration boundaries.

Clarify:

canonical interface

canonical time type

implementation location

test/fixed clock strategy

migration rule

Domain explicit now pattern

Evidence:
docs/evidence/TB-TMAR-ARCH-BASELINE/clock-plan.md

14. ID Generation Abstraction

Audit repository-wide:

UuidV7.New()

Guid.NewGuid()

custom generators

random ID generation

Target:
IIdGenerator / IUuidGenerator central abstraction.

Rules:

Domain does not call implementation APIs directly when determinism matters

Application creates IDs via abstraction where creation orchestration belongs there

pure Domain receives ID explicitly when appropriate

deterministic tests must be possible

Evidence:
docs/evidence/TB-TMAR-ARCH-BASELINE/id-generation-plan.md

15. Error Architecture + Localization

Audit repository-wide for:

hardcoded Persian exception strings

hardcoded English exception strings

InvalidOperationException used as user-facing business error

inconsistent error codes

endpoint-local translation

duplicated validation text

missing ProblemDetails detail mapping

Current anti-pattern example:

throw new InvalidOperationException("slug رده پس از نرمال‌سازی خالی شد.");

Target:

semantic stable ErrorCode

typed Domain/Application error or result

no localized user-facing text in Domain

Host/API maps error code to localized ProblemDetails

detail resolved based on locale

validation errors also localized through standard pipeline

Example:
catalog.category.invalid_slug
→ localized ProblemDetails.detail

The system must support unlimited locales.
Do not design around only FA/EN.

Evidence:
docs/evidence/TB-TMAR-ARCH-BASELINE/error-localization-plan.md

16. Locale Policy

Audit repository-wide:

hardcoded "fa"

hardcoded "en"

locale Trim logic

fallback logic

normalization duplication

culture parsing

language selection in Domain

page/store locale handling

per-feature locale conventions

Target:
central locale/culture policy or value abstraction.

Must define:

normalization

validation

supported locale source

fallback chain

current request/page/store locale

invariant vs localized business data

responsibility boundaries across Domain/Application/Host/Infrastructure

Unlimited locales must be supported.

Evidence:
docs/evidence/TB-TMAR-ARCH-BASELINE/locale-policy.md

17. Cache Architecture

Existing intended foundation:

ICache

ICacheKeyBuilder

ICacheInvalidator

CacheRegistration.AddToobaCache

Memory

None

Redis deferred

Audit repository-wide for:

direct IMemoryCache

MemoryCache

static dictionaries used as cache

module-specific custom cache bypasses

Known examples to inspect, but not scope limit:

StoreLandingPageComposer

StoreMenuComposer

StoreAppearanceProjection

Target:
all application/module cache consumption through the shared abstraction.

Future Redis provider requirements:

distributed invalidation

stampede protection

jittered TTL

tenant/store/locale-aware keys

metrics

safe negative caching

no Redis-specific API leaking into modules

Do NOT implement Redis in this task.

Evidence:
docs/evidence/TB-TMAR-ARCH-BASELINE/cache-plan.md

18. Architecture Tests / CI Guard Design

Design enforceable tests for NEW drift.

Must cover at minimum:

Domain ↛ Infrastructure

Domain ↛ Host

Domain ↛ foreign Application

Application ↛ foreign Infrastructure

Module A.Infrastructure ↛ Module B.Infrastructure

no global business DbContext

no cross-schema FK

no cross-schema SQL JOIN pattern

no NEW Host business DbContext writes

no NEW Host SaveChanges/BeginTransaction on business module DbContexts

no NEW Host business-decision ownership

no NEW Application→foreign Application dependency after Contracts migration gate activates

all NEW use-cases use MediatR Command/Query handlers

hardcoded localized user-facing Domain exception text forbidden where mechanically enforceable

direct system clock forbidden in selected layers

direct Guid.NewGuid/UuidV7.New forbidden in selected layers

direct IMemoryCache forbidden outside approved provider/boundary

ownership registry/module namespace consistency

every Domain assembly belongs to one bounded context

no foreign Domain entity types crossing module boundary

Important:
Semantic bounded-context ownership cannot be fully inferred automatically.

CI prevents structural drift.
Ownership registry + architecture review prevents semantic drift.

Evidence:
docs/evidence/TB-TMAR-ARCH-BASELINE/architecture-tests-plan.md

19. Proposed Canonical Architecture Locks

DO NOT canonicalize these in this planning task.
Return them as PROPOSED LOCKS for Architect approval.

At minimum propose:

ARCH-OWN-001
Every Domain type has exactly one bounded-context owner determined by business invariant and lifecycle, not persistence convenience.

ARCH-OWN-002
No new Domain type may enter a module unless that module owns the capability or the ownership registry is changed in the same approved architecture change.

ARCH-OWN-003
Cross-capability types must have one clear owner or be split; a convenience module may not become a dumping ground.

ARCH-CONTRACT-001
Cross-module business dependencies use Tooba.<Module>.Contracts, not foreign Application/Infrastructure.

ARCH-DOMAIN-001
Domain errors are semantic/error-code based; localized FA/EN user-facing text is forbidden in Domain.

ARCH-TIME-001
Application/Infrastructure orchestration uses IClock; pure Domain may receive explicit now.

ARCH-ID-001
ID generation uses approved abstraction at orchestration boundaries; direct random/UUID implementation calls are forbidden in selected Domain/Application paths.

ARCH-HOST-001
Host is transport/composition root only; no new business write, transaction, pricing/inventory/seller/campaign decision or domain ownership may be introduced.

ARCH-READ-001
New cross-module read composition uses declared read contracts/gateways; direct foreign DbContext composition is legacy-only and cannot expand.

ARCH-DB-001
Each module owns its schema, DbContext and migrations; no cross-schema FK/JOIN or foreign-module business write DbContext access.

ARCH-CQRS-001
All new application use-cases use CQRS + MediatR Handler.

ARCH-CQRS-002
Approved MediatR version = 12.5.0.

ARCH-VAL-001
New request validation uses FluentValidation through the MediatR pipeline.

ARCH-LOCALE-001
Locale normalization/fallback is centralized and must support unlimited locales.

ARCH-CACHE-001
New cache consumption uses ICache abstraction; direct IMemoryCache use cannot expand.

ARCH-FOLDER-001
Physical module folder reorganization happens only after ownership/dependency repair, never as cosmetic architecture work.

Evidence:
docs/evidence/TB-TMAR-ARCH-BASELINE/proposed-locks.md

20. Migration Strategy — NO BIG BANG

Produce a phased plan that allows product development to continue.

Required rule:

NEW code follows the target architecture immediately after the first approved Foundation implementation task.

OLD code migrates when:

touched by feature work

part of a high-risk Host violation

part of an extraction-critical dependency

required to establish a bounded-context boundary

Required migration waves:

Architecture Foundation

Host Write Removal

CQRS Adoption

Contracts Extraction

Domain Ownership Corrections

Read Gateway Migration

Error/Locale Standardization

Cache Adoption Cleanup

Physical Folder Reorganization

Microservice Extraction Readiness Verification

For each wave:

objective

prerequisites

risk

S/M/L effort

exit criteria

Evidence:
docs/evidence/TB-TMAR-ARCH-BASELINE/migration-roadmap.md

21. Priority Matrix

Classify all findings:

Critical Now
High
Medium
Later

Must explicitly include:

Host direct writes

Host business decisions

missing CQRS/MediatR

Contracts inside Application

wrong bounded-context ownership

Domain impurity

IClock absence

ID generator abstraction

localized Domain errors

locale duplication

direct IMemoryCache

read-side microservice coupling

flat module folder layout

Do not hide additional discovered issues.
Add them to the matrix.

Evidence:
docs/evidence/TB-TMAR-ARCH-BASELINE/priority-matrix.md

22. Exact Next Task Sequence

Return the next 5–8 concrete implementation tasks.

Each must include:

Task ID proposal

task name

scope

dependency

S/M/L effort

risk

acceptance proof

The FIRST implementation task must be deliberately small and safe:

Architecture Foundation only:

install MediatR 12.5.0

add FluentValidation

add IClock

add IIdGenerator/IUuidGenerator

add pipeline behaviors

add architecture guards

freeze NEW Host violations

establish error-code foundation if safe

no mass conversion of old Directories

no Host cleanup wave yet

no project/folder moves

no Redis

Later tasks should address:

Host dangerous writes

Contracts extraction

CQRS strangler migration

Domain ownership moves

read gateways

localization/error migration

cache cleanup

folder organization

Evidence:
docs/evidence/TB-TMAR-ARCH-BASELINE/next-tasks.md

23. Capability Map

Consult:
docs/architecture/TOOBA-CAPABILITY-MAP.md

This task is a material architecture review, so consultation is justified.

Update ONLY architecture/recovery planning facts.

Add compact section:
TMAR / Architecture Recovery

Record:

verified current architectural strengths

verified architectural debt

target direction

Last Verified Task = TB-TMAR-ARCH-BASELINE

Do not rewrite unrelated capability sections.

24. Anti-Pattern Gate

Reject any plan that proposes:

Big Bang rewrite

all-at-once MediatR/CQRS conversion

deleting all Directories immediately

moving folders before ownership/dependency repair

treating file/folder cleanup as architecture recovery

network calls for every read immediately

Redis before cache adoption cleanup

interface-per-pure-function overengineering

injecting IClock into every Domain entity

hardcoded localized FA/EN text in Domain errors

foreign-module DbContext writes

cross-schema FK/JOIN

Host as business orchestrator

a bounded context chosen because persistence was convenient

auditing only named examples/prefixes

assuming compile success means ownership is correct

bypassing user-work preservation

Evidence:
docs/evidence/TB-TMAR-ARCH-BASELINE/antipattern-scan.md

25. Recovery SoT

At completion produce a concise source-of-truth section:

Program:
TMAR — Tooba Microservice-Ready Architecture Recovery

Current Phase:
Architecture Recovery Planning Complete

Last Product Implementation:
TB-P10-T022-R21

Architecture Planning Task:
TB-TMAR-ARCH-BASELINE

Active Repair:
none

Active Implementation:
none

Next Recommended Task:
the first task from next-tasks.md

Branch:
main

HEAD==origin/main:
YES/NO

18ca10c9 ancestor:
YES/NO

User work preserved:
YES/NO

Production implementation performed:
NO

Evidence:
docs/evidence/TB-TMAR-ARCH-BASELINE/recovery-sot.md

26. Acceptance Criteria

PASS only if ALL are true:

whole repository architecture inspected

current architecture evidenced with exact paths

ALL Domain projects audited for bounded-context ownership

named examples treated only as examples

Host debt fully mapped

Host freeze policy designed

CQRS/MediatR strangler plan exists

MediatR target version recorded as 12.5.0

FluentValidation pipeline plan exists

Contracts migration exists

read-gateway plan exists

IClock plan exists

ID generation plan exists

error/localization plan exists

unlimited-locale policy exists

cache plan exists

architecture ownership registry designed

architecture/CI tests designed

no Big Bang rewrite

module folder target exists but no physical move performed

exact priority matrix exists

exact next 5–8 tasks exist

ZERO production implementation changes

ZERO package installation

ZERO runtime behavior changes

user work preserved

canonical Result delivered through Bridge

Worker stops completely

27. Result Contract

Return ONLY canonical BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Applicable-Locks
Recovery-Start
Current-State
Module-Topology
Persistence-Boundaries
Dependency-Map
CQRS-MediatR-Plan
Directory-Classification
Host-Audit
Host-Freeze
Host-Cleanup-Waves
Domain-Ownership-Map
Domain-Purity
Ownership-Registry
Module-Layout-Target
Contracts-Plan
Read-Gateway-Plan
Clock-Plan
Id-Generation-Plan
Error-Localization-Plan
Locale-Policy
Cache-Plan
Architecture-Tests-Plan
Proposed-Locks
Migration-Roadmap
Priority-Matrix
Next-Tasks
Capability-Map
AntiPattern-Scan
Recovery-SoT
Git
Architectural-Concerns
Blockers

Do not include Worker-IDLE.

After sending canonical Result through Bridge:
STOP completely.

Do NOT:

poll

fetch next task

continue implementation

write Worker IDLE

END_TOOBA_TASK