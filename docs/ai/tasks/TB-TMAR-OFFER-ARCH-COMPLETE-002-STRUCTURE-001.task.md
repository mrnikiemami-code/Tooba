PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-OFFER-ARCH-COMPLETE-002-STRUCTURE-001
Parent-Task: TB-TMAR-OFFER-HOST-RESIDUE-REPAIR-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: OFFER_ARCH_COMPLETE_002_STRUCTURE
Title: Structure-certify Offer under ARCH-COMPLETE-002
Backend-Only: YES

Architect verdict on parent

TB-TMAR-OFFER-HOST-RESIDUE-REPAIR-001 is ARCHITECT-ACCEPTED.

Verified on main:

commit b9008efdfcdb0057b45357c002861aff90e03df4 exists

Host StorefrontPrimaryOfferResolver is gone

Offer selection policy is owned by Offer via Contracts port + Application implementation

StorefrontComposer consumes only Offer.Contracts for that policy

OfferGlobalUsings is gone

Host temp residue is gone

HostOfferSellerAuthorizer remains thin security composition only

Offer is still NOT ARCH-COMPLETE-002 certified

One objective only

Close Offer structure certification under ARCH-COMPLETE-002.

This task includes only:

exact path↔namespace repair

endpoint-reachable FluentValidation coverage

structure guard tightening

manifest + SoT certification

high-level recovery closure

Do NOT redesign Offer behavior.
Do NOT reopen Host residue work.
Do NOT touch Payment.
Do NOT resume Checkout.
Do NOT touch frontend.

A. Contracts path↔namespace repair

File:
src/backend/Modules/Offer/Tooba.Offer.Contracts/Errors/OfferErrorCodes.cs

Change namespace from:
Tooba.Offer.Contracts

to:
Tooba.Offer.Contracts.Errors

Keep type name:
OfferErrorCodes

Do NOT merge it with Domain OfferErrorCodes.
They are separate layers:

Contracts.Errors = stable cross-boundary semantic codes

Domain.Errors = domain-internal error codes

Update only exact Offer production/test consumers that reference the Contracts type.

Where both Contracts.Errors.OfferErrorCodes and Domain.Errors.OfferErrorCodes are needed in the same file:

use fully-qualified name at that exact site, or

use a local file-scoped alias with an explicit semantic name such as ContractOfferErrorCodes
Do NOT create project-wide/global aliases.

No namespace workaround.

B. Validators capability

Create folder:
src/backend/Modules/Offer/Tooba.Offer.Application/Validators

Namespace:
Tooba.Offer.Application.Validators

Use FluentValidation.

Create exactly these 5 validators:

GetOfferQueryValidator

CreateOfferCommandValidator

UpdateOfferCommandValidator

SetOfferPriceCommandValidator

SetOfferInventoryCommandValidator

Do NOT create a validator for ListSellerOffersQuery.

Classification:
ListSellerOffersQuery = NO_VALIDATOR_REQUIRED_AUTH_SCOPED_QUERY

Reason:
SellerPartyId is supplied by the trusted seller authorization boundary, not arbitrary request payload.

GetOfferQueryValidator

Rules:

OfferId != Guid.Empty

SellerPartyId != Guid.Empty

CreateOfferCommandValidator

Transport/input shape only:

CatalogVariantId != Guid.Empty

SellerPartyId != Guid.Empty

Channel must be defined enum value

SellerSku: when non-empty, trimmed length <= existing canonical seller-SKU limit if such constant already exists; otherwise DO NOT invent a new limit

Status: when non-empty, must be one of Draft / Active

ReturnPolicyChoice: when non-empty, must be one of existing supported contract choices already used by Offer

CustomReturnWindowDays: when present, >= 1

MinimumOrderQuantity: when present, > 0

MaximumOrderQuantity: when present, > 0

if both min/max are present, min <= max

Do NOT duplicate DB uniqueness, seller existence, catalog existence, return-policy option policy, or domain business rules in validator.

UpdateOfferCommandValidator

Rules:

OfferId != Guid.Empty

SellerPartyId != Guid.Empty

SellerSku: same transport-shape rule as Create

Status: when non-empty, one of Active / Suspended / Archived

ReturnPolicyChoice: same supported-shape rule

CustomReturnWindowDays: when present, >= 1

MinimumOrderQuantity: when present, > 0

MaximumOrderQuantity: when present, > 0

if both present, min <= max

Do NOT turn optional patch members into required fields.

SetOfferPriceCommandValidator

Rules:

OfferId != Guid.Empty

SellerPartyId != Guid.Empty

Amount >= 0

Currency: when provided, trimmed length == 3

Market: when provided, non-whitespace after trim

Do NOT validate active-price existence or market policy here.

SetOfferInventoryCommandValidator

Rules:

OfferId != Guid.Empty

SellerPartyId != Guid.Empty

OnHand >= 0

Reason: when provided, enforce only existing canonical length limit if one already exists in Inventory contract/domain; otherwise only trim/non-whitespace shape and do NOT invent a numeric limit

C. Validator discovery

Do NOT add bespoke manual validation calls to endpoints or handlers.

Verify existing MediatR validation pipeline discovers Offer validators automatically.

No endpoint direct validator invocation.
No handler direct validator invocation.

MediatR remains 12.5.

D. Physical structure guard tightening

Edit:
src/backend/Modules/Offer/Tooba.Offer.Tests/Architecture/OfferPhysicalStructureGuardTests.cs

Requirements:

Replace loose namespace StartsWith checks with exact path-derived namespace equality for production .cs files.

Include Contracts folders in namespace checks, especially:

Contracts/Dtos -> Tooba.Offer.Contracts.Dtos

Contracts/Ports -> Tooba.Offer.Contracts.Ports

Contracts/Errors -> Tooba.Offer.Contracts.Errors

Include Application/Validators -> Tooba.Offer.Application.Validators

Preserve exception for EF generated migrations/model snapshot if they do not use the regular single-line namespace form.

Do NOT weaken existing folder/root checks.

No empty ceremonial folders.

The guard must fail on:

wrong namespace for a physical folder

flat root dumping

unapproved top-level folder

alias workaround

E. Validator coverage guard

Add/strengthen Offer architecture guard so it explicitly inventories all 6 endpoint-reachable requests:

ListSellerOffersQuery = NO_VALIDATOR_REQUIRED_AUTH_SCOPED_QUERY

GetOfferQuery = validator required/present

CreateOfferCommand = validator required/present

UpdateOfferCommand = validator required/present

SetOfferPriceCommand = validator required/present

SetOfferInventoryCommand = validator required/present

Guard must prove:

exactly 6 endpoint-reachable requests

5 validators present

no validator for ListSellerOffersQuery required

all 6 use IRequest

endpoints use ISender

no endpoint direct Application service/Directory invocation

MediatR package/version remains 12.5.0

No representative substitution.

F. Structure manifest

Update:
docs/architecture/tmar-module-structure-manifests.json

Add Offer:

module = Offer
structureCertified = true
lockVersion = ARCH-COMPLETE-002

Projects:

Tooba.Offer.Application

rootAllowlist: []

forbiddenRootFiles:

OfferContractMapping.cs

IOfferStore.cs

OfferReadModelComposer.cs

OfferRequests.cs

OfferHandlers.cs

OfferQueries.cs

OfferQueryHandlers.cs

forbiddenTopLevelFolders: []

Approved capability folders include:

Commands

Queries

Mappings

Ports

ReadModels

Policies

Validators

Tooba.Offer.Endpoints

rootAllowlist:

OfferEndpointModule.cs

forbiddenRootFiles:

OfferSellerEndpoints.cs

IOfferSellerAuthorizer.cs

OfferErrorCatalogContributor.cs

OfferErrorResources.cs

OfferEndpointLocalizer.cs

forbiddenTopLevelFolders: []

Tooba.Offer.Infrastructure

rootAllowlist: []

forbiddenRootFiles:

OfferModule.cs

OfferDbContext.cs

OfferStore.cs

OfferEvents.cs

OfferModuleMigration.cs

OfferSchemaMigrator.cs

OfferDevelopmentSeedGateway.cs

OfferOutboxRegistration.cs

forbiddenTopLevelFolders: []

Remove Offer from:
uncertifiedHttpOwningModules

Do NOT alter other module manifest entries except any exact gate expectation required for Offer addition.

G. SoT / high-level closure

Update:

docs/architecture/tmar-current-state.json

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

durable guard assertions

Record:

Offer:

state = COMPLETE_REFERENCE_PATTERN

httpApplicability = HTTP_OWNING

endpointOwnership = MODULE_ENDPOINTS

cqrs = MEDIATR_12_5

structureCertifiedUnderArchComplete002 = true

hostResidue = BUSINESS_RESIDUE_REMOVED_THIN_SECURITY_ADAPTER_ONLY

validatorCoverage = 5_REQUIRED_PRESENT_1_NO_VALIDATOR_REQUIRED

pathNamespace = EXACT

selectionOwner = OFFER_CONTRACT_PORT_APPLICATION_POLICY

Add Offer to:
structureLock.certifiedModules

Expected set becomes:
Order, Cart, StoreContext, Offer
(ordering may follow canonical manifest order, but guards and SoT must agree)

Set:
lastAcceptedTask = TB-TMAR-OFFER-HOST-RESIDUE-REPAIR-001
lastAcceptedCommit = b9008efdfcdb0057b45357c002861aff90e03df4

For this task's pending review:
nextTask = USER_REVIEW_OFFER_ARCH_COMPLETE_002_STRUCTURE_001

Do NOT advance to Payment inside this task.

Checkout remains:
PAUSED_AT_SAFE_W5_CHECKPOINT

Frontend:
frontendFrozen = true

H. Evidence

Create:
docs/evidence/TB-TMAR-OFFER-ARCH-COMPLETE-002-STRUCTURE-001/offer-structure-certification.md

Record:

parent acceptance

exact namespace repair

endpoint request inventory

validator matrix

NO_VALIDATOR_REQUIRED rationale

exact manifest entry

structure guard behavior

Host residue remains closed

certification state

validation results

I. Validation

Run only:

Offer validator-focused tests

OfferArchitectureGuardTests

OfferPhysicalStructureGuardTests

full Tooba.Offer.Tests

Host Offer/Storefront focused guards if compilation requires

TmarCompleteReferenceStructureGateTests

TmarDurableGuardTests

one final:
dotnet build src/backend/Tooba.slnx

Do not run broad unrelated suites.

PASS criteria

PASS only if:

Contracts/Errors namespace exactly matches path

5 required validators exist

ListSellerOffersQuery explicitly documented/guarded NO_VALIDATOR_REQUIRED

all 6 endpoint requests are real MediatR 12.5 requests via ISender

physical namespace guard uses exact equality

no root dumping

no namespace alias workaround

manifest contains Offer as certified

Offer removed from uncertifiedHttpOwningModules

structureLock includes Offer

Host Offer business residue remains removed

HostOfferSellerAuthorizer remains thin

Cart/Order/StoreContext certifications unchanged

Checkout remains W5 paused

frontend untouched

Offer tests pass

structure/durable guards pass

full build passes

Canonical Result

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-OFFER-ARCH-COMPLETE-002-STRUCTURE-001
Parent-Task: TB-TMAR-OFFER-HOST-RESIDUE-REPAIR-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-Host-Residue-Repair-State:
Contracts-Errors-Namespace-State:
Endpoint-Reachable-Request-Count:
Validator-Required-Count:
No-Validator-Required-Count:
Validator-Coverage-State:
MediatR-State:
ISender-State:
Path-Namespace-State:
Root-Allowlist-State:
Manifest-State:
Offer-Structure-Certification-State:
Uncertified-List-State:
Host-Residue-State:
HostOfferSellerAuthorizer-State:
CrossModule-Boundary-State:
Focused-Validation:
Offer-Tests:
Full-Build:
Cart-Certification-State:
Order-Certification-State:
StoreContext-Certification-State:
Checkout-State:
Frontend-Production-Changes:
Recovery-HighLevel-Closure:
Residual-Defects:
Git:
User-Work-Preserved:
Recovery-Next-Task:
Next-Recommended-Task:
END_TOOBA_WORKER_RESULT

STOP

After Result:
STOP completely.
Do not start Payment automatically.
Do not resume Checkout.
Do not touch SharedDB.
Do not poll.
Wait for Architect verification.

END_TOOBA_TASK