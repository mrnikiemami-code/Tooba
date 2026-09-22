PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-INVENTORY-APPLICABILITY-REVERIFY-001
Parent-Task: TB-TMAR-OFFER-FINAL-REVERIFY-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: FAST-SAFE
Track: INVENTORY_APPLICABILITY_REVERIFY
Title: Inventory HTTP Applicability + Microservice Boundary Final Reverification
Backend-Only: YES

Architect decision

Offer final reverify is accepted after direct repository verification.

Architect directly confirmed:

Offer is now in the canonical COMPLETE HTTP module manifest.

Tooba.Offer.Endpoints is real.

Offer Application has real Commands/Queries.

recovery SoT says Offer COMPLETE and Inventory is the only remaining module under applicability review.

nextTask is this exact Task-ID.

This task is Inventory-only.

The purpose is NOT to force an Endpoints project if Inventory is truly internal-only.
The purpose is to prove the real applicability and either:
A) close Inventory as INTERNAL_ONLY + COMPLETE_REFERENCE_PATTERN, or
B) discover real Inventory-owned HTTP and repair it properly.

No assumptions.

1. Current direct findings

Current Inventory module has:

Tooba.Inventory.Domain

Tooba.Inventory.Application

Tooba.Inventory.Contracts

Tooba.Inventory.Infrastructure

Tooba.Inventory.Tests

No Tooba.Inventory.Endpoints project currently exists.

No Host/Tooba.Host/Inventory/ directory currently exists.

Current architecture guard already verifies:

no foreign OfferDbContext

Contracts boundary

IClock/IIdGenerator/IModuleCallTracer usage

no TypeForwardedTo

no clock/id bypass

no silent catch

physical folder/namespace structure

seller inventory write returns Result

checkout release behavior remains idempotent

Current Contracts folders include:

Seller/

Checkout/

Orders/

Availability/

Errors/

Fulfillment/

Returns/

Inventory appears likely to be an internal participant exposed through module Contracts rather than its own HTTP surface, but this MUST be proven repo-wide.

2. Applicability audit — mandatory

Search the entire production repo for every Inventory-owned HTTP or transport surface.

At minimum search for:

/inventory

/stock

/availability

inventory-specific seller/admin/storefront routes

MapInventory...

InventoryEndpoints

direct Host calls to Inventory Application/Infrastructure from HTTP route handlers

direct Host InventoryDbContext

route handlers invoking Inventory directories/ports directly

seller inventory mutation routes currently owned by Offer or other module

checkout/order/fulfillment/returns inventory flows

background jobs/consumers

Classify every hit as:

INVENTORY_OWNED_HTTP

FOREIGN_MODULE_HTTP_USING_INVENTORY_CONTRACT

INTERNAL_APPLICATION_FLOW

MESSAGE_CONSUMER

BOOTSTRAP/DI_ONLY

TEST_ONLY

Do not infer ownership from naming alone.
Ownership follows business capability and route semantics.

3. Seller inventory route ownership — critical

Offer currently has seller inventory mutation through Offer HTTP use case.

Audit this carefully.

Determine whether:

the route is correctly Offer-owned because it mutates the inventory state of an Offer through Inventory.Contracts.Seller, OR

it is actually an Inventory capability incorrectly hosted under Offer.

Do not duplicate the same route into Inventory.Endpoints.

If current ownership is correct:
record explicit rationale:
Offer HTTP owns offer inventory mutation; Inventory is internal contract participant.

If incorrect:
return INCOMPLETE and create a concrete repair path.
Do not silently reassign route ownership.

4. Internal-only criteria

Inventory may be marked:
HTTP_APPLICABILITY = INTERNAL_ONLY

ONLY if ALL are proven:

no Inventory-owned public HTTP route exists

no Host Inventory business endpoint implementation exists

no direct Inventory HTTP presentation responsibility exists

external modules call Inventory through Contracts/Gates/Events only

seller/checkout/order/fulfillment/returns flows use Inventory Contracts or internal Application boundaries appropriately

no foreign module directly references Inventory.Infrastructure

no foreign DbContext use

no Host business composer acts as Inventory application layer

Inventory data ownership remains module-owned

Inventory can be extracted behind contracts/events without rewriting business logic

If any Inventory-owned HTTP exists, INTERNAL_ONLY is invalid.

5. CQRS applicability rule

Because Inventory may be internal-only, do NOT create ceremonial Commands/Queries just to satisfy a generic pattern.

Apply ARCH-COMPLETE-001 correctly:

For INTERNAL_ONLY:

no Endpoints project required

no fake HTTP CQRS required

actual application use-case boundaries must still be clean

public synchronous interactions must be through Contracts/Gates

internal application services/adapters may remain if cohesive

However:
if any actual Inventory-owned HTTP surface exists, then:
Inventory.Endpoints → ISender → Inventory.Application
becomes mandatory.

6. Cross-module dependency audit

Verify project references and production source usage.

Inventory may depend on foreign modules only through stable Contracts where required.

Specifically inspect:

Offer

Cart

Checkout

Order

Fulfillment

Returns

Promotion

Payment

Forbidden:

foreign Application

foreign Domain

foreign Infrastructure

foreign DbContext

Host business callback

If a foreign non-Contracts reference exists, repair it only if bounded and clearly Inventory-related.
Do not reopen the foreign module broadly.

7. Inventory Contracts quality

Audit all Inventory.Contracts surfaces for microservice extraction readiness.

For each folder:

Seller

Checkout

Orders

Availability

Fulfillment

Returns

Errors

Verify:

DTOs/interfaces do not expose EF/domain/internal types

no implementation logic

no Host types

no ASP.NET types

stable machine errors where expected

no internal persistence leakage

If an interface is actually application-internal and should not be public Contracts, explain and fix only if necessary.

8. Data ownership

Verify:

InventoryDbContext exists only in Inventory.Infrastructure

migrations belong to Inventory

no Host InventoryDbContext authority

no foreign DbContext joins into Inventory tables

no cross-module SQL joins/FKs introduced by current production code

If direct cross-module SQL is found:
INCOMPLETE unless safely repairable within this task.

9. Event/message boundaries

Audit Inventory event/message interactions.

Verify:

incoming/outgoing integration messages use stable Contracts/events

no direct foreign Application handler coupling

outbox/inbox pattern preserved

extraction to a separate service would only require transport replacement, not business rewrite

Do not redesign broker topology.

10. Result/error semantics

Inventory seller/public contract writes already return Result in current guard.

Verify all externally callable Inventory contract operations:

expected business failures use stable machine semantics

no localized prose classification

no Contains/StartsWith message mapping

unknown exceptions are not swallowed

no PlatformHttpException

no ex.Message leakage across contract boundaries

Do not invent new HTTP presentation for internal-only Inventory.

11. Time/id/tracing

Verify production Inventory orchestration uses:

IClock

IIdGenerator

IModuleCallTracer where cross-module calls apply

Forbidden:

DateTimeOffset.UtcNow

DateTime.UtcNow

Guid.NewGuid()

UuidV7.New()

raw StartActivity

hidden fallback constructors like ?? new SystemUtcClock()

Current architecture guard already checks much of this; keep it durable.

12. Physical structure

Verify final module structure remains:

Tooba.Inventory.Domain

Tooba.Inventory.Application

Tooba.Inventory.Contracts

Tooba.Inventory.Infrastructure

Tooba.Inventory.Tests

No Endpoints project if INTERNAL_ONLY.

Application existing folders:

Ports

Models

Checkout

Orders

Contracts:

Seller

Checkout

Orders

Availability

Errors

Fulfillment

Returns

Infrastructure:

Persistence

Directories

Adapters

Events

Messaging

DependencyInjection

No root dump.
Path↔namespace aligned.
No TypeForwardedTo.

If a real responsibility folder is missing, add it only when needed by real code.

13. Durable applicability guard

Extend InventoryArchitectureGuardTests with explicit HTTP applicability proof.

If INTERNAL_ONLY, add guards that fail if future code introduces Inventory-owned HTTP without architecture update.

Required checks:

Tooba.Inventory.Endpoints absent by declared design

no Host/Inventory business endpoints

no MapInventoryEndpoints

no production Inventory-owned route literals under Host/other modules

known seller inventory mutation remains owned by Offer endpoint and crosses via Inventory.Contracts

Inventory is not added to HostModuleEndpointOwnershipTests HTTP manifest

current recovery state declares:
httpApplicability = INTERNAL_ONLY
endpointOwnership = NOT_APPLICABLE
cqrs = INTERNAL_USE_CASE_BOUNDARIES

Do not write brittle global route regexes that flag unrelated uses of word "inventory".
Use explicit ownership checks.

14. Microservice extraction proof

Create a concrete extraction note proving how Inventory separates later:

Today:
foreign modules
→ Inventory.Contracts
→ Inventory.Application/Infrastructure
→ Inventory DB

Future:
foreign services
→ API/message adapter generated around same Contracts semantics
→ Inventory service Application/Infrastructure
→ Inventory DB

Identify:

synchronous contract surfaces

async event surfaces

database ownership

no Host dependency

no required rewrite

This is required for COMPLETE_REFERENCE_PATTERN INTERNAL_ONLY.

15. Focused validation

Run:

Inventory.Tests

InventoryArchitectureGuardTests

HostModuleEndpointOwnershipTests

TmarDurableGuardTests

final dotnet build src/backend/Tooba.slnx

Do NOT run:

broad Host suite

Checkout workflow

Tax/Pricing

frontend

No new unnecessary test matrix.

16. Recovery SoT — mandatory same cycle

If Inventory is proven INTERNAL_ONLY and clean:

Update:

docs/architecture/tmar-current-state.json

TOOBA-TMAR-MASTER-RECOVERY.md

TOOBA-ARCHITECT-BOOTSTRAP.md

task recovery-sot

Move Inventory from internalApplicabilityReviewModules
to a canonical complete/internal section.

Preferred machine-state entry:

module: Inventory
state: COMPLETE_REFERENCE_PATTERN
httpApplicability: INTERNAL_ONLY
endpointOwnership: NOT_APPLICABLE
cqrs: INTERNAL_USE_CASE_BOUNDARIES
lastAcceptedTask: TB-TMAR-INVENTORY-APPLICABILITY-REVERIFY-001
lastAcceptedCommit: accepted implementation/audit commit

Do NOT add Inventory to HostModuleEndpointOwnershipTests HTTP manifest.

Set nextTask:
TB-TMAR-GOLDEN-WAVE-FINAL-CLOSURE-001

If Inventory is NOT internal-only:

state must remain open

next task must be exact repair Task-ID

do not fake COMPLETE.

17. Evidence

Create:
docs/evidence/TB-TMAR-INVENTORY-APPLICABILITY-REVERIFY-001/

Required:

recovery-start.md

inventory-http-applicability-audit.md

inventory-route-ownership-audit.md

inventory-cross-module-contract-audit.md

inventory-contract-surface-audit.md

inventory-data-ownership-audit.md

inventory-event-boundary-audit.md

inventory-result-error-audit.md

inventory-physical-tree.md

inventory-microservice-extraction.md

architecture-guard-audit.md

recovery-state-sync.md

recovery-sot.md

18. Protected state

Protected COMPLETE:

Cart

Settlement

Fulfillment

Returns

Notification

Support

Wallet

Payment

Promotion

Offer

Checkout:
PAUSED_AT_SAFE_W5_CHECKPOINT

Tax/Pricing:
untouched in this wave

Frontend:
frozen

Do not modify completed modules except tiny guard/recovery manifest updates required by this task.

No reset.
No clean.
No force push.
No broad git add.
Stashes/user files untouched.

19. Success criteria — INTERNAL_ONLY path

PASS only if ALL:

Inventory-HTTP-Applicability: INTERNAL_ONLY
Inventory-Endpoint-Ownership: NOT_APPLICABLE
Inventory-Host-HTTP-Authority: NONE
Inventory-Host-Business-Authority: NONE
Inventory-Host-DbAuthority: NONE
Inventory-CrossModule-Boundary: CONTRACTS_ONLY
Inventory-Contracts-Surface: EXTRACTION_SAFE
Inventory-Data-Ownership: MODULE_OWNED
Inventory-Result-Semantics: STABLE
Inventory-Time-Id-Tracing: COMPLIANT
Inventory-Physical-State: VERIFIED_ON_DISK_AND_NAMESPACE
Inventory-Architecture-Guards: APPLICABILITY_ENFORCED
Inventory-Microservice-Extraction: READY_WITHOUT_REWRITE
Recovery-State: CURRENT_AND_MACHINE_READABLE
Recovery-Next-Task: TB-TMAR-GOLDEN-WAVE-FINAL-CLOSURE-001
Inventory-State: COMPLETE_REFERENCE_PATTERN

If HTTP-owning path is discovered, PASS is allowed only after proper Endpoints+CQRS repair; otherwise return INCOMPLETE.

20. Canonical Result

Return ONLY BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Track
Recovery-Start
Inventory-HTTP-Applicability
Inventory-Endpoint-Ownership
Inventory-Route-Ownership-Audit
Inventory-Host-HTTP-Authority
Inventory-Host-Business-Authority
Inventory-Host-DbAuthority
Inventory-CrossModule-Boundary
Inventory-Contracts-Surface
Inventory-Data-Ownership
Inventory-Event-Boundary
Inventory-Result-Semantics
Inventory-Time-Id-Tracing
Inventory-Physical-State
Inventory-Architecture-Guards
Inventory-Microservice-Extraction
Focused-Validation
Skipped-Validation
Full-Validation
AntiPattern-Gate
Residual-Defects
Inventory-State
Offer-State
Promotion-State
Payment-State
Wallet-State
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
Recovery-State
Recovery-Next-Task
Current-State-Manifest
Git
Blockers
User-Work-Preserved
Next-Recommended-Task

If clean internal-only:
Next-Recommended-Task: TB-TMAR-GOLDEN-WAVE-FINAL-CLOSURE-001

If repair needed:
Next-Recommended-Task: TB-TMAR-INVENTORY-APPLICABILITY-REVERIFY-001-R1

After Result:
STOP completely.
Do NOT start final closure.
Do NOT poll.
Do NOT fetch next task.
Do NOT write Worker IDLE.

END_TOOBA_TASK