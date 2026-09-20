PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID:
TB-TMAR-BOUNDARY-V1

Parent-Task:
TB-TMAR-HOST-W2

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
TMAR Boundary Verification — Verify Cross-Module Domain/Application Leaks Reported by Independent Repository Review

Task Type:
AUDIT + NARROW ENFORCEMENT ONLY

0. Architect Intent

TB-TMAR-HOST-W2 is accepted.

Product-Resume-Safety remains NOT_YET.

Before continuing deeper Host cleanup, verify the independent repository review findings that appear to conflict with the earlier TMAR baseline.

The independent review cloned the same repository and reported possible cross-module leaks including:

Cart.Domain → Offer.Domain

Order.Domain → Offer.Domain

Pricing.Domain → Offer.Domain

Payment.Infrastructure → Wallet.Domain

Order.Application references multiple foreign Application projects and acts as a synchronous hub

architecture tests may not currently detect some of these relationships

These are high-priority because they directly affect painless future Microservice extraction.

This task must determine the factual truth from the repository and strengthen guards where required.

Do NOT assume the earlier baseline is correct.
Do NOT assume the independent review is correct.
Prove each claim with project references / assembly references / source usage.

1. Recovery / Git Safety

Repository:
D:\Users\User\source\repos\SarvNewVer

Read:
docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

Verify and record:

branch

exact HEAD SHA

exact origin/main SHA

HEAD==origin/main

git status --short

git diff --name-only

git diff --cached --name-only

18ca10c9 ancestor

Expected previous accepted tip:
3cde812947ddaa55c5ae1fbdb1fefce61c8ef8f8

If conflicting tracked user work exists:
STOP with RECOVERY_CONFLICT.

Never use destructive git operations.

Evidence:
docs/evidence/TB-TMAR-BOUNDARY-V1/recovery-start.md

2. Verify Exact Project References

Inspect ALL module .csproj references repository-wide.

Produce a machine-readable dependency graph and a human-readable report.

At minimum classify edges:

Domain → same-module Domain/shared

Domain → foreign Domain

Domain → foreign Application

Domain → Infrastructure

Application → foreign Application

Application → foreign Domain

Application → foreign Infrastructure

Infrastructure → foreign Domain

Infrastructure → foreign Application

Infrastructure → foreign Infrastructure

Host → module Application/Infrastructure

Explicitly verify the reported claims:

Cart.Domain → Offer.Domain

Order.Domain → Offer.Domain

Pricing.Domain → Offer.Domain

Payment.Infrastructure → Wallet.Domain

Order.Application → foreign Application modules

For each claim return:

CONFIRMED / NOT_CONFIRMED

exact .csproj

exact <ProjectReference>

exact referenced project

representative code/type usage if present

architectural impact

Evidence:
docs/evidence/TB-TMAR-BOUNDARY-V1/dependency-verification.md

Machine-readable:
docs/evidence/TB-TMAR-BOUNDARY-V1/dependency-graph.json

3. Verify Source-Level Coupling

Project-reference analysis alone is not enough.

For every CONFIRMED foreign-module edge:

identify the actual foreign types used

classify each as:

domain entity/value object leakage

public contract-like type living in wrong project

technical/shared primitive

persistence-only dependency

application orchestration dependency

integration/event dependency

Explain whether the edge can be removed by:

moving a boundary type to *.Contracts

duplicating/owning a local value object where appropriate

replacing synchronous reference with a Gate/Contract

integration event

anti-corruption adapter

no action because the edge is actually shared BuildingBlocks and not module leakage

Do NOT implement broad fixes yet.

Evidence:
docs/evidence/TB-TMAR-BOUNDARY-V1/source-coupling.md

4. Order.Application Hub Analysis

If Order.Application has many foreign Application references:

Map every foreign dependency and classify why it exists:

Pricing

Inventory

Offer

Payment

Tax

Promotion

Fulfillment

Party/Identity

other

For each:

sync read

sync write/orchestration

validation

transaction participant

event publication

DTO reuse

Determine whether Order.Application is:

legitimate process orchestrator

over-coupled hub

both

Produce a migration recommendation:

Contracts extraction

Saga/process manager

integration events

read gateway

keep synchronous temporarily

Do NOT invent a distributed saga unless current consistency requirements justify it.

Evidence:
docs/evidence/TB-TMAR-BOUNDARY-V1/order-hub-analysis.md

5. Architecture Test Gap Analysis

Compare repository reality against current ArchitectureBoundaryTests.

Explain why any confirmed bad edge was not caught.

Inspect:

project-reference scanning logic

assembly scanning logic

excluded projects

naming assumptions

allowlists/baselines

false negatives

Add ONLY the smallest safe architecture guard improvements needed to make CONFIRMED structural violations visible going forward.

Important:
Existing confirmed legacy violations may be explicitly baselined so CI remains green, but:

baseline must be exact

baseline must be reviewable

no wildcard suppression

new violations must fail

baseline must be designed to shrink

Evidence:
docs/evidence/TB-TMAR-BOUNDARY-V1/architecture-test-gap.md

6. Contracts Extraction Readiness

Do NOT create all *.Contracts projects in this task.

Instead produce an exact first extraction sequence based on verified edges.

For each recommended Contracts project:

module owner

types/interfaces to move first

consuming modules

compatibility adapter needed?

risk

dependency reduction achieved

Prioritize extraction-critical modules first.

Evidence:
docs/evidence/TB-TMAR-BOUNDARY-V1/contracts-sequence.md

7. Product Resume Safety

Re-assess whether feature development may resume in parallel after this verification.

Return exactly:

Product-Resume-Safety: NOT_YET

or

Product-Resume-Safety: SAFE_WITH_TMAR_PARALLEL

SAFE_WITH_TMAR_PARALLEL requires:

critical new-debt guards active

confirmed cross-module leaks are known and frozen from expansion

no unknown critical structural contradiction remains

new feature work can be constrained to CQRS + Contracts + Host freeze rules

If confirmed foreign Domain/Infrastructure leaks exist without guards, return NOT_YET.

8. Decide Next TMAR Task Automatically

Choose exactly one next task recommendation based on evidence:

A. TB-TMAR-CONTRACTS-W1
if cross-module dependency leakage is confirmed and Contracts extraction is now higher priority than another Host slice.

B. TB-TMAR-HOST-W3
if reported boundary leaks are not structurally critical or are already safely guarded, and Host write debt remains the highest immediate risk.

C. TB-TMAR-BOUNDARY-R1
if a narrow structural repair is required before either A or B.

Do NOT ask the user.

9. No Scope Creep

Do NOT:

mass refactor dependency graph

create 31 Contracts projects

move Domain ownership

reorganize folders

rewrite Order workflows

start distributed transactions

install Redis

change product behavior

clean repository history

run git filter-repo

delete evidence/artifacts

10. Tests

Required:

dependency graph verification tests/tool output

ArchitectureBoundaryTests

TMAR foundation tests

any newly added architecture guard tests

No unrelated broad suite unless needed.

Evidence:
docs/evidence/TB-TMAR-BOUNDARY-V1/tests.md

11. Capability Map / Bootstrap / Recovery SoT

Update narrowly:

docs/architecture/TOOBA-CAPABILITY-MAP.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/evidence/TB-TMAR-BOUNDARY-V1/recovery-sot.md

Record:

independent review claims verified or rejected individually

exact confirmed cross-module structural debt

Last Verified Task = TB-TMAR-BOUNDARY-V1

12. Acceptance Criteria

PASS only if:

every reported cross-module claim is explicitly CONFIRMED/NOT_CONFIRMED

exact csproj evidence exists

actual source/type usage is classified

Order.Application hub is mapped

architecture test false negatives are explained

new structural drift is guarded

no wildcard suppressions

exact Contracts extraction sequence is produced

no broad refactor performed

Product-Resume-Safety assessed

next task chosen automatically

user work preserved

canonical Result delivered

Worker stops completely

13. Result Contract

Return ONLY canonical BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Recovery-Start
Dependency-Graph
Claim-Verification
Source-Coupling
Order-Application-Hub
Architecture-Test-Gaps
Architecture-Guards
Contracts-Sequence
Tests
Capability-Map
Bootstrap
Recovery-SoT
Git
Architectural-Concerns
Blockers
Product-Resume-Safety
Next-Recommended-Task

After canonical Result through Bridge:
STOP completely.

Do NOT poll.
Do NOT fetch next task.
Do NOT write Worker IDLE.

END_TOOBA_TASK