PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID:
TB-TMAR-HOST-W3

Parent-Task:
TB-TMAR-FE-ADMIN-W6

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

Title:
TMAR Host Wave 3 — Remove One High-Value Direct Host Write Slice Behind CQRS/Application Boundary

Task Type:
IMPLEMENTATION — EVIDENCE-SELECTED HOST DEBT REDUCTION

0. Architect Intent

FE-ADMIN-W6 is accepted.

Verified program state:

Product-Resume-Safety = SAFE_WITH_TMAR_PARALLEL

Backend structural recovery is stable for parallel work

Frontend low-risk flat-admin migration pattern is PROVEN

Flat-Admin-Exit-State = READY_TO_PIVOT

Orders admin is NEEDS_BACKEND/WORKFLOW_BOUNDARY_FIRST

God-Component-Recovery-Readiness = DEFER

Architecture-Priority = HOST

known Checkout shared-ACID debt remains deferred to design-first work

Host direct-write debt remains a high-value backend extraction blocker

existing Host migrations already proved the pattern for StoreLandingPage and StoreMenu

This task returns to Host cleanup and removes exactly ONE high-value Host business-write slice.

Primary objectives:

inventory CURRENT remaining Host direct-write/business-decision debt from live repository state

select exactly ONE coherent Host write slice

move Host write/orchestration behind CQRS/Application boundary

preserve product behavior and transaction semantics

shrink Host-write baseline exactly

do not broaden into Order/Checkout distributed-consistency redesign

No Big Bang Host rewrite.
No cosmetic file moves.
No unrelated refactor.

1. Recovery / Git Safety

Repository:
D:\Users\User\source\repos\SarvNewVer

Read durable sources:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TOOBA-MICROSERVICE-MIGRATION-NOTES.md

docs/architecture/TMAR-architecture-locks.md

docs/architecture/TOOBA-CAPABILITY-MAP.md

docs/evidence/TB-TMAR-FE-ADMIN-W6/recovery-sot.md

latest Host-write architecture baseline

docs/evidence/TB-TMAR-HOST-W2/recovery-sot.md

docs/evidence/TB-TMAR-ARCH-BASELINE/recovery-sot.md where useful

Verify:

branch main

exact HEAD SHA

exact origin/main SHA

HEAD == origin/main

18ca10c9 ancestor

git status --short

git diff --name-only

git diff --cached --name-only

user work preserved

Expected previous accepted tip:
f612d90c13c93480fc93aa4606b8c41cab1278e3

If conflicting tracked user work exists:
STOP with RECOVERY_CONFLICT.

Never:

git reset

git clean

destructive checkout/restore

unsafe rebase

blind stash manipulation

broad git add .

Evidence:
docs/evidence/TB-TMAR-HOST-W3/recovery-start.md

2. Refresh Host Direct-Write Inventory

Inspect current Host production code for direct business writes and business decisions.

Search broadly for at least:

SaveChanges

SaveChangesAsync

Add

AddAsync

Update

Remove

BeginTransaction

TransactionScope

direct module DbContext injections

direct repository writes if repository abstractions are used in Host

business-rule decisions in Host involving price/inventory/offer/seller/campaign/order/payment/status truth

Classify each candidate:

DIRECT_DB_WRITE

DIRECT_TRANSACTION

BUSINESS_DECISION

PRESENTATION_COMPOSITION_ONLY

FALSE_POSITIVE

NEEDS_REVIEW

Produce:
docs/evidence/TB-TMAR-HOST-W3/host-write-inventory.md
docs/evidence/TB-TMAR-HOST-W3/host-write-inventory.json

Do not rely on stale baseline only.
Use live repository evidence.

3. Select Exactly ONE Coherent Host Slice

Choose the highest-value SAFE slice from current inventory.

Selection criteria:

actual direct Host write/transaction or business truth

clear bounded-context owner

behavior can be characterized

CQRS/Application migration can be done without distributed-consistency redesign

meaningful Host baseline shrink

moderate regression radius

no giant multi-context workflow unless only boundary relocation is required

Do NOT select:

Checkout shared-ACID redesign

Order lifecycle orchestration requiring saga/process design

a cosmetic read-only Composer just to make numbers look better

a slice whose real ownership is unknown

Before implementation record:

Host file/member

owning module/capability

exact DbContext/repository/domain services used

transaction behavior

current business decisions

endpoint/route callers

target Command/Handler/Application abstraction

expected baseline reduction

why selected

Evidence:
docs/evidence/TB-TMAR-HOST-W3/slice-selection.md

4. Characterization Before Migration

Before changing structure, lock current behavior.

Cover as applicable:

request/command inputs

authorization/store/tenant behavior

validation behavior

write effects

transaction boundaries

error semantics

idempotency if relevant

ordering of dependent writes

cache invalidation if relevant

response shape/status

No broad snapshots.
No fake test-only business shortcuts.

Evidence:
docs/evidence/TB-TMAR-HOST-W3/characterization-tests.md

5. Target CQRS/Application Shape

Target:

HTTP Endpoint / Host Presentation
→ ISender
→ Command
→ Handler in owning module Application
→ Domain / module-owned application abstraction
→ Infrastructure persistence implementation

Rules:

business IRequestHandler<> MUST live in Application

EF/DbContext/transaction implementation belongs in Infrastructure

Host must not own business write truth

Host may map transport request to Command and map result to response

existing Directory may temporarily remain behind Handler if it is the verified strangler seam

do not create a fake Handler that merely calls Host code

Do NOT introduce a global transaction behavior.

Evidence:
docs/evidence/TB-TMAR-HOST-W3/target-shape.md

6. Move Direct Persistence Out of Host

For selected slice:

remove direct Host DbContext/repository write usage

remove Host SaveChanges* / transaction ownership for that slice

move persistence responsibility to owning module Infrastructure

move business orchestration to Application

preserve domain ownership

preserve existing transaction semantics where currently local to one module

If current Host slice writes a module-owned table via another module's DbContext:
document ownership concern explicitly and do NOT silently bless wrong ownership.
Use current owning module only if evidence supports it; otherwise STOP with NEEDS_DESIGN.

Evidence:
docs/evidence/TB-TMAR-HOST-W3/host-write-removal.md

7. Business Decision Removal

Audit selected Host slice for decisions such as:

price choice

sellability

inventory availability

seller selection

campaign validity

state transition rules

default/fallback business truth

Move those decisions to Application/Domain owner where required.

Host may retain:

HTTP parsing

auth/session boundary

route composition

response formatting

presentation-only composition

Do not move presentation-only formatting into Domain/Application.

Evidence:
docs/evidence/TB-TMAR-HOST-W3/business-decision-removal.md

8. Transaction Semantics

Preserve current semantics.

If selected slice uses transaction:

characterize exact transaction scope

move transaction implementation to owning module Infrastructure if module-local

preserve isolation/order/rollback behavior

If transaction spans multiple bounded contexts:
DO NOT redesign it here.
If necessary, select another Host slice.

ARCH-TX-001 remains active:
No NEW business workflow may rely on single ACID transaction spanning multiple bounded contexts.

Evidence:
docs/evidence/TB-TMAR-HOST-W3/transaction-semantics.md

9. Cache Boundary Compliance

If selected Host slice touches cache:

use existing ICache / ICacheKeyBuilder / ICacheInvalidator

do not add direct IMemoryCache

preserve invalidation behavior

do not install Redis

no polling or magic TTL workaround

If existing direct Host cache bypass is found in selected slice, repair only if naturally part of the slice.

Evidence:
docs/evidence/TB-TMAR-HOST-W3/cache-compliance.md

10. Error / Locale / Time / ID Compliance

New code must follow current locks:

semantic stable errors

no hardcoded localized Domain/Application user-facing messages in any language

HTTP boundary localizes ProblemDetails.detail

unlimited locale support

IClock for orchestration time

IIdGenerator for new IDs

pure Domain methods may receive now explicitly

Do not mass-migrate unrelated legacy code.

Evidence:
docs/evidence/TB-TMAR-HOST-W3/foundation-compliance.md

11. Host Baseline Shrink

Update exact Host-write baseline.

Required:

selected direct Host write entries removed

no new Host write entries

no baseline widening

no wildcard suppression

Also verify:

App→App baseline does not grow

Infra→foreign App baseline does not grow

Domain→foreign Domain remains clean

Infra→foreign Domain remains clean

cross-context transaction baseline does not grow

source-size baseline does not grow

Evidence:
docs/evidence/TB-TMAR-HOST-W3/architecture-guards.md

12. God-File / Source-Size Safety

No new hand-written source >800 LOC.
Existing oversized files are shrink-only.

If selected slice touches oversized file:

keep diff narrow

no broad decomposition

characterization before any extraction

do not create a new giant Handler/Directory

If moving logic would make an Application file exceed 800 LOC:
split by use-case/capability now instead of growing a god file.

Evidence:
docs/evidence/TB-TMAR-HOST-W3/source-size-compliance.md

13. Tests

Required:

characterization tests for selected behavior

affected module tests

Host endpoint/integration tests where present

ArchitectureBoundaryTests

TMAR foundation tests

Host-write guard

source-size guard

transaction guard

Contracts guards

No broad all-module test-project creation.

Evidence:
docs/evidence/TB-TMAR-HOST-W3/tests.md

14. Orders / Frontend Interaction Check

Because frontend Orders admin was classified:
NEEDS_BACKEND/WORKFLOW_BOUNDARY_FIRST

Assess whether this Host slice materially improves Orders/admin boundary readiness.

Return:
Orders-Frontend-Readiness: IMPROVED
or
Orders-Frontend-Readiness: UNCHANGED

Do NOT migrate frontend Orders in this task.

Evidence:
docs/evidence/TB-TMAR-HOST-W3/orders-frontend-readiness.md

15. Host Recovery State

After migration, assess current Host debt.

Return one:
Host-Recovery-State: CONTINUE_HOST
Host-Recovery-State: READY_TO_PIVOT

READY_TO_PIVOT requires:

new Host write growth frozen

selected high-value direct writes removed

remaining Host debt is either lower-risk residual or blocked by separate ownership/consistency design

new features have a clear CQRS/Application path

continuing Host cleanup is no longer the highest architectural value

Evidence:
docs/evidence/TB-TMAR-HOST-W3/host-recovery-state.md

16. Architecture Priority Checkpoint

Choose the highest-value next architectural area using actual evidence.

Return exactly one:
Architecture-Priority: HOST
Architecture-Priority: CHECKOUT_DESIGN
Architecture-Priority: FE_GODFILE
Architecture-Priority: CONTRACTS

Guidance:

choose HOST if another safe high-value direct-write slice remains

choose CHECKOUT_DESIGN if shared-ACID migration risk now dominates

choose FE_GODFILE only if characterization readiness has become sufficient

choose CONTRACTS only if remaining App→App/Infra→App boundary debt clearly outranks others

Do not choose by task sequence convenience.

Evidence:
docs/evidence/TB-TMAR-HOST-W3/architecture-priority.md

17. Next Task Decision

Choose automatically:

A. TB-TMAR-HOST-W4
if Host-Recovery-State = CONTINUE_HOST and HOST remains highest value.

B. TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN
if CHECKOUT_DESIGN is highest value.

C. TB-TMAR-FE-GODFILE-W1
only if FE_GODFILE is highest value and readiness is demonstrably READY.

D. TB-TMAR-CONTRACTS-W7
if CONTRACTS is highest value.

Do not ask the user.

18. Product Safety

No product behavior change.

Do NOT:

alter routes/contracts externally

change pricing/inventory semantics

change seller/campaign truth

change UI

change storefront behavior

redesign Checkout

implement Saga

change order lifecycle unless strictly preserving existing behavior behind new boundary

19. Recovery State Updates

Update narrowly:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TMAR-architecture-locks.md only if durable locks change

docs/architecture/TOOBA-CAPABILITY-MAP.md only if material ownership evidence changes

docs/evidence/TB-TMAR-HOST-W3/recovery-sot.md

Record:

selected Host slice

removed direct writes

Host baseline before/after

Orders-Frontend-Readiness

Host-Recovery-State

Architecture-Priority

next task

20. Acceptance Criteria

PASS only if:

current Host direct-write inventory refreshed

exactly one coherent Host write slice selected

behavior characterized before migration

direct Host persistence removed for selected slice

business handler lives in Application

persistence/transaction belongs to Infrastructure

Host baseline shrinks

no new dependency debt

no new shared-ACID workflow

no product behavior change

source-size guards green

tests pass with no workaround/suppression

Orders frontend readiness assessed

Host recovery state assessed

Architecture-Priority returned

user work preserved

next task automatically selected

canonical Result delivered

Worker stops completely

21. Result Contract

Return ONLY canonical BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Recovery-Start
Host-Write-Inventory
Slice-Selection
Characterization-Tests
Target-Shape
Host-Write-Removal
Business-Decision-Removal
Transaction-Semantics
Cache-Compliance
Foundation-Compliance
Architecture-Guards
Source-Size-Compliance
Tests
Orders-Frontend-Readiness
Host-Recovery-State
Architecture-Priority
Capability-Map
Bootstrap
Master-Recovery-State
Recovery-SoT
Git
Architectural-Concerns
Blockers
Product-Resume-Safety
Next-Recommended-Task

Expected:
Product-Resume-Safety: SAFE_WITH_TMAR_PARALLEL

After canonical Result through Bridge:
STOP completely.

Do NOT poll.
Do NOT fetch next task.
Do NOT write Worker IDLE.

END_TOOBA_TASK