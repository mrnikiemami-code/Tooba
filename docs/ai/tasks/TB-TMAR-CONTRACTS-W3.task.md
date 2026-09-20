PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID:
TB-TMAR-CONTRACTS-W3

Parent-Task:
TB-TMAR-CONTRACTS-W2

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
TMAR Contracts Wave 3 — Remove Returns→Wallet Application Coupling and One High-Value Order-Hub Dependency

Task Type:
IMPLEMENTATION — NARROW BOUNDARY REPAIR

0. Architect Intent

CONTRACTS-W2 is accepted.

Current state:

Product-Resume-Safety = SAFE_WITH_TMAR_PARALLEL

user has explicitly asked TMAR to continue for now

Domain→foreign Domain baseline is empty

Infra→foreign Domain baseline is empty

Host write debt is frozen and shrinking

App→App and Infra→foreign Application debt are frozen

cross-context shared-ACID growth is frozen by ARCH-TX-001

Checkout shared-ACID debt remains known and deferred to design-first work

This task continues reducing extraction-critical coupling.

Primary objectives:

persist the Master Recovery file inside the repository

remove Returns.Infrastructure → Wallet.Application

remove exactly ONE additional high-value foreign Application dependency, preferably from the Order.Application hub, through a proper Contracts boundary

shrink exact dependency baselines

preserve current business/transaction behavior

do NOT redesign checkout consistency yet

1. Recovery / Git Safety

Repository:
D:\Users\User\source\repos\SarvNewVer

External durable master recovery:
D:\Users\User\source\repos\SarvNewVerRequirment\reference\TOOBA-TMAR-MASTER-RECOVERY.md

Read:

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TOOBA-MICROSERVICE-MIGRATION-NOTES.md

docs/architecture/TMAR-architecture-locks.md

docs/architecture/TOOBA-CAPABILITY-MAP.md

docs/evidence/TB-TMAR-CONTRACTS-W2/recovery-sot.md

docs/evidence/TB-TMAR-BOUNDARY-V1/contracts-sequence.md

docs/evidence/TB-TMAR-BOUNDARY-V1/order-hub-analysis.md

Verify:

branch main

exact HEAD SHA

exact origin/main SHA

HEAD==origin/main

git status --short

git diff --name-only

git diff --cached --name-only

18ca10c9 ancestor

user work preserved

Expected previous accepted tip:
b2742d3b62c3db8c06e41a975df569b12745b0db

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
docs/evidence/TB-TMAR-CONTRACTS-W3/recovery-start.md

2. Persist TMAR Master Recovery

Copy:

D:\Users\User\source\repos\SarvNewVerRequirment\reference\TOOBA-TMAR-MASTER-RECOVERY.md

to:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

Do not weaken or rewrite its architectural intent.

Update only factual current-state fields if repository evidence is newer:

CONTRACTS-W2 accepted

current task = TB-TMAR-CONTRACTS-W3

Product-Resume-Safety = SAFE_WITH_TMAR_PARALLEL

primary objective remains painless incremental Microservice migration

This file becomes the first recovery entry point.

Evidence:
docs/evidence/TB-TMAR-CONTRACTS-W3/master-recovery.md

3. Remove Returns.Infrastructure → Wallet.Application

BOUNDARY/W2 evidence reports:
Returns.Infrastructure → Wallet.Application

Inspect exact ProjectReference and actual consumed types.

Goal:
migrate that dependency to Tooba.Wallet.Contracts.

Rules:

no Wallet Domain entities exposed

no Wallet Application implementation types exposed

no Wallet Infrastructure leakage

extend Wallet.Contracts only with stable public boundary interfaces/DTOs/value contracts actually required by Returns

preserve current behavior and error semantics

After migration:

remove Returns.Infrastructure → Wallet.Application ProjectReference if no longer required

shrink exact tmar-infra-to-foreign-application.json

no replacement foreign Application or Domain dependency

Evidence:
docs/evidence/TB-TMAR-CONTRACTS-W3/returns-wallet-contract.md

4. Select ONE Order-Hub Dependency for Contract Extraction

Order.Application is known to be both:

a legitimate checkout orchestrator

an over-coupled synchronous hub

Do NOT redesign the hub in this task.

Select exactly ONE foreign Application dependency currently used by Order.Application.

Selection priority:

stable read/query or authority contract

high extraction value

low consistency risk

no need for Saga/process redesign

no widening of transaction semantics

no broad multi-module rewrite

Likely candidates may include:

Pricing

Tax

Offer

Inventory

Promotion

Cart

But choose from actual repository evidence, not this list.

Before implementation write:

current edge

exact interface/types consumed

whether usage is read/authority/write/orchestration

why safe to extract now

target Tooba.<Module>.Contracts

expected baseline shrink

consistency caveat

Evidence:
docs/evidence/TB-TMAR-CONTRACTS-W3/order-slice-selection.md

5. Extract Only the Selected Public Contract

Create or expand the selected module's *.Contracts project.

Contracts project rules:

stable public module boundary only

no EF

no Host

no Infrastructure

no Domain entity leakage

no internal Application implementation types

no convenience dumping of unrelated interfaces

suitable for future in-process OR HTTP/gRPC adapter

Move/forward only the minimal boundary types necessary.

Compatibility:

preserve public behavior

use temporary type forwarding only if genuinely required and explicitly documented

do not retain misleading namespace ownership indefinitely without a follow-up note

Evidence:
docs/evidence/TB-TMAR-CONTRACTS-W3/order-contract-extraction.md

6. Dependency Direction

After this task:

Preferred:
Order.Application → <SelectedModule>.Contracts

Not:
Order.Application → <SelectedModule>.Application

For Returns:
Returns.Infrastructure → Wallet.Contracts

Not:
Returns.Infrastructure → Wallet.Application/Domain

No new:

Domain→foreign Domain

Infra→foreign Domain

App→new foreign Application

Infra→new foreign Application

7. Checkout / Transaction Semantics

This task MUST NOT change:

CheckoutDirectory shared-ACID behavior

reservation/order/cart atomicity

transaction scope

transaction isolation

retry/compensation semantics

If selected Order dependency participates in the existing Checkout transaction:

extract interface/type boundary only

preserve invocation semantics

document the future microservice consistency concern

Do NOT implement:

Saga

process manager

choreography redesign

distributed transaction

compensation

8. Microservice Migration Locks

Read and preserve:
docs/architecture/TOOBA-MICROSERVICE-MIGRATION-NOTES.md

Ensure these remain canonical:

no full rewrite

contracts/extract approach

Checkout is a future consistency redesign hotspot

no NEW cross-bounded-context shared ACID workflow

peripheral modules should be extracted before Checkout when actual service extraction begins

Do not physically extract a service in this task.

9. CQRS / Error / Locale / Time / ID

All NEW code must follow TMAR:

MediatR 12.5.0 for new application use-cases

handlers in Application

FluentValidation where applicable

IClock

IIdGenerator

semantic error codes

no hardcoded user-facing localized Domain messages in ANY language

unlimited-locale safe

Do not mass-migrate legacy code outside this slice.

10. Source-Size / God-File Safety

No new hand-written file >800 LOC.

No oversized legacy file may grow beyond baseline.

If a touched file is oversized:

keep the diff minimal

do not refactor it structurally in this task

characterization tests required before any future decomposition

Evidence:
docs/evidence/TB-TMAR-CONTRACTS-W3/source-size-compliance.md

11. Architecture Baselines Must Shrink

At minimum expected:

remove Returns.Infrastructure → Wallet.Application from Infra→foreign Application baseline

remove selected Order.Application → foreign Application edge from App→App baseline

No wildcard allowances.

Also verify:

Domain→foreign Domain remains empty

Infra→foreign Domain remains empty

source-size baseline does not increase

cross-context transaction baseline does not increase

Evidence:
docs/evidence/TB-TMAR-CONTRACTS-W3/architecture-guards.md

12. Tests

Required:

Returns/Wallet:

focused behavior/characterization tests around the moved contract

affected Returns tests

affected Wallet tests

Selected Order dependency:

characterization tests for current interaction before contract move if coverage is insufficient

affected Order tests

affected provider-module tests

Architecture:

Contracts cleanliness tests

ArchitectureBoundaryTests

TMAR foundation tests

source-size guards

transaction guard tests

Do not create unit-test projects for every module.

Evidence:
docs/evidence/TB-TMAR-CONTRACTS-W3/tests.md

13. Product Resume Safety

Expected:
Product-Resume-Safety: SAFE_WITH_TMAR_PARALLEL

Downgrade to NOT_YET only if this task discovers a new critical unknown risk that invalidates safe parallel feature work.

If downgraded, state exact new evidence.

14. Next Task Decision

Choose automatically after PASS:

A. TB-TMAR-CONTRACTS-W4
if another low-risk, high-value App→App/Infra→App contract extraction remains best.

B. TB-TMAR-HOST-W3
if Host write debt is now the best next slice.

C. TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN
if enough contracts are stabilized and the next architectural value is formalizing checkout consistency.

D. TB-TMAR-FE-BASELINE
if backend critical structural debt is sufficiently controlled and frontend architecture recovery can safely begin in parallel.

Do not ask the user.

15. Capability Map / Bootstrap / Master Recovery / Recovery SoT

Update narrowly:

docs/architecture/TOOBA-CAPABILITY-MAP.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-MICROSERVICE-MIGRATION-NOTES.md

docs/evidence/TB-TMAR-CONTRACTS-W3/recovery-sot.md

Record:

CONTRACTS-W3 changes

removed dependency edges

current Product-Resume-Safety

Last Verified Task = TB-TMAR-CONTRACTS-W3

16. No Scope Creep

Do NOT:

redesign Checkout

implement Saga

migrate multiple Order dependencies at once

create Contracts for all modules

move Domain ownership

reorganize backend folders

refactor giant files

install Redis

rewrite Git history

alter frontend in this task

change product behavior

17. Acceptance Criteria

PASS only if:

Master Recovery copied into repository

Returns.Infrastructure→Wallet.Application removed

one Order.Application foreign Application edge replaced by Contracts

no replacement bad edge introduced

baselines shrink exactly

Contracts remain clean

transaction behavior unchanged

source-size guards remain green

focused tests pass

user work preserved

Product-Resume-Safety assessed

next task chosen automatically

canonical Result delivered

Worker stops completely

18. Result Contract

Return ONLY canonical BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Recovery-Start
Master-Recovery
Returns-Wallet-Contract
Order-Slice-Selection
Order-Contract-Extraction
Dependency-Changes
Transaction-Semantics
Architecture-Guards
Characterization-Tests
Tests
Source-Size-Compliance
Capability-Map
Bootstrap
Master-Recovery-State
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