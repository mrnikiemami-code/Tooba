PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID:
TB-TMAR-CHECKOUT-IMPL-W4

Parent-Task:
TB-TMAR-HOST-STRUCTURE-W1-R1

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
TMAR Checkout Implementation Wave 4 — Remove Order.Infrastructure → Inventory.Application Residuals Behind Inventory.Contracts

Task Type:
IMPLEMENTATION — BACKEND-ONLY BOUNDARY CLEANUP / BEHAVIOR-PRESERVING

Backend-Only:
YES

TMAR-Execution-Mode:
BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE

0. Architect Intent

TB-TMAR-HOST-STRUCTURE-W1-R1 is accepted.

Durable guards now ACTIVE:

ARCH-FE-FREEZE-001

ARCH-FOLDER-OWNERSHIP-001

ARCH-RECOVERY-001

ARCH-USERWORK-001

ARCH-BASELINE-001

ARCH-NOWORKAROUND-001

ARCH-DATA-001

HOST-FOLDER-001

HOST-HYGIENE-001

Verified checkout state:

Order-owned in-process CheckoutProcessManager is active

Inventory reservation uses Inventory.Contracts

Cart conversion uses Cart.Contracts

Order.Application → Inventory.Application removed

Order.Application → Cart.Application removed

shared TransactionScope across Order + Inventory + Cart remains intact

no async Saga/messaging/compensation runtime

Checkout-Implementation-W4-Readiness = READY

W4 candidate = Order.Infrastructure → Inventory.Contracts cleanup for Cancel/Restore/PaymentBridge

Orders frontend remains frozen by ARCH-FE-FREEZE-001 regardless of readiness

Primary objectives:

remove checkout/order-related Order.Infrastructure → Inventory.Application coupling where evidence supports a stable Inventory.Contracts seam

cover Cancel / Restore / PaymentBridge inventory interactions

preserve current synchronous behavior and transaction semantics

keep inventory authority in Inventory

do NOT broaden into unrelated Order/Inventory workflows

do NOT touch frontend

do NOT remove shared TransactionScope

do NOT activate async compensation runtime

1. Recovery / Git Safety

Repository:
D:\Users\User\source\repos\SarvNewVer

Expected previous accepted tip:
f58744c51485a667f222d48be70337de7b7cc935

Read:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TOOBA-MICROSERVICE-MIGRATION-NOTES.md

docs/architecture/TMAR-architecture-locks.md

docs/evidence/TB-TMAR-CHECKOUT-IMPL-W3/recovery-sot.md

docs/evidence/TB-TMAR-CHECKOUT-IMPL-W3/order-infra-inventory-residual.md

docs/evidence/TB-TMAR-CHECKOUT-IMPL-W3/order-hub-residuals.md

docs/evidence/TB-TMAR-HOST-STRUCTURE-W1-R1/recovery-sot.md

TMAR execution mode artifact (BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE)

Verify:

branch main

HEAD == origin/main

18ca10c9 ancestor

git status --short

git diff --name-only

git diff --cached --name-only

user work preserved

no frontend production changes

If tracked user work conflicts:
STOP with RECOVERY_CONFLICT.

Never:

git reset

git clean

destructive checkout/restore

unsafe rebase

blind stash manipulation

broad git add .

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W4/recovery-start.md

2. Frontend Freeze Enforcement

ARCH-FE-FREEZE-001 is ACTIVE.

This task may READ frontend only if compatibility evidence is required.

It MUST NOT modify production code under:
src/frontend/**

Required final:
Frontend-Production-Changes: NONE

Any production frontend modification means FAIL unless Architect/User explicitly releases freeze in a future task.

3. Reconstruct Exact Residual Dependency

Inspect all Order.Infrastructure → Inventory.Application usages from live code.

At minimum audit:

Cancel flow

Restore flow

PaymentBridge flow

any inventory release/restore/reservation reconciliation path

For each usage document:

source file/type

exact Inventory.Application type/method

call semantics

write effect

transaction semantics

idempotency assumptions

failure behavior

whether synchronous response is required

whether it is checkout/order lifecycle related

whether a Contracts seam already exists or must be extended

Classify each:

SAFE_CONTRACT_EXTRACTION

NEEDS_COMPENSATION_DESIGN

NON_CHECKOUT_RESIDUAL

NEEDS_OWNERSHIP_DESIGN

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W4/order-infra-inventory-usage.md

4. Select Exact W4 Scope

Migrate only the coherent Inventory interactions needed for:

Cancel

Restore

PaymentBridge

ONLY where classified SAFE_CONTRACT_EXTRACTION.

If one of these requires compensation/runtime redesign:

leave that one deferred

document exact reason

do NOT fake full dependency removal

The goal is correct boundary reduction, not forcing zero at any cost.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W4/scope-selection.md

5. Extend Inventory.Contracts Minimally

Prefer extending existing Tooba.Inventory.Contracts.

Potential contract capabilities, derive from code:

release reservation / inventory

restore reservation / stock intent

reconcile payment-related reservation state

query reservation reference/status if strictly required

Rules:

no Inventory Domain entities

no Inventory Application types

no DbContext/repository leakage

no generic mega-facade

inventory semantics remain Inventory-owned

future remote adapter friendly

correlation/idempotency fields only where semantically justified

stable error/result semantics

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W4/inventory-contract-extension.md

6. Replace Order.Infrastructure → Inventory.Application

Preferred target:
Order.Infrastructure → Inventory.Contracts

If all selected usages can move safely:

remove direct ProjectReference to Inventory.Application

shrink Infra→foreign App baseline exactly

If residual non-selected usages remain:

retain only what is actually still needed

document exact residual

do not widen baseline

do not introduce reflection/service-locator workarounds

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W4/order-infra-dependency.md

7. Inventory-Owned Adapter / Implementation

Implementation must remain Inventory-owned.

Allowed:

adapter in Inventory.Application or Inventory.Infrastructure according to existing ownership pattern

local persistence in Inventory.Infrastructure

Not allowed:

Order writing Inventory tables

Order reimplementing stock/reservation rules

shared repository crossing contexts

direct Inventory DbContext from Order

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W4/inventory-adapter.md

8. Preserve Cancel / Restore / PaymentBridge Behavior

Characterize before refactor and preserve:

cancel semantics

restore semantics

payment-related reservation handling

retry behavior

duplicate behavior

transaction boundaries

error mapping

ordering of side effects

idempotency/correlation where present

No user-visible behavior change.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W4/behavior-preservation.md

9. Shared TransactionScope Preservation

Current Checkout shared TransactionScope MUST remain intact.

This task must not:

split Order/Inventory/Cart commits

move participant work to independent transactions

remove TransactionScope

change rollback semantics

Also verify selected Cancel/Restore/PaymentBridge paths do not accidentally introduce a new cross-context transaction.

ARCH-TX-001 remains active.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W4/transaction-preservation.md

10. Compensation Boundary Discipline

This task may define stable contract methods that will later support compensation.

It MUST NOT activate:

async compensation

background compensation runner

retry scheduler

timeout handling

distributed Saga messages

compensation event choreography

Current synchronous behavior stays authoritative.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W4/compensation-scope.md

11. Idempotency / Correlation

For selected operations:

preserve stable process/correlation identity

define idempotency key behavior where required

duplicate release/restore operations must not create duplicate business effects

do not use cache/in-memory dedupe as correctness authority

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W4/idempotency-correlation.md

12. Layering / Contracts Compliance

Verify:

Order.Infrastructure consumes Inventory.Contracts only for selected seam

Inventory implementation remains Inventory-owned

no new Application→Application edge

no Domain→foreign Domain

no Infra→foreign Domain

no service locator

no reflection workaround

no business handler in Infrastructure

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W4/layering.md

13. No-Workaround Guard

ARCH-NOWORKAROUND-001 is ACTIVE.

Specifically reject:

fake pass-through interface living in Order

catch-and-ignore for inventory failure

magic retry loops

sleeps/timeouts

hardcoded fallback stock state

duplicate conditional logic copied from Inventory

test-only branches

suppressing architecture tests

If the correct boundary is not possible, STOP and report blocker.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W4/no-workaround-compliance.md

14. Data Safety

ARCH-DATA-001 is ACTIVE.

Prefer no schema migration.

If any schema change is truly required:

additive only

module-owned

no cross-module FK

no destructive backfill

rollback/compatibility documented

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W4/data-safety.md

15. Source-Size / Folder Ownership

ARCH-FOLDER-OWNERSHIP-001 is ACTIVE.

Requirements:

new backend source placed under correct responsibility/capability folders

no project-root dumping

no Common/Misc/Helpers

no new file >800 LOC

existing oversized files shrink-only

Host root structure from W1 remains intact

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W4/source-structure-compliance.md

16. Architecture Guards

Required verification:

Order.Infrastructure → Inventory.Application baseline shrinks if edge fully removed

no baseline widening

no wildcard suppression

App→App does not grow

Infra→foreign App does not grow

Domain→foreign Domain remains clean

Infra→foreign Domain remains clean

frontend freeze guard remains green

Host folder guard remains green

checkout locks ARCH-CHECKOUT-001…005 remain active

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W4/architecture-guards.md

17. Tests

Required:

characterization tests for Cancel

characterization tests for Restore

PaymentBridge-focused tests

Inventory contract adapter tests

duplicate/idempotency tests where applicable

Checkout/Order/Inventory focused suite

ArchitectureBoundaryTests

TMAR foundation tests

durable guard tests

source-size/folder guards

Required:
NEW_FAILURES=0

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W4/tests.md

18. Residual Dependency Audit

After implementation, refresh:

Order.Application foreign Application edges

Order.Infrastructure foreign Application edges

especially Inventory / Cart / Promotion

Classify each residual:

SAFE_CONTRACT_CANDIDATE

COMPENSATION_RELATED

NON_CHECKOUT

TEMPORARY_ACCEPTABLE

NEEDS_DESIGN

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W4/residual-dependencies.md

19. Checkout W5 Readiness

Return exactly:
Checkout-Implementation-W5-Readiness: READY
or
Checkout-Implementation-W5-Readiness: DEFER

READY requires:

selected Order.Infrastructure → Inventory.Application residuals removed or precisely bounded

current checkout behavior preserved

no new dependency debt

next seam is explicit

frontend remains frozen and unaffected

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W4/w5-readiness.md

20. W5 Candidate

Do NOT implement W5 here.

Choose next backend step from evidence, likely:

Promotion contract seam

remaining Cart/Inventory infrastructure contract cleanup

Inbox/idempotent-consumer primitive

preparation for first local transaction separation while Process Manager remains in-process

Return:
W5-Candidate: <name>

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W4/w5-candidate.md

21. Architecture Priority

Frontend is frozen and MUST NOT be selected.

Return exactly one:
Architecture-Priority: CHECKOUT_IMPLEMENTATION
Architecture-Priority: CONTRACTS
Architecture-Priority: HOST_STRUCTURE
Architecture-Priority: BACKEND_GODFILE

Do NOT return any FE_* priority while freeze is active.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W4/architecture-priority.md

22. Next Task Decision

Choose automatically:

A. TB-TMAR-CHECKOUT-IMPL-W5
if W5 readiness = READY and CHECKOUT_IMPLEMENTATION is highest value.

B. TB-TMAR-CONTRACTS-W7
if Contracts prerequisite is highest value.

C. TB-TMAR-HOST-STRUCTURE-W2
only if structural audit reveals significant remaining SAFE move-only backend debt and HOST_STRUCTURE is highest value.

D. TB-TMAR-BACKEND-GODFILE-W1
only if characterization readiness is sufficient and BACKEND_GODFILE is highest value.

Frontend tasks are forbidden.

23. Product Safety

No user-visible behavior change.

Do NOT:

change checkout API

change order semantics

change inventory semantics

activate async Saga

change frontend

remove shared TransactionScope

execute distributed compensation

24. Recovery Updates

Update narrowly:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TOOBA-MICROSERVICE-MIGRATION-NOTES.md

docs/architecture/TMAR-architecture-locks.md only if a durable new backend rule is justified

docs/architecture/TOOBA-CAPABILITY-MAP.md only if ownership facts materially change

docs/evidence/TB-TMAR-CHECKOUT-IMPL-W4/recovery-sot.md

Must preserve:
TMAR-Execution-Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE

25. Acceptance Criteria

PASS only if:

frontend production untouched

exact Order.Infrastructure → Inventory.Application usages audited

safe selected residuals moved to Inventory.Contracts

dependency baseline shrinks if full edge removable

Inventory remains authority

behavior preserved

shared TransactionScope preserved

no async compensation/runtime

no workaround

no baseline widening

source placement guards pass

tests pass with NEW_FAILURES=0

W5 readiness assessed

W5 candidate selected

backend-only Architecture-Priority returned

user work preserved

canonical Result delivered

Worker stops completely

26. Result Contract

Return ONLY canonical BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Recovery-Start
TMAR-Execution-Mode
Frontend-Production-Changes
Order-Infra-Inventory-Usage
Scope-Selection
Inventory-Contract-Extension
Order-Infra-Dependency
Inventory-Adapter
Behavior-Preservation
Transaction-Preservation
Compensation-Scope
Idempotency-Correlation
Layering
No-Workaround-Compliance
Data-Safety
Source-Structure-Compliance
Architecture-Guards
Tests
Residual-Dependencies
Checkout-Implementation-W5-Readiness
W5-Candidate
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
TMAR-Execution-Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Frontend-Production-Changes: NONE
Product-Resume-Safety: SAFE_WITH_TMAR_PARALLEL

After canonical Result:
STOP completely.

Do NOT poll.
Do NOT fetch next task.
Do NOT write Worker IDLE.

END_TOOBA_TASK
