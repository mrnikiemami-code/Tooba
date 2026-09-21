PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID:
TB-TMAR-NEXT-MODULE-BATCH-002

Parent-Task:
TB-TMAR-NEXT-MODULE-BATCH-001-R1

Channel:
tooba-main

WorkerId:
tooba-worker-01

AgentType:
cursor

Program:
TMAR — Tooba Microservice-Ready Architecture Recovery

Mode:
FAST-SAFE

Track:
NEXT_REFERENCE_MODULE_BATCH

Title:
Wallet + Payment Golden Recovery Batch

Backend-Only:
YES

TMAR-Execution-Mode:
BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE

0. Accepted baseline

Accepted:

Foundation-State: RESULT_PATTERN_FOUNDATION_COMPLETE

Offer-State: COMPLETE_REFERENCE_PATTERN

Inventory-State: COMPLETE_REFERENCE_PATTERN

Inventory-Physical-State: VERIFIED_ON_DISK_AND_NAMESPACE

Promotion-State: COMPLETE_REFERENCE_PATTERN

Promotion-Physical-State: VERIFIED_ON_DISK_AND_NAMESPACE

Checkout-State: PAUSED_AT_SAFE_W5_CHECKPOINT

Frontend frozen

Deferred by USER:

Tax physical-folder/namespace re-review

Pricing physical-folder/namespace re-review

DO NOT modify Tax/Pricing in this task.

Current baseline SHA must be verified from origin/main before starting.
Do not trust a stale SHA in task text.

1. Architect acceptance of parent

TB-TMAR-NEXT-MODULE-BATCH-001-R1 is ACCEPTED after direct repository verification.

Architect verified on current main:

physical trees for Inventory/Promotion

path↔namespace alignment

root-dump guards

no TypeForwardedTo

required IClock/IIdGenerator/IModuleCallTracer

silent release catch removed

idempotent release behavior exists

localized exception prose removed

Host DbContext guards exist

Proceed to Batch-002.

2. Scope

Recover ONLY:

Wallet

Payment

Do NOT include:

Returns

Order

Checkout process manager

Cart

Tax

Pricing

frontend

Cross-module consumers may receive compile-only namespace/contract updates when strictly required.

3. Why Wallet + Payment

This pair has high extraction value and historical cross-module coupling:

Payment previously depended on Wallet Application/Domain

TMAR Contracts waves moved payment-wallet interaction toward Wallet.Contracts

financial boundaries require explicit ownership before future extraction

Goal:
make both modules strict Modular-Monolith Golden references without redesigning payment product behavior.

4. Mandatory physical Visual Studio structure

THIS IS PART OF DEFINITION OF DONE FROM THE START.

No module may be COMPLETE unless Visual Studio Solution Explorer shows meaningful physical responsibility folders and namespaces match them.

For every handwritten production .cs:

classify responsibility

move to meaningful physical folder

namespace = module + layer + responsibility

update in-repo consumers atomically

no TypeForwardedTo / fake namespace / alias shim

No root dumping where ownership is identifiable.
No empty ceremonial folders.

Suggested target, adapt to real responsibilities:

Wallet Domain:

Aggregates

Entities

ValueObjects

Events

Policies

Wallet Application:

Commands

Queries

Handlers

Ports

Models

Payments

Refunds

Wallet Contracts:

Payments

Refunds

Dtos

Ports

Errors

Wallet Infrastructure:

Persistence

Repositories/Directories

Adapters

Events

Messaging/Outbox

DependencyInjection

Payment Domain:

Aggregates

Entities

ValueObjects

Events

Policies

Payment Application:

Commands

Queries

Handlers

Ports

Models

Payment Contracts:
create ONLY if real public cross-module contracts exist.
Do not create ceremonial Contracts project.

Payment Infrastructure:

Persistence

Gateways

Adapters

Providers

Events

Messaging/Outbox

DependencyInjection

Payment Endpoints:
create ONLY if Payment truly owns HTTP use cases.
Do not create an empty ceremonial Endpoints project.

Tests:

Architecture

Behavior/UseCases as needed

5. First-pass audit

Before repair, audit Wallet + Payment for:

Architecture:

project references

foreign Application/Domain/Infrastructure references

Host DbContext access

direct cross-module SQL/JOIN/FK

public contracts living in Application

TypeForwardedTo

wrong namespace/layer ownership

root-dumped production source

oversized multi-responsibility source files

Foundation adoption:

DateTimeOffset.UtcNow / DateTime.UtcNow

Guid.NewGuid / UuidV7.New

new SystemUtcClock / UuidV7IdGenerator fallback construction

PlatformHttpException

SemanticException expected-flow misuse

raw Results.Json for owned Result routes

manual ProblemDetails/status/Accept-Language mapping

raw ex.Message response leakage

ActivitySource / StartActivity

catch(Exception), empty catch, silent ignore

localized exception prose

Write:

wallet-audit.md

payment-audit.md

physical-namespace-audit.md

host-leak-scan.md

foundation-adoption-scan.md

6. Wallet ownership

Wallet owns:

wallet/account/balance/ledger domain

wallet debit/credit semantics

wallet currency/value rules

wallet-owned persistence

payment/refund wallet ports where already established in Wallet.Contracts

Preserve historical TMAR contracts:

order-payment Wallet contract

refund-credit Wallet contract

Do not move Payment business concepts into Wallet merely because Wallet is called.

Wallet Domain must not depend on:

Payment

Returns

Order

Host

EF/ASP.NET

foreign Application/Domain implementation

Wallet Application must not depend on foreign Application.
Use Contracts/ports.

Wallet Infrastructure may implement Wallet-owned ports, but must not open foreign DbContexts.

7. Payment ownership

Payment owns:

payment attempt/payment lifecycle/provider orchestration belonging to Payment

Payment persistence

provider/gateway adapters

payment-specific domain rules

Payment must consume Wallet only through Wallet.Contracts.

Forbidden:

Wallet.Application

Wallet.Domain

Wallet.Infrastructure

WalletDbContext

direct wallet mutation

cross-module SQL

Do not redesign checkout/payment point-of-no-return semantics.
Checkout remains paused.

If Payment has Order/other foreign implementation coupling:
repair only if a stable existing Contracts boundary is available or a very small clear contract extraction is necessary.
Do not turn this into a broad Checkout task.

8. CQRS + MediatR rule

Do NOT add MediatR ceremonially just because a project exists.

For each module:

if module owns an HTTP/application use case, target:
Endpoint → ISender → Command/Query → Handler → Domain/Ports

expected business failures → Result / Result<T>

if module has no owned HTTP/application use case, document N/A and do not create fake commands/handlers

Existing real application use cases that are currently Directory/service driven and directly exposed through owned HTTP SHOULD be converted if bounded and safe.

MediatR version exactly 12.5.0.

9. Result Pattern

Expected business outcomes:

Result

Result<T>

Use existing:

SemanticError

ApiResponseFactory

ErrorDescriptor/localization

Do NOT:

add second error model

catch arbitrary Exception into Result

throw SemanticException for expected flow when Result is canonical

unwrap Result.Value unsafely

manually switch error codes in endpoints

Unexpected/system failures remain exceptions.

10. HTTP / Endpoints

Audit who currently owns Wallet/Payment HTTP transport.

If module owns HTTP:

thin Endpoints project

ISender

ApiResponseFactory

no DbContext/business logic

If Host currently owns module transport:

remove Host business authority

Host may only map module endpoints/composition

move bounded owned HTTP to module Endpoints if warranted

If no owned HTTP surface exists:

do not invent one

document Endpoint-State: NOT_APPLICABLE

No empty route group just for appearances.

11. Error/localization

For actual user-facing semantic errors:

stable machine code

ErrorDescriptor

explicit status/classification

resource localization through central system

English default

Persian where applicable

Do not put localized user-facing prose in Domain/Infrastructure exceptions.

No ceremonial resx when there is no HTTP/user-facing semantic surface.

12. Clock / IDs

Use injected:

IClock

IIdGenerator

No direct:

DateTime.UtcNow

DateTimeOffset.UtcNow

Guid.NewGuid

UuidV7.New

No hidden fallback:

clock ?? new SystemUtcClock()

ids ?? new UuidV7IdGenerator()

Pure Domain may receive explicit now/id.

13. Observability

Preserve Golden foundation.

No new raw ActivitySource/StartActivity.

Use IModuleCallTracer only for genuine module calls such as Payment→Wallet Contracts when useful.

Result business failure is business_failure, not system Activity Error.

No duplicate spans.

14. Data / transaction safety

Each module owns:

schema

DbContext

migrations

No:

cross-module FK

cross-module SQL JOIN

foreign DbContext

new shared ACID across Wallet/Payment/Order

Do not resume Checkout consistency work.

If an existing cross-context transaction is encountered:
document it as residual/baseline unless this exact task can remove it safely without behavior redesign.

No destructive migration.

15. Physical decomposition

If Wallet/Payment currently have broad files:
split by cohesive responsibility.

Do not merely rename/move a god-file unchanged.

Avoid over-fragmentation.

All handwritten source must satisfy source-size architecture guards.

16. Architecture guards

Add high-value module guards.

Both modules:

root production .cs dump = 0 except tiny explicit justified allowlist

physical folder ↔ namespace alignment

layer/project namespace prefixes

no TypeForwardedTo

Domain no foreign App/Domain/Infrastructure/Host/EF/ASP.NET

Application no foreign Application/Infrastructure/DbContext/Host

Infrastructure no foreign DbContext or cross-module SQL

no DateTime/Guid bypass

no hidden default fallback constructors

no raw StartActivity

no silent catch/catch-ignore

no localized exception prose

Host no WalletDbContext/PaymentDbContext production authority

Payment specifically:

no Wallet.Application/Domain/Infrastructure refs

Wallet interactions only through Wallet.Contracts

Wallet specifically:

public payment/refund ports stay in Contracts, not Application if cross-module

HTTP guard only if owned HTTP exists:

ISender + Result + ApiResponseFactory

no raw Results.Json / manual error mapping

17. Evidence: Visual Studio physical structure

Mandatory:

docs/evidence/TB-TMAR-NEXT-MODULE-BATCH-002/wallet-physical-tree.md
docs/evidence/TB-TMAR-NEXT-MODULE-BATCH-002/payment-physical-tree.md

List EVERY handwritten production .cs:

relative physical path

namespace

responsibility

Evidence must prove how module appears in Visual Studio.

18. Validation — Fast-Safe

Required:

Wallet focused tests/architecture guards

Payment focused tests/architecture guards

focused behavior tests for changed financial seams

one final:
dotnet build src/backend/Tooba.slnx

Conditional:

Host focused tests only changed composition/endpoint seams

Returns tests only if Wallet refund Contracts changed

Order tests only if Payment/Wallet public contract compile/runtime seam changed

BuildingBlocks only if shared production changed (prefer none)

Do NOT run:

full Host integration suite merely for reassurance

Inventory/Promotion/Offer full suites unless production contract behavior is changed

Tax/Pricing

frontend

No flaky retry/sleep.

19. Strict scope / forbidden

DO NOT:

modify Tax/Pricing production

resume Checkout W6

redesign payment workflows

redesign wallet business policy

touch frontend

widen architecture baselines

use broad rewrite

add fake Endpoints/Contracts/MediatR projects for symmetry

use service locator

use TypeForwardedTo

create compatibility wrappers to conceal wrong ownership

20. AntiPattern Gate

Reject:

root dumping

namespace masquerading

wrong project ownership

TypeForwardedTo

Host DbContext/business authority

foreign Application/Domain implementation dependency

direct clock/id

DI fallback implementations

expected business exception control flow where Result applies

catch-all Exception→Result

silent catch

localized exception prose

raw Results.Json/manual ProblemDetails on owned Result HTTP

raw StartActivity

cross-module SQL/FK

new cross-context shared ACID

ceremonial folders/projects

frontend changes

Checkout resume

21. Success state

On true closure:

Wallet-State:
COMPLETE_REFERENCE_PATTERN

Wallet-Physical-State:
VERIFIED_ON_DISK_AND_NAMESPACE

Payment-State:
COMPLETE_REFERENCE_PATTERN

Payment-Physical-State:
VERIFIED_ON_DISK_AND_NAMESPACE

Foundation-State:
RESULT_PATTERN_FOUNDATION_COMPLETE

Offer-State:
COMPLETE_REFERENCE_PATTERN

Inventory-State:
COMPLETE_REFERENCE_PATTERN

Promotion-State:
COMPLETE_REFERENCE_PATTERN

Tax-State:
DEFERRED_PHYSICAL_REVIEW_BY_USER

Pricing-State:
DEFERRED_PHYSICAL_REVIEW_BY_USER

Checkout-State:
PAUSED_AT_SAFE_W5_CHECKPOINT

Frontend-Production-Changes:
NONE

Batch-State:
COMPLETE

Module-Recovery-State:
NEXT_REFERENCE_BATCH_002_COMPLETE

Next-Recommended-Task:
TB-TMAR-NEXT-MODULE-BATCH-003

If one module is blocked:

preserve clean module

mark blocked module INCOMPLETE

recommend only focused R1 for blocked module

22. Canonical Result

Return ONLY BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Track
Recovery-Start
Wallet-Audit
Wallet-Physical-Tree
Wallet-Repairs
Wallet-Result-Adoption
Wallet-CQRS-State
Wallet-Endpoint-State
Wallet-Foundation-Adoption
Wallet-Guards
Wallet-Validation
Wallet-State
Wallet-Physical-State
Payment-Audit
Payment-Physical-Tree
Payment-Repairs
Payment-Result-Adoption
Payment-CQRS-State
Payment-Endpoint-State
Payment-Foundation-Adoption
Payment-Guards
Payment-Validation
Host-Leak-Scan
Foundation-Adoption-Scan
Focused-Validation
Skipped-Validation
Full-Validation
AntiPattern-Gate
Residual-Defects
Batch-State
Foundation-State
Offer-State
Inventory-State
Promotion-State
Tax-State
Pricing-State
Checkout-State
Frontend-Production-Changes
Git
Blockers
User-Work-Preserved
Module-Recovery-State
Next-Recommended-Task

After canonical Result:
STOP completely.
Do NOT poll.
Do NOT fetch next task.
Do NOT write Worker IDLE.

END_TOOBA_TASK