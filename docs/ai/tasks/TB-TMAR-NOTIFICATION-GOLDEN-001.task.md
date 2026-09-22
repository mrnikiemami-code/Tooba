PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-NOTIFICATION-GOLDEN-001
Parent-Task: TB-TMAR-RETURNS-GOLDEN-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: FAST-SAFE
Track: NOTIFICATION_GOLDEN_CLOSURE
Title: Notification Endpoints Ownership + MediatR CQRS + Host Cleanup Golden Closure
Backend-Only: YES

Architect decision

Returns is accepted after direct repo verification. This task is Notification-only.

Golden target:
Tooba.Notification.Endpoints
→ ISender
→ Notification.Application Commands/Queries/Handlers
→ Result/SemanticError
→ Contracts-only foreign boundaries
→ Infrastructure

Host = composition/security adapters only.

Direct findings

Notification has Domain/Application/Contracts/Infrastructure/Tests but NO Endpoints project.

Current HTTP ownership:
src/backend/Host/Tooba.Host/Notifications/NotificationEndpoints.cs

Routes:
Customer:

GET /v1/customer/notifications/

GET /v1/customer/notifications/unread-count

POST /v1/customer/notifications/{id:guid}/read

POST /v1/customer/notifications/read-all

DELETE /v1/customer/notifications/{id:guid}

Seller:

GET /v1/seller/notifications/

GET /v1/seller/notifications/unread-count

POST /v1/seller/notifications/{id:guid}/read

POST /v1/seller/notifications/read-all

DELETE /v1/seller/notifications/{id:guid}

Host endpoint directly owns:

INotificationDirectory calls

NotificationRecipientQuery construction

list response projection

customer actor/dev actor resolution

SellerPanelAccess

manual unauthorized/seller error JSON

Notification.Application currently has Models/Ports/Rendering but no real Commands/Queries MediatR surface.

Required end state

Create:
src/backend/Modules/Notification/Tooba.Notification.Endpoints/

Flow:
Endpoints → ISender → Application CQRS → Result → Ports → Infrastructure

Delete:
Host/Notifications/NotificationEndpoints.cs

Preferred:
no production Host/Notifications/ directory remains.

Endpoints project

Create Tooba.Notification.Endpoints, add to src/backend/Tooba.slnx.

Allowed refs:

Notification.Application

Notification.Contracts if needed

BuildingBlocks presentation/security

Forbidden:

Host

Notification.Infrastructure

foreign Application/Infrastructure

DbContext

Folders:

Customer/

Seller/

Errors/Resources only if needed

CQRS

Create real MediatR 12.5.0 use cases.

Queries:

ListCustomerNotifications

GetCustomerUnreadNotificationCount

ListSellerNotifications

GetSellerUnreadNotificationCount

Commands:

MarkCustomerNotificationRead

MarkAllCustomerNotificationsRead

DismissCustomerNotification

MarkSellerNotificationRead

MarkAllSellerNotificationsRead

DismissSellerNotification

Use one cohesive folder per use case:
Commands/<UseCase>/...
Queries/<UseCase>/...

Each route must call ISender.
Register Notification Application assembly in AddToobaCqrsFoundation.
No giant handler files.

Response ownership

Move current Host list projection behind typed Application response models.
Preserve exact JSON:
items[].notificationId/type/category/title/body/targetRoute/isRead/createdAt
skip/take/totalCount/unreadCount

Endpoint only parses transport, resolves actor, sends request, maps Result.

Customer actor boundary

Preserve current behavior:

authenticated session → UserId

Development/Testing header X-Tooba-Dev-Actor-User-Id

dev/testing fallback to current Storefront guest actor behavior

production unauthenticated → customer.session.required 401

BUT Notification.Endpoints/Application must not reference:

CurrentAuthenticatedSession

StorefrontCheckoutComposer

Host namespace

Introduce/reuse minimal neutral INotificationCustomerAuthorizer/actor resolver.
Host adapter = security/session/testing compatibility only.
No DbContext, NotificationDirectory, business logic, or manual error mapping.

Seller auth boundary

Introduce/reuse INotificationSellerAuthorizer.
Host implementation = security-only.
Pass sellerPartyId/actor explicitly to CQRS.
No Host reference from module.

Result/error semantics

Expected failures use:

Result / Result<T>

SemanticError

ApiResponseFactory

centralized error catalog/localization

Preserve:

customer.session.required

seller auth stable codes/status

mark-read/delete 204 vs 404 behavior

mark-all count

list/unread success shapes

Forbidden:

manual {title,errorCode}

ex.Message

PlatformHttpException as Notification business outcome

Contains/StartsWith/prose classification

unknown InvalidOperationException swallowed

Unknown exceptions propagate.

Cross-module boundaries

Preserve Contracts-only refs to:

Payment.Contracts

Fulfillment.Contracts

Returns.Contracts

Order.Contracts

No foreign Application/Domain/Infrastructure or DbContexts.

Physical structure

Must match Cart/Settlement/Fulfillment/Returns quality.

Projects:

Domain

Application

Contracts

Infrastructure

Endpoints

Tests

Application:

Commands/<UseCase>/

Queries/<UseCase>/

Models/

Ports/

Rendering/

Errors/ if needed

Endpoints:

Customer/

Seller/

No root dump.
Path↔namespace aligned.
No TypeForwardedTo/namespace masquerading.

Host cleanup

After task:

Host/Notifications/NotificationEndpoints.cs absent

preferably Host/Notifications absent

no INotificationDirectory in Host HTTP behavior

no NotificationRecipientQuery in Host

no list projection in Host

no manual notification error mapper in Host

no NotificationDbContext Host business/query authority

Program may only register tiny auth adapters + call MapNotificationEndpoints().

Behavior preservation

Preserve:

customer/seller paging

locale fallback fa

unread count

mark one read

mark all count

dismiss/soft-delete

204/404

recipient scoping

customer ownership

seller party scope

dev/testing actor compatibility

production unauthorized behavior

rendering/copy

event/outbox behavior

Accidental behavior change = 0.

Architecture guards

Upgrade NotificationArchitectureGuardTests to enforce:

Endpoints project exists

path↔namespace

Application ref only; no Host/Infra

no foreign Application/Infrastructure

no DbContext

Endpoints use ISender + ApiResponseFactory

no direct INotificationDirectory in Endpoints

real MediatR Commands/Queries/Handlers

use-case folders

no Host NotificationEndpoints

no Host notification projection/business authority

no message/prose heuristics

no TypeForwardedTo

no clock/id bypass

no hidden DI fallback

no silent catch

no raw StartActivity

no localized exception prose

Focused validation

Required:

Notification.Tests

customer list/paging/locale

customer unread

customer mark read/read-all/dismiss

seller equivalent paths

204/404

production unauthorized

dev/testing actor compatibility

Result/error semantics

architecture guards

dotnet build src/backend/Tooba.slnx

Skip:

broad Host suite

Checkout

Tax/Pricing

frontend

unrelated modules

Evidence

Create:
docs/evidence/TB-TMAR-NOTIFICATION-GOLDEN-001/

Required:

recovery-start.md

notification-http-ownership-audit.md

notification-cqrs-audit.md

notification-auth-boundary.md

notification-host-authority-audit.md

notification-response-contract-audit.md

notification-error-semantics-audit.md

notification-physical-tree.md

behavior-preservation-audit.md

architecture-guard-audit.md

recovery-sot.md

Physical tree lists every handwritten Notification production .cs:
path | namespace | responsibility

Protected state

Cart/Settlement/Fulfillment/Returns remain COMPLETE.
Checkout remains PAUSED_AT_SAFE_W5_CHECKPOINT.
Tax/Pricing/frontend untouched.
Do NOT start Support/Wallet/Payment/Promotion.

No reset/clean/force push/broad git add.
Preserve stashes/user files.

Completion scan

Before PASS scan entire repo for:

NotificationEndpoints

INotificationDirectory usage in Host HTTP

NotificationDbContext in Host

/notifications

NotificationRecipientQuery in Host

CustomerUnauthorized

SellerError

MapListResponse

Notification Application ports implemented in Host

manual Notification error mapping

exception message heuristics

Any production ownership leak => INCOMPLETE.

Success criteria

PASS only if ALL:

Notification-HTTP-Ownership: MODULE_ENDPOINTS
Notification-Endpoints-State: REAL_PROJECT_PRESENT
Notification-CQRS-State: MEDIATR_12_5_APPLICATION_HANDLERS
Notification-Host-Endpoints: REMOVED
Notification-Host-Business-Authority: NONE
Notification-Host-DbAuthority: NONE
Notification-CrossModule-Boundary: CONTRACTS_ONLY
Notification-Result-Adoption: HTTP_USE_CASES_ADOPTED
Notification-Error-Classification: STABLE_CODES_ONLY
Notification-Prose-Mapping: NONE
Notification-Unexpected-Exception-Swallow: NONE
Notification-Physical-State: VERIFIED_ON_DISK_AND_NAMESPACE
Notification-Architecture-Guards: ENFORCED
Notification-Behavior-Preservation: VERIFIED
Notification-State: COMPLETE_REFERENCE_PATTERN

Canonical Result

Return ONLY BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Track
Recovery-Start
Notification-HTTP-Ownership-Audit
Notification-Endpoints-State
Notification-CQRS-State
Notification-Application-UseCases
Notification-Auth-Boundary
Notification-Response-Contract
Notification-Result-Adoption
Notification-Error-Classification
Notification-Prose-Mapping
Notification-Unexpected-Exception-Swallow
Notification-Host-Authority-Audit
Notification-Host-Endpoints
Notification-Host-Business-Authority
Notification-Host-DbAuthority
Notification-CrossModule-Boundary
Notification-Physical-State
Notification-Architecture-Guards
Notification-Behavior-Preservation
Focused-Validation
Skipped-Validation
Full-Validation
AntiPattern-Gate
Residual-Defects
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
Next-Recommended-Task: TB-TMAR-NOTIFICATION-GOLDEN-001-R1

If complete:
Next-Recommended-Task: ARCHITECT_SELECT_NEXT_REOPENED_MODULE

After Result:
STOP completely.
Do NOT start another module.
Do NOT poll.
Do NOT fetch next task.
Do NOT write Worker IDLE.

END_TOOBA_TASK