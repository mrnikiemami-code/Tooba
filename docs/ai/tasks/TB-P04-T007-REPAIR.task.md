Tooba — TB-P04-T007 — REPAIR — REAL SHOPEIVA PRESERVATION

BEGIN_TOOBA_CURSOR_TASK_V1

Protocol-Version: 1
Task-ID: TB-P04-T007
Repair: YES
Phase: P04 — Experience Foundation
Type: Storefront Fidelity Repair
Repository: https://github.com/mrnikiemami-code/Tooba
Branch: main
Execution-Mode: PIPELINE

Architect Decision

Previous TB-P04-T007 result is REJECTED.

Functional backend composition is useful and may be preserved.

Visual integration is NOT accepted.

The live screenshots were directly reviewed by the Architect.

Observed critical failures:

The Tooba Storefront does not visually preserve Shopeiva.
Home is a simplified custom composition.
PDP is a simplified custom composition.
Large portions of desktop viewport are blank.
Content is constrained to a narrow strip.
The real Shopeiva header/navigation/mega-menu/card/PDP composition is missing.
Mobile evidence is incomplete and visually broken.
The result is "Shopeiva-like", not Shopeiva mounted on Tooba.

This violates the locked commercial rule:

PRESERVE SHOPEIVA
→ MINIMUM CHANGE
→ CONNECT TOOBA BACKEND
→ SELL QUICKLY

Do NOT build another custom approximation.

Repository Recovery

Run:

git rev-parse --show-toplevel
git fetch origin
git branch --show-current
git rev-parse HEAD
git rev-parse origin/main
git status --short --branch

Expected predecessor:

e6836198852d9b648b5f10416064c05cc012442f

Require:

branch = main
HEAD == origin/main
safe/known working tree

No force push.
No destructive reset.
No silent stash.
No history rewrite.

CORE REPAIR RULE — NON-NEGOTIABLE

Do NOT implement:

Shopeiva-like
inspired by Shopeiva
simplified Shopeiva
Tooba interpretation of Shopeiva

Implement:

REAL SHOPEIVA COMPONENT/LAYOUT PRESERVATION

Use the runtime Shopeiva reference already studied in TB-P04-T006.

The rendered Tooba page should remain recognizably the purchased Shopeiva template.

The primary work is:

replace demo/mock data bindings with Tooba live API bindings

NOT:

redesign components
recreate layouts from memory
make simplified replacements
REUSE EXPECTATION

For these areas, prefer direct component/layout reuse or the closest technically safe adaptation:

Header
Top promo bar
Search bar
Navigation
Category navigation
Mega menu
Hero composition
Category cards
Product cards
Product sections/carousels
Footer
Trust/newsletter area
PDP breadcrumb
PDP gallery
PDP product info
PDP pricing block
PDP seller area
PDP quantity/CTA area
PDP feature/trust blocks
PDP tabs

If a Shopeiva component cannot be reused directly because of incompatible project versions/dependencies:

document exact incompatibility;

port the component structure/styles with minimum transformation;

preserve DOM/layout/styling behavior as closely as practical;

do NOT replace it with a new simplified design.

VISUAL FIDELITY TARGET

The Tooba live pages should visually remain close to the actual Shopeiva runtime captured in TB-P04-T006.

This includes:

page width
header density
navigation hierarchy
card proportions
section spacing
category presentation
product card composition
PDP 3-column/structured composition
footer density
responsive behavior

The theme accent may be changed from red to approved blue.

Theme change must NOT alter layout.

DESKTOP WIDTH REQUIREMENT

At:

1440x900

the Storefront must use the viewport similarly to Shopeiva.

Forbidden:

content constrained to a tiny side strip
large unexplained white area
main content using only a fraction of viewport width

Measure and record:

documentElement.clientWidth
main content bounding width
main content width / viewport width

For Home/Listing/PDP desktop:

main visual canvas should intentionally occupy the normal Shopeiva content width

Do not artificially center a 200–700px app inside 1440px.

ROUTE — HOME

Use actual Shopeiva Home structure.

Minimum visible Shopeiva fidelity:

top promo area
full header
search
nav/category controls
hero/promotional composition
category section
product section using Shopeiva card family
footer/trust region

Use real Tooba product/category data where business truth is needed.

Static Shopeiva presentational assets may be preserved for initial sellable version if legally part of purchased template and not false business claims.

Do NOT replace a full Shopeiva section with a plain gradient box merely because backend data is not ready.

ROUTE — LISTING

Use actual Shopeiva PLP/listing visual structure.

Preserve:

header
breadcrumb/category context
filter/sort visual pattern
product card grid
pagination/loading patterns

Business data must come from Tooba.

Do not invent a different listing design.

ROUTE — PDP

Use actual Shopeiva PDP composition.

Preserve as much as practical:

header
breadcrumbs
gallery
thumbnail rail
product identity
rating seam
variant/color selection
price area
availability
quantity
wishlist/actions
seller block
trust/feature blocks
tabs/detail sections
footer

Map Tooba semantics into Shopeiva UI.

For multi-seller:

primary resolved offer in the main purchase box
other sellers as a minimal additional seller seam

Do NOT redesign the entire PDP because Tooba has multiple sellers.

BUSINESS TRUTH

Keep current correct backend separations:

Product != Variant
Product != Offer
Offer != Price
Product != Inventory
Pricing != Tax

No:

Product.Price
Product.Stock
cross-module SQL JOIN
frontend direct DB
frontend offer guessing

The existing Host Storefront resolver can remain temporarily if deterministic and documented.

THEME

Approved initial visual change:

Shopeiva red primary accent
→ professional Tooba blue

Use central tokens/variables.

Preserve semantic colors.

No broad visual redesign.

INITIAL SELLABLE SCOPE

Keep:

Persian
RTL
single initial presentation currency/market UX

Do NOT spend this repair on:

full multilingual UI
full LTR
advanced currency switcher
theme configurator
deep visual polish

Those come later.

MEDIA

The current presentation SVG seam is acceptable only as a temporary backend/media limitation.

However:

The Shopeiva layout must preserve the actual image/gallery dimensions and behavior.

Do not let missing Media pipeline collapse the visual composition.

Use purchased-template demo/presentation assets where necessary for visual shell proof, clearly separated from business truth.

CART CTA

Cart mutation remains outside this repair unless trivial to connect safely.

Do NOT fake add-to-cart.

The visual CTA should remain in the exact Shopeiva-style purchase area.

REAL MOBILE

Capture with actual CSS viewport:

390x844

No fake narrow mode.

Mobile must preserve Shopeiva responsive behavior.

No horizontal page overflow.

REQUIRED LIVE EVIDENCE

Capture from Tooba live app:

docs/evidence/TB-P04-T007/repair/

Required:

01-home-1440x900-rtl.png
02-listing-1440x900-rtl.png
03-pdp-1440x900-rtl.png
04-home-390x844-rtl.png
05-listing-390x844-rtl.png
06-pdp-390x844-rtl.png
07-pdp-multi-seller-1440x900.png
08-header-megamenu-1440x900.png
09-footer-1440x900.png
10-blue-theme-product-cards.png

All must be:

Tooba runtime
live API business data
no fixture banner
no dev/debug UI
SIDE-BY-SIDE FIDELITY EVIDENCE

Create:

docs/evidence/TB-P04-T007/repair/shopeiva-vs-tooba-fidelity.md

For:

Home
Listing
PDP
Header
Product Card
Footer

record:

Shopeiva runtime screenshot/reference
Tooba live screenshot
preserved component/layout
necessary deviation
reason

The accepted result must demonstrate:

Tooba looks like Shopeiva with Tooba data

not:

Tooba is merely inspired by Shopeiva
MEASURED LAYOUT EVIDENCE

Create:

docs/evidence/TB-P04-T007/repair/layout-measurements.md

For Home/Listing/PDP desktop:

viewport width
main shell width
main content width
blank unexplained region
page horizontal overflow

For mobile:

CSS viewport
PNG dimensions
page horizontal overflow
FULL VALIDATION — REQUIRED

The previous RESULT did not satisfy the original validation contract.

Run ALL now.

Backend:

dotnet restore src/backend/Tooba.slnx
dotnet build src/backend/Tooba.slnx
dotnet test src/backend/Tooba.slnx

NO test filters.

Require:

warnings = 0
errors = 0
failed = 0
skipped = 0

Report exact total passed count.

Frontend:

cd src/frontend
npm ci
npm run typecheck
npm run lint
npm run test:grid
npm run test:workspace
npm run test:product-workspace

Run the focused Storefront tests explicitly.

Then:

npm run build

Finally:

git diff --check
git status --short --branch

Do not substitute "existing node_modules" for npm ci.

VISUAL REVIEW SELF-CHECK

Before PASS:

Does Home still look like a custom Tooba page? -> must be NO
Does PDP still look simplified compared with Shopeiva? -> must be NO
Is more than ~20% of desktop viewport unexplained blank space? -> must be NO
Is Shopeiva header/navigation recognizable? -> must be YES
Are Shopeiva product cards recognizable? -> must be YES
Is Shopeiva PDP composition recognizable? -> must be YES
Is mobile actual 390px responsive? -> must be YES

Cursor self-check is not Architect acceptance.

SoT

Keep:

TB-P04-T006 = ACCEPTED
TB-P04-T007 = REPAIR / AWAITING_ARCHITECT_ACCEPT
P04 = IN_PROGRESS
TB-P04-T008 = NOT ISSUED

Record the locked rule:

Initial sellable UI must preserve Shopeiva with minimum change.
Save Envelope VERBATIM

Save exactly:

docs/ai/tasks/TB-P04-T007-REPAIR.task.md
Result Contract

Return:

BEGIN_TOOBA_CURSOR_RESULT_V1

Protocol-Version: 1
Task-ID: TB-P04-T007
Repair: YES
Phase: P04 — Experience Foundation
Status: PASS | BLOCKED | RECOVERY_CONFLICT

Summary:
...

Repository-Recovery:
- ...

Shopeiva-Direct-Reuse:
- Header:
- Mega menu:
- Home sections:
- Product cards:
- Listing:
- PDP:
- Footer:

Necessary-Deviations:
- ...

Measured-Desktop:
- Home viewport/content:
- Listing viewport/content:
- PDP viewport/content:
- unexplained blank area:
- page overflow:

Measured-Mobile:
- viewport:
- PNG dimensions:
- horizontal overflow:

Live-Data:
- Catalog:
- Categories:
- Offers:
- Seller:
- Pricing:
- Tax:
- Inventory:
- Promotion:
- Media:

Multi-Seller:
- ...

Theme:
- primary blue token:
- layout changed:
- semantic colors preserved:

Visual-Evidence:
- 01:
- 02:
- 03:
- 04:
- 05:
- 06:
- 07:
- 08:
- 09:
- 10:
- Fidelity file:
- Layout measurements:

Validation:
- dotnet restore:
- dotnet build:
- warnings:
- errors:
- dotnet test:
- total passed:
- failed:
- skipped:
- npm ci:
- typecheck:
- lint:
- grid tests:
- workspace tests:
- product-workspace tests:
- storefront tests:
- frontend build:
- git diff --check:

Architecture:
- Product.Price:
- Product.Stock:
- cross-module SQL join:
- frontend domain guessing:

Git:
- commit:
- push:
- final HEAD:
- origin/main:
- final status:
- synchronized:

Source-of-Truth:
- TB-P04-T006: ACCEPTED
- TB-P04-T007: AWAITING_ARCHITECT_ACCEPT
- TB-P04-T008 Issued: NO
- P04: IN_PROGRESS
- Recovery-ready:

Visual-Concerns:
- ...

Architectural-Concerns:
- ...

Blockers:
- ...

END_TOOBA_CURSOR_RESULT_V1

After RESULT:

Surface the new LIVE Tooba Home/Listing/PDP screenshots directly in this SAME chat/session.

CRITICAL — RESULT DOES NOT END THIS AGENT SESSION

After RESULT:

PIPELINE_STATE: WAITING_FOR_ARCHITECT_IN_SAME_SESSION

Then:

DO NOT CLOSE OR LEAVE THIS SESSION.
WAIT HERE FOR THE NEXT AUTHORIZED ENVELOPE.

Do not finish.
Do not exit.
Do not leave PIPELINE.
Do not self-advance.
Do not invent TB-P04-T008.

RESULT = CONTROL RETURNED TO ARCHITECT.
RESULT != WORKFLOW FINISHED.

Cursor PASS is not Architect ACCEPT.

Leaving or ending the pipeline after RESULT is a protocol violation.

END_TOOBA_CURSOR_TASK_V1
