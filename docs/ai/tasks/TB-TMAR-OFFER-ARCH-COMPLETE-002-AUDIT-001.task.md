PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-OFFER-ARCH-COMPLETE-002-AUDIT-001
Parent-Task: TB-TMAR-CART-MULTICURRENCY-LINES-001-R1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: OFFER_ARCH_COMPLETE_002_BOUNDED_AUDIT
Title: Audit Offer for Host residue and ARCH-COMPLETE-002 certification
Backend-Only: YES

Architect verdict on parent

TB-TMAR-CART-MULTICURRENCY-LINES-001-R1 is ARCHITECT-ACCEPTED.

Verified on main:

commit 1aa6ab8b99a689319f25e8c31f55c332bc3f3ab4 exists

cross-currency shipping sum is removed

Order shipping/checkout derives effective currency from sole Cart line currency

cart.DefaultCurrency is no longer Order transaction authority

mixed/currency-less Cart fails closed at Order boundary

Cart multi-currency behavior remains intact

Cart / Order / StoreContext certifications remain intact

This task MUST stamp that acceptance at recovery level before moving Offer forward.

One objective only

AUDIT ONLY.

Produce the exact deterministic implementation map required to make Offer:

Host-business-residue free

capability-foldered and path↔namespace clean

MediatR 12.5 / FluentValidation complete

ARCH-COMPLETE-002 structure-certifiable

microservice-extraction-ready

Do NOT implement production changes in this task.

Known current state

Offer is already:

COMPLETE_REFERENCE_PATTERN

HTTP_OWNING

MODULE_ENDPOINTS

MEDIATR_12_5

last accepted Offer task = TB-TMAR-OFFER-FINAL-REVERIFY-001

not yet listed as ARCH-COMPLETE-002 structure-certified

Known Host Offer-named residue:

src/backend/Host/Tooba.Host/Seller/HostOfferSellerAuthorizer.cs

src/backend/Host/Tooba.Host/Storefront/StorefrontPrimaryOfferResolver.cs

src/backend/Host/Tooba.Host/OfferGlobalUsings.cs

Do not assume all three are equally wrong:

tiny platform/auth adapters may be allowed if they are genuinely Host security composition only

business selection/ranking logic is NOT allowed to remain in Host

global aliases must not hide architectural coupling

Parent recovery stamp

Update ONLY recovery bookkeeping for the accepted Cart R1:

lastAcceptedTask = TB-TMAR-CART-MULTICURRENCY-LINES-001-R1

lastAcceptedCommit = 1aa6ab8b99a689319f25e8c31f55c332bc3f3ab4

note Cart multi-currency = accepted with Order single-currency fail-closed compatibility guard

nextTask = USER_REVIEW_OFFER_ARCH_COMPLETE_002_AUDIT_001

Keep Checkout W5 paused and frontend frozen.

Exact Offer audit scope

Inspect:

Offer production projects

src/backend/Modules/Offer/Tooba.Offer.Domain

src/backend/Modules/Offer/Tooba.Offer.Application

src/backend/Modules/Offer/Tooba.Offer.Contracts

src/backend/Modules/Offer/Tooba.Offer.Infrastructure

src/backend/Modules/Offer/Tooba.Offer.Endpoints

Offer architecture tests

Tooba.Offer.Tests/Architecture/OfferArchitectureGuardTests.cs

Tooba.Offer.Tests/Architecture/OfferPhysicalStructureGuardTests.cs

Exact Host Offer residue

Host/Tooba.Host/Seller/HostOfferSellerAuthorizer.cs

Host/Tooba.Host/Storefront/StorefrontPrimaryOfferResolver.cs

Host/Tooba.Host/OfferGlobalUsings.cs

Direct Host callers of StorefrontPrimaryOfferResolver

Inspect only the direct callers and the exact definition of StorefrontOfferCandidate.

Structure SoT

docs/architecture/tmar-module-structure-manifests.json

docs/architecture/tmar-current-state.json

Do NOT audit unrelated Host modules.

Required audit questions
A. Host residue classification

Classify every identified Offer-related Host file/reference as exactly one:

REMOVE_TO_OFFER_MODULE

KEEP_AS_EXPLICIT_THIN_HOST_SECURITY_ADAPTER

REMOVE_DEAD_RESIDUE

RENAME_OR_REMOVE_GLOBAL_ALIAS

For every KEEP decision, prove it contains NO:

Offer business rule

ranking/buy-box logic

pricing decision

inventory decision

persistence

response composition

B. StorefrontPrimaryOfferResolver ownership

Determine:

exact callers

exact definition/location of StorefrontOfferCandidate

whether candidate is Host-only presentation DTO or reusable Offer-facing contract

exact destination for resolver logic

Preferred architecture:

Offer-owned business selection policy must not stay in Host

no Offer -> Host reference

no foreign DbContext

no direct Pricing/Inventory implementation dependency in Offer policy

If moving the candidate would require a cross-module ownership redesign, report the smallest contract extraction required.

Do NOT implement it.

C. Application structure

List all root .cs files and top-level folders in:

Application

Endpoints

Infrastructure

Compare against ARCH-COMPLETE-002.

Check capability folders:

Commands/<UseCase>

Queries/<UseCase>

Mappings

Ports

ReadModels

Seller endpoint capability

Errors/Resources

Infrastructure integration folders

Identify exact path↔namespace mismatches.

D. MediatR coverage

Enumerate every request reachable from Offer.Endpoints.

For each:

Command/Query type

handler exists

MediatR 12.5

endpoint uses ISender

no endpoint direct Directory/Application service invocation

Report total endpoint-reachable requests.

E. FluentValidation coverage

For every endpoint-reachable Command/Query classify:

VALIDATOR_REQUIRED

NO_VALIDATOR_REQUIRED

For VALIDATOR_REQUIRED:

exact validator path or missing gap

Do not create empty validators for zero-input queries.

F. Contracts / foreign boundaries

Check Offer production code for:

foreign Application refs

foreign Infrastructure refs

foreign DbContext

Host ref

direct cross-module persistence

business logic in Host to compensate for missing Offer contract

List exact violations only.

G. Error / locale / clock / id / Result

Confirm or identify gaps:

Result/SemanticError for expected outcomes

no message parsing

localized text only presentation boundary

no Persian prose in Domain/Application/Infrastructure

IClock

IIdGenerator

no DateTime.UtcNow / DateTimeOffset.UtcNow / Guid.NewGuid()

H. Structure certification map

Produce the exact proposed manifest entry for Offer:

Application root allowlist

Endpoints root allowlist

Infrastructure root allowlist

forbidden root files

forbidden top-level folders if needed

Do NOT add manifest entry in audit.

I. Next implementation task

Propose ONE implementation task with no discovery left.

It may include:

Host business residue extraction

physical folder/namespace fixes

validator gaps

guards

manifest + SoT certification

ONLY if all are tightly related to Offer ARCH-COMPLETE-002 closure.

If scope is too large, split into exactly:

Host residue ownership repair

structure certification

State which split is necessary and why.

Output artifact

Create only:

docs/evidence/TB-TMAR-OFFER-ARCH-COMPLETE-002-AUDIT-001/offer-arch-complete-002-audit.md

Must contain:

Host residue table

resolver ownership/caller map

Application/Endpoints/Infrastructure structure map

endpoint-reachable MediatR request inventory

validator coverage matrix

cross-module dependency findings

error/locale/time/id findings

proposed manifest entry

exact next implementation file list

recovery acceptance stamp evidence

No production code edits.

Validation

Run only:

Offer architecture tests

Offer physical structure tests

TmarDurableGuardTests

TmarCompleteReferenceStructureGateTests

No broad suite.
No full build unless required to compile changed recovery test assertions.

PASS criteria

PASS only if:

Cart R1 acceptance is stamped in recovery

no Offer production code changed

all Offer Host residue is explicitly classified

StorefrontPrimaryOfferResolver ownership is settled

endpoint-reachable MediatR inventory is complete

validator coverage is complete

structure gaps are exact, not generic

proposed manifest is deterministic

next task has no discovery loop

Cart/Order/StoreContext certifications unchanged

Checkout remains W5 paused

frontend frozen

Canonical Result

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-OFFER-ARCH-COMPLETE-002-AUDIT-001
Parent-Task: TB-TMAR-CART-MULTICURRENCY-LINES-001-R1
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-Cart-R1-Acceptance-Stamped:
Production-Code-Changes:
Offer-Current-State:
Host-Residue-Classification:
PrimaryOfferResolver-Ownership:
PrimaryOfferResolver-Callers:
Offer-Structure-State:
Path-Namespace-State:
Endpoint-Reachable-Request-Count:
MediatR-Coverage:
Validator-Coverage:
CrossModule-Boundary-State:
Error-Locale-Time-Id-State:
Proposed-Manifest:
Implementation-Scope-Decision:
Exact-Next-Implementation-Files:
Focused-Validation:
Cart-Certification-State:
Order-Certification-State:
StoreContext-Certification-State:
Checkout-State:
Frontend-Production-Changes:
Residual-Defects:
Git:
User-Work-Preserved:
Recovery-Next-Task:
Next-Recommended-Task:
END_TOOBA_WORKER_RESULT

STOP

After Result:
STOP completely.
Do not implement Offer closure.
Do not start Payment.
Do not poll.
Wait for Architect verification.

END_TOOBA_TASK