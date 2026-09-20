PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID:
TB-TMAR-CONTRACTS-W6

Parent-Task:
TB-TMAR-CONTRACTS-W5

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
TMAR Contracts Wave 6 — Remove Promotion→Offer Application Coupling and Cart→Pricing Application Coupling

Task Type:
IMPLEMENTATION — NARROW BOUNDARY REPAIR

0. Architect Intent

CONTRACTS-W5 is accepted.

Current verified state:

Product-Resume-Safety = SAFE_WITH_TMAR_PARALLEL

Frontend-Recovery-Readiness = DEFER

App→App baseline reduced from 12 to 10

remaining known high-value App→App edges include:

Order.Application → Cart.Application

Order.Application → Inventory.Application

Order.Application → Promotion.Application

Promotion.Application → Offer.Application

Cart.Application → Pricing.Application

Checkout shared-ACID remains known and frozen

no Checkout/Saga redesign in this wave

Primary objectives:

remove Promotion.Application → Offer.Application

remove Cart.Application → Pricing.Application

reuse existing Offer.Contracts and Pricing.Contracts where sufficient

shrink App→App baseline from 10 by exactly the resolved edges

preserve all current business and checkout behavior

reassess whether backend dependency cleanup is now mature enough to pivot to Host W3, Checkout consistency design, or Frontend baseline

Do not migrate Order.Application write-sensitive dependencies in this task unless required only for compile compatibility and without architectural expansion.

1. Recovery / Git Safety

Repository:
D:\Users\User\source\repos\SarvNewVer

Read:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TOOBA-MICROSERVICE-MIGRATION-NOTES.md

docs/architecture/TMAR-architecture-locks.md

docs/architecture/TOOBA-CAPABILITY-MAP.md

docs/evidence/TB-TMAR-CONTRACTS-W5/recovery-sot.md

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
571bbb5904c1be37423e044b1e8224007420b584

If tracked user work conflicts:
STOP with RECOVERY_CONFLICT.

Never use destructive Git operations.

Evidence:
docs/evidence/TB-TMAR-CONTRACTS-W6/recovery-start.md

2. Remove Promotion.Application → Offer.Application

Inspect exact:

ProjectReference

Offer.Application interfaces/types used by Promotion.Application

source usage semantics

Prefer Tooba.Offer.Contracts.

Target:
Promotion.Application → Tooba.Offer.Contracts

Not:
Promotion.Application → Tooba.Offer.Application

Rules:

reuse existing stable Offer contracts where semantically correct

extend Offer.Contracts minimally only if required

no Offer Domain entities

no Offer internal Application implementation types

no convenience duplication of existing contracts

preserve promotion/campaign behavior exactly

After migration:

remove Promotion.Application → Offer.Application ProjectReference if no longer needed

shrink App→App baseline

no replacement bad dependency

Evidence:
docs/evidence/TB-TMAR-CONTRACTS-W6/promotion-offer-contract.md

3. Remove Cart.Application → Pricing.Application

Inspect exact:

ProjectReference

Pricing.Application interfaces/types consumed by Cart.Application

whether Tooba.Pricing.Contracts created in W5 already contains the required boundary

Target:
Cart.Application → Tooba.Pricing.Contracts

Not:
Cart.Application → Tooba.Pricing.Application

Rules:

use stable Pricing contract interfaces/DTOs only

preserve current price resolution semantics

preserve campaign-aware cart pricing behavior from prior accepted product tasks

do not change quote authority

do not move pricing business logic into Cart

do not expose Pricing Domain entities

After migration:

remove Cart.Application → Pricing.Application ProjectReference if no longer required

shrink App→App baseline

Evidence:
docs/evidence/TB-TMAR-CONTRACTS-W6/cart-pricing-contract.md

4. Pricing Authority Preservation

This is CRITICAL.

Cart currently depends on Pricing as the canonical price authority.

The migration must preserve:

canonical AuthoredPrice ownership in Pricing

campaign-aware cart pricing behavior

price revalidation rules

no client price authority

no duplicated price calculation inside Cart

no first-item/first-seller shortcuts

no fake/fallback pricing logic introduced to satisfy compilation

If current Pricing.Contracts is insufficient, extend it with the smallest stable authority contract.

Do not move business rules across bounded contexts.

Evidence:
docs/evidence/TB-TMAR-CONTRACTS-W6/pricing-authority.md

5. Promotion / Offer Boundary Preservation

Promotion must consume Offer through a stable public boundary.

Do not:

leak SellerOffer entity

expose Offer DbContext

copy Offer business rules into Promotion

duplicate sellability logic

Offer remains authoritative for Offer semantics.

Evidence:
docs/evidence/TB-TMAR-CONTRACTS-W6/promotion-offer-boundary.md

6. Dependency / Baseline Shrink

Expected:

remove Promotion.Application → Offer.Application

remove Cart.Application → Pricing.Application

No new:

App→App

Infra→foreign Application

Domain→foreign Domain

Infra→foreign Domain

Verify:

App→App baseline shrinks from 10 accordingly

Domain→foreign Domain remains empty

Infra→foreign Domain remains empty

source-size baseline does not increase

cross-context transaction baseline does not increase

Host write baseline does not increase

No wildcard allowances.

Evidence:
docs/evidence/TB-TMAR-CONTRACTS-W6/architecture-guards.md

7. Checkout / Transaction Semantics

Do NOT modify:

CheckoutDirectory TransactionScope

reserve/order/cart atomicity

transaction isolation

compensation semantics

retry semantics

Order lifecycle

This wave is compile-time/module-boundary cleanup only.

No:

Saga

Process Manager

choreography rewrite

distributed transaction

8. Contracts Guard

Keep generic Contracts cleanliness active for ALL *.Contracts.

Contracts must not reference:

Host

Infrastructure

foreign Application implementation assemblies

EF packages

Domain entities unless explicitly architecture-approved

Do not weaken existing guard.

Evidence:
docs/evidence/TB-TMAR-CONTRACTS-W6/contracts-guard.md

9. CQRS / Error / Locale / Time / ID

All NEW code must obey:

MediatR 12.5.0 for new use-cases

business handlers in Application

FluentValidation where applicable

IClock

IIdGenerator

semantic errors

no hardcoded localized user-facing Domain messages in ANY language

unlimited-locale safe

no FA/EN-only architecture assumptions

No unrelated mass migration.

10. Source-Size / God-File Safety

No new hand-written source file >800 LOC.

Existing oversized legacy files must not grow beyond baseline.

If touching an oversized file:

keep diff minimal

no decomposition here

characterization tests before future structural split

Evidence:
docs/evidence/TB-TMAR-CONTRACTS-W6/source-size-compliance.md

11. Tests

Promotion/Offer:

focused behavior tests around moved Offer boundary

campaign/promotion behavior unchanged

Cart/Pricing:

characterization of current pricing gateway behavior

campaign-aware cart price resolution

price revalidation behavior

existing cart quantity/reload/merge behavior where affected

Architecture:

ArchitectureBoundaryTests

Contracts cleanliness

TMAR foundation tests

source-size guard

transaction guard

Do not create broad new test projects.

Evidence:
docs/evidence/TB-TMAR-CONTRACTS-W6/tests.md

12. Backend Structural Readiness Assessment

After migration, assess remaining App→App and Infra→App debt.

Return:
Backend-Structural-Readiness: STABLE_FOR_PARALLEL_RECOVERY
or
Backend-Structural-Readiness: NEEDS_MORE_BOUNDARY_WORK

Criteria for STABLE_FOR_PARALLEL_RECOVERY:

no Domain/Infra→Domain leakage

App→App debt is frozen and reduced to known hub/orchestration edges

new features can use Contracts without expanding old coupling

critical Host/write/transaction/god-file guards are active

If stable, backend does NOT need to be fully clean before Frontend baseline begins.

Evidence:
docs/evidence/TB-TMAR-CONTRACTS-W6/backend-readiness.md

13. Frontend Recovery Readiness Reassessment

Do NOT modify frontend code.

Return exactly:
Frontend-Recovery-Readiness: READY
or
Frontend-Recovery-Readiness: DEFER

If backend is stable enough for parallel recovery, READY is acceptable even if backend debt remains.

Future FE baseline must audit, not blindly change:

flat app/admin

oversized TS/TSX

use client

manual test discovery

duplicate editors/carousels

antd

SSR/SEO boundaries

data-fetching strategy without assuming TanStack Query/SWR is required

Evidence:
docs/evidence/TB-TMAR-CONTRACTS-W6/frontend-readiness.md

14. Product Resume Safety

Expected:
Product-Resume-Safety: SAFE_WITH_TMAR_PARALLEL

Downgrade only for newly discovered critical unknown risk.

User still wants TMAR to continue for now.

15. Next Task Decision

Choose automatically:

A. TB-TMAR-CONTRACTS-W7
if another low-risk/high-value contract extraction clearly dominates.

B. TB-TMAR-HOST-W3
if Host write debt is now the best backend cleanup.

C. TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN
if boundary work is mature enough to formalize distributed consistency design.

D. TB-TMAR-FE-BASELINE
if Frontend-Recovery-Readiness = READY.

Prefer FE-BASELINE if backend is stable enough for parallel recovery and no new critical backend blocker exists.

Do not ask the user.

16. Capability Map / Bootstrap / Master Recovery / Recovery SoT

Update narrowly:

docs/architecture/TOOBA-CAPABILITY-MAP.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-MICROSERVICE-MIGRATION-NOTES.md

docs/evidence/TB-TMAR-CONTRACTS-W6/recovery-sot.md

Record:

W6 edge reductions

backend structural readiness

frontend recovery readiness

current Product-Resume-Safety

next recommended task

Last Verified Task = TB-TMAR-CONTRACTS-W6

17. No Scope Creep

Do NOT:

redesign Checkout

implement Saga

migrate Order.Cart/Order.Inventory write-sensitive edges

create Contracts for every module

move Domain ownership

reorganize folders

split giant files

modify frontend

install Redis

rewrite Git history

change product behavior

18. Acceptance Criteria

PASS only if:

Promotion.Application → Offer.Application removed

Cart.Application → Pricing.Application removed

no replacement bad dependency introduced

pricing authority remains in Pricing

promotion semantics remain with Offer boundary intact

App→App baseline shrinks

transaction semantics unchanged

source-size guards remain green

focused tests pass

Backend-Structural-Readiness assessed

Frontend-Recovery-Readiness assessed

Product-Resume-Safety assessed

user work preserved

next task chosen automatically

canonical Result delivered

Worker stops completely

19. Result Contract

Return ONLY canonical BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Recovery-Start
Promotion-Offer-Contract
Cart-Pricing-Contract
Pricing-Authority
Promotion-Offer-Boundary
Dependency-Changes
Transaction-Semantics
Architecture-Guards
Contracts-Guard
Characterization-Tests
Tests
Source-Size-Compliance
Backend-Structural-Readiness
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