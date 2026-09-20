PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_REPAIR_TASK

Task-ID:
TB-TMAR-OFFER-REFERENCE-W1-R4

Parent-Task:
TB-TMAR-OFFER-REFERENCE-W1-R3

Channel:
tooba-main

WorkerId:
tooba-worker-01

AgentType:
cursor

Status:
REPAIR

Program:
TMAR — Tooba Microservice-Ready Architecture Recovery

Track:
OFFER_REFERENCE_MODULE

Title:
Offer Golden Module R4 — Remove Fake CQRS, Eliminate SellerPanel Offer BFF, Normalize Use-Case Folders, and Complete Cross-Module Boundaries

Backend-Only:
YES

TMAR-Execution-Mode:
BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE

0. Architect Review of R3

R3 is accepted as an IN-PROGRESS repair, NOT as completion.

The Architect directly re-read main at:

86d40bb99b34182c2ba373e3cfc6a0636077861b

Verified improvements:

Commands / Queries / handlers now exist.

Domain no longer references Offer.Contracts.

TypeForwarders removed.

Offer.Infrastructure no longer references Catalog.Application / Party.Application.

Endpoints now inject ISender.

Host SellerPanelComposer no longer owns Create/Patch aggregate mutation.

However, current code still contains serious non-Golden defects.

R4 must fix these defects rather than only documenting them.

1. Architect-verified R4 defects
A. CQRS is currently partly ceremonial / duplicated

Current OfferSellerEndpoints does this pattern:

send ListSellerOffersQuery

ignore the query result

call IOfferSellerPanel.ListOffersAsync(...)

and for Get:

send GetOfferQuery

then call IOfferSellerPanel.GetOfferAsync(...)

and Create/Patch:

send Offer commands

then call IOfferSellerPanel.GetOfferAsync(...) for response shaping.

This means CQRS exists, but the real read path still goes through the Host BFF.

This is not an acceptable Golden pattern.

B. Application foldering is not yet the intended reference pattern

Current:
Commands/OfferRequests.cs
Commands/OfferHandlers.cs
Queries/OfferQueries.cs
Queries/OfferQueryHandlers.cs

These aggregate many unrelated use cases in broad files.

For the Golden module, organize real use cases into responsibility folders/files.

Expected pattern:

Commands/CreateOffer/

CreateOfferCommand.cs

CreateOfferHandler.cs

Commands/UpdateOffer/

command

handler

Commands/ActivateOffer/
...
Queries/GetOffer/
...
Queries/ListSellerOffers/
...

Do not mechanically split trivial private helpers, but each externally meaningful use case must be independently discoverable.

C. IOfferSellerPanel remains a leak

Current Application still exposes:

IOfferSellerPanel

with:

enriched Offer reads

Pricing write

Inventory write

This interface mixes Offer, Pricing, Inventory, Host/BFF concerns.

It must not remain as a Golden Offer Application port.

D. Host SellerPanelComposer remains Offer-adjacent business/read logic

It still:

queries OfferDbContext directly

joins/enriches Catalog/Pricing/Inventory data

emits Offer list/detail DTOs

performs seller Offer ownership checks

writes Price

writes Inventory

owns default inventory-location fallback

contains Persian user-facing literals

Offer cannot be considered complete while its seller Offer path still depends on this Host god-composer.

E. Price and Inventory routes are owned by the wrong module surface

Current Offer.Endpoints exposes:

/offers/{offerId}/price
/offers/{offerId}/inventory

but Price truth belongs to Pricing and Stock truth belongs to Inventory.

A Golden Offer module must not become authority for those write use cases.

Preserve external URL compatibility, but move command ownership to the owning module boundary.

F. Endpoint orchestration is too large

Current Create endpoint sends multiple commands:

Create

optional Activate

optional SetReturnPolicy

Patch similarly sequences multiple commands.

This is use-case orchestration in transport.

If API semantics are "create seller offer with requested initial state/policy", expose one Application command that owns that transaction/use-case semantics.

Likewise patch/update should be one cohesive Application use case where current API requires atomic/coherent behavior.

G. Error handling still has ad-hoc boundary text

Current endpoint contains literal:
"Offer not found."

SellerPanelComposer contains Persian messages.

Golden module should use stable code + localizer/resource boundary mapping, not scattered literals.

2. Git / Recovery Safety

Repository:
D:\Users\User\source\repos\SarvNewVer

Expected baseline:
86d40bb99b34182c2ba373e3cfc6a0636077861b

Verify:

branch main

HEAD == origin/main

no unexpected tracked modifications

staged = 0

protected ancestor 18ca10c9

stashes untouched

user work preserved

User .rar archives are user work:

do NOT delete

do NOT stage

do NOT rename

do NOT ignore broadly unless already governed by existing policy

Forbidden:

git reset

git clean

destructive restore/checkout

unsafe rebase

stash pop/drop

broad git add .

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R4/recovery-start.md

3. Normalize Offer Application use-case structure

Refactor current aggregate files into clear per-use-case folders/files.

At minimum real use cases must have independent folders:

Commands:

CreateOffer

UpdateOffer

ActivateOffer

SuspendOffer

ArchiveOffer

SetReturnPolicy

SetOrderQuantityLimits

Queries:

GetOffer

ListSellerOffers

If Create/Update become richer cohesive commands in this wave, rename based on actual semantics and remove obsolete duplicate commands.

Namespace must follow physical ownership, e.g.:
Tooba.Offer.Application.Commands.CreateOffer

Do not keep everything in root Tooba.Offer.Application.

Update endpoint imports accordingly.

Add guard that rejects future dumping of multiple public command/handler use cases into broad OfferRequests.cs / OfferHandlers.cs style files.

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R4/usecase-foldering.md

4. Remove fake/duplicate CQRS reads

For every Offer-owned endpoint:

The result returned to HTTP MUST come from the MediatR use case/query path.

Forbidden pattern:
await sender.Send(query); then ignore result and call another service for the actual data.

Required:

query handler returns the final Offer-owned read result required by endpoint
OR

query handler returns an Offer read DTO composed through explicit cross-module read ports.

No Host BFF call after an Offer query.

Remove Offer read methods from IOfferSellerPanel.

Expected:

ListOffersAsync removed from IOfferSellerPanel

GetOfferAsync removed from IOfferSellerPanel

Offer endpoints do not inject/use IOfferSellerPanel for List/Get/Create/Patch response shaping

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R4/read-path.md

5. Build an Offer-owned seller read model correctly

The seller Offer list/detail is Offer-centric but enriched with:

Catalog product/title

Pricing amount/currency

Inventory availability

possibly return-policy/tax information where actually required by current DTO

Do NOT query foreign DbContexts from Host or Offer.

Create explicit contract/gate reads from owning modules.

Use existing Contracts if sufficient:

Catalog.Contracts

Pricing.Contracts

Inventory.Contracts

Tax.Contracts only if truly needed

If missing:

add minimal read contracts to owning module *.Contracts

implement adapter in owning module Infrastructure

register via module DI

Do not introduce foreign Application references.

Offer Application may orchestrate an Offer-centric read model through those stable contracts.

No cross-module SQL join.
No cross-module EF navigation.
No shared mega-DbContext.

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R4/read-model-boundaries.md

6. Remove Offer read/enrichment responsibility from SellerPanelComposer

After R4, SellerPanelComposer must NOT:

query OfferDbContext for Offer list/detail

shape SellerOfferListItem

shape SellerOfferDetailPage

resolve Offer product titles

resolve Offer pricing

resolve Offer inventory

perform Offer ownership lookup

implement any Offer endpoint response enrichment

SellerPanelComposer may keep non-Offer seller dashboard/order concerns temporarily ONLY if not required to finish Offer.

Any methods/fields now unused by SellerPanelComposer must be removed cleanly.

Do not retain dead Offer dependencies.

Expected Host reductions:

OfferDbContext injection removed from SellerPanelComposer if no non-Offer need remains

Offer-specific helper methods removed

Offer-specific localized literals removed from this Host path

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R4/host-offer-removal.md

7. Price write ownership

Current Offer endpoint routes for price must preserve external URL behavior, but Pricing owns the write.

Choose the cleanest existing-compatible design based on repository reality:

Preferred:

Pricing.Application owns command/use case

Pricing.Endpoints maps the existing seller Offer price route

Host/composition only maps module endpoints

Acceptable alternative only if route relocation is unsafe:

Offer endpoint calls a stable Pricing.Contracts command port/gate

implementation belongs to Pricing

no Pricing DbContext in Offer/Host

Offer does not validate Pricing business rules

Do not place Pricing write handler in Offer.Application.

After R4:

IOfferSellerPanel.SetOfferPriceAsync removed

SellerPanelComposer does not write Pricing

no direct PricingDbContext in Offer seller route

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R4/pricing-boundary.md

8. Inventory write ownership

Same rule for inventory.

Preserve route compatibility.

Preferred:

Inventory owner handles seller Offer inventory write through its own application boundary

route may physically live in Inventory endpoint module if that is the cleanest ownership

If Inventory currently lacks Endpoints, add only the minimal owner endpoint/application seam needed for this route; do not refactor the whole Inventory module.

After R4:

IOfferSellerPanel.SetOfferInventoryAsync removed

SellerPanelComposer does not write Inventory

default-location policy does NOT live in SellerPanelComposer

no direct InventoryDbContext in Offer seller route

Any fallback/default location behavior must belong to Inventory policy/application, not Offer/Host.

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R4/inventory-boundary.md

9. Collapse / remove IOfferSellerPanel

Goal:
IOfferSellerPanel must be deleted by end of R4.

If a residual interface remains, it must be renamed/re-scoped to a non-Offer concern and live in the correct owner module.

Offer Application may not depend on Host implementation.

Offer Endpoints may not depend on Host BFF.

Architecture guard:
fail if IOfferSellerPanel appears in Offer production code after R4.

10. Make Create/Patch cohesive use cases

Current transport layer sequences multiple commands.

Refactor so API-level business intent is handled by one Application use case per endpoint where appropriate.

For Create:
support current request semantics:

CatalogVariantId

SellerPartyId

SellerSku

initial status if supported

return policy if supplied

quantity limits if supplied

Application owns sequencing/invariants and persistence semantics.

For Patch:
one cohesive command should apply the supported patch atomically/coherently rather than transport sending several independent commands.

Do not create distributed transactions across Pricing/Inventory/Tax.

Offer-owned fields only.

Endpoint should be thin:
authorize → construct command → send → map response.

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R4/cohesive-usecases.md

11. Seller ownership and authorization

Separate:

authentication/transport actor extraction at Endpoint/Host edge

seller ownership/business access semantics in Application use case/query

Do not rely only on endpoint post-filter like:
offer.SellerPartyId != sellerPartyId

Prefer seller-scoped Query/Command inputs and handler enforcement.

Stable semantic error codes only.

No hidden fail-open guard.

Audit OpenOfferUseCaseGuard.

If it is an intentionally permissive placeholder with no real value:

remove it from Golden Offer
OR

replace it with an actual authorization/permission port wired to current platform access-control boundary.

Do not keep an always-pass guard in a reference module simply for shape.

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R4/access-boundary.md

12. Error/localization cleanup

Offer final path must not contain scattered hardcoded response prose.

Replace ad-hoc:

"Offer not found."

Persian Offer-specific Host literals

duplicated error-code literals such as "offer.missing" where a canonical constant should exist

Use:

OfferErrorCodes

SemanticError/SemanticException

endpoint localizer/resource boundary

Status mapping must be explicit and stable:

validation/business conflict

not found

forbidden where applicable

Do not treat every SemanticException as HTTP 400 if canonical error semantics distinguish statuses.

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R4/error-boundary.md

13. Language and namespace standardization

Scan ALL non-generated Offer production files, not only changed files.

Requirements:

namespaces reflect folder/project ownership

identifiers English

XML docs/comments English

no Persian prose in Domain/Application/Infrastructure

Endpoints localization text only through localizer/resources

generated EF migrations exempt from comment-language cleanup

no malformed BOM/encoding side effects

Host files touched for Offer extraction:
remove Offer-related Persian hardcoded text.

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R4/language-namespace-scan.md

14. Source size / god file gate

R4 must measure:

all Offer production files

SellerPanelComposer after extraction

Expected:

no Offer source >800 LOC

Offer use-case files cohesive and small

SellerPanelComposer must shrink materially because Offer read/write responsibilities are removed

Do not split SellerPanelComposer mechanically just to hit a number.
Only remove/extract responsibilities to proper owners.

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R4/source-size.md

15. Strengthen architecture guards again

Add/upgrade guards proving:

No IOfferSellerPanel in Offer production.

Offer endpoint List/Get/Create/Patch do not call Host BFF.

Offer endpoint does not send-and-ignore query result.

Offer seller read result comes from MediatR query.

No OfferDbContext outside Offer.Infrastructure persistence.

SellerPanelComposer contains no OfferDbContext / Offer read-shaping methods.

No PricingDbContext/InventoryDbContext write path for these routes in SellerPanelComposer.

Offer Application has no foreign Application refs.

per-use-case folder/file structure exists.

no OfferRequests.cs, OfferHandlers.cs, OfferQueries.cs, OfferQueryHandlers.cs broad dumping files remain.

no Persian prose in Offer Domain/Application/Infrastructure.

no TypeForwarders.

no Domain→Contracts.

no production source >800 LOC.

Do not guard only filenames if semantic/dependency verification can be done structurally.

16. Tests

Required behavior tests:

Offer:

seller-scoped list

seller-scoped get

unauthorized/wrong-seller get

create with initial status/policy/limits

patch with status/policy/limits

archive reactivation failure

duplicate SKU/listing

missing Catalog Variant

missing Seller Party

Read enrichment:

Catalog title

price

available inventory

missing enrichment data graceful behavior

Pricing route:

reaches Pricing owner boundary

no Host DbContext write

Inventory route:

reaches Inventory owner boundary

default location behavior belongs to Inventory

no Host DbContext write

Architecture:
all new guards green.

Run:

Offer.Tests

affected Pricing tests

affected Inventory tests

focused Host seller tests

backend solution build

No fake/skipped tests.

17. Final R4 residual scan

Search full repository for Offer-related legacy patterns:

IOfferSellerPanel

OfferDbContext outside Offer.Infrastructure/tests/migration tooling

direct _offers.Offers usage in Host

direct _prices.Prices in SellerPanelComposer

direct _inventory.Positions in SellerPanelComposer

SellerOfferListItem construction in Host

SellerOfferDetailPage construction in Host

Offer-specific PlatformHttpException literals

Offer namespace mismatches

direct foreign Application refs from Offer

TypeForwardedTo

Persian literals in Offer non-generated production

direct time/id APIs

catch-ignore

polling/magic delays

test-only branches

Produce:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R4/residual-scan.md
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R4/residual-scan.json

No mandatory Offer defect may be silently deferred.

18. Recovery State

Update:

Master Recovery

Architect Bootstrap

Capability Map only if boundary facts changed

R4 recovery-sot

If R4 closes every mandatory Offer defect except final verification:
return:

Module-Recovery-State: READY_FOR_FINAL_VERIFICATION
Next-Recommended-Task: TB-TMAR-OFFER-REFERENCE-W1-R5

Do NOT return COMPLETE yet.

R5 will be an Architect-grade final verification/repair wave:
full repository scan, build/tests, structure, namespace, MediatR, boundary, localization, no workarounds, then COMPLETE or further repair.

19. Git discipline

Stage only task-owned files.
No git add ..

Do not stage user .rar archives.

Before commit:
git diff --cached --name-only

Frontend:
NONE.

Stashes:
NONE touched.

20. Acceptance Criteria

PASS only if:

per-use-case Application folder structure exists

broad aggregate command/query files removed

endpoint CQRS is real, not ceremonial

no ignored MediatR query results

IOfferSellerPanel deleted

Offer list/detail enrichment no longer in Host

SellerPanelComposer no longer uses OfferDbContext for Offer path

Pricing write is owned by Pricing boundary

Inventory write is owned by Inventory boundary

default Inventory location policy moved to Inventory owner

Create/Patch endpoint use cases are cohesive

seller ownership enforced in Application

permissive OpenOfferUseCaseGuard removed or replaced with real boundary

hardcoded Offer error prose removed

Offer namespaces ownership-correct

Offer production language standardized

architecture guards strengthened

tests/build green

anti-pattern scan clean

frontend untouched

user work preserved

truthful state = READY_FOR_FINAL_VERIFICATION

next task = R5

Result Contract

Return ONLY canonical BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Track
Recovery-Start
Architect-Verified-R3-State
TMAR-Execution-Mode
Frontend-Production-Changes
UseCase-Foldering
CQRS-Read-Path
Offer-Seller-Panel-Removal
Read-Model-Boundaries
Pricing-Boundary
Inventory-Boundary
Cohesive-Create-Patch
Access-Boundary
Error-Boundary
Language-Namespace-Scan
Source-Size
Architecture-Guards
Focused-Validation
Residual-Scan
AntiPattern-Gate
Capability-Map
Bootstrap
Master-Recovery-State
Recovery-SoT
Git
Architectural-Concerns
Blockers
User-Work-Preserved
Product-Resume-Safety
Module-Recovery-State
Next-Recommended-Task

Expected:
Module-Recovery-State: READY_FOR_FINAL_VERIFICATION
Next-Recommended-Task: TB-TMAR-OFFER-REFERENCE-W1-R5

After canonical Result:
STOP completely.
Do NOT poll.
Do NOT fetch next task.
Do NOT write Worker IDLE.

END_TOOBA_REPAIR_TASK
