PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID:
TB-TMAR-CHECKOUT-IMPL-W5

Parent-Task:
TB-TMAR-CHECKOUT-IMPL-W4

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
TMAR Checkout Implementation Wave 5 — Extract Promotion Boundary and Continue Order Hub Decoupling

Task Type:
IMPLEMENTATION — BACKEND-ONLY CONTRACT EXTRACTION / BEHAVIOR-PRESERVING

Backend-Only:
YES

TMAR-Execution-Mode:
BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE

0. Architect Intent

TB-TMAR-CHECKOUT-IMPL-W4 is accepted.

Verified state:

Order.Infrastructure → Inventory.Application removed for selected Cancel/Restore/PaymentBridge lifecycle interactions

Inventory lifecycle seam now goes through Tooba.Inventory.Contracts

shared TransactionScope remains intact

no async Saga/compensation runtime

frontend production unchanged

Checkout-Implementation-W5-Readiness = READY

W5 candidate = Promotion contract seam

Architecture-Priority = CHECKOUT_IMPLEMENTATION

Product-Resume-Safety = SAFE_WITH_TMAR_PARALLEL

Known residuals:

Order.Application → Promotion.Application

Order.Infrastructure → Cart.Application for ICartDirectory / conversion adapter fallback

Order.Infrastructure → Catalog.Application

Order.Infrastructure → Payment.Application

Primary objectives:

remove checkout/order-critical Order.Application → Promotion.Application coupling behind Promotion.Contracts

preserve Promotion as business authority

preserve checkout behavior and shared TransactionScope

do not broaden into unrelated Promotion workflows

audit residual Order.Infrastructure foreign Application edges for next-wave prioritization

keep frontend frozen

no async Saga/compensation

1. Recovery / Git Safety

Repository:
D:\Users\User\source\repos\SarvNewVer

Expected previous accepted tip:
0440354ad64dbe7a4d4c3b21a780789986a24627

Read:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TOOBA-MICROSERVICE-MIGRATION-NOTES.md

docs/architecture/TMAR-architecture-locks.md

docs/architecture/TOOBA-CAPABILITY-MAP.md

docs/evidence/TB-TMAR-CHECKOUT-IMPL-W4/recovery-sot.md

docs/evidence/TB-TMAR-CHECKOUT-IMPL-W4/residual-dependencies.md

docs/evidence/TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN/order-hub-decomposition-plan.md

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

Never use destructive Git operations.
Never use broad git add ..

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W5/recovery-start.md

2. Frontend Freeze Enforcement

ARCH-FE-FREEZE-001 remains ACTIVE.

No production modifications under:
src/frontend/**

Required:
Frontend-Production-Changes: NONE

3. Reconstruct Exact Promotion Usage from Order.Application

Inspect all current Order.Application → Promotion.Application usages.

For each:

source file/type

exact Promotion.Application type/method

checkout/order lifecycle role

read vs write

transaction participation

failure semantics

idempotency assumptions

whether result is authoritative

whether it affects eligibility, reservation, consumption, release, or validation

Classify:

CHECKOUT_VALIDATION

PROMOTION_CONSUMPTION

PROMOTION_RELEASE

READ_ONLY

NON_CHECKOUT

NEEDS_PROCESS_DESIGN

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W5/promotion-usage.md

4. Select Exact Promotion Contract Seam

Migrate only the coherent Promotion interactions required by checkout/order coordination.

Potential conceptual capabilities, derive from code:

validate promotion/campaign applicability

reserve/consume promotion usage

release/revert usage if current synchronous flow already supports it

return canonical promotion decision/reference

Do not create a broad Promotion facade.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W5/promotion-scope.md

5. Create/Extend Promotion.Contracts

Target:
Tooba.Promotion.Contracts

Requirements:

no Promotion Domain entity leakage

no Promotion Application implementation types

no DbContext/repository leakage

stable request/result contracts

correlation/idempotency where semantically required

future remote-adapter friendly

Promotion remains authority for eligibility/consumption rules

Possible conceptual boundary:

ICheckoutPromotionPort
or evidence-backed equivalent

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W5/promotion-contract.md

6. Remove Order.Application → Promotion.Application If Safe

Preferred:
Order.Application → Promotion.Contracts

If all relevant usages can safely migrate:

remove ProjectReference

shrink App→App baseline exactly

If non-checkout usages remain:

keep only those exact residuals

document them precisely

do not fake full removal

baseline must not widen

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W5/order-promotion-dependency.md

7. Promotion-Owned Adapter / Implementation

Promotion implementation remains Promotion-owned.

Allowed:

adapter in Promotion.Application or Infrastructure according to existing ownership pattern

Not allowed:

Order reimplementing promotion eligibility/consumption

Order writing Promotion tables

shared repository crossing contexts

foreign DbContext access

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W5/promotion-adapter.md

8. Integrate Into Checkout Process Manager

If promotion interaction is part of current checkout orchestration:

Process Manager invokes Promotion.Contracts

preserve current order of operations

preserve process state semantics

preserve correlation/idempotency

do not duplicate business decisions in Process Manager

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W5/process-manager-promotion.md

9. Shared TransactionScope Preservation

Current shared TransactionScope MUST remain intact.

Do NOT:

split participant commits

activate independent Promotion transaction semantics that weaken current atomic behavior

remove TransactionScope

change rollback ordering

If Promotion currently participates outside shared TX, preserve that exact behavior.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W5/transaction-preservation.md

10. Failure / Rollback Behavior

Characterize and preserve:

invalid promotion

promotion consumption failure

duplicate checkout

Order failure after promotion interaction

Cart/Inventory failure after promotion interaction

process-state persistence failure

Do NOT activate async compensation runtime.

Current synchronous behavior remains authoritative.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W5/failure-behavior.md

11. Idempotency / Correlation

Promotion contract must preserve or explicitly define:

ProcessId

submission/idempotency identity

promotion/campaign reference

duplicate consume behavior

retry-safe semantics

No cache-only dedupe.
No in-memory authority.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W5/idempotency-correlation.md

12. No-Workaround / Baseline Integrity

ARCH-NOWORKAROUND-001 and ARCH-BASELINE-001 are ACTIVE.

Reject:

fake pass-through interface in Order

reflection/service locator

catch-and-ignore

magic retries/sleeps

copied Promotion business rules

test-only branches

baseline widening

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W5/guard-compliance.md

13. Data Safety / Source Placement

ARCH-DATA-001 and ARCH-FOLDER-OWNERSHIP-001 remain ACTIVE.

Prefer no schema change.

Any new source:

correct backend responsibility folder

no root dumping

no Common/Misc/Helpers

no new file >800 LOC

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W5/source-data-compliance.md

14. Residual Order.Infrastructure Foreign-App Audit

Audit current residual edges after W5:

Order.Infrastructure → Cart.Application

Order.Infrastructure → Catalog.Application

Order.Infrastructure → Payment.Application

any others

For each classify:

SAFE_CONTRACT_CANDIDATE

CHECKOUT_CRITICAL

ORDER_LIFECYCLE

ADAPTER_FALLBACK_DEBT

TEMPORARY_ACCEPTABLE

NEEDS_DESIGN

Do NOT automatically refactor them in W5.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W5/order-infra-residuals.md

15. Architecture Guards

Verify:

App→App baseline shrinks if Order→Promotion.Application fully removed

Infra→foreign App does not grow

Domain→foreign Domain remains clean

Infra→foreign Domain remains clean

checkout locks remain active

frontend freeze guard green

folder guards green

source-size guard green

no baseline widening

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W5/architecture-guards.md

16. Tests

Required:

promotion contract characterization

checkout with valid promotion

invalid promotion path

duplicate/idempotency path

rollback/failure path

Checkout/Order/Promotion focused suite

ArchitectureBoundaryTests

TMAR foundation tests

durable guard tests

source-size/folder tests

Required:
NEW_FAILURES=0

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W5/tests.md

17. Checkout W6 Readiness

Return:
Checkout-Implementation-W6-Readiness: READY
or
Checkout-Implementation-W6-Readiness: DEFER

READY requires:

Promotion seam stable

no new dependency debt

shared TransactionScope behavior preserved

next residual seam explicit

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W5/w6-readiness.md

18. W6 Candidate

Do NOT implement W6 here.

Choose highest-value backend candidate from evidence, likely one of:

Order.Infrastructure → Cart.Contracts cleanup

Order.Infrastructure → Payment.Contracts cleanup

Order.Infrastructure → Catalog.Contracts cleanup

Inbox/idempotent-consumer primitive

first local-transaction separation preparation

Return:
W6-Candidate: <name>

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W5/w6-candidate.md

19. Architecture Priority

Frontend is frozen.

Return exactly one:
Architecture-Priority: CHECKOUT_IMPLEMENTATION
Architecture-Priority: CONTRACTS
Architecture-Priority: HOST_STRUCTURE
Architecture-Priority: BACKEND_GODFILE

Do not return FE_*.

Evidence:
docs/evidence/TB-TMAR-CHECKOUT-IMPL-W5/architecture-priority.md

20. Next Task Decision

Choose automatically:

A. TB-TMAR-CHECKOUT-IMPL-W6
if W6 readiness = READY and CHECKOUT_IMPLEMENTATION is highest value.

B. TB-TMAR-CONTRACTS-W7
if Contracts prerequisite is highest value.

C. TB-TMAR-HOST-STRUCTURE-W2
only if safe structural backend debt clearly outranks checkout.

D. TB-TMAR-BACKEND-GODFILE-W1
only if characterization readiness is sufficient and backend godfile risk is highest.

Frontend tasks forbidden.

21. Recovery Updates

Update narrowly:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TOOBA-MICROSERVICE-MIGRATION-NOTES.md

docs/architecture/TMAR-architecture-locks.md only if a durable new backend rule is justified

docs/architecture/TOOBA-CAPABILITY-MAP.md only if ownership facts materially change

docs/evidence/TB-TMAR-CHECKOUT-IMPL-W5/recovery-sot.md

Must preserve:
TMAR-Execution-Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE

22. Acceptance Criteria

PASS only if:

frontend production untouched

exact Promotion usages audited

coherent Promotion contract seam extracted

Order.Application→Promotion.Application removed if safely possible

Promotion remains authority

checkout behavior preserved

shared TransactionScope preserved

no async Saga/compensation

idempotency/correlation preserved

no workaround

no baseline widening

residual Order.Infrastructure edges audited

tests pass with NEW_FAILURES=0

W6 readiness assessed

W6 candidate selected

backend-only priority returned

user work preserved

canonical Result delivered

Worker stops completely

23. Result Contract

Return ONLY canonical BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Recovery-Start
TMAR-Execution-Mode
Frontend-Production-Changes
Promotion-Usage
Promotion-Scope
Promotion-Contract
Order-Promotion-Dependency
Promotion-Adapter
Process-Manager-Promotion
Transaction-Preservation
Failure-Behavior
Idempotency-Correlation
Guard-Compliance
Source-Data-Compliance
Order-Infra-Residuals
Architecture-Guards
Tests
Checkout-Implementation-W6-Readiness
W6-Candidate
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
