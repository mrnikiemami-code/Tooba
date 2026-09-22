PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-SUPPORT-GOLDEN-001
Parent-Task: TB-TMAR-NOTIFICATION-GOLDEN-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: FAST-SAFE
Track: SUPPORT_GOLDEN_CLOSURE
Title: Support Endpoints Ownership + MediatR CQRS + Host Authority Cleanup
Backend-Only: YES

Architect decision

Notification is accepted after direct repo verification.
This task is Support-only.

Golden target:
Tooba.Support.Endpoints
→ ISender
→ Support.Application Commands/Queries/Handlers
→ Result/SemanticError
→ Contracts-only foreign boundaries
→ Infrastructure

Host = composition/security/bootstrap only.

Direct findings

Support currently has Domain/Application/Infrastructure/Tests but no Endpoints project.

Current HTTP ownership:
src/backend/Host/Tooba.Host/Support/SupportEndpoints.cs

It owns customer/seller/admin ticket routes and directly calls ISupportDirectory.

Current Host endpoint also contains:

customer actor resolution

SellerPanelAccess/AdminPanelAccess

direct AccessControl.Application/Domain capability checks

PlatformHttpException

manual error JSON

generic catch (InvalidOperationException) -> support.*.rejected

idempotency header parsing

wire DTOs

admin demo preview

authorization fail-open logic in Host

Current Support.Application is not real CQRS:

Commands/SupportCommands.cs is just data contracts

Queries/SupportQueries.cs is just query models

no MediatR handlers

Application.csproj references only Domain

Host also contains:
SupportDevelopmentSeedHost.cs

This file directly touches Support.Infrastructure + SupportDbContext for development seeding. Treat development bootstrap separately from HTTP ownership; keep only if genuinely host-level bootstrap, but it must not become a production business seam.

Required end state

Create:
src/backend/Modules/Support/Tooba.Support.Endpoints/

All Support HTTP routes move out of Host.

Delete:
Host/Support/SupportEndpoints.cs

Preferred:
Host/Support/ contains only SupportDevelopmentSeedHost.cs if still justified strictly for Development bootstrap.

Endpoints project

Create Tooba.Support.Endpoints, add to src/backend/Tooba.slnx.

Allowed refs:

Support.Application

BuildingBlocks presentation/security

shared AccessControl Contracts/neutral abstractions only if available

Forbidden:

Host

Support.Infrastructure

AccessControl.Application/Domain if avoidable

foreign Application/Infrastructure

DbContexts

Folders:

Customer/

Seller/

Admin/

Errors/Resources only if needed

No root dump.

Move exact HTTP routes

Move all Support routes from Host preserving paths/verbs.

Customer:

GET /v1/customer/support/tickets

POST /v1/customer/support/tickets

GET /v1/customer/support/tickets/{ticketId:guid}

POST /v1/customer/support/tickets/{ticketId:guid}/replies

POST /v1/customer/support/tickets/{ticketId:guid}/close

POST /v1/customer/support/tickets/{ticketId:guid}/reopen

Seller:

GET /v1/seller/support/tickets

POST /v1/seller/support/tickets

GET /v1/seller/support/tickets/{ticketId:guid}

POST /v1/seller/support/tickets/{ticketId:guid}/replies

POST /v1/seller/support/tickets/{ticketId:guid}/close

POST /v1/seller/support/tickets/{ticketId:guid}/reopen

Admin:

GET /v1/admin/support/tickets

GET /v1/admin/support/tickets/{ticketId:guid}

POST /v1/admin/support/tickets/{ticketId:guid}/replies

PATCH /v1/admin/support/tickets/{ticketId:guid}

GET /v1/admin/support/demo-preview

Host only calls:
app.MapSupportEndpoints();

Real MediatR CQRS

Convert Support HTTP use cases to real MediatR 12.5.0 commands/queries/handlers.

Expected Queries:

ListCustomerTickets

GetCustomerTicket

ListSellerTickets

GetSellerTicket

ListAdminTickets

GetAdminTicket

GetSupportDemoPreview if appropriate for app boundary, otherwise endpoint-only dev transport read if no business dependency

Expected Commands:

CreateCustomerTicket

ReplyCustomerTicket

CloseCustomerTicket

ReopenCustomerTicket

CreateSellerTicket

ReplySellerTicket

CloseSellerTicket

ReopenSellerTicket

ReplyAdminTicket

PatchAdminTicket

Use real use-case folders:
Commands/<UseCase>/
Queries/<UseCase>/

No SupportCommands.cs/SupportQueries.cs dumping-ground files.

Application handlers call ISupportDirectory/ports.
Register Support.Application assembly in CQRS foundation.

Authorization boundary

Support.Endpoints must not reference Host.

Current seller/admin capability checks are wrongly embedded in Host.

Create/reuse minimal endpoint auth abstractions:

ISupportCustomerAuthorizer

ISupportSellerAuthorizer

ISupportAdminAuthorizer

These must return explicit actor/seller/admin identity and enforce current authorization/capability semantics.

Preferred:

security adapter implementation may remain Host-level only if it is pure auth plumbing

no SupportDirectory

no SupportDbContext

no support business logic

no manual error response mapping

Important:
Preserve current seller capability permissions:

support.view

support.create

support.reply

Preserve current admin capability semantics:

support.view

support.manage

Current admin auth contains a fail-open when authorization decision is Unavailable. Do NOT silently change this behavior in this task. Preserve and document it as compatibility behavior unless a pre-existing architectural lock already forbids it. Do not generalize it into business logic.

Result pattern

All expected Support business outcomes:

Result / Result<T>

SemanticError

ApiResponseFactory

centralized descriptors/localization

Forbidden:

manual Rejected(...)

manual Missing()

manual ToError(...)

PlatformHttpException for Support business outcomes

generic catch InvalidOperationException => rejected

ex.Message

prose/Contains/StartsWith heuristics

unknown exception swallow

Unknown/unexpected exceptions propagate.

Stable error semantics

Audit exact Support Domain/Infrastructure stable codes.

Expected public semantics include at least:

customer.session.required

support.missing

support.rejected

support.reply.rejected

support.action.rejected

support.patch.rejected

seller.authorization.denied

admin.authorization.denied

support.demo.not_ready

Do not invent duplicate aliases.
Map exact stable machine codes only.

Idempotency

Preserve Idempotency-Key behavior for:

customer/seller create

customer/seller/admin replies

Endpoint parses header and passes explicit key into command.
Application/business semantics remain unchanged.

Wire DTO ownership

Move:

CreateTicketBody

ReplyTicketBody

AdminPatchBody

into Support.Endpoints transport models.
Do not leak ASP.NET types into Application.

Cross-module boundaries

Support currently references Notification.Contracts from Infrastructure; preserve Contracts-only boundary.

Do not reference Notification.Application/Domain/Infrastructure.

For AccessControl:
do not make Support.Application depend directly on AccessControl.Application/Domain to reproduce Host authorization.
Keep authorization at endpoint/security boundary via neutral interfaces/adapters.

Demo preview

Preserve /v1/admin/support/demo-preview:

Development only

non-development returns 404

not-ready remains 503 + support.demo.not_ready

Move wire behavior into Support.Endpoints.
If SupportDemoSnapshotStore is Infrastructure-owned, expose the smallest proper Support Application/contract seam; Endpoints must not reference Support.Infrastructure.

SupportDevelopmentSeedHost

Audit separately.

Allowed to remain ONLY if:

Development-only composition/bootstrap

no production HTTP/business ownership

no runtime app use-case

clearly named bootstrap

architecture guard allowlists it explicitly

If it contains avoidable cross-module Application/Domain authority, reduce only what is necessary without broad AccessControl recovery.

Do not delete useful dev seeding just to make Host empty.

Physical structure

Must match Cart/Settlement/Fulfillment/Returns/Notification.

Projects:

Tooba.Support.Domain

Tooba.Support.Application

Tooba.Support.Infrastructure

Tooba.Support.Endpoints

Tooba.Support.Tests

Contracts only if truly needed; do not add ceremonial project

Application:

Commands/<UseCase>/

Queries/<UseCase>/

Models/

Ports/

Errors/

Endpoints:

Customer/

Seller/

Admin/

Errors/Resources if needed

Infrastructure:

Persistence/

Directories/

Adapters/

Seeds/

Messaging/

DependencyInjection/

Migrations/

No root dump.
Path↔namespace aligned.
No TypeForwardedTo.
No namespace masquerading.

Host cleanup

After task:

Host/Support/SupportEndpoints.cs absent

no ISupportDirectory in Host HTTP

no Support business/query/presentation mapping in Host

no manual Support semantic JSON in Host

no SupportDbContext production authority except Development seed/bootstrap/migration allowlist

Program may:

register Support auth adapters

register endpoint presentation

call MapSupportEndpoints()

Behavior preservation

Preserve:

customer list/create/get/reply/close/reopen

seller list/create/get/reply/close/reopen

admin list/get/reply/patch

admin demo preview

page/pageSize/status/requester/category/priority/q filtering

ticket ownership/scoping

seller capabilities

admin capabilities including current Unavailable compatibility

idempotency

internal note behavior

related entity fields

notifications via Notification.Contracts

support events/outbox

Accidental behavior change = 0.

Architecture guards

Upgrade SupportArchitectureGuardTests to enforce:

Endpoints project exists

path↔namespace

Endpoint -> Application only; no Host/Infrastructure

no foreign Application/Infrastructure

no direct ISupportDirectory in Endpoints

uses ISender

ApiResponseFactory for expected failures

real MediatR Commands/Queries/Handlers

use-case folders

no SupportEndpoints in Host

no generic InvalidOperationException swallow

no manual rejected/missing/error mapping

Contracts-only Notification boundary

no TypeForwardedTo

no root dump

no clock/id bypass

no silent catch

no localized exception prose

Explicitly allow SupportDevelopmentSeedHost only if it remains Development bootstrap and nothing more.

Focused tests

Required:

Support.Tests

customer route behavior

seller route + capability behavior

admin route + capability behavior

create/reply idempotency

close/reopen

missing ticket

patch behavior

demo preview dev/non-dev/not-ready

Result/error semantics

unknown exception propagation if mapper created

architecture guards

final dotnet build src/backend/Tooba.slnx

Skip:

broad Host suite

Checkout

Tax/Pricing

frontend

unrelated modules

Evidence

Create:
docs/evidence/TB-TMAR-SUPPORT-GOLDEN-001/

Required:

recovery-start.md

support-http-ownership-audit.md

support-cqrs-audit.md

support-auth-boundary.md

support-host-authority-audit.md

support-error-semantics-audit.md

support-dev-seed-audit.md

support-physical-tree.md

behavior-preservation-audit.md

architecture-guard-audit.md

recovery-sot.md

Physical tree lists all handwritten Support production .cs:
path | namespace | responsibility

Protected state

Cart/Settlement/Fulfillment/Returns/Notification stay COMPLETE.
Checkout stays PAUSED_AT_SAFE_W5_CHECKPOINT.
Tax/Pricing/frontend untouched.
Do NOT start Wallet/Payment/Promotion.

No reset/clean/force push/broad git add.
Preserve stashes/user files.

Completion scan

Before PASS scan entire repo for:

SupportEndpoints

ISupportDirectory in Host HTTP

SupportDbContext in Host

/support/

SupportCommands.cs

SupportQueries.cs

PlatformHttpException in Support HTTP flow

generic InvalidOperationException catches

manual support error JSON

Support Application ports implemented in Host

Any production ownership leak => INCOMPLETE.

Success criteria

PASS only if ALL:

Support-HTTP-Ownership: MODULE_ENDPOINTS
Support-Endpoints-State: REAL_PROJECT_PRESENT
Support-CQRS-State: MEDIATR_12_5_APPLICATION_HANDLERS
Support-Host-Endpoints: REMOVED
Support-Host-Business-Authority: NONE
Support-Host-DbAuthority: NONE_EXCEPT_DEV_BOOTSTRAP_ALLOWLIST
Support-CrossModule-Boundary: CONTRACTS_ONLY
Support-Result-Adoption: HTTP_USE_CASES_ADOPTED
Support-Error-Classification: STABLE_CODES_ONLY
Support-Prose-Mapping: NONE
Support-Unexpected-Exception-Swallow: NONE
Support-Physical-State: VERIFIED_ON_DISK_AND_NAMESPACE
Support-Architecture-Guards: ENFORCED
Support-Behavior-Preservation: VERIFIED
Support-State: COMPLETE_REFERENCE_PATTERN

Canonical Result

Return ONLY BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Track
Recovery-Start
Support-HTTP-Ownership-Audit
Support-Endpoints-State
Support-CQRS-State
Support-Application-UseCases
Support-Auth-Boundary
Support-Result-Adoption
Support-Error-Classification
Support-Prose-Mapping
Support-Unexpected-Exception-Swallow
Support-Host-Authority-Audit
Support-Host-Endpoints
Support-Host-Business-Authority
Support-Host-DbAuthority
Support-CrossModule-Boundary
Support-Dev-Seed-Audit
Support-Physical-State
Support-Architecture-Guards
Support-Behavior-Preservation
Focused-Validation
Skipped-Validation
Full-Validation
AntiPattern-Gate
Residual-Defects
Support-State
Notification-State
Returns-State
Fulfillment-State
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
Next-Recommended-Task: TB-TMAR-SUPPORT-GOLDEN-001-R1

If complete:
Next-Recommended-Task: ARCHITECT_SELECT_NEXT_REOPENED_MODULE

After Result:
STOP completely.
Do NOT start another module.
Do NOT poll.
Do NOT fetch next task.
Do NOT write Worker IDLE.

END_TOOBA_TASK