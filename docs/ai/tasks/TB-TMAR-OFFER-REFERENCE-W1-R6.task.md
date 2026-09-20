PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_REPAIR_TASK

Task-ID:
TB-TMAR-OFFER-REFERENCE-W1-R6

Parent-Task:
TB-TMAR-OFFER-REFERENCE-W1-R5

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
Offer Golden Module R6 — Remove All Host OfferDbContext Leaks and Close Admin/Storefront/Grid/Seed Boundaries

Backend-Only:
YES

TMAR-Execution-Mode:
BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE

0. Architect Review of R5

R5 correctly returned:

Module-Recovery-State: INCOMPLETE

The Architect directly inspected current main and confirmed the blocker is real.

Representative verified Host leaks:

AdminPanelComposer

Direct:

OfferDbContext

_offers.Offers

active offer counts

seller IDs derived from Offer persistence

seller listing composition from Offer persistence

ProductWorkspaceComposer

Direct:

OfferDbContext

_offers.Offers

product/variant → Offer mapping

then Host continues into Pricing/Inventory enrichment

MerchandisingCampaignAdminComposer

Direct:

OfferDbContext

campaign Offer candidate/detail composition through Offer persistence

AdminProductGridQueryEngine

Direct:

OfferDbContext

Offer-count/filter/sort metrics

AdminSellersGridQueryEngine

Direct:

OfferDbContext

seller discovery and offer-count metrics

StorefrontComposer

Direct:

OfferDbContext

Storefront Offer selection/composition

R5 evidence also reports Host seed/bootstrap OfferDbContext use.

Therefore Offer is NOT complete until Host stops depending on Offer persistence.

This task is limited to removing Offer ownership leaks.
Do NOT rewrite unrelated Catalog/Order/Pricing/Inventory architecture unless a narrow contract seam is required.

1. Git / Recovery Safety

Repository:
D:\Users\User\source\repos\SarvNewVer

Use actual current HEAD from repo.
Expected lineage includes R5 tip:
38d39a7998acbc7ccbd444d249f6b9255183d24a
plus canonical result/tip-alignment commits if present.

Verify:

branch main

HEAD == origin/main

no unexpected tracked modifications

staged = 0

protected commit 18ca10c9 remains ancestor

stashes untouched

user work preserved

User .rar archives are protected user work:

do not stage

do not delete

do not rename

do not overwrite

Forbidden:

git reset

git clean

destructive checkout/restore

unsafe rebase

stash pop/drop

broad git add .

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R6/recovery-start.md

2. First perform an exact repository-wide Offer persistence leak inventory

Search ALL production code outside:

src/backend/Modules/Offer/Tooba.Offer.Infrastructure/**

for:

OfferDbContext

Tooba.Offer.Infrastructure.Persistence

.Offers

Offer EF entities/config access

Offer migrations/snapshot access

direct DbSet<SellerOffer>

direct EF query over SellerOffer

GetRequiredService<OfferDbContext>

constructor injection of OfferDbContext

Classify every match:

HOST_ADMIN

HOST_STOREFRONT

HOST_GRID

HOST_SEED_BOOTSTRAP

FOREIGN_MODULE

TEST_ONLY

TOOLING_ONLY

LEGITIMATE_OFFER_INFRASTRUCTURE

Produce:

docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R6/offer-persistence-leak-inventory.md

docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R6/offer-persistence-leak-inventory.json

Mandatory completion target:
production Offer persistence access exists ONLY in Offer.Infrastructure.

3. Design one stable Offer read boundary — do not create ad-hoc interfaces per Host file

Do not solve each Host leak with a bespoke one-off interface.

Create/extend stable Offer Contracts read APIs that represent reusable Offer-owned information.

The exact shape must be driven by current usages, but likely capabilities include:

active Offer count

distinct seller IDs with Offers

Offer counts by seller

Offer counts by Catalog Variant / Product mapping inputs

Offer references by IDs

Offers by Catalog Variant IDs

seller/variant membership

active/sellable Offer lookup for Storefront

candidate Offer lookup for merchandising

batch lookup to avoid N+1

Prefer narrowly cohesive ports, e.g. read/query gateway(s), not a god IOfferEverythingGateway.

Rules:

Contract DTOs in Tooba.Offer.Contracts...

no EF types

no Domain aggregate leakage unless already an explicitly approved public contract

no Host DTOs in Offer.Contracts

batch APIs where Host currently loops

no localized strings

no direct Pricing/Inventory data inside Offer contracts

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R6/offer-read-contract.md

4. Implement the Offer-owned read adapter in Offer.Infrastructure

Implement the read contract against OfferDbContext inside:

Tooba.Offer.Infrastructure

Requirements:

all EF access stays inside Offer.Infrastructure

AsNoTracking for pure reads

projection before materialization where practical

batch queries

no cross-module joins

no Pricing/Inventory/Catalog/Party DbContext

no Host models

no UI localization

no business decisions belonging to foreign modules

Register through Offer module DI.

Do not create a generic repository abstraction purely to hide DbContext.

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R6/offer-read-adapter.md

5. AdminPanelComposer extraction

Remove OfferDbContext from AdminPanelComposer.

Current Offer-owned needs include at least:

active Offer count

seller IDs derived from Offers

seller Offer counts/listing metrics

Replace with Offer Contracts read boundary.

Rules:

AdminPanelComposer may compose results from multiple module Contracts

it must not know Offer EF/persistence

do not move Admin dashboard DTOs into Offer

preserve output behavior

After R6:

no OfferDbContext field/ctor arg

no _offers.Offers

no using Tooba.Offer.Infrastructure.Persistence

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R6/admin-panel-extraction.md

6. ProductWorkspaceComposer extraction

Remove OfferDbContext from ProductWorkspaceComposer.

Current flow:
Catalog Product/Variant → Offer IDs → Pricing/Inventory enrichment.

Required:

Catalog still owns Product/Variant

Offer contract returns Offer mapping/count/reference data for the provided variant IDs

Pricing/Inventory remain owned by their modules

Do NOT let Catalog query Offer persistence.
Do NOT put Pricing/Inventory values into Offer persistence DTO merely for convenience.

Preserve workspace behavior.

After R6:

no OfferDbContext

no Offer.Infrastructure reference

no direct SellerOffer EF query

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R6/product-workspace-extraction.md

7. AdminProductGridQueryEngine extraction

Remove OfferDbContext from grid engine.

Offer metrics used by:

offerCount

search/filter/sort paths that rely on Offer membership/count

Use Offer Contracts batch metrics.

Be careful:
grid semantics must remain correct for:

filtering

sorting

advanced filters

pagination

Do not turn every row into N+1 remote-style calls.

If cross-module metric sorting currently loads an ID/metric dictionary in memory, preserve behavior through a batch contract.

After R6:

no OfferDbContext in AdminProductGridQueryEngine

no Offer Infrastructure reference

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R6/admin-product-grid-extraction.md

8. AdminSellersGridQueryEngine extraction

Remove OfferDbContext.

Current semantics:

seller universe = sellers that have Offer rows

offer-count metric per seller

Use Offer Contracts:

seller IDs with Offers

offer counts by seller IDs/batch

Preserve:

search

filters

advanced filters

sorting

pagination

No N+1.

After R6:

no OfferDbContext

no Offer.Infrastructure reference

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R6/admin-sellers-grid-extraction.md

9. MerchandisingCampaignAdminComposer extraction

Remove OfferDbContext from merchandising campaign admin path.

Campaign may reference SellerOffer IDs, but the owning Promotion/Pricing/Catalog/Party logic must not read Offer persistence directly through Host.

Use Offer contract lookup:

validate/resolve Offer references

batch resolve candidate Offer metadata that Offer owns

Continue to obtain:

pricing from Pricing owner

stock/availability from Inventory owner

title from Catalog owner

seller display from Party owner

Do not create a cross-module mega DTO inside Offer.

After R6:

no OfferDbContext

no Offer.Infrastructure reference

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R6/merchandising-extraction.md

10. StorefrontComposer extraction

This is mandatory and high-risk. Characterize before editing.

Find every use of Offer persistence in StorefrontComposer.

Classify each as:

Offer selection

Offer status/channel filtering

seller preference

Offer-to-variant mapping

Offer ID retrieval

anything else

Replace those with Offer Contracts batch/read APIs.

Important:

Storefront may compose Catalog + Offer + Pricing + Inventory + Promotion

but Host must consume module boundaries, not DbContexts

do not move storefront composition into Offer

do not create cross-module SQL joins

avoid N+1

preserve seller selection semantics exactly

If current Storefront selection includes deterministic ordering/fallback among multiple Offers, encode Offer-owned selection semantics in the Offer query boundary if that logic is truly Offer-owned.
If selection is storefront presentation policy, keep policy in Storefront but use Offer contract data.

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R6/storefront-extraction.md

11. Seed / bootstrap / development data

Find all Host/development seed uses of OfferDbContext.

Classify:

production startup seed

development-only sample seed

migration/bootstrap helper

test fixture

For production/development module data seeding:
Offer-owned data creation must be owned by Offer module initialization/application boundary, not Host manipulating OfferDbContext.

Preferred:

Offer Infrastructure development seed component registered/composed by Host
OR

Offer Application command invoked by explicit development seed orchestrator

Do not use MediatR purely inside migrations if inappropriate.
Do not move environment detection/business unrelated logic into Domain.

Tests may instantiate OfferDbContext directly if clearly integration tests and not production architecture.

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R6/seed-bootstrap-extraction.md

12. Foreign module scan

Search all modules outside Host for OfferDbContext / Offer.Infrastructure.Persistence.

Any foreign production module direct persistence dependency:

replace with Offer.Contracts

no grandfathering for Golden completion

Tests/tooling:
classify explicitly.

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R6/foreign-module-scan.md

13. Remove stale project references and usings

After extraction:

Host should not need project reference to Offer.Infrastructure merely for persistence access unless Host composition root truly needs Infrastructure to register module implementation.

Composition-root reference to Infrastructure may be legitimate.

Business/composer source must not use persistence namespace.

Do NOT remove a project reference required for module bootstrapping blindly.

Differentiate:

composition dependency: allowed

business/read-model persistence dependency: forbidden

Remove stale usings/ctor args/fields.

14. Architecture guards

Strengthen guards so future changes fail if:

OfferDbContext appears in Host production source

Tooba.Offer.Infrastructure.Persistence appears in Host business/composer/grid/storefront/admin files

OfferDbContext appears in any foreign production module

Offer EF entity query occurs outside Offer.Infrastructure

Host uses _offers.Offers

direct Offer persistence is reintroduced in AdminPanelComposer

ProductWorkspaceComposer

AdminProductGridQueryEngine

AdminSellersGridQueryEngine

MerchandisingCampaignAdminComposer

StorefrontComposer

Allow:

Offer Infrastructure itself

Offer integration tests

migration tooling where exact exception is justified

Guard exceptions must be explicit paths, not blanket folder wildcard.

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R6/offer-persistence-guards.md

15. Performance / query-shape guard

Because direct DbContext removal can accidentally create N+1:

Verify:

batch methods used for product grids

batch methods used for seller grids

batch methods used for Storefront cards

no per-product/per-seller Offer query loop

no load-all Offer table where a filtered projection is possible

no synchronous blocking

Document before/after query shape qualitatively.

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R6/query-shape.md

16. Language / localization scope

Do NOT use this task to translate all Host legacy Persian comments/text.

Only clean Offer-specific hardcoded text introduced/remaining in paths touched for Offer extraction.

Unrelated Host localization debt belongs to its owner track.

Offer Domain/Application/Infrastructure language guard remains active.

17. Tests

Required focused tests:

Offer read contract:

active count

seller IDs

counts by seller

counts/mapping by variant

batch offer references

active/non-archived filtering semantics

channel/status filtering used by Storefront if applicable

Admin:

dashboard Offer count/seller count unchanged

seller list/grid offer counts unchanged

product workspace Offer counts/mapping unchanged

product grid offer count filters/sorts unchanged

Merchandising:

Offer candidate resolution unchanged

missing/invalid Offer behavior unchanged

Storefront:

product cards resolve correct Offer(s)

inactive/archived Offer exclusion unchanged

preferred seller behavior unchanged

multiple Offer deterministic selection unchanged

no Offer result case unchanged

price/inventory enrichment still owner-derived

Seed:

development bootstrap still works without Host OfferDbContext

Architecture:

no Host/foreign OfferDbContext leak

Run:

full Offer.Tests

affected Host tests

affected Storefront tests

affected Admin/Grid tests

affected Promotion/Merch tests

solution build

No fake/skipped assertions.

18. Final repository-wide scan

Search actual repository after changes for:

OfferDbContext

Tooba.Offer.Infrastructure.Persistence

_offers.Offers

direct SellerOffer EF queries

Host DbSet access to Offers

GetRequiredService<OfferDbContext>

Create exact remaining-match table:

Path | Classification | Allowed? | Reason

Expected production matches:
ONLY Offer.Infrastructure.

Test/tooling matches are allowed only if justified.

Evidence:

docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R6/final-persistence-scan.md

docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R6/final-persistence-scan.json

19. Anti-pattern gate

Scan R6 diff for:

wrapper interfaces that merely mirror DbContext without stable semantics

N+1 loops

giant god read gateway

Task.Run

polling

magic sleeps/timeouts

catch-ignore

hardcoded first Offer shortcut

.First() selection without documented deterministic policy

hidden fallback that changes seller selection

direct time/id bypass

service locator

broad suppression

widened architecture baseline

Expected:
AntiPattern-Gate: CLEAN

20. Completion decision

If and only if:

no Host production OfferDbContext access remains

no foreign production OfferDbContext access remains

all current behavior preserved

no N+1 introduced

architecture guards green

tests/build green

R5 final gates remain green

then return:

Module-Recovery-State: COMPLETE_REFERENCE_PATTERN
Next-Recommended-Task: USER_REVIEW_OFFER

If any mandatory Offer persistence leak remains:

Module-Recovery-State: INCOMPLETE
Next-Recommended-Task: TB-TMAR-OFFER-REFERENCE-W1-R7

Do not switch module.

21. Recovery docs

Update truthfully:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TOOBA-CAPABILITY-MAP.md only if boundary capability facts changed

docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R6/recovery-sot.md

If COMPLETE:
record that Host no longer owns/queries Offer persistence and the module is ready for user inspection.

22. Git discipline

Stage only task-owned files.
No broad git add ..

Before commit:
git diff --cached --name-only

Do not stage/delete .rar user files.

At end:

push origin/main

HEAD == origin/main

no staged files

no unexpected tracked modifications

stashes untouched

user work preserved

23. Result Contract

Return ONLY canonical BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Track
Recovery-Start
Architect-Verified-R5-Blocker
TMAR-Execution-Mode
Frontend-Production-Changes
Persistence-Leak-Inventory
Offer-Read-Contract
Offer-Read-Adapter
Admin-Panel-Extraction
Product-Workspace-Extraction
Admin-Product-Grid-Extraction
Admin-Sellers-Grid-Extraction
Merchandising-Extraction
Storefront-Extraction
Seed-Bootstrap-Extraction
Foreign-Module-Scan
Project-Reference-Hygiene
Query-Shape
Architecture-Guards
Focused-Validation
Full-Validation
Final-Persistence-Scan
AntiPattern-Gate
Residual-Defects
Capability-Map
Bootstrap
Master-Recovery-State
Recovery-SoT
Git
Blockers
User-Work-Preserved
Product-Resume-Safety
Module-Recovery-State
Next-Recommended-Task

Expected only if objectively clean:
Module-Recovery-State: COMPLETE_REFERENCE_PATTERN
Next-Recommended-Task: USER_REVIEW_OFFER

Otherwise:
Module-Recovery-State: INCOMPLETE
Next-Recommended-Task: TB-TMAR-OFFER-REFERENCE-W1-R7

After canonical Result:
STOP completely.
Do NOT poll.
Do NOT fetch next task.
Do NOT write Worker IDLE.

END_TOOBA_REPAIR_TASK