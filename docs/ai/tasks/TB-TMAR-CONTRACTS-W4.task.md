PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID:
TB-TMAR-CONTRACTS-W4

Parent-Task:
TB-TMAR-CONTRACTS-W3

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
TMAR Contracts Wave 4 — Remove Cart→Offer Application Coupling and One Additional Order-Hub Application Edge

Task Type:
IMPLEMENTATION — NARROW BOUNDARY REPAIR

0. Architect Intent

CONTRACTS-W3 is accepted.

Current state:

Product-Resume-Safety = SAFE_WITH_TMAR_PARALLEL

user has asked TMAR to continue until they explicitly return to product feature work

Master Recovery is now inside the repository

Domain→foreign Domain = clean for confirmed debt

Infra→foreign Domain = clean for confirmed debt

App→App baseline reduced from 15 to 14

Infra→foreign Application baseline reduced from 30 to 29

Checkout shared-ACID remains known, frozen, and intentionally not redesigned yet

God-file growth is frozen

Host write debt is frozen and partially reduced

Primary objectives:

remove Cart.Application → Offer.Application

remove exactly ONE additional Order.Application → foreign Application dependency using a proper Contracts boundary

shrink exact dependency baselines

preserve checkout and transaction semantics

keep TMAR safe for parallel product work

decide whether next TMAR value is another Contracts wave, Host W3, Checkout consistency design, or Frontend baseline

No Big Bang extraction.

1. Recovery / Git Safety

Repository:
D:\Users\User\source\repos\SarvNewVer

Read durable recovery sources:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TOOBA-MICROSERVICE-MIGRATION-NOTES.md

docs/architecture/TMAR-architecture-locks.md

docs/architecture/TOOBA-CAPABILITY-MAP.md

docs/evidence/TB-TMAR-CONTRACTS-W3/recovery-sot.md

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
b04b9675f3c6f4d029c0b099b3db1d98d59a168a

If conflicting tracked user work exists:
STOP with RECOVERY_CONFLICT.

No destructive Git operations.

Evidence:
docs/evidence/TB-TMAR-CONTRACTS-W4/recovery-start.md

2. Remove Cart.Application → Offer.Application

This edge is explicitly known to remain.

Inspect:

exact ProjectReference

exact Offer.Application interfaces/types consumed by Cart.Application

whether equivalent or near-equivalent types now exist in Tooba.Offer.Contracts

Target:
Cart.Application → Tooba.Offer.Contracts

Not:
Cart.Application → Tooba.Offer.Application

Rules:

move/forward only stable boundary types

no Offer Domain entity leakage

no Offer internal Application implementation leakage

preserve all cart behavior

preserve pricing/inventory/offer semantics

do not redesign Cart orchestration

After migration:

remove Cart.Application → Offer.Application ProjectReference if no longer required

shrink exact App→App baseline

no replacement bad edge

Evidence:
docs/evidence/TB-TMAR-CONTRACTS-W4/cart-offer-contract.md

3. Select ONE Additional Order.Application Dependency

Remaining known Order.Application foreign Application edges include:

Cart

Inventory

Pricing

Promotion

Tax
(and any repository-current edge after W3)

Select exactly ONE by evidence.

Selection priority:

stable read/authority contract

smallest semantic surface

high extraction value

no need for distributed consistency redesign

no change to checkout atomicity

no broad multi-module refactor

Preferred if evidence supports:
Pricing or Tax read/authority contract before Cart/Inventory write-sensitive edges.

Do NOT choose merely because it is easy.

Before implementation record:

exact current edge

exact interface/types

read/write/orchestration classification

target Contracts module

expected baseline reduction

why this edge is safest/highest-value now

transaction/consistency caveat

Evidence:
docs/evidence/TB-TMAR-CONTRACTS-W4/order-slice-selection.md

4. Extract Minimal Public Contract

Create or expand exactly the selected module's Tooba.<Module>.Contracts.

Contracts rules:

stable public boundary only

no EF

no Host

no Infrastructure

no Domain entity leakage

no internal Application implementation types

no convenience dumping

suitable for future in-process and HTTP/gRPC adapter implementations

If a shared value is genuinely domain-neutral, BuildingBlocks may be considered only with explicit justification.
Do not move business-specific semantics to BuildingBlocks for convenience.

Evidence:
docs/evidence/TB-TMAR-CONTRACTS-W4/order-contract-extraction.md

5. Preserve Transaction / Checkout Semantics

This task MUST NOT change:

CheckoutDirectory TransactionScope

reservation/order/cart atomicity

isolation behavior

retry behavior

compensation semantics

order lifecycle states

If the selected Order dependency participates in checkout:

change only the compile-time module boundary

preserve invocation order and sync semantics

document future extraction risk

Do NOT implement:

Saga

Process Manager

distributed transaction

event choreography conversion

compensation workflow

6. Architecture Baseline Shrink

Expected:

App→App baseline removes Cart.Application → Offer.Application

App→App baseline removes one selected Order.Application edge

No new:

App→App

Infra→foreign Application

Domain→foreign Domain

Infra→foreign Domain

Verify:

cross-context transaction baseline does not increase

source-size baseline does not increase

Host write baseline does not increase

No wildcard allowances.

Evidence:
docs/evidence/TB-TMAR-CONTRACTS-W4/architecture-guards.md

7. Architecture Guard Quality

Ensure Contracts project cleanliness is enforced generically:

*.Contracts must NOT reference:

Host

Infrastructure

module Domain entities unless explicitly architecture-approved and justified

foreign Application implementation assemblies

EF packages

If current guard is too narrow for Offer/Wallet only, generalize safely across all current *.Contracts projects.

Do not create false-positive rules that block legitimate BuildingBlocks primitives.

Evidence:
docs/evidence/TB-TMAR-CONTRACTS-W4/contracts-guard.md

8. CQRS / Error / Locale / Time / ID

All NEW code must obey:

MediatR 12.5.0 for new application use-cases

handlers in Application

FluentValidation where applicable

IClock

IIdGenerator

semantic error codes

no hardcoded localized user-facing Domain messages in ANY language

unlimited locale support

no FA/EN-only assumptions

Do not mass-migrate unrelated legacy code.

9. God-File / Source-Size Safety

No new hand-written source file >800 LOC.

Existing oversized legacy files may not grow above baseline.

If touching an oversized file:

keep diff narrow

no structural decomposition here

characterization tests required before future splitting

Evidence:
docs/evidence/TB-TMAR-CONTRACTS-W4/source-size-compliance.md

10. Tests

Cart/Offer:

focused Cart behavior tests for moved Offer contract usage

Offer contract compatibility tests if needed

Selected Order dependency:

characterization test BEFORE contract move if current coverage is insufficient

affected Order tests

affected provider-module tests

Architecture:

Contracts cleanliness

ArchitectureBoundaryTests

TMAR foundation tests

source-size guard

cross-context transaction guard

Do not create broad all-module test projects.

Evidence:
docs/evidence/TB-TMAR-CONTRACTS-W4/tests.md

11. Frontend Recovery Readiness Assessment

Do NOT change frontend code in this task.

Assess whether backend structural recovery is now stable enough to start a parallel frontend architecture baseline next.

Use current known frontend concerns:

flat app/admin area

oversized TS/TSX components

large use client surface

manual test-file discovery

duplicate/overlapping libraries

storefront SSR/SEO boundaries

no assumption that TanStack Query/SWR is automatically required

Return:
Frontend-Recovery-Readiness: READY
or
Frontend-Recovery-Readiness: DEFER

If READY, recommend:
TB-TMAR-FE-BASELINE

Evidence:
docs/evidence/TB-TMAR-CONTRACTS-W4/frontend-readiness.md

12. Product Resume Safety

Expected:
Product-Resume-Safety: SAFE_WITH_TMAR_PARALLEL

Downgrade only if NEW critical evidence is discovered.

User has not yet asked to return to feature work, so continue TMAR regardless.

13. Next Task Decision

Choose automatically:

A. TB-TMAR-CONTRACTS-W5
if another low-risk/high-value boundary extraction remains clearly best.

B. TB-TMAR-HOST-W3
if remaining Host write debt is now higher-value.

C. TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN
if contract boundaries are mature enough and shared-ACID consistency design should be formalized.

D. TB-TMAR-FE-BASELINE
if frontend recovery is READY and can proceed safely in parallel.

Do not ask the user.

14. Capability Map / Bootstrap / Master Recovery / Recovery SoT

Update narrowly:

docs/architecture/TOOBA-CAPABILITY-MAP.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-MICROSERVICE-MIGRATION-NOTES.md

docs/evidence/TB-TMAR-CONTRACTS-W4/recovery-sot.md

Record:

W4 dependency reductions

current Product-Resume-Safety

Frontend-Recovery-Readiness

next recommended TMAR task

Last Verified Task = TB-TMAR-CONTRACTS-W4

15. No Scope Creep

Do NOT:

redesign Checkout

implement Saga

migrate multiple Order dependencies

create Contracts for every module

move Domain ownership

reorganize folders

split giant files

modify frontend

install Redis

rewrite Git history

change product behavior

16. Acceptance Criteria

PASS only if:

Cart.Application → Offer.Application removed

exactly one additional Order.Application → foreign Application edge removed

no replacement bad dependency introduced

Contracts boundary is minimal and clean

App→App baseline shrinks by expected edges

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
Cart-Offer-Contract
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