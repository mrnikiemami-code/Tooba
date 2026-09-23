PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-CART-ARCH-COMPLETE-002-REVERIFY-001
Parent-Task: TB-TMAR-CART-HOST-RESIDUAL-REVERIFY-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Track: CART_ARCH_COMPLETE_002_CERTIFICATION
Title: Cart Structure + Validation Reverify and ARCH-COMPLETE-002 Certification
Backend-Only: YES

Architect verdict before task

Accepted:

Cart Host reverse-audit

HOST_CART_ILLEGAL_AUTHORITY = 0

Cart expiry reconciliation is Cart-owned

Cart persistence-hours policy is Cart-owned

Host global Cart Domain/Application/Infrastructure imports removed

Cart remains COMPLETE_REFERENCE_PATTERN

Current gap:
Cart is NOT yet ARCH-COMPLETE-002 STRUCTURE_CERTIFIED.

Verified current shape is already mostly coherent, but:

structure has not been certified against the new reusable gate

Cart has no FluentValidation validators today

endpoint-reachable request validation must be classified and completed

Reference standard

Use:
docs/architecture/TMAR-COMPLETE-REFERENCE-STRUCTURE-STANDARD.md

Lock:
ARCH-COMPLETE-002

Definition marker:
COMPLETE_REFERENCE_PATTERN_REQUIRES_ENDPOINTS_CQRS_RESULT_CONTRACTS_VALIDATION_CAPABILITY_STRUCTURE_GUARDS_SOT

Do not weaken the standard for Cart.

Scope

Exactly two responsibilities:

Reverify/correct Cart physical structure against ARCH-COMPLETE-002.

Complete FluentValidation coverage for endpoint-reachable Cart MediatR requests.

Then, only if both pass, certify Cart in the reusable structure manifest.

Do NOT redesign business logic, reopen Host ownership, change routes, change DB schema/migrations, touch frontend, resume Checkout W6, or modify Order production code.

Current verified Cart structure

Application:

Commands/{AddCartLine,ChangeCartLineQuantity,CreateGuestCart,MergeCartAfterLogin,RemoveCartLine}

Conversion

Errors

Lifetime

Models

Ports

Presentation

Queries/{GetCart,GetCurrentCart}

GlobalUsings.Domain.cs

GlobalUsings.Layout.cs

Endpoints:

Storefront/CartStorefrontEndpoints.cs

Errors/

Resources/

CartEndpointModule.cs

Infrastructure:

DependencyInjection/CartModule.cs

Directories/

Events/

Lifetime/

Messaging/

Persistence/{CartDbContext.cs,Migrations/}

Security/

GlobalUsings.Domain.cs

GlobalUsings.Layout.cs

This is broadly acceptable for a small single-capability module. Do NOT create artificial nested folders merely to imitate Order.

Structure audit

Audit every production .cs in:

Tooba.Cart.Application

Tooba.Cart.Endpoints

Tooba.Cart.Infrastructure

Application root

GlobalUsings.Domain.cs and GlobalUsings.Layout.cs may remain only if genuinely project-wide shared imports.
If retained:

explicitly allowlist them

document why they are not capability-specific

ensure they do not hide forbidden foreign module Application/Infrastructure/Domain dependencies

No other root .cs.

Endpoints root

Expected root allowlist:

CartEndpointModule.cs

Errors/Resources are shared.
Storefront/CartStorefrontEndpoints.cs must remain path↔namespace aligned.

Infrastructure root

GlobalUsings.Domain.cs and GlobalUsings.Layout.cs may remain only if genuinely project-wide.
DependencyInjection/CartModule.cs may remain in its existing coherent DI folder.
Do not move it to root merely for uniformity.
No capability implementation at Infrastructure root.

Namespace

Audit all namespace-bearing sources.
Global using files are exempt because they declare no namespace.
Migrations stay under Persistence/Migrations.

Foreign-boundary audit

Cart Application must not depend on foreign:

Application

Infrastructure

Domain

Allowed:

Contracts

BuildingBlocks

No foreign DbContext.
No Host dependency.

Endpoint-reachable request inventory

Confirm exact current endpoint inventory:

CreateGuestCartCommand

GetCurrentAuthenticatedCartQuery

GetCartQuery

MergeCartAfterLoginCommand

AddCartLineCommand

ChangeCartLineQuantityCommand

RemoveCartLineCommand

Classify every request:

VALIDATOR_REQUIRED

NO_VALIDATOR_REQUIRED

No request may be unclassified.

Required validation semantics

Reuse canonical shared pipeline:
MediatR
→ ValidationBehavior
→ FluentValidation
→ ValidationException
→ SafeErrorMapper
→ validation.failed + stable field error codes

Do NOT create a Cart-specific pipeline.

Expected classification

Likely NO_VALIDATOR_REQUIRED:

CreateGuestCartCommand — parameterless

GetCurrentAuthenticatedCartQuery — parameterless; authentication is business/security behavior

MergeCartAfterLoginCommand — inputs intentionally optional; authenticated-user requirement is business/security validation

VALIDATOR_REQUIRED:

GetCartQuery — CartId non-empty

AddCartLineCommand — CartId, OfferId, ExpectedVersion >= 0; quantity only syntactic if existing API/domain contract supports it

ChangeCartLineQuantityCommand — CartId, LineId, ExpectedVersion >= 0; preserve zero quantity semantics if zero removes line

RemoveCartLineCommand — CartId, LineId, ExpectedVersion >= 0

GuestSecret remains optional unless an existing explicit max-length contract exists.

Business validation exclusion

FluentValidation must NOT perform:

authentication

cart existence

ownership/access

guest-secret correctness

persisted version conflict

offer existence

stock/inventory

merge eligibility

active-cart state

campaign eligibility

quantity rules requiring domain/catalog state

Stable codes

Create:
Tooba.Cart.Application.Validation.CartValidationCodes

Use stable machine codes, e.g.:

cart.validation.cart_id_required

cart.validation.offer_id_required

cart.validation.line_id_required

cart.validation.expected_version_min

No raw/localized message identity.

Validator location

Place validators with requests:

Commands/AddCartLine/AddCartLineCommandValidator.cs

Commands/ChangeCartLineQuantity/ChangeCartLineQuantityCommandValidator.cs

Commands/RemoveCartLine/RemoveCartLineCommandValidator.cs

Queries/GetCart/GetCartQueryValidator.cs

Shared syntactic rules may live under Application/Validation/.
No empty validators.

Endpoint expected-version logic

Do NOT redesign TryReadExpectedVersion.
Endpoint parsing may remain presentation-level.
The MediatR validator must still validate the resulting integer domain, e.g. non-negative.

Durable validator coverage guard

Add a Cart guard that:

scans Cart.Endpoints recursively

discovers every constructed *Command / *Query

compares them to an explicit classification manifest

verifies every VALIDATOR_REQUIRED request resolves IValidator<TRequest> through actual AddToobaCqrsFoundation

rejects representative-validator substitution

rejects new endpoint requests without classification

ARCH-COMPLETE-002 manifest

On successful audit, update:
docs/architecture/tmar-module-structure-manifests.json

Add Cart:

structureCertified: true

lockVersion: ARCH-COMPLETE-002

explicit root allowlists for Cart.Application, Cart.Endpoints, Cart.Infrastructure

Update:
docs/architecture/tmar-current-state.json

structureLock.certifiedModules becomes exactly:

Order

Cart

Remove Cart from uncertified HTTP-owning modules.
Do NOT certify any other module.

Evidence

Create:
docs/evidence/TB-TMAR-CART-ARCH-COMPLETE-002-REVERIFY-001/cart-structure-validation-certification.md

Include:

exact Application root files + classification

exact Endpoints root allowlist

exact Infrastructure root files + classification

path↔namespace result

foreign boundary result

complete endpoint-request validation table

validators added

NO_VALIDATOR_REQUIRED rows + reason

manifest certification details

Validation — MINIMUM REQUIRED ONLY

Run:

Cart architecture guard(s)

Cart validator coverage guard

reusable TmarCompleteReferenceStructureGateTests

TmarDurableGuardTests

focused validation short-circuit tests

dotnet build src/backend/Tooba.slnx

Do NOT run broad unrelated Host/module suites.

If production business behavior must change:
Status = INCOMPLETE
STOP.

Production-code change allowance

Allowed:

Cart FluentValidation validators

Cart stable validation codes/shared syntactic rules

structure-only namespace/folder correction if a real violation exists

Forbidden:

handler/business logic redesign

DB schema changes

Host authority changes

route changes

PASS criteria

PASS only if:

all Cart Application/Endpoints/Infrastructure files audited

root allowlists explicit and justified

path↔namespace alignment passes

no structure debt hidden by global usings

Cart Application foreign boundaries clean

all endpoint-reachable requests classified

every VALIDATOR_REQUIRED request has concrete DI-resolvable validator

no empty validators

business validation stays outside FluentValidation

reusable ARCH-COMPLETE-002 manifest includes Cart

structureLock.certifiedModules includes exactly Order + Cart

no other module falsely certified

Cart remains COMPLETE_REFERENCE_PATTERN

Host Cart illegal authority remains 0

full current-head backend build passes

frontend unchanged

Checkout W6 not started

SoT final state

Cart:
COMPLETE_REFERENCE_PATTERN + ARCH-COMPLETE-002 STRUCTURE_CERTIFIED

Host:
HOST_CART_ILLEGAL_AUTHORITY = 0

Order:
preserve existing ARCH-COMPLETE-002 STRUCTURE_CERTIFIED

Checkout:
PAUSED_AT_SAFE_W5_CHECKPOINT

Next task:
USER_REVIEW_CART_ARCH_COMPLETE_002

Do NOT start another module automatically.

Canonical Result

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT

Task-ID: TB-TMAR-CART-ARCH-COMPLETE-002-REVERIFY-001
Parent-Task: TB-TMAR-CART-HOST-RESIDUAL-REVERIFY-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT

Summary:
Cart-Structure-Audit-State:
Application-Root-Allowlist:
Endpoints-Root-Allowlist:
Infrastructure-Root-Allowlist:
Path-Namespace-State:
Foreign-Boundary-State:
Endpoint-Reachable-Requests:
Validator-Required-Count:
No-Validator-Required-Count:
Validators-Added:
Validation-Error-Semantics:
Business-Validation-Separation:
Validator-Coverage-Guard:
Reusable-Structure-Gate-State:
Cart-Structure-Certification:
Other-Modules-Certification-State:
Host-Cart-Illegal-Authority:
Architecture-Guard-Validation:
Focused-Validation:
Full-Validation:
Production-Business-Changes:
Cart-Final-State:
Order-Structure-Lock-Preserved:
Checkout-State:
Frontend-Production-Changes:
Residual-Defects:
Git:
User-Work-Preserved:
Next-Recommended-Task:

END_TOOBA_WORKER_RESULT

STOP

After Result:
STOP completely.
Do not start another module.
Do not resume Checkout W6.
Do not poll.
Wait for Architect verification.

END_TOOBA_TASK