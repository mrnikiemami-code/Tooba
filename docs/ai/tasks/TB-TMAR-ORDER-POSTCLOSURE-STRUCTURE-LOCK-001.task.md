PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-ORDER-POSTCLOSURE-STRUCTURE-LOCK-001
Parent-Task: TB-TMAR-ORDER-POSTCLOSURE-STRUCTURE-002
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Track: TMAR_COMPLETE_REFERENCE_STRUCTURE_LOCK
Title: Lock Capability Foldering + Namespace Rules into COMPLETE_REFERENCE_PATTERN
Backend-Only: YES
Implementation-Mode: ARCHITECTURE_LOCK_AND_GUARDS

Architect decision

Accepted:

Order Application structure hardening

Order Endpoints structure hardening

Order Infrastructure structure hardening

FluentValidation coverage hardening

Order remains:
COMPLETE_REFERENCE_PATTERN

This task performs NO business migration.

Its purpose is to make the structural rules durable so this problem cannot silently recur in Order or future TMAR module closures.

Locked architecture rule

A module cannot qualify as COMPLETE_REFERENCE_PATTERN merely because:

build passes

CQRS exists

endpoint ownership is correct

For HTTP-owning modules, complete-reference quality also requires coherent physical organization.

Application lock

Application MUST be capability-driven.

Rules:

capability-specific .cs files do not live at project root

Commands/Queries/Models/Ports/Policies/Services are grouped under their owning capability

path ↔ namespace alignment required

root is reserved only for explicitly justified module-wide shared abstractions

miscellaneous *Contracts.cs dumping at root is forbidden

MediatR validators live with the request or under an explicit Validation/shared rules capability

business validation must not be moved into FluentValidation

Endpoints lock

Endpoints MUST be capability-driven.

Rules:

capability *Endpoints.cs files do not live at project root

Admin/Customer/Seller/Storefront or equivalent capability grouping must be visible

path ↔ namespace alignment required

root may contain module endpoint composition entry only, plus explicitly shared folders such as Errors/Resources

module endpoint composition maps each capability exactly once

Host must not duplicate module route ownership

Infrastructure lock

Infrastructure MUST be capability/integration-driven.

Rules:

capability-specific implementation/bridge/service files do not live at project root

root is reserved for module composition entry only unless explicitly justified

persistence lives under Persistence

migrations stay under Persistence/Migrations

foreign adapters are grouped under a coherent Integrations/<Module> or owning capability path

the same integration must not be fragmented across competing top-level folders

path ↔ namespace alignment required

no namespace-alias workaround to hide physical organization debt

Important scope boundary

Do NOT mass-refactor all previously completed modules in this task.

This task locks the rule for:

Order immediately and durably

every future module that is newly declared/re-declared COMPLETE_REFERENCE_PATTERN

Existing completed modules are NOT automatically certified for this new structure lock until separately reverified.

Do not create a giant cleanup wave now.

Architecture version

Update architecture lock/version metadata.

Current:
locksVersion = ARCH-COMPLETE-001

Advance to:
ARCH-COMPLETE-002

Definition must explicitly include physical structure/folder/namespace quality.

Suggested semantic marker:
COMPLETE_REFERENCE_PATTERN_REQUIRES_ENDPOINTS_CQRS_RESULT_CONTRACTS_VALIDATION_CAPABILITY_STRUCTURE_GUARDS_SOT

Use exact naming consistently across SoT/docs/guards.

SoT structure-hardening state

Extend docs/architecture/tmar-current-state.json with a durable structure certification concept.

Preferred shape:

"structureLock": {
  "version": "ARCH-COMPLETE-002",
  "rules": [
    "APPLICATION_CAPABILITY_FOLDERS",
    "ENDPOINTS_CAPABILITY_FOLDERS",
    "INFRASTRUCTURE_CAPABILITY_INTEGRATION_FOLDERS",
    "PATH_NAMESPACE_ALIGNMENT",
    "ROOT_ALLOWLIST",
    "NO_NAMESPACE_ALIAS_WORKAROUND"
  ],
  "certifiedModules": [
    "Order"
  ]
}

Equivalent coherent schema is allowed.

Do NOT falsely mark other complete modules structure-certified without auditing them.

Durable Order guards

Preserve and treat these as architecture-lock guards:

OrderApplicationOrganizationGuardTests

OrderEndpointOrganizationGuardTests

OrderInfrastructureOrganizationGuardTests

OrderEndpointValidatorCoverageGuardTests

existing Host ownership/reverse-audit guards

Strengthen only if necessary so they cannot silently become stale.

At minimum, guards must fail if:

Application

a new .cs file is added to Application root without explicit allowlist

a moved capability file namespace no longer matches path

Endpoints

a new capability *Endpoints.cs is added to Endpoints root

root contains unexpected .cs

capability mapping is duplicated

path/namespace diverges

Infrastructure

a new capability implementation is added to root

forbidden duplicate top-level integration folder returns

path/namespace diverges

Persistence/Migrations moves unexpectedly

Reusable future-module gate

Create a reusable architecture helper/guard mechanism for future TMAR closure.

Do NOT hardcode a fragile scan over every historical module.

Preferred approach:

a reusable test/helper in Host.Tests or BuildingBlocks.Tests

accepts a module structure manifest

validates root allowlists, path/namespace alignment, and capability-root constraints

Order is registered as first certified manifest

Future module closure tasks can add their manifest and become certified.

The reusable mechanism must support at least:

Application root allowlist

Endpoints root allowlist where HTTP-owning

Infrastructure root allowlist

expected namespace prefix from relative path

ignored framework folders (bin, obj, migrations where appropriate)

Avoid a massive bespoke Order-only string list as the only enforcement layer.

Order-specific detailed guards may remain in addition.

Final closure template lock

Update TMAR architecture/recovery documentation so future final-closure tasks MUST include:

Application organization audit

Endpoints organization audit

Infrastructure organization audit

path↔namespace audit

root allowlist audit

FluentValidation coverage audit for MediatR transport requests

explicit NO_VALIDATOR_REQUIRED classification where applicable

A future module must not be marked COMPLETE_REFERENCE_PATTERN without these gates.

Documentation

Update:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/tmar-current-state.json

Create:

docs/architecture/TMAR-COMPLETE-REFERENCE-STRUCTURE-STANDARD.md

It must define:

Application standard

with examples

Endpoints standard

with examples

Infrastructure standard

with examples

Namespace standard
Root allowlist principle
Validation standard

input validation vs business validation

Certification process

how a module becomes structure-certified

Existing modules

state clearly:
Order certified now.
Other existing Complete modules require future reverify before claiming ARCH-COMPLETE-002 structure certification.

Evidence

Create:

docs/evidence/TB-TMAR-ORDER-POSTCLOSURE-STRUCTURE-LOCK-001/architecture-lock.md

Include:

previous gap

new lock version

rules locked

guards

reusable manifest mechanism

Order certification

explicit non-certification of unaudited modules

no business changes

Validation

Run:

full Tooba.Order.Tests

all Order organization guards

Order validator coverage guard

Host endpoint ownership/reverse-audit guards

Tmar durable guards

new reusable structure-lock tests

dotnet build src/backend/Tooba.slnx

No business changes

Production business logic changes are forbidden.

Allowed production changes:

none

Allowed changes:

tests/guards

architecture helper/test infrastructure

docs

SoT/lock metadata

If production business code must change:
Status = INCOMPLETE
STOP.

SoT final state

On PASS:

Order remains:
COMPLETE_REFERENCE_PATTERN

Order additionally becomes:
ARCH-COMPLETE-002 STRUCTURE_CERTIFIED

Checkout remains:
PAUSED_AT_SAFE_W5_CHECKPOINT

nextTask:
USER_REVIEW_ORDER_STRUCTURE_LOCK_COMPLETE

Do NOT start Checkout W6.
Do NOT start another module.

PASS criteria

PASS only if:

ARCH-COMPLETE-002 is documented and SoT-recorded.

Application capability-folder rule is locked.

Endpoints capability-folder rule is locked.

Infrastructure capability/integration-folder rule is locked.

path↔namespace alignment is locked.

root allowlist principle is locked.

FluentValidation transport coverage is part of future closure definition.

Order has durable regression guards.

reusable future-module structure gate exists.

Order alone is certified unless another module was actually audited.

no business production code changed.

full current-head backend build passes.

Checkout W6 not started.

frontend unchanged.

Canonical Result

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT

Task-ID: TB-TMAR-ORDER-POSTCLOSURE-STRUCTURE-LOCK-001
Parent-Task: TB-TMAR-ORDER-POSTCLOSURE-STRUCTURE-002
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT

Summary:
Architecture-Lock-Version:
Definition-Marker-State:
Application-Structure-Lock:
Endpoints-Structure-Lock:
Infrastructure-Structure-Lock:
Namespace-Lock:
Root-Allowlist-Lock:
Validation-Coverage-Lock:
Reusable-Structure-Gate-State:
Order-Structure-Certification:
Other-Modules-Certification-State:
Order-Guard-State:
Documentation-State:
SoT-State:
Production-Business-Changes:
Architecture-Guard-Validation:
Focused-Validation:
Full-Validation:
Host-Authority-State:
Order-Final-State:
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

Do not start Checkout W6.
Do not start another module.
Do not begin mass structure cleanup of other modules.
Do not poll.
Wait for Architect verification and user review.

END_TOOBA_TASK