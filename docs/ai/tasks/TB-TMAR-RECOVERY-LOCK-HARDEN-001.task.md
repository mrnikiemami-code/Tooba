PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-RECOVERY-LOCK-HARDEN-001
Parent-Task: TB-TMAR-WALLET-GOLDEN-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: FAST-SAFE
Track: RECOVERY_LOCK_HARDENING
Title: Canonical Recovery Sync + COMPLETE_REFERENCE_PATTERN Machine Lock Hardening
Backend-Only: YES

Why this task exists

Architect directly audited current repository after Wallet PASS and found the root cause of prior false COMPLETE claims:

ARCH-CQRS-001 only said NEW use-cases use CQRS, so legacy HTTP modules could still be called COMPLETE without MediatR conversion.

HOST-MODULE-ENDPOINT-001 documentation was correct in intent, but HostModuleEndpointOwnershipTests only guarded Offer routes. The test name sounded global but implementation was Offer-specific.

TOOBA-TMAR-MASTER-RECOVERY.md still contains older Host wording allowing HTTP transport/endpoint mapping ownership and stale module/next-task state.

TOOBA-REFERENCE-MODULE-PATTERN.md contains the correct module-owned Endpoints direction, but its CQRS completeness wording is not strict enough for all HTTP-owning COMPLETE modules.

Therefore this is a mandatory architecture/recovery closure before moving to Payment.

This task changes architecture docs/guards/recovery state only.
Do NOT refactor Payment/Promotion/Offer/Inventory in this task.

1. Canonical definition of COMPLETE_REFERENCE_PATTERN

Add a hard canonical rule to:

docs/architecture/TMAR-architecture-locks.md

docs/architecture/TOOBA-REFERENCE-MODULE-PATTERN.md

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

For an HTTP-owning module:

COMPLETE_REFERENCE_PATTERN requires ALL:

real physical Tooba.<Module>.Endpoints project

module owns its business HTTP routes

Host only maps module endpoint module / composition

Endpoint invokes Application use-case through ISender

real MediatR 12.5.0 Command/Query + Handler in Application

expected business outcomes use Result/SemanticError + centralized HTTP presentation

foreign module dependencies are Contracts/Gates/Events only

no foreign DbContext/cross-module SQL ownership

physical folder + namespace ownership verified

architecture guards enforce these properties

behavior preservation evidence exists

recovery SoT updated in the same accepted task cycle

For true internal-only modules:

Endpoints may be NOT_APPLICABLE

this must be explicitly proven and recorded

CQRS requirement applies to actual application use-case boundaries, not ceremonial empty handlers

internal-only status cannot be assumed merely because no endpoint project exists

Add a new explicit lock ID:
ARCH-COMPLETE-001

Suggested wording:
A module MUST NOT be marked COMPLETE_REFERENCE_PATTERN unless its declared HTTP applicability, endpoint ownership, CQRS/MediatR boundary, Result/error semantics, physical structure, cross-module contracts, Host authority, behavior preservation, and recovery state are all verified. Green build/tests alone are insufficient.

2. Strengthen CQRS lock

Replace/augment ARCH-CQRS-001 so it no longer protects only NEW code.

Canonical:

all NEW use-cases use CQRS + MediatR

additionally, any HTTP-owning module claiming COMPLETE_REFERENCE_PATTERN must have its HTTP use-cases behind real MediatR Commands/Queries/Handlers

no direct Directory/Application service invocation from module HTTP endpoints for business use-cases

no fake/ceremonial handlers

Keep:
ARCH-CQRS-002 = MediatR exactly 12.5.0.

3. Strengthen endpoint ownership lock

HOST-MODULE-ENDPOINT-001 must explicitly say:

For HTTP-owning COMPLETE modules:
Module.Endpoints -> ISender -> Module.Application

Host may:

register adapters/services

map Map<Module>...()

own global auth/session/tenant/middleware/platform endpoints

Host must NOT:

implement module-owned business routes

call module business directories directly from HTTP endpoints

own module-specific business response mapping/composers/query engines

Development-only bootstrap is separately allowlisted and does not count as endpoint/business ownership.

4. Fix stale Host wording

In MASTER and BOOTSTRAP, remove/supersede old wording that says Host may own generic HTTP transport/endpoint implementation in a way that conflicts with module-owned Endpoints.

Canonical Host target:

Host owns:

process startup / DI / composition root

middleware

global authentication/session/tenant/correlation

platform-level endpoints such as health/readiness

tiny security adapters for module Endpoints

explicit Development bootstrap allowlists

Module owns:

module HTTP route implementation

wire DTOs specific to that module

module success/failure presentation composition

business use-case dispatch

Use a clear SUPERSEDES_OLD_HOST_THIN_TRANSPORT note so future recovery cannot select the stale interpretation.

5. Canonical module recovery state

Update MASTER and BOOTSTRAP to current accepted truth.

Accepted COMPLETE:

Cart — TB-TMAR-CART-GOLDEN-001-R1, commit 35198728bf17381eaaec5db1e8033478675397fb

Settlement — TB-TMAR-SETTLEMENT-GOLDEN-001, commit f450523d08f1d42a00e28f8972a509bc202516b0

Fulfillment — TB-TMAR-FULFILLMENT-GOLDEN-001, commit 37180cc4df674e1269c1c103647ab5c96aacf64a

Returns — TB-TMAR-RETURNS-GOLDEN-001, commit 9b4bdedf0d2301c5455cf9c0af7d69f3b37039b5

Notification — TB-TMAR-NOTIFICATION-GOLDEN-001, commit 5c947708af5c66a3031786ccdfc34a726ec8746e

Support — TB-TMAR-SUPPORT-GOLDEN-001, commit b2d3e6f7df85750b5b5d9c42b19f3fa996ba5d91

Wallet — TB-TMAR-WALLET-GOLDEN-001, commit f81c11e9b21c4bb5e05385b253db28fdb1c62402

Remaining:

Payment — REOPENED_ENDPOINT_CQRS_OWNERSHIP

Promotion — REOPENED_ENDPOINT_CQRS_OWNERSHIP

Offer — NEEDS_FINAL_REVERIFY (Endpoints exists)

Inventory — NEEDS_APPLICABILITY_REVERIFY (may be internal-only)

Also preserve:

Checkout = PAUSED_AT_SAFE_W5_CHECKPOINT

Tax = UNTOUCHED in this current repair wave

Pricing = UNTOUCHED in this current repair wave

Frontend = BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE

Remove stale statements that currently claim Inventory/Promotion COMPLETE_REFERENCE_PATTERN as authoritative current state.
They may remain only in historical chronology clearly labeled HISTORICAL / SUPERSEDED.

Next task after this closure:
TB-TMAR-PAYMENT-GOLDEN-001

6. Add durable recovery-state file

Create:
docs/architecture/tmar-current-state.json

Purpose:
machine-readable recovery bootstrap.

Required fields:

program

executionMode

frontendFrozen

checkoutState

lastAcceptedTask

lastAcceptedCommit

nextTask

completeReferenceModules[]

reopenedModules[]

internalApplicabilityReviewModules[]

locksVersion / definition marker

recoveryPhrase

For each module entry include:

module

state

httpApplicability: HTTP_OWNING | INTERNAL_ONLY | TO_VERIFY

endpointOwnership: MODULE_ENDPOINTS | NOT_APPLICABLE | TO_VERIFY

cqrs: MEDIATR_12_5 | TO_VERIFY

lastAcceptedTask

lastAcceptedCommit

Current last accepted:
Wallet task/commit above.

Recovery phrase:
برگردیم به TMAR؛ TOOBA-TMAR-MASTER-RECOVERY.md و آخرین recovery-sot را مبنا بگیر.

Do not put secrets or machine-specific transient data in JSON.

7. Make recovery freshness a machine guard

Update TmarDurableGuardTests.cs to enforce:

tmar-current-state.json exists and parses

execution mode is BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE

current-state nextTask is not stale

MASTER and BOOTSTRAP contain the same next task

MASTER/BOOTSTRAP mention ARCH-COMPLETE-001

MASTER/BOOTSTRAP current state contains Wallet COMPLETE

stale authoritative Next TMAR task: TB-TMAR-NEXT-MODULE-BATCH-002 is absent

current-state complete list includes exactly current accepted golden modules in this wave:
Cart, Settlement, Fulfillment, Returns, Notification, Support, Wallet

current-state remaining state includes Payment, Promotion, Offer, Inventory with correct statuses

Do not write a brittle test that rejects historical mentions in chronology.
Only reject stale authoritative current/next state.

8. Fix HostModuleEndpointOwnershipTests to be genuinely durable

Current test is Offer-only. Replace/extend it so its scope matches its name and architecture lock.

At minimum for current COMPLETE HTTP-owning modules:

Cart

Settlement

Fulfillment

Returns

Notification

Support

Wallet

Offer only if currently endpoint-owned but NOT yet reverified; do not mark Offer complete here

For each accepted COMPLETE HTTP module verify:

real Endpoints .csproj exists

Host Program maps module endpoint mapping

no legacy Host module endpoint implementation file exists

Endpoints project does not reference Host or module Infrastructure

Endpoints project references Application

Endpoints production sources contain ISender usage for actual business routes

Application contains real MediatR IRequest/IRequestHandler use-case implementation

Host does not directly own the old known endpoint file paths

Keep module-specific guards too; this is a durable umbrella guard, not a replacement.

Do not hard-code only one Offer route ever again.

If generic detection is safer than route-by-route hardcoding, use a small explicit canonical manifest for COMPLETE modules rather than filename heuristics.

9. Reference module pattern correction

Update TOOBA-REFERENCE-MODULE-PATTERN.md:

Current wording:
Application: Ports/ (create UseCases/ only when real use-case files exist)

Must no longer imply that a COMPLETE HTTP module can have no CQRS use-case folders.

Canonical foldering:

Application/Commands/<UseCase>/

Application/Queries/<UseCase>/

Models/

Ports/

Errors/
as applicable

For an HTTP-owning COMPLETE module, there must be real Commands/Queries/Handlers for its HTTP business use-cases.

Do not require empty Commands or Queries folders where a module genuinely has only one side.

10. Recovery SoT for this task

Create:
docs/evidence/TB-TMAR-RECOVERY-LOCK-HARDEN-001/

Required:

recovery-start.md

root-cause-audit.md

complete-definition.md

endpoint-guard-audit.md

cqrs-guard-audit.md

recovery-state-sync.md

recovery-sot.md

Root-cause audit must explicitly record:

CQRS lock was NEW-only

Host endpoint umbrella guard was Offer-only

stale Host wording conflicted with newer module Endpoints pattern

stale recovery state allowed old COMPLETE claims to survive

fixes applied so this failure mode cannot silently recur

11. Validation

Run focused only:

HostModuleEndpointOwnershipTests

TmarDurableGuardTests

affected architecture test project(s)

final dotnet build src/backend/Tooba.slnx

Do not run broad Host suite.

12. Protected state

No production module behavior changes.
No frontend changes.
No Checkout changes.
No Tax/Pricing changes.
No Payment/Promotion/Offer/Inventory refactor in this task.

No reset.
No clean.
No force push.
No broad git add.
Stashes untouched.

13. Success criteria

PASS only if:

Recovery-State: CURRENT_AND_MACHINE_READABLE
Recovery-Next-Task: TB-TMAR-PAYMENT-GOLDEN-001
Architecture-Complete-Lock: ARCH-COMPLETE-001_ENFORCED
Architecture-CQRS-Lock: COMPLETE_HTTP_MODULES_MANDATORY
Architecture-Endpoint-Lock: MODULE_OWNED_MANDATORY
Host-Old-Thin-Transport-Interpretation: SUPERSEDED
HostModuleEndpointOwnershipTests: MULTI_MODULE_DURABLE
TmarDurableGuardTests: RECOVERY_FRESHNESS_ENFORCED
Wallet-State: COMPLETE_REFERENCE_PATTERN
Support-State: COMPLETE_REFERENCE_PATTERN
Notification-State: COMPLETE_REFERENCE_PATTERN
Returns-State: COMPLETE_REFERENCE_PATTERN
Fulfillment-State: COMPLETE_REFERENCE_PATTERN
Settlement-State: COMPLETE_REFERENCE_PATTERN
Cart-State: COMPLETE_REFERENCE_PATTERN
Payment-State: REOPENED_ENDPOINT_CQRS_OWNERSHIP
Promotion-State: REOPENED_ENDPOINT_CQRS_OWNERSHIP
Offer-State: NEEDS_FINAL_REVERIFY
Inventory-State: NEEDS_APPLICABILITY_REVERIFY
Checkout-State: PAUSED_AT_SAFE_W5_CHECKPOINT
Frontend-Production-Changes: NONE

14. Canonical Result

Return ONLY BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Track
Root-Cause-Audit
Recovery-State
Recovery-Next-Task
Architecture-Complete-Lock
Architecture-CQRS-Lock
Architecture-Endpoint-Lock
Host-Old-Thin-Transport-Interpretation
HostModuleEndpointOwnershipTests
TmarDurableGuardTests
Current-State-Manifest
Master-Recovery-State
Architect-Bootstrap-State
Reference-Pattern-State
Focused-Validation
Full-Validation
Residual-Defects
Wallet-State
Support-State
Notification-State
Returns-State
Fulfillment-State
Settlement-State
Cart-State
Payment-State
Promotion-State
Offer-State
Inventory-State
Checkout-State
Tax-State
Pricing-State
Frontend-Production-Changes
Git
Blockers
User-Work-Preserved
Next-Recommended-Task

Next-Recommended-Task:
TB-TMAR-PAYMENT-GOLDEN-001

After Result:
STOP completely.
Do NOT start Payment.
Do NOT poll.
Do NOT fetch next task.
Do NOT write Worker IDLE.

END_TOOBA_TASK