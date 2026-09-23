PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-CART-HOST-RESIDUAL-REVERIFY-001
Parent-Task: TB-TMAR-CART-GOLDEN-001-R1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Track: CART_HOST_RESIDUAL_REVERIFY
Title: Remove Remaining Cart Business Authority from Host
Backend-Only: YES

Architect context

Cart is currently recorded as:
COMPLETE_REFERENCE_PATTERN

But Order reverse-audit exposed that old Golden closures can still leave business authority in Host.

This task performs a strict Host -> Cart reverse audit and removes ALL illegal Cart business authority from production Host.

Do NOT trust prior Cart COMPLETE status.
Current code is authoritative.

Important parallel-state note

Another independent task is currently in progress:

TB-TMAR-ORDER-POSTCLOSURE-STRUCTURE-LOCK-001

Do NOT touch or interfere with that task's files/changes.
Do NOT modify Order structure-lock work.
If overlap/conflict occurs:
RECOVERY_CONFLICT

Reference pattern

Use current hardened Order as reference for closure quality:

module owns endpoints

Endpoint -> ISender -> explicit Command/Query -> handler

real MediatR 12.5

FluentValidation only for transport/input validation

Result/SemanticError stable codes

module-owned Application/Infrastructure

foreign modules via Contracts/ports

Host only composition/auth/session/tenant/background execution shell

capability-based foldering + path/namespace alignment

Mandatory discovery — production Host only

Audit all:

src/backend/Host/Tooba.Host/**/*.cs

Exclude:

Tooba.Host.Tests

bin/obj/generated

Discover Cart relation using ALL of:

filenames containing Cart

type declarations containing Cart

method names containing Cart

/cart and /carts route registrations

Tooba.Cart.Application

Tooba.Cart.Infrastructure

Tooba.Cart.Domain

Tooba.Cart.Contracts

Tooba.Cart.Endpoints

ICart*

CartLifetime*

cart-related global usings

cart-related DI registrations

background workers

cross-module policies implementing Cart ports

Every discovered production Host occurrence must be classified exactly:

ILLEGAL_CART_AUTHORITY

ALLOWED_THIN_HOST_COMPOSITION

ALLOWED_HOST_EXECUTION_SHELL

NON_CART_HOST_CONCERN

DEAD_CART_RESIDUE

Do not hide findings under generic summaries.

Known verified Host residuals to inspect
1. CartExpiryHostedService.cs

Currently Host owns business reconciliation:

resolves ICartDirectory

calls ExpireDueCartsAsync

supplies DateTimeOffset.UtcNow

supplies batch size

performs Cart expiry orchestration per tenant

This is NOT acceptable as business authority in Host.

Target:

Host may retain only a thin background execution shell:

BackgroundService lifecycle

poll delay

tenant iteration

tenant context assignment

worker registry / telemetry / logging

cancellation

Cart module must own:

due-cart expiry reconciliation

current business time via IClock

batch reconciliation behavior

Cart directory call/business action

Create a Cart-owned boundary such as:
ICartExpiryReconciler.ReconcileAsync(batchSize, ct)

Exact naming may differ if Cart already has an equivalent use case.

Host worker should resolve ONE Cart-owned reconciler and call it.

Host worker must NOT directly resolve ICartDirectory.

2. CartExpiryHostOptions.cs

Classify carefully.

Host worker scheduling configuration may remain in Host if it contains only:

Enabled

PollIntervalSeconds

BatchSize execution/config knobs

Business lifetime/expiry policy must NOT live here.

Cart lifetime policy belongs to Cart.

3. GlobalUsings.CartSettlementApp.cs

Currently includes direct Cart Application/Infrastructure global usings:

Tooba.Cart.Application.Lifetime

Tooba.Cart.Application.Conversion

Tooba.Cart.Infrastructure.DependencyInjection

Audit every actual production use.

Goal:

remove broad Cart Application/Infrastructure global-usings from Host where they are not required

use explicit composition-only references where legitimately needed

Host business files must not gain accidental Cart internals through global using

Do NOT keep global imports just because old Host code compiled with them.

4. GlobalUsings.CartSettlementDomain.cs

Currently exposes Cart Domain globally into Host:

Tooba.Cart.Domain.Aggregates

Tooba.Cart.Domain.Entities

Tooba.Cart.Domain.Events

This is a major audit target.

Find every production Host caller that relies on these Cart Domain global usings.

For each:

if business logic -> move to Cart

if stale -> delete

if cross-module read -> use Cart.Contracts

Host should NOT globally import Cart Domain

PASS target:
No broad Cart.Domain global using in Host.

5. Program.cs Cart references

Audit these known registrations/usages:

using Tooba.Cart.Endpoints

AddCartEndpointPresentation()

MapCartEndpoints()

Cart Application assembly passed to AddToobaCqrsFoundation

CartLifetimeOptions binding

ICartPersistenceHoursSource binding through CommerceHoldPolicy

Cart module registration/composition

Classify each.

Allowed:

endpoint/module composition

MediatR assembly composition if canonical architecture still requires Host-level registration

host execution options

Potentially illegal:

business policy source implemented in Host

direct Cart Application policy ownership

duplicated Cart lifetime/hold decision in Host

CommerceHoldPolicy / CheckoutReservationHoldPolicy

Audit specifically:

Host/CommerceHoldPolicy.cs

Host/CheckoutReservationHoldPolicy.cs

related admin hold-policy endpoints/composers/settings

Determine whether Host currently owns Cart persistence-hours business policy.

If CommerceHoldPolicy implements:
Tooba.Cart.Application.Ports.ICartPersistenceHoursSource

and computes Cart persistence/lifetime decisions in Host, that is illegal Cart authority.

Target:

Cart persistence/lifetime rule must be Cart-owned

persisted source may belong to Catalog/settings module if that is the actual owner

bridge to Cart must be via Contracts/port

Host may compose interfaces only, not implement business policy

Do not break Order/Payment hold semantics; use owning module contracts.

Storefront / Admin / bootstrap audit

Search for Cart types used indirectly through old global usings.

At minimum inspect:

Storefront composers

checkout identity/hold helpers

Admin policy/settings surfaces

development bootstrap/seed files

background services

Program DI

Development seed/migration may remain only if explicitly classified and contains no runtime business authority.

Cart module target

If illegal Host authority exists, move it into the correct Cart project:

Application: use case/policy/ports

Infrastructure: persistence/integration implementation

Endpoints: HTTP ownership

Contracts: stable foreign module seam

Do not dump code into Cart root.
Follow capability foldering.

Potential capability homes:

Application/Lifetime

Infrastructure/Lifetime

Application/HoldPolicy

Infrastructure/...
depending on existing Cart structure.

Path ↔ namespace alignment required.

CQRS / Validation

If any Cart HTTP/business route is still Host-owned:

migrate to Cart.Endpoints

use ISender

explicit Command/Query

real handler

Result/SemanticError

validator for meaningful transport input

Do NOT introduce FluentValidation for business-state decisions.

If no Host Cart HTTP business route exists, do not invent new CQRS just for symmetry.

Time

Any moved Cart business timing must use:
IClock

Forbidden in Cart Application/business infrastructure:

DateTimeOffset.UtcNow

DateTime.UtcNow

Host worker shell may use framework timing only for poll delay, not business expiry time.

Foreign boundaries

Cart Application must not reference foreign:

Application

Infrastructure

Domain

Use Contracts/ports only.

No foreign DbContext.
No cross-schema SQL join.

Host final target

Production Host may retain Cart references ONLY when explicitly classified as:

ALLOWED_THIN_HOST_COMPOSITION

Examples:

AddCartEndpointPresentation

MapCartEndpoints

module registration

MediatR assembly registration if canonical foundation requires it

ALLOWED_HOST_EXECUTION_SHELL

Example:

CartExpiryHostedService after becoming a thin shell calling one Cart-owned reconciler

Host must NOT retain:

Cart business orchestration

direct ICartDirectory business use

Cart Domain global imports

Cart business policy implementation

direct Cart state decisions

Cart lifetime calculation

Cart persistence policy authority

Cart DbContext runtime authority

duplicate Cart routes

Global using cleanup target

PASS requires:

no global using Tooba.Cart.Domain.* in production Host

no unnecessary broad global using Tooba.Cart.Application.*

no unnecessary broad global using Tooba.Cart.Infrastructure.*

Prefer explicit imports in the few composition files that legitimately need them.

If the global-using file becomes Cart-empty, rename/delete appropriately without disturbing Settlement imports.

Required reverse-audit evidence

Create:

docs/evidence/TB-TMAR-CART-HOST-RESIDUAL-REVERIFY-001/cart-host-reverse-audit.md

Table columns:

Host file

Symbol/method

Cart dependency

Business decision?

Cart state read/write?

Classification

Action

Final owner

Why allowed if retained

Also include counts:

ILLEGAL_CART_AUTHORITY

DEAD_CART_RESIDUE

ALLOWED_THIN_HOST_COMPOSITION

ALLOWED_HOST_EXECUTION_SHELL

NON_CART_HOST_CONCERN

PASS requires:

ILLEGAL_CART_AUTHORITY = 0

DEAD_CART_RESIDUE = 0

Durable guard

Add/strengthen Cart architecture guard that scans production Host and prevents regression.

It must detect at minimum:

CartDbContext

Tooba.Cart.Domain

Tooba.Cart.Application business imports

Tooba.Cart.Infrastructure business imports

Cart-named Host files

cart route ownership

direct ICartDirectory use

cart-related global usings

Explicit allowlist only for legitimate composition/shell files.

Do not use broad wildcard allowlists.

Validation — only required tests

Run only tests materially required by touched scope:

Cart architecture/Host-boundary guards

Cart expiry/lifetime focused tests if expiry moved

hold-policy focused tests only if hold-policy ownership changes

Host reverse-audit guard

endpoint ownership guard if routes touched

dotnet build src/backend/Tooba.slnx

Do NOT run broad unrelated module suites.

If Order structure-lock files conflict/change:
RECOVERY_CONFLICT

SoT

On PASS:

Cart remains or is re-certified:
COMPLETE_REFERENCE_PATTERN

Record:
HOST_CART_ILLEGAL_AUTHORITY = 0

Do NOT mark Cart structure-certified under ARCH-COMPLETE-002 unless this task actually audits Application/Endpoints/Infrastructure structure against that standard.

Do not falsely certify.

Checkout remains:
PAUSED_AT_SAFE_W5_CHECKPOINT

Next task:
USER_REVIEW_CART_HOST_REVERIFY

Do NOT start another Cart task automatically.

PASS criteria

PASS only if:

exhaustive Host->Cart inventory completed

all illegal Cart business authority removed from Host

CartExpiry business reconciliation is Cart-owned

Host expiry worker is shell only

no Host global Cart.Domain imports remain

unnecessary Cart Application/Infrastructure global usings removed

CommerceHoldPolicy/Cart persistence policy ownership explicitly resolved

no direct runtime Cart DbContext authority in Host

no duplicate Cart route ownership in Host

Cart foreign boundaries remain Contracts/ports only

business time uses IClock

durable Host->Cart regression guard exists

focused tests pass

full current-head backend build passes

Order STRUCTURE-LOCK work untouched

frontend unchanged

Otherwise:
Status = INCOMPLETE

Canonical Result

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT

Task-ID: TB-TMAR-CART-HOST-RESIDUAL-REVERIFY-001
Parent-Task: TB-TMAR-CART-GOLDEN-001-R1
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT

Summary:
Host-Cart-Reverse-Audit-State:
Host-Cart-Files-Scanned:
CartExpiry-Ownership-State:
CartExpiry-Worker-State:
CommerceHoldPolicy-Cart-State:
Cart-Domain-GlobalUsings-State:
Cart-Application-GlobalUsings-State:
Cart-Infrastructure-GlobalUsings-State:
Program-Composition-State:
Cart-Route-Ownership-State:
Cart-DbAuthority-State:
Cart-Foreign-Boundary-State:
Clock-State:
Illegal-Cart-Authority-Count:
Dead-Cart-Residue-Count:
Allowed-Thin-Host-Composition:
Allowed-Host-Execution-Shell:
Architecture-Guard-Validation:
Focused-Validation:
Full-Validation:
Host-Removed-This-Task:
Host-Still-Remaining-For-Cart:
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

Do not start another Cart task.
Do not start Checkout W6.
Do not touch Order STRUCTURE-LOCK work.
Do not poll.
Wait for Architect verification.

END_TOOBA_TASK