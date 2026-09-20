PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID:
TB-TMAR-CONTRACTS-W5

Parent-Task:
TB-TMAR-CONTRACTS-W4

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
TMAR Contracts Wave 5 — Remove Inventory→Offer Application Coupling and One More Low-Risk Order-Hub Edge

Task Type:
IMPLEMENTATION — NARROW BOUNDARY REPAIR

0. Architect Intent

CONTRACTS-W4 is accepted.

Current state:

Product-Resume-Safety = SAFE_WITH_TMAR_PARALLEL

user still wants TMAR to continue

Frontend-Recovery-Readiness = DEFER

App→App baseline reduced from 14 to 12

remaining known Order.Application foreign Application edges include Cart / Inventory / Pricing / Promotion

Inventory.Application → Offer.Application remains

Checkout shared-ACID remains known/frozen and MUST NOT be redesigned here

Primary objectives:

remove Inventory.Application → Offer.Application

remove exactly ONE additional low-risk Order.Application → foreign Application edge

shrink App→App baseline again

preserve all checkout/transaction/business behavior

reassess whether backend structural cleanup is mature enough to begin Frontend baseline next

1. Recovery / Git Safety

Repository:
D:\Users\User\source\repos\SarvNewVer

Read:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TOOBA-MICROSERVICE-MIGRATION-NOTES.md

docs/architecture/TMAR-architecture-locks.md

docs/architecture/TOOBA-CAPABILITY-MAP.md

docs/evidence/TB-TMAR-CONTRACTS-W4/recovery-sot.md

docs/evidence/TB-TMAR-BOUNDARY-V1/order-hub-analysis.md

docs/evidence/TB-TMAR-BOUNDARY-V1/contracts-sequence.md

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
b0a2c54d89e2213dd5440d43cdaef0c6556009db

If tracked user work conflicts:
STOP with RECOVERY_CONFLICT.

No destructive Git operations.

Evidence:
docs/evidence/TB-TMAR-CONTRACTS-W5/recovery-start.md

2. Remove Inventory.Application → Offer.Application

Inspect the exact ProjectReference and consumed Offer.Application types.

Prefer reuse of the already-created Tooba.Offer.Contracts boundary if it contains the required stable contracts.

Target:
Inventory.Application → Tooba.Offer.Contracts

Not:
Inventory.Application → Tooba.Offer.Application

Rules:

no Offer Domain entity leakage

no internal Offer.Application implementation types

do not duplicate equivalent contracts

preserve inventory behavior exactly

do not redesign reservation/stock semantics

After migration:

remove Inventory.Application → Offer.Application ProjectReference if no longer needed

shrink exact App→App baseline

no replacement bad dependency

Evidence:
docs/evidence/TB-TMAR-CONTRACTS-W5/inventory-offer-contract.md

3. Select ONE Additional Order.Application Edge

Remaining known candidates include:

Cart

Inventory

Pricing

Promotion

Select exactly ONE based on evidence.

Selection priority:

read/authority contract

stable data shape

no write-side distributed consistency implication

high extraction value

minimal regression radius

Preferred if evidence supports:
Pricing first, then Promotion.
Avoid Cart/Inventory write-sensitive orchestration unless the exact edge is read-only and demonstrably safe.

Before implementation document:

exact ProjectReference

exact types/interfaces

call semantics

sync read/write/orchestration classification

target Contracts project

transaction relevance

expected baseline shrink

Evidence:
docs/evidence/TB-TMAR-CONTRACTS-W5/order-slice-selection.md

4. Extract Minimal Contract

Create or expand exactly the selected module's Tooba.<Module>.Contracts.

Rules:

stable public boundary only

no EF

no Host

no Infrastructure

no Domain entity leakage

no internal Application implementation types

no broad copy of Application API

compatible with future in-process or HTTP/gRPC adapter

If selected module already has Contracts, extend it minimally.

Evidence:
docs/evidence/TB-TMAR-CONTRACTS-W5/order-contract-extraction.md

5. Preserve Checkout / Shared-ACID Semantics

Do NOT modify:

CheckoutDirectory TransactionScope

reservation/order/cart atomicity

isolation level

retry semantics

compensation behavior

order lifecycle behavior

If selected edge participates in checkout:
change compile-time boundary only.

No:

Saga

Process Manager

distributed transaction

event choreography migration

6. Architecture Baseline Shrink

Expected:

remove Inventory.Application → Offer.Application

remove one selected Order.Application → foreign Application edge

No new:

App→App

Infra→foreign Application

Domain→foreign Domain

Infra→foreign Domain

Verify:

source-size baseline unchanged or shrinks

cross-context transaction baseline unchanged

Host write baseline does not grow

No wildcard allowances.

Evidence:
docs/evidence/TB-TMAR-CONTRACTS-W5/architecture-guards.md

7. Contracts Guard

Keep generic Contracts cleanliness enforcement active across ALL *.Contracts projects.

Verify Contracts do NOT reference:

Infrastructure

Host

foreign Application implementation assemblies

EF packages

Domain entities unless explicitly approved and justified

Do not weaken current guards.

Evidence:
docs/evidence/TB-TMAR-CONTRACTS-W5/contracts-guard.md

8. CQRS / Error / Locale / Time / ID

All NEW code must obey TMAR:

MediatR 12.5.0 for new use-cases

business handlers in Application

FluentValidation where applicable

IClock

IIdGenerator

semantic errors

no hardcoded localized user-facing Domain messages in ANY language

unlimited-locale safe

Do not mass-migrate unrelated legacy code.

9. Source-Size / God-File Safety

No new hand-written source >800 LOC.
Existing oversized legacy files may not grow above baseline.

If touching an oversized file:

keep diff minimal

no structural decomposition here

characterization tests before future decomposition

Evidence:
docs/evidence/TB-TMAR-CONTRACTS-W5/source-size-compliance.md

10. Tests

Required:

Inventory/Offer focused behavior tests

Offer.Contracts compatibility tests as needed

selected Order edge characterization test before move if coverage is insufficient

affected Order tests

affected provider-module tests

ArchitectureBoundaryTests

TMAR foundation tests

Contracts cleanliness guard

source-size guard

transaction guard

Do not create all-module test projects.

Evidence:
docs/evidence/TB-TMAR-CONTRACTS-W5/tests.md

11. Frontend Recovery Readiness Reassessment

Do NOT modify frontend code.

Reassess whether backend critical boundary stabilization is now sufficient to begin a dedicated frontend baseline task in parallel.

Known frontend concerns to preserve for future FE baseline:

flat app/admin structure

oversized TS/TSX components

high use client count

manual test discovery

overlapping editor/carousel libraries

storefront SSR/SEO server/client boundary

data-fetching layer should be audited, NOT assumed to require TanStack Query/SWR

Return exactly:
Frontend-Recovery-Readiness: READY
or
Frontend-Recovery-Readiness: DEFER

If READY, next task MAY be TB-TMAR-FE-BASELINE.

Evidence:
docs/evidence/TB-TMAR-CONTRACTS-W5/frontend-readiness.md

12. Product Resume Safety

Expected:
Product-Resume-Safety: SAFE_WITH_TMAR_PARALLEL

Downgrade only if NEW critical evidence appears.

User has not yet returned to product feature work, so TMAR continues.

13. Next Task Decision

Choose automatically:

A. TB-TMAR-CONTRACTS-W6
if another low-risk/high-value contract extraction is clearly best.

B. TB-TMAR-HOST-W3
if Host write debt is now higher-value.

C. TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN
if contract boundaries are mature enough and formal consistency design should come next.

D. TB-TMAR-FE-BASELINE
if frontend recovery is READY and can safely proceed in parallel.

Do not ask the user.

14. Capability Map / Bootstrap / Master Recovery / Recovery SoT

Update narrowly:

docs/architecture/TOOBA-CAPABILITY-MAP.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-MICROSERVICE-MIGRATION-NOTES.md

docs/evidence/TB-TMAR-CONTRACTS-W5/recovery-sot.md

Record:

W5 dependency reductions

current Product-Resume-Safety

Frontend-Recovery-Readiness

next recommended task

Last Verified Task = TB-TMAR-CONTRACTS-W5

15. No Scope Creep

Do NOT:

redesign Checkout

implement Saga

migrate multiple Order edges

create Contracts for every module

move Domain ownership

reorganize backend folders

split giant files

modify frontend

install Redis

rewrite Git history

change product behavior

16. Acceptance Criteria

PASS only if:

Inventory.Application → Offer.Application removed

exactly one additional Order.Application → foreign Application edge removed

no replacement bad dependency introduced

Contracts remain minimal and clean

App→App baseline shrinks

transaction semantics unchanged

source-size guards remain green

focused tests pass

Frontend-Recovery-Readiness assessed

Product-Resume-Safety assessed

user work preserved

next task chosen automatically

canonical Result delivered

Worker stops completely

17. Result Contract

Return ONLY canonical BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Recovery-Start
Inventory-Offer-Contract
Order-Slice-Selection
Order-Contract-Extraction
Dependency-Changes
Transaction-Semantics
Architecture-Guards
Contracts-Guard
Characterization-Tests
Tests
Source-Size-Compliance
Frontend-Recovery-Readiness
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