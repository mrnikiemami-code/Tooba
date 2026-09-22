PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-OFFER-FINAL-REVERIFY-001
Parent-Task: TB-TMAR-PROMOTION-GOLDEN-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: FAST-SAFE
Track: OFFER_FINAL_REVERIFY
Title: Offer Final Reverify + Price/Inventory CQRS Closure + COMPLETE Lock Admission
Backend-Only: YES

Architect decision

Promotion is accepted after direct repository verification.

Architect directly confirmed on main:

Tooba.Promotion.Endpoints exists.

Host Promotion endpoints/composer are absent.

real Promotion Commands/Queries exist.

Promotion is in the durable COMPLETE HTTP manifest.

tmar-current-state.json records Promotion COMPLETE.

next task is this exact Offer final reverify task.

This task is Offer-only.

Offer is NOT automatically COMPLETE merely because it already has Endpoints and CQRS.
It must satisfy the hardened ARCH-COMPLETE-001 definition exactly.

Do NOT start Inventory.

0. Direct Offer audit findings

Offer is already substantially ahead of the reopened modules:

Projects exist:

Tooba.Offer.Domain

Tooba.Offer.Application

Tooba.Offer.Contracts

Tooba.Offer.Infrastructure

Tooba.Offer.Endpoints

Tooba.Offer.Tests

Current positives verified:

Tooba.Offer.Endpoints is real.

Host seller endpoint no longer maps Offer routes.

Host calls MapOfferModule().

seller create/update/list/get use ISender/MediatR.

real Commands/Queries folders exist.

Application/Infrastructure foreign refs are Contracts-only.

Offer.Contracts exposes IOfferQueryGateway for external reads.

Result pattern/ApiResponseFactory guards exist.

physical structure guards exist.

no current TypeForwarders file exists.

OfferStatus and SalesChannel are physically in Tooba.Offer.Contracts.Dtos.

However, one direct architectural defect remains under the hardened COMPLETE rule.

1. Critical defect: price/inventory HTTP routes bypass Application CQRS

Directly verified in:
Tooba.Offer.Endpoints/Seller/OfferSellerEndpoints.cs

Routes:

POST/PUT /v1/seller/offers/{offerId:guid}/price

POST/PUT /v1/seller/offers/{offerId:guid}/inventory

currently do this:

Endpoint
→ ISellerOfferPricingGateway.SetPriceAsync(...)
→ then ISender(GetOfferQuery)

and:

Endpoint
→ ISellerOfferInventoryGateway.SetInventoryAsync(...)
→ then ISender(GetOfferQuery)

This violates the hardened canonical invariant:

HTTP-owning COMPLETE module:
Module.Endpoints → ISender → Module.Application

Endpoints must not own business mutation orchestration by invoking foreign/module gateways directly.

Therefore Offer cannot be admitted to COMPLETE_REFERENCE_PATTERN until these two routes are closed through real Application commands.

2. Required final flow

For every Offer business HTTP route:

Offer.Endpoints
→ ISender
→ Offer.Application
→ Offer/foreign Contracts ports
→ Infrastructure / external module implementation

Host:

security/composition only.

No business gateway invocation directly from Endpoints.

3. Create SetOfferPrice command

Create cohesive use-case folder:

Tooba.Offer.Application/Commands/SetOfferPrice/

Required:

SetOfferPriceCommand

SetOfferPriceCommandHandler

Inputs preserve current transport behavior:

offerId

sellerPartyId

amount

currency

market

Handler responsibilities:

invoke existing Pricing contract gateway (ISellerOfferPricingGateway or current stable contract)

preserve exact stable Result semantics

after successful write, return the current Offer response expected by existing HTTP contract, either:

compose/read via Offer Application read port/query service, or

return a result that endpoint can obtain without a second business gateway call.

Preferred:
one command returns the final response model needed by HTTP.

Do NOT create command handler that simply returns void and leaves orchestration in Endpoint.

Endpoint price route must become:

authorize

sender.Send(new SetOfferPriceCommand(...))

api.From(result)

No direct pricing gateway in Endpoint.

4. Create SetOfferInventory command

Create:

Tooba.Offer.Application/Commands/SetOfferInventory/

Required:

SetOfferInventoryCommand

SetOfferInventoryCommandHandler

Inputs preserve:

offerId

sellerPartyId

onHand

reason

Handler:

invokes current Inventory contract gateway

preserves stable Result semantics

returns final Offer response expected by current route

Endpoint inventory route becomes:

authorize

ISender

ApiResponseFactory

No direct inventory gateway in Endpoint.

5. Remove foreign business gateways from Endpoints

After closure, OfferSellerEndpoints.cs must NOT inject/use:

ISellerOfferPricingGateway

ISellerOfferInventoryGateway

If no longer needed by any Endpoints source, remove direct project references from Tooba.Offer.Endpoints.csproj to:

Inventory.Contracts

Pricing.Contracts

Keep them in Application where the actual use-case owns the dependency.

Endpoint project target refs should be only what transport genuinely needs:

Offer.Application

Offer.Contracts if wire DTOs/contracts genuinely used

BuildingBlocks

ASP.NET framework

This is important for future extraction:
HTTP assembly should not orchestrate neighboring services itself.

6. Preserve exact route/API behavior

Do NOT change:

/v1/seller/offers

create/list/get/update behavior

POST/PUT dual verbs for price

POST/PUT dual verbs for inventory

request DTO shapes

seller authorization behavior

response shape

error status/code behavior

price/inventory mutation behavior

Offer re-read behavior after successful mutation

No first-item shortcut.
No cached/stale response substitution.

7. Reverify all Offer CQRS use cases

Directly verify all Offer seller HTTP routes map to real MediatR requests.

Expected HTTP use cases include at minimum:
Queries:

ListSellerOffers

GetOffer

Commands:

CreateOffer

UpdateOffer

SetOfferPrice

SetOfferInventory

Existing internal commands:

ActivateOffer

ArchiveOffer

SuspendOffer

SetReturnPolicy

SetOrderQuantityLimits
remain legitimate; verify physical use-case folders and handlers.

No ceremonial handlers.
No endpoint business mutation outside ISender.

8. Result/error semantics reverify

Offer already has strong Result guards; final scan must still prove:

expected failures use Result/Result<T>

SemanticError stable codes

ApiResponseFactory

no semantic exception control flow

no local endpoint error mapper

no InvalidOperationException message classification

no Contains/StartsWith prose heuristic

no ex.Message exposure

unknown exceptions propagate

Do not weaken existing Offer error architecture.

9. Physical/namespace final cleanup

Direct audit found stale historical comments in:
OfferPhysicalStructureGuardTests

It still contains comments/exclusion logic referring to:

“Type-forwarder enums intentionally retain Tooba.Offer.Domain namespace.”

But current real files:

Contracts/Dtos/OfferStatus.cs

Contracts/Dtos/SalesChannel.cs

correctly use:
Tooba.Offer.Contracts.Dtos

Clean stale TypeForwarder-specific exemptions/comments from the guard.

Final rule:

no TypeForwardedTo

no TypeForwarders file

no Contracts type masquerading as Domain

namespace follows physical ownership

Do NOT reintroduce type forwarding.

10. Cross-module boundaries final reverify

Offer.Application currently references Contracts only:

Catalog.Contracts

Inventory.Contracts

Party.Contracts

Pricing.Contracts

Offer.Contracts

Offer.Infrastructure similarly uses foreign Contracts.

Final guard must ensure:

no Catalog.Application/Domain/Infrastructure

no Inventory.Application/Domain/Infrastructure

no Party.Application/Domain/Infrastructure

no Pricing.Application/Domain/Infrastructure

no foreign DbContext

no Host business callback

External Host/admin/storefront readers of Offer should use:
Offer.Contracts (IOfferQueryGateway etc.), not Offer.Application or persistence.

Preserve this good pattern.

11. Host authority final reverify

Allowed Host Offer-specific code:

HostOfferSellerAuthorizer security adapter

DI/composition

MapOfferModule()

consumers of Offer.Contracts

Forbidden:

OfferDbContext

Offer Infrastructure persistence

Offer Application business commands/services invoked directly from Host HTTP

Offer business route implementation in Host

Offer mutation composer in Host

Inspect entire Host, not only SellerPanelEndpoints.

12. Durable guards

Strengthen/update Offer architecture guards so they enforce hardened COMPLETE semantics:

Must fail if:

any Offer business Endpoint route directly invokes pricing/inventory/business gateway

price/inventory routes do not use ISender

SetOfferPrice/SetOfferInventory handlers are missing

Endpoints project references Pricing.Contracts/Inventory.Contracts unnecessarily after migration

Host remaps Offer routes

Application loses MediatR handlers

foreign refs cease to be Contracts-only

TypeForwardedTo/TypeForwarders reappear

namespace/path mismatch appears

direct UtcNow/Guid.NewGuid appears

raw StartActivity appears

expected semantic exceptions/prose mapping reappear

Remove stale TypeForwarder allowances from physical guard.

13. Durable umbrella COMPLETE manifest

Offer is currently intentionally NOT in CompleteHttpModules; there is only an Offer ownership smoke test.

After final successful reverify:
add Offer to:
HostModuleEndpointOwnershipTests.CompleteHttpModules

Canonical entry:

Module = Offer

Endpoints project = Tooba.Offer.Endpoints

Host map = MapOfferModule()

CQRS required = true

legacy Host seller Offer routes remain absent

The old separate Offer smoke tests may remain if useful, but must not substitute for COMPLETE manifest membership.

14. Focused tests only

Required:

Offer.Tests

price mutation characterization

inventory mutation characterization

seller authorization

Result semantics

price/inventory Endpoint → ISender guards

physical/namespace guard

foreign Contracts-only guard

HostModuleEndpointOwnershipTests

TmarDurableGuardTests

final dotnet build src/backend/Tooba.slnx

Do NOT run:

broad Host suite

Checkout workflow

Tax/Pricing tests

frontend

Inventory broad suite

No retries/sleeps/workarounds.

15. Behavior-preservation requirement

This is primarily final closure, not redesign.

Preserve:

all Offer seller routes

current Offer lifecycle

price/inventory integrations

return policy/order quantity rules

seller SKU uniqueness behavior

catalog/party lookups

read model composition

tracing gateways

outbox/events

pricing/inventory stable error behavior

Accidental functional behavior change = 0.

16. Recovery SoT update — mandatory same cycle

On PASS update:

docs/architecture/tmar-current-state.json

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

task recovery-sot

Move Offer from:
internalApplicabilityReviewModules

to:
completeReferenceModules

Set:

module = Offer

state = COMPLETE_REFERENCE_PATTERN

httpApplicability = HTTP_OWNING

endpointOwnership = MODULE_ENDPOINTS

cqrs = MEDIATR_12_5

lastAcceptedTask = TB-TMAR-OFFER-FINAL-REVERIFY-001

accepted implementation commit recorded unambiguously

Set nextTask:
TB-TMAR-INVENTORY-APPLICABILITY-REVERIFY-001

Remaining:

Inventory = NEEDS_APPLICABILITY_REVERIFY

Checkout remains paused.
Tax/Pricing untouched.
Frontend frozen.

Do not create impossible self-referential final-tip hash loops.
Evidence may state both accepted implementation commit and final pushed HEAD.

17. Evidence

Create:
docs/evidence/TB-TMAR-OFFER-FINAL-REVERIFY-001/

Required:

recovery-start.md

offer-final-http-ownership-audit.md

offer-price-inventory-cqrs-audit.md

offer-host-authority-audit.md

offer-cross-module-contract-audit.md

offer-result-semantics-audit.md

offer-physical-tree.md

behavior-preservation-audit.md

architecture-guard-audit.md

recovery-state-sync.md

recovery-sot.md

18. Protected state

COMPLETE and protected:

Cart

Settlement

Fulfillment

Returns

Notification

Support

Wallet

Payment

Promotion

Checkout:
PAUSED_AT_SAFE_W5_CHECKPOINT

Tax/Pricing:
untouched in this repair wave

Frontend:
frozen

Do NOT start Inventory.

No reset.
No clean.
No force push.
No broad git add.
Stashes/user files untouched.

19. Completion scan

Before PASS scan repo for:

direct ISellerOfferPricingGateway use in Offer.Endpoints

direct ISellerOfferInventoryGateway use in Offer.Endpoints

Offer business routes in Host

OfferDbContext in Host/foreign modules

Offer.Application refs from Host business HTTP

Pricing/Inventory Application/Infrastructure refs from Offer

TypeForwardedTo

TypeForwarders

stale namespace masquerading

UtcNow/Guid.NewGuid/raw StartActivity

semantic exception/prose heuristics

ex.Message exposure

Any unresolved production defect => INCOMPLETE.

20. Success criteria

PASS only if ALL:

Offer-HTTP-Ownership: MODULE_ENDPOINTS
Offer-Endpoints-State: REAL_PROJECT_PRESENT
Offer-CQRS-State: MEDIATR_12_5_APPLICATION_HANDLERS
Offer-Price-Route: ISENDER_APPLICATION_COMMAND
Offer-Inventory-Route: ISENDER_APPLICATION_COMMAND
Offer-Endpoint-Business-Gateway-Calls: NONE
Offer-Host-Business-Authority: NONE
Offer-Host-DbAuthority: NONE
Offer-CrossModule-Boundary: CONTRACTS_ONLY
Offer-External-Consumers: OFFER_CONTRACTS_ONLY
Offer-Result-Adoption: HTTP_USE_CASES_ADOPTED
Offer-Error-Classification: STABLE_CODES_ONLY
Offer-Prose-Mapping: NONE
Offer-Unexpected-Exception-Swallow: NONE
Offer-TypeForwarding: NONE
Offer-Physical-State: VERIFIED_ON_DISK_AND_NAMESPACE
Offer-Architecture-Guards: ENFORCED
Offer-Behavior-Preservation: VERIFIED
Offer-Microservice-Extraction: READY_WITHOUT_REWRITE
Recovery-State: CURRENT_AND_MACHINE_READABLE
Recovery-Next-Task: TB-TMAR-INVENTORY-APPLICABILITY-REVERIFY-001
Offer-State: COMPLETE_REFERENCE_PATTERN

21. Canonical Result

Return ONLY BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Track
Recovery-Start
Offer-HTTP-Ownership
Offer-Endpoints-State
Offer-CQRS-State
Offer-Application-UseCases
Offer-Price-Route
Offer-Inventory-Route
Offer-Endpoint-Business-Gateway-Calls
Offer-Host-Authority-Audit
Offer-Host-Business-Authority
Offer-Host-DbAuthority
Offer-CrossModule-Boundary
Offer-External-Consumers
Offer-Result-Adoption
Offer-Error-Classification
Offer-Prose-Mapping
Offer-Unexpected-Exception-Swallow
Offer-TypeForwarding
Offer-Physical-State
Offer-Architecture-Guards
Offer-Behavior-Preservation
Offer-Microservice-Extraction
Recovery-State
Recovery-Next-Task
Current-State-Manifest
Focused-Validation
Skipped-Validation
Full-Validation
AntiPattern-Gate
Residual-Defects
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
Inventory-State
Checkout-State
Tax-State
Pricing-State
Frontend-Production-Changes
Git
Blockers
User-Work-Preserved
Next-Recommended-Task

If incomplete:
Next-Recommended-Task: TB-TMAR-OFFER-FINAL-REVERIFY-001-R1

If complete:
Next-Recommended-Task: TB-TMAR-INVENTORY-APPLICABILITY-REVERIFY-001

After Result:
STOP completely.
Do NOT start Inventory.
Do NOT poll.
Do NOT fetch next task.
Do NOT write Worker IDLE.

END_TOOBA_TASK