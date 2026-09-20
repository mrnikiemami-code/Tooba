PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID:
TB-TMAR-CONTRACTS-W2

Parent-Task:
TB-TMAR-CONTRACTS-W1

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
TMAR Contracts Wave 2 — Remove Next Highest-Value Application/Infrastructure Cross-Module Coupling

Task Type:
IMPLEMENTATION — NARROW BOUNDARY REPAIR

0. Architect Intent

CONTRACTS-W1 is accepted.

Product-Resume-Safety is already SAFE_WITH_TMAR_PARALLEL, but the user explicitly wants TMAR to continue until they return to product feature work.

This task continues architecture debt reduction without blocking future product work.

Primary objectives:

copy durable microservice migration notes into the repository

remove Payment.Infrastructure → Wallet.Application if safely achievable through Wallet.Contracts

select ONE additional highest-value App→App or Infra→App coupling from the verified Contracts sequence and migrate it behind a proper Contracts boundary

shrink exact debt baselines

inventory cross-bounded-context TransactionScope/shared-ACID workflows and freeze NEW ones from expansion

do NOT redesign Checkout/Saga yet

1. Recovery / Git Safety

Repository:
D:\Users\User\source\repos\SarvNewVer

External durable migration note:
D:\Users\User\source\repos\SarvNewVerRequirment\reference\TOOBA-MICROSERVICE-MIGRATION-NOTES.md

Read:

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TMAR-architecture-locks.md

docs/architecture/TOOBA-CAPABILITY-MAP.md

docs/evidence/TB-TMAR-CONTRACTS-W1/recovery-sot.md

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
d7a70a8618ee599d7be47816651329382c107ab1

If conflicting tracked user work exists:
STOP with RECOVERY_CONFLICT.

No destructive Git operations.

Evidence:
docs/evidence/TB-TMAR-CONTRACTS-W2/recovery-start.md

2. Persist Microservice Migration Notes

Copy:

D:\Users\User\source\repos\SarvNewVerRequirment\reference\TOOBA-MICROSERVICE-MIGRATION-NOTES.md

to:

docs/architecture/TOOBA-MICROSERVICE-MIGRATION-NOTES.md

Do not weaken or rewrite its architectural intent.

If repository evidence is newer, only update current task/status facts.

This file becomes a durable architecture source alongside:

TOOBA-ARCHITECT-BOOTSTRAP.md

TMAR-architecture-locks.md

TOOBA-CAPABILITY-MAP.md

latest recovery-sot.md

Evidence:
docs/evidence/TB-TMAR-CONTRACTS-W2/migration-notes.md

3. Remove Payment.Infrastructure → Wallet.Application

CONTRACTS-W1 removed:
Payment.Infrastructure → Wallet.Domain

but retained:
Payment.Infrastructure → Wallet.Application

Inspect exact types/interfaces consumed.

Target:
Payment.Infrastructure → Wallet.Contracts

Rules:

do not move Wallet implementation into Contracts

do not expose Domain entities

do not expose DbContext/repository implementation

move only stable boundary interface/DTO/value contracts required by Payment

Wallet.Application/Infrastructure may implement/serve those contracts internally

After migration:

remove Payment.Infrastructure → Wallet.Application ProjectReference if no longer required

shrink tmar-infra-to-foreign-application.json

no replacement foreign Application/Domain coupling

Evidence:
docs/evidence/TB-TMAR-CONTRACTS-W2/payment-wallet-contract.md

4. Select ONE Additional Contracts Slice Automatically

Using:

contracts-sequence.md

current App→App baseline

current Infra→App baseline

extraction impact

regression radius

select exactly ONE additional coherent coupling to migrate.

Priority:

high-frequency or extraction-critical dependency

stable contract surface

low-to-moderate behavior risk

reduces future network/service coupling

avoids Checkout consistency redesign in this task

Preferred candidates may include verified Inventory / Pricing / Tax contract surfaces, but selection must be evidence-based.

Before implementation document:

source module

consuming module(s)

current project-reference edge

actual foreign types/interfaces

target Contracts project

expected edge reduction

why this slice is safest/highest-value now

Evidence:
docs/evidence/TB-TMAR-CONTRACTS-W2/slice-selection.md

5. Contracts Rules

Any created/expanded Tooba.<Module>.Contracts project must:

contain stable public boundary types only

have no EF

have no Host

have no Infrastructure

not expose Domain entities

not expose internal Application implementation types

support future in-process or HTTP/gRPC adapter implementation

Do not copy entire Application APIs into Contracts.

6. Cross-Bounded-Context Transaction Inventory

The microservice migration notes identify a critical future issue:
shared-database ACID transactions across multiple bounded contexts.

Repository-wide, identify:

TransactionScope

explicit BeginTransaction

transaction orchestration that spans calls to multiple business modules

shared-ACID assumptions involving Cart / Order / Inventory / Payment / Checkout or any other bounded contexts

For each occurrence record:

file/member

owning module

participating bounded contexts/modules

transaction mechanism

current atomicity assumption

future extraction risk

classification:

LOCAL_MODULE_TRANSACTION

CROSS_CONTEXT_SHARED_ACID

NEEDS_REVIEW

Do NOT redesign those workflows now.

Evidence:
docs/evidence/TB-TMAR-CONTRACTS-W2/cross-context-transactions.md

Machine-readable:
docs/evidence/TB-TMAR-CONTRACTS-W2/cross-context-transactions.json

7. Freeze NEW Cross-Context Shared ACID

Add a TMAR architecture rule:

ARCH-TX-001:
No NEW business workflow may rely on a single ACID transaction spanning multiple bounded contexts.

Existing confirmed cross-context/shared-database transactions are explicit migration debt.

Add an architecture/CI guard where mechanically safe.

If full semantic detection cannot be automated:

baseline known transaction orchestrators exactly

block new TransactionScope/transaction orchestration in protected cross-module Application/Host paths unless explicitly architecture-approved

no wildcard suppression

Do NOT add a global MediatR TransactionBehavior.

Evidence:
docs/evidence/TB-TMAR-CONTRACTS-W2/transaction-guard.md

Update:
docs/architecture/TMAR-architecture-locks.md
docs/architecture/TOOBA-MICROSERVICE-MIGRATION-NOTES.md

8. Checkout Consistency — DESIGN DEFERRED

Do NOT implement:

Saga

Process Manager

compensation workflow

distributed transaction

event choreography redesign

Only record a future task candidate:

TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN

Its future scope must include:

invariants

point-of-no-return

participants

compensation

idempotency

retries

duplicate delivery

timeout/recovery

intermediate states

Outbox/Integration Event usage

This future task is DESIGN FIRST, not implementation.

9. Order.Application Hub

Do not redesign the Order hub here.

If the selected Contracts slice touches Order:

reduce only the selected dependency

preserve current consistency/behavior

update hub map

do not convert multiple sync calls at once

10. CQRS / Error / Locale / Time / ID Compliance

All NEW code must follow existing TMAR locks:

MediatR 12.5.0 for new use-cases

handlers in Application

FluentValidation where appropriate

IClock

IIdGenerator

semantic errors

no hardcoded localized user-facing Domain messages in ANY language

unlimited-locale-safe

11. Source-Size Compliance

No new hand-written source >800 LOC.
No oversized legacy file may grow above baseline.

If touching an oversized file:

keep diff minimal

no decomposition unless characterization tests exist first

12. Architecture Baselines Must Shrink

Expected:

Infra→foreign Application baseline shrinks at least for Payment→Wallet.Application

App→App or Infra→App baseline shrinks for the selected second slice

no new Domain→foreign Domain

no new Infra→foreign Domain

no new App→App edge elsewhere

no wildcard allowances

Evidence:
docs/evidence/TB-TMAR-CONTRACTS-W2/architecture-guards.md

13. Tests

Required:

focused characterization tests before moving live boundary behavior

Payment/Wallet affected tests

selected second slice affected tests

Contracts cleanliness tests

ArchitectureBoundaryTests

TMAR foundation tests

source-size guards

transaction guard tests/proofs

Do not create all module test projects.

Evidence:
docs/evidence/TB-TMAR-CONTRACTS-W2/tests.md

14. Product Resume Safety

Return exactly:

Product-Resume-Safety: SAFE_WITH_TMAR_PARALLEL

unless this task discovers a NEW critical unknown architectural risk that makes new product work unsafe.

If downgraded to NOT_YET, explain the newly discovered blocking evidence precisely.

15. Next Task Decision

Choose automatically among:

A. TB-TMAR-CONTRACTS-W3
if contract leakage remains the highest-value low-risk debt.

B. TB-TMAR-HOST-W3
if Host direct-write debt is now the best next cleanup while product work can remain safe.

C. TB-TMAR-CHECKOUT-CONSISTENCY-DESIGN
if transaction inventory shows a critical consistency design should be formalized next.

D. TB-TMAR-GODFILE-W1
only if a critical oversized file blocks further architecture work; characterization tests must come first.

Do not ask the user.

16. Capability Map / Bootstrap / Recovery SoT

Update narrowly:

docs/architecture/TOOBA-CAPABILITY-MAP.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TOOBA-MICROSERVICE-MIGRATION-NOTES.md

docs/evidence/TB-TMAR-CONTRACTS-W2/recovery-sot.md

Record:

imported migration notes

removed dependency edges

transaction inventory/guard

Last Verified Task = TB-TMAR-CONTRACTS-W2

17. No Scope Creep

Do NOT:

redesign Checkout

implement Saga

create Contracts for every module

migrate all Order dependencies

move Domain ownership

reorganize folders

split giant files broadly

install Redis

rewrite Git history

alter frontend

change product behavior

18. Acceptance Criteria

PASS only if:

migration notes copied into repository

Payment.Infrastructure→Wallet.Application removed if verified migratable

one additional evidence-selected contracts slice migrated

architecture baselines shrink

Contracts remain clean

cross-context transaction inventory exists

ARCH-TX-001 canonicalized

new shared-ACID expansion is guarded/baselined

no Saga/Checkout redesign implemented

focused tests pass

source-size guards remain green

user work preserved

Product-Resume-Safety assessed

next task chosen automatically

canonical Result delivered

Worker stops completely

19. Result Contract

Return ONLY canonical BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Recovery-Start
Migration-Notes
Payment-Wallet-Contract
Slice-Selection
Contracts-Changes
Dependency-Changes
Cross-Context-Transactions
Transaction-Guard
Checkout-Consistency-Future
Architecture-Guards
Characterization-Tests
Tests
Source-Size-Compliance
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