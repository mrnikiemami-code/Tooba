PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID:
TB-TMAR-SETTLEMENT-GOLDEN-001

Parent-Task:
TB-TMAR-CART-GOLDEN-001-R1

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
SETTLEMENT_GOLDEN_CLOSURE

Title:
Settlement Endpoints Ownership + CQRS/Result Verification + Physical Golden Closure

Backend-Only:
YES

Architect decision

Cart is the accepted reference pattern. Every reopened HTTP-owning module must now satisfy:
Module.Endpoints -> ISender -> Application Commands/Queries/Handlers -> Result/SemanticError -> Contracts-only boundaries -> Infrastructure.
Host is composition root only.

This task is Settlement-only. Do NOT start another module.

Direct repository findings

Settlement currently has Domain/Application/Infrastructure/Tests but no Tooba.Settlement.Endpoints.

Current HTTP ownership is still:
src/backend/Host/Tooba.Host/Settlement/SettlementEndpoints.cs

Current routes:
Seller:

GET /v1/seller/settlement/balance

GET /v1/seller/settlement/entries

GET /v1/seller/settlement/statements

GET /v1/seller/settlement/payout-requests

POST /v1/seller/settlement/payout-requests

Admin:

GET /v1/admin/settlement/balances

GET /v1/admin/settlement/payout-queue

POST /v1/admin/settlement/payout-queue/query

POST /v1/admin/settlement/payout-requests/{payoutRequestId:guid}/process

POST /v1/admin/settlement/payout-requests/{payoutRequestId:guid}/retry

Settlement Application already has Commands/Queries/Models/Ports/Errors and is already registered in the CQRS foundation. Current Host endpoints already use ISender + ApiResponseFactory, but endpoint ownership/auth orchestration are still in Host.

Required end state

Create:
src/backend/Modules/Settlement/Tooba.Settlement.Endpoints/

Target:
Settlement.Endpoints -> ISender -> Settlement.Application -> Result -> Infrastructure

Host may only:

composition/startup

global auth/tenant/session plumbing

call app.MapSettlementEndpoints()

Host must not own:

Settlement endpoint implementation

Settlement business/query/presentation logic

Settlement grid semantics

Settlement DbContext authority outside bootstrap/migration allowlist

manual Settlement error mapping

Create real Endpoints project

Create Tooba.Settlement.Endpoints and add to src/backend/Tooba.slnx.

Allowed refs:

Settlement.Application

BuildingBlocks presentation/security primitives as needed

Forbidden refs:

Host

Settlement.Infrastructure

foreign Application/Infrastructure

DbContext

Namespace root:
Tooba.Settlement.Endpoints

Move all Settlement routes

Move exact route ownership from Host to module Endpoints. Preserve exact paths/verbs.

Delete Host SettlementEndpoints.cs.

Host should only call:
app.MapSettlementEndpoints();

CQRS verification

Every route must:

send exactly one real Command/Query through ISender

keep business/use-case logic in Application handlers

have no direct directory/DbContext/business orchestration in Endpoints

MediatR must resolve via existing Tooba CQRS foundation, version 12.5.0.

Do not add ceremonial duplicate handlers.

Authorization boundary

Current Host Settlement endpoints rely on SellerPanelAccess / SettlementAdminAccess / CurrentAuthenticatedSession / IAuthorizationGuard / tenant/control-plane context.

Settlement.Endpoints MUST NOT reference Host.

Preferred:

use shared neutral security/auth abstractions

pass explicit actor/seller/admin identity into Commands/Queries

If shared abstractions are insufficient:

introduce the smallest transport/security-only abstraction

Host implementation may remain only as global auth plumbing

no Settlement business rules, DbContext, query or domain logic in that adapter

Prefer deleting:
Host/Settlement/SettlementAdminAccess.cs

Target if feasible:
no production files under Host/Settlement/.

Result/error semantics

Expected Settlement business outcomes:

Result / Result<T>

SemanticError

centralized descriptors/localization

ApiResponseFactory

Forbidden:

PlatformHttpException as Settlement business outcome

manual {title,errorCode,detail}

raw semantic Results.Json

ex.Message

localized prose matching

Contains-based message heuristics

generic InvalidOperationException swallow

Audit Settlement Application error mapping for the same anti-pattern previously found in Cart.
Unknown unexpected exceptions must propagate.

Grid ownership

Current admin payout grid normalization depends on Host grid policy types.

Settlement.Endpoints/Application must not depend on Host.
Remove Tooba.Host.Grid / AdminListGridPolicies dependency from Settlement HTTP flow.

Use neutral BuildingBlocks Grid primitives if available, or move Settlement-specific normalization into Settlement Application/Endpoints.

Preserve:

search

filters

sort

paging

Pending/Failed defaults

response shape

Physical/folder standard

Settlement must match Cart/Order quality.

Required:

Tooba.Settlement.Domain

Tooba.Settlement.Application

Tooba.Settlement.Infrastructure

Tooba.Settlement.Endpoints

Tooba.Settlement.Tests

Contracts only if genuinely public and needed; no ceremonial project

Application:

Commands/<UseCase>/

Queries/<UseCase>/

Models/

Ports/

Errors/

Endpoints:

Seller/

Admin/

Errors/Resources only if real

no giant root endpoint dump

Infrastructure:

Persistence/

Directories/

Queries/

Gateways/

Bridges/

Messaging/

DependencyInjection/

Observability/
as applicable

Every handwritten .cs:

physical path matches responsibility

namespace matches path

root dump = 0 except tiny explicit bootstrap/endpoint-module/global-usings allowance

no TypeForwardedTo

no namespace masquerading

Host cleanup

Repository-wide Settlement scan must end with:

no Host Settlement endpoint implementation

no Settlement business composer

no Host Settlement grid/query engine

no SettlementDbContext production authority except bootstrap/migration allowlist

no PartyDbContext usage for Settlement

no Settlement-specific manual error mapping

Preferred:
remove Host/Settlement/ entirely if no legitimate global-auth adapter remains.

Behavior preservation

Preserve Seller:

balance

entries

statements

payout list

payout request

seller authorization

Preserve Admin:

balances

payout queue

payout grid query

process payout

retry payout

admin authorization

Preserve:

amount/currency

idempotency

payout state transitions

process/retry rules

seller names

grid semantics

outbox/event behavior

Accidental behavior change = 0.

Architecture guard upgrade

Upgrade Tooba.Settlement.Tests/Architecture/SettlementArchitectureGuardTests.cs to enforce:

Endpoints:

project exists

path/namespace aligned

no Host/Infrastructure ref

ISender

ApiResponseFactory

no DbContext

no manual semantic envelope

no ex.Message

no prose/message heuristics

Application:

real Commands/Queries/Handlers

foreign refs Contracts-only

no Host

no foreign DbContext

no message heuristic mapping

Host:

Host/Settlement/SettlementEndpoints.cs absent

no Settlement business/query/presentation authority

SettlementDbContext only bootstrap/migration allowlist

PartyDbContext Settlement usage = 0

General:

no TypeForwardedTo

no root dump

path↔namespace

no clock/id bypass

no hidden DI fallback

no silent catch

no raw StartActivity

Focused tests only

Required:

Settlement.Tests

focused seller Settlement routes

focused admin Settlement routes

payout grid query

process/retry payout

authorization compatibility

Result/error presentation

unexpected exception propagation if error handling changes

final dotnet build src/backend/Tooba.slnx

Do NOT run:

broad Host suite

Checkout workflow

Tax/Pricing

frontend

unrelated modules

Evidence

Create:
docs/evidence/TB-TMAR-SETTLEMENT-GOLDEN-001/

Required:

recovery-start.md

settlement-http-ownership-audit.md

settlement-cqrs-audit.md

settlement-auth-boundary.md

settlement-host-authority-audit.md

settlement-error-semantics-audit.md

settlement-physical-tree.md

behavior-preservation-audit.md

architecture-guard-audit.md

recovery-sot.md

Physical tree must list every handwritten production Settlement .cs:
path | namespace | responsibility

Protected state

Do NOT modify:

Cart except compile-only integration if unavoidable

Checkout semantics

Tax

Pricing

frontend

unrelated modules

No reset/clean/force push/broad git add.
Preserve stashes and user files.

Success criteria

PASS only if ALL true:

Settlement-HTTP-Ownership:
MODULE_ENDPOINTS

Settlement-Endpoints-State:
REAL_PROJECT_PRESENT

Settlement-CQRS-State:
MEDIATR_12_5_APPLICATION_HANDLERS

Settlement-Host-Endpoints:
REMOVED

Settlement-Host-Business-Authority:
NONE

Settlement-Host-DbAuthority:
NONE

Settlement-Grid-Ownership:
MODULE_OWNED

Settlement-CrossModule-Boundary:
CONTRACTS_ONLY

Settlement-Result-Adoption:
HTTP_USE_CASES_ADOPTED

Settlement-Error-Classification:
STABLE_CODES_ONLY

Settlement-Prose-Mapping:
NONE

Settlement-Unexpected-Exception-Swallow:
NONE

Settlement-Physical-State:
VERIFIED_ON_DISK_AND_NAMESPACE

Settlement-Architecture-Guards:
ENFORCED

Settlement-Behavior-Preservation:
VERIFIED

Settlement-State:
COMPLETE_REFERENCE_PATTERN

Cart-State:
COMPLETE_REFERENCE_PATTERN

Checkout-State:
PAUSED_AT_SAFE_W5_CHECKPOINT

Frontend-Production-Changes:
NONE

Completion scan before PASS

Scan entire repo for:

MapSettlementEndpoints

SettlementEndpoints

SettlementDbContext

SettlementPanelComposer

AdminPayoutGridQueryEngine

/settlement/

Settlement Application ports implemented in Host

Settlement manual error mapping

message/prose exception heuristics

Any production ownership leak => INCOMPLETE with exact path.

Canonical Result

Return ONLY BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Track
Recovery-Start
Settlement-HTTP-Ownership-Audit
Settlement-Endpoints-State
Settlement-CQRS-State
Settlement-Application-UseCases
Settlement-Auth-Boundary
Settlement-Grid-Ownership
Settlement-Result-Adoption
Settlement-Error-Classification
Settlement-Prose-Mapping
Settlement-Unexpected-Exception-Swallow
Settlement-Host-Authority-Audit
Settlement-Host-Endpoints
Settlement-Host-Business-Authority
Settlement-Host-DbAuthority
Settlement-CrossModule-Boundary
Settlement-Physical-State
Settlement-Architecture-Guards
Settlement-Behavior-Preservation
Focused-Validation
Skipped-Validation
Full-Validation
AntiPattern-Gate
Residual-Defects
Settlement-State
Cart-State
Checkout-State
Tax-State
Pricing-State
Frontend-Production-Changes
Git
Blockers
User-Work-Preserved
Next-Recommended-Task

If incomplete:
Next-Recommended-Task:
TB-TMAR-SETTLEMENT-GOLDEN-001-R1

If complete:
Next-Recommended-Task:
ARCHITECT_SELECT_NEXT_REOPENED_MODULE

After Result:
STOP completely.
Do NOT start another module.
Do NOT poll.
Do NOT fetch next task.
Do NOT write Worker IDLE.

END_TOOBA_TASK