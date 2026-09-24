PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-OFFER-HOST-RESIDUE-REPAIR-001
Parent-Task: TB-TMAR-OFFER-ARCH-COMPLETE-002-AUDIT-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: OFFER_HOST_RESIDUE_REPAIR
Title: Remove Offer business policy and hidden Offer aliases from Host
Backend-Only: YES

Architect verdict

TB-TMAR-OFFER-ARCH-COMPLETE-002-AUDIT-001 is ARCHITECT-ACCEPTED at commit:
7a0b36061db85fc4ba57e117bcf50d9ee390fa73

Closed findings:

HostOfferSellerAuthorizer = allowed thin Host security adapter

StorefrontPrimaryOfferResolver = Offer business policy and must leave Host

OfferGlobalUsings = remove

Host .tmp-t014-test-out = dead residue

Offer structure certification is a separate follow-up task

audit changed no Offer/Host production code

One objective only

Remove Offer-owned business selection policy and hidden Offer type aliases from Host while preserving storefront behavior exactly.

Do NOT structure-certify Offer here.
Do NOT add validators here.
Do NOT repair Contracts/Errors namespace here.
Do NOT touch Payment, Checkout W6, SharedDB or frontend.

Architecture decision

Boundary:
Host Storefront composition -> Tooba.Offer.Contracts port -> Offer.Application implementation

Host must NOT call Offer.Application implementation directly.

Contracts

Create:
src/backend/Modules/Offer/Tooba.Offer.Contracts/Dtos/OfferSelectionCandidate.cs
Namespace:
Tooba.Offer.Contracts.Dtos

Record fields:

Guid OfferId

Guid CatalogVariantId

Guid SellerPartyId

decimal AmountExclusiveOfTax

decimal AvailableUnits

Create:
src/backend/Modules/Offer/Tooba.Offer.Contracts/Ports/IPrimaryOfferSelectionPolicy.cs
Namespace:
Tooba.Offer.Contracts.Ports

Methods:

OfferSelectionCandidate? Resolve(IReadOnlyList<OfferSelectionCandidate> candidates)

Guid? ResolveVariantId(Guid? requestedVariantId, IReadOnlyCollection<Guid> productVariantIds, IReadOnlyList<OfferSelectionCandidate> candidates)

No Pricing/Inventory/Application/Infrastructure types in this contract.

Application

Create:
src/backend/Modules/Offer/Tooba.Offer.Application/Policies/PrimaryOfferSelectionPolicy.cs
Namespace:
Tooba.Offer.Application.Policies

Implement IPrimaryOfferSelectionPolicy.

Preserve exact semantics:

empty => null

AvailableUnits > 0 first

then lowest AmountExclusiveOfTax

then OfferId

ResolveVariantId keeps requested variant only when it belongs to product and has a candidate; otherwise fallback to Resolve(...).CatalogVariantId

No DbContext, no Host dependency, no lookup, no time/id/randomness.

Registration

Edit:
src/backend/Modules/Offer/Tooba.Offer.Infrastructure/DependencyInjection/OfferModule.cs

Register:
IPrimaryOfferSelectionPolicy -> PrimaryOfferSelectionPolicy

Use Singleton because implementation is pure/stateless.

Host Storefront

Delete:
src/backend/Host/Tooba.Host/Storefront/StorefrontPrimaryOfferResolver.cs

Edit:
src/backend/Host/Tooba.Host/Storefront/StorefrontComposer.cs

Inject IPrimaryOfferSelectionPolicy from Offer.Contracts.

At the exact 3 audited call sites:

map StorefrontOfferCandidate -> OfferSelectionCandidate

call Contracts port

map selected OfferId / VariantId back to existing Host presentation records

StorefrontOfferCandidate remains Host presentation shape.

Do NOT move Host-only presentation fields into Offer:

SellerDisplayName

SellerSku

Currency

Market

TaxCategoryLabel

No using Tooba.Offer.Application... in StorefrontComposer.

Remove hidden aliases

Delete:
src/backend/Host/Tooba.Host/OfferGlobalUsings.cs

Repair these exact 9 audited consumers:

AccessControl/AccessControlDevelopmentSeed.cs

Admin/AdminPanelComposer.cs

Admin/CatalogAttributeSchemaDevelopmentBootstrap.cs

Admin/MerchandisingCampaignAdminEndpoints.cs

Admin/ProductWorkspaceComposer.cs

Admin/ProductWorkspaceDevelopmentBootstrap.cs

Configuration/ToobaPlatformOptions.cs

Outbox/OutboxWorkerSeams.cs

Storefront/StorefrontDemoCatalogBootstrap.cs

Use normal explicit:
using Tooba.Offer.Contracts.Dtos;

If a real collision exists, use the fully-qualified Offer type only at that site.
Do NOT create another global/namespace alias workaround.

Allowed thin Host adapter

Keep:
src/backend/Host/Tooba.Host/Seller/HostOfferSellerAuthorizer.cs

Only compilation-level using cleanup is allowed.

It must remain pure security composition:

no ranking

no pricing

no inventory decision

no persistence

no response composition

no Offer business service call

Dead residue

Delete:
src/backend/Host/Tooba.Host/.tmp-t014-test-out/

Do not replace it.

Tests

Add:
src/backend/Modules/Offer/Tooba.Offer.Tests/Application/PrimaryOfferSelectionPolicyTests.cs

Cover:

in-stock beats cheaper out-of-stock

lower amount wins among in-stock

OfferId deterministic tie-break

empty => null

valid requested variant retained

requested variant without candidate falls back

unknown variant falls back

no candidate => null variant

Edit Host storefront tests so they no longer directly call deleted Host resolver.
Host tests should verify mapping/composition only.

Architecture guard

Strengthen:
src/backend/Modules/Offer/Tooba.Offer.Tests/Architecture/OfferArchitectureGuardTests.cs

Assert:

Host StorefrontPrimaryOfferResolver.cs absent

OfferGlobalUsings.cs absent

no global using OfferStatus

no global using SalesChannel

.tmp-t014-test-out absent

Contracts owns IPrimaryOfferSelectionPolicy

Application owns PrimaryOfferSelectionPolicy

Host StorefrontComposer consumes Contracts port, not Application implementation

no equivalent primary-offer ordering policy remains in Host

HostOfferSellerAuthorizer remains thin

Offer -> Host dependency remains zero

Do NOT certify ARCH-COMPLETE-002 here.

Recovery / SoT

Keep recovery current.

On PASS update:

docs/architecture/tmar-current-state.json

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

durable recovery guard expectations

Record:

Offer audit = ACCEPTED

Host residue = BUSINESS_RESIDUE_REMOVED_THIN_SECURITY_ADAPTER_ONLY

selection owner = OFFER_CONTRACT_PORT_APPLICATION_POLICY

global alias = REMOVED

temp residue = REMOVED

structure certification = PENDING_TB_TMAR_OFFER_ARCH_COMPLETE_002_STRUCTURE_001

Checkout = PAUSED_AT_SAFE_W5_CHECKPOINT

frontendFrozen = true

nextTask = TB-TMAR-OFFER-ARCH-COMPLETE-002-STRUCTURE-001

Do NOT mark Offer structure-certified yet.

Evidence

Create:
docs/evidence/TB-TMAR-OFFER-HOST-RESIDUE-REPAIR-001/offer-host-residue-repair.md

Record before/after residue, policy boundary, 3 caller changes, 9 alias consumers, thin-adapter justification, temp-residue removal, behavior parity, recovery state, validation.

Validation

Run only:

PrimaryOfferSelectionPolicyTests

OfferArchitectureGuardTests

relevant Host StorefrontCompositionTests

Offer endpoint/handler tests only if compilation requires

TmarDurableGuardTests

TmarCompleteReferenceStructureGateTests

one final dotnet build src/backend/Tooba.slnx

PASS criteria

PASS only if:

Host primary-offer business resolver is gone

equivalent rule exists only under Offer ownership

Host consumes Offer Contracts port only

behavior preserved

OfferGlobalUsings gone

all 9 consumers compile explicitly

temp Host residue gone

HostOfferSellerAuthorizer remains thin

no Offer -> Host dependency

Offer not yet structure-certified

recovery points directly to structure task

Checkout/frontend unchanged

tests/build pass

Canonical Result

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-OFFER-HOST-RESIDUE-REPAIR-001
Parent-Task: TB-TMAR-OFFER-ARCH-COMPLETE-002-AUDIT-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Audit-Acceptance-State:
Host-PrimaryOfferResolver-State:
Offer-Selection-Contract-State:
Offer-Selection-Policy-State:
Storefront-Callers-State:
Host-Global-Alias-State:
Alias-Consumer-Repair-State:
Host-Temp-Residue-State:
HostOfferSellerAuthorizer-State:
Offer-To-Host-Dependency-State:
Behavior-Parity:
Architecture-Guards:
Focused-Validation:
Full-Build:
Offer-Structure-Certification-State:
Cart-Certification-State:
Order-Certification-State:
StoreContext-Certification-State:
Checkout-State:
Frontend-Production-Changes:
Recovery-State:
Residual-Defects:
Git:
User-Work-Preserved:
Recovery-Next-Task:
Next-Recommended-Task:
END_TOOBA_WORKER_RESULT

STOP

After Result:
STOP completely.
Do not start structure certification automatically.
Do not start Payment.
Do not resume Checkout.
Do not poll.
Wait for Architect verification.

END_TOOBA_TASK