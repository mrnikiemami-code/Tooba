# Tooba — TB-P04-T005 — VISUAL REPAIR 3 — Measurable Admin UX Acceptance

Captured from Architect chat overlay (not Download).

BEGIN_TOOBA_CURSOR_TASK_V1

Protocol-Version: 1
Task-ID: TB-P04-T005
Visual-Repair: YES
Visual-Repair-Round: 3
Phase: P04 — Experience Foundation
Type: Serious UI Visual Repair / Measurable Acceptance
Repository: https://github.com/mrnikiemami-code/Tooba
Branch: main
Execution-Mode: PIPELINE
Architect-Decision-On-Previous-Result: VISUAL_REJECTED_AFTER_DIRECT_IMAGE_REVIEW

Direct Architect Findings

The Architect directly opened and reviewed the Round-2 screenshots.

Cursor self-review is NOT accepted as visual evidence.

Concrete observed defects:

Product List
Data Grid still looks like a technical/demo table
toolbar hierarchy is weak
raw/native-looking controls remain visually dominant
horizontal scrollbar is immediately visible
column presentation is cramped and awkward
product identity is weak
large amount of unused page space remains
the grid does not resemble a premium operational product
Workspace / Overview
workspace occupies only part of a wide desktop canvas
significant unused space remains
content feels boxed into a narrow area
English seed/product identity dominates Persian Admin UI
audit/activity cards still contain technical implementation wording
visual hierarchy remains closer to developer/admin prototype than commercial SaaS
Commercial
seller rows are still too raw
commercial hierarchy is weak
price/seller/availability relationship is not visually strong enough
horizontal overflow remains
operator must read technical table details instead of scanning a clear seller-offer surface
Inventory
summary improved but table remains visually plain
offer/seller context is still technical
inventory health is not strongly scannable
available quantity is not visually dominant enough
Mobile Evidence — CRITICAL FAILURE

The submitted screenshot labeled:

08-workspace-mobile-rtl.png

is NOT accepted as credible mobile evidence.

Observed:

desktop sidebar remains visible
desktop-style top bar remains visible
workspace remains desktop-width
large unused black area exists
layout appears to be a desktop composition inside a wider screenshot rather than a true ~390px responsive viewport

Therefore:

Mobile visual acceptance = FAILED

This repair must use real browser viewport dimensions and must capture the viewport itself, not a forced-narrow CSS simulation inside a desktop screenshot.

Repository Recovery

Run:

git rev-parse --show-toplevel
git fetch origin
git branch --show-current
git rev-parse HEAD
git rev-parse origin/main
git status --short --branch

Expected predecessor:

c8af60945810d9df3d970f23aba527f8ff2a5f4f

Require safe synchronized main.

Do not force push, reset unknown work, rewrite history, or silently stash.

NON-NEGOTIABLE VISUAL ACCEPTANCE METRICS

This round replaces subjective Cursor self-review with measurable constraints.

1. Desktop Canvas Utilization

At a real browser viewport around:

1440x900

the Admin shell + main workspace must intentionally use the viewport.

Requirements:

no unexplained blank region larger than ~20% of usable horizontal viewport
main content is fluid after sidebar
main workspace is not constrained to an arbitrary narrow fixed width
sidebar has intentional fixed/collapsible width

A screenshot with the application using only ~70-75% of the browser while the rest is blank FAILS.

2. Real Mobile Viewport

Capture with actual browser viewport:

390x844

or:

393x852

Requirements:

desktop sidebar MUST NOT remain permanently visible
desktop topbar layout MUST adapt
no page-level horizontal overflow
no desktop table compressed into tiny columns
main viewport screenshot width must actually be mobile-sized
mobile navigation must be purposeful

Report exact viewport dimensions from browser tooling.

forceNarrow is forbidden for acceptance evidence.

3. Product Grid

At 1440px desktop width:

core product columns must fit intentionally
no immediate unnecessary page-level horizontal scrollbar

If optional columns exceed space:

use sensible default visible-column set
column chooser keeps secondary columns hidden by default
sticky key/product column may be used

Default view should prioritize:

Product
Publication
Category/Brand
Variants
Offers
Price
Available
Updated
Actions

Do not display filter range sliders in every header by default.

Filters belong in:

professional filter popover/drawer/panel

or a compact filter row that does not visually overwhelm the grid.

4. Human Product Identity

Acceptance seed/view data must use polished human-readable Persian-facing values.

Example quality:

پیراهن مردانه لینن
فروشگاه آرمان
دیجی‌استایل نمونه
انبار مرکزی تهران
انبار اصفهان

Do NOT use as primary visible labels:

Live Workspace Shirt
LIVE-A
LIVE-B
workspace-live-shirt
Catalog row loaded...
Offer/Price/Stock queried separately...

Technical codes may appear only as secondary metadata/LTR islands.

5. No Architecture Lecture in Operator UI

Remove operator-visible prose such as:

قیمت و موجودی روی Product نیستند
Catalog row loaded; Offer/Price/Stock queried separately
Workspace opened from Catalog identity
ساختار بازار: گونه → پیشنهاد فروشنده ...

These are useful architecture evidence, NOT production UX.

The UI should embody architecture without explaining implementation details to the operator.

Architecture proof belongs in:

docs/evidence
docs/architecture

not the production interface.

6. Product Workspace Header

Must visually include:

product thumbnail
Persian product title
secondary product code
publication badge
commercial readiness
variant / offer / available summary
clear primary action
overflow for secondary actions

The title should not be duplicated multiple times.

7. Commercial Surface

At a glance, operator must distinguish seller offers.

Use a polished grid/card model with:

seller avatar/logo placeholder
seller display name
seller SKU secondary
offer status
channel
tax-exclusive price
availability
location count
updated/validity
actions

No raw UUIDs.

No architecture explanation paragraph.

No immediate horizontal scrollbar in default desktop view.

8. Inventory Surface

Top summary must clearly emphasize:

Available
On Hand
Reserved
Locations
Health / low stock

Location rows must show:

human location
seller/offer display context
On Hand
Reserved
Available
status

Use numeric alignment and semantic health indicators.

9. Overview

Do not repeat the same metrics in multiple generic card groups.

Create a deliberate hierarchy:

A. product identity / readiness
B. compact key metrics
C. actionable warnings
D. descriptive product information
E. activity/history preview

Technical activity details stay behind a History/Audit section or secondary inspector.

10. Persian-First RTL Production Copy

RTL acceptance screenshots must be Persian-first.

Allowed LTR islands:

SKU
codes
currency code
technical identifiers where secondary

Do not let English product names dominate the main title if testing Persian Admin quality.

11. Admin Shell

Improve sidebar/topbar to include coherent iconography and operational grouping.

At minimum:

Dashboard seam
Products active
Orders
Customers
Sellers
Inventory
Promotions
Content
Analytics
Settings

Only Products must be functional.

Inactive routes may be disabled/not navigable.

The shell must not feel like plain text in a white column.

12. Design System Component Quality

Replace visually native/browser-default-looking elements where current primitives are insufficient.

Especially inspect:

select
column controls
filter controls
pagination
buttons
checkboxes
range/resize affordances

Do not leak internal Grid resize sliders into normal product-list presentation.

Column resizing should occur through column separators/handles, not visible blue range inputs in the normal table header.

This is CRITICAL.

Professional Grid Interaction Requirement

The Data Grid capability foundation can retain accessibility fallback mechanisms internally.

But the production Product list must NOT visually expose implementation/debug controls like:

range sliders below every column header
raw reorder widgets
technical grid-state controls

Normal operator UX should be:

drag/handle resize
column chooser
filter panel
sort header
saved view
bulk toolbar

Accessible fallback can be in menus/dialogs.

Mobile Product Workspace

At true 390px:

sidebar collapsed to drawer/menu
single-column content
summary cards 1–2 per row as readable
section navigation becomes select/scroll tabs
commercial seller offers become cards or concise rows
inventory becomes mobile cards/list
no tiny text
no horizontal page scroll
sticky action optional

Capture both:

mobile overview
mobile commercial or inventory
Dark Mode

Capture real RTL dark at desktop.

Must preserve:

surface separation
table readability
muted text contrast
badge contrast
sidebar hierarchy
input boundaries
focus visibility
NEW EVIDENCE REQUIREMENTS

Capture with actual browser viewport tooling.

Store under:

docs/evidence/TB-P04-T005/visual-repair-3/

Required screenshots:

01-list-1440x900-rtl-light.png
02-overview-1440x900-rtl-light.png
03-variants-1440x900-rtl-light.png
04-commercial-1440x900-rtl-light.png
05-inventory-1440x900-rtl-light.png
06-seo-content-1440x900-rtl-light.png
07-publication-1440x900-rtl-light.png
08-mobile-overview-390x844-rtl-light.png
09-mobile-commercial-390x844-rtl-light.png
10-ltr-1440x900-light.png
11-dark-1440x900-rtl.png
12-conflict-1440x900-rtl.png

File dimensions MUST match their claimed viewport approximately.

Report actual PNG width × height.

Screenshot Integrity Script

Before RESULT, inspect dimensions programmatically and produce evidence:

file
pixel width
pixel height
claimed viewport
PASS/FAIL

Create:

docs/evidence/TB-P04-T005/visual-repair-3/screenshot-dimensions.md

A "mobile" PNG wider than ~500px FAILS unless devicePixelRatio is explicitly documented and CSS viewport dimensions are separately proven.

Browser Overflow Check

For each acceptance route, programmatically check:

document.documentElement.scrollWidth <= document.documentElement.clientWidth

for mobile.

For desktop Product list, page-level overflow must be false.

Internal grid horizontal overflow may exist only when intentionally activated by user-visible extra columns, not in default view.

Create:

docs/evidence/TB-P04-T005/visual-repair-3/overflow-check.md
Remove Production Debug Copy

Search the Admin production source for these concepts and remove them from visible operator copy:

Live Workspace
Catalog row loaded
queried separately
Workspace opened from
forceNarrow
scope=edit
Stale save

They may remain in test fixtures/source identifiers where not user-visible.

Functional / Architecture Preservation

Must remain true:

live Host HTTP
multi-seller
multi-location
real 409
Product != Offer
Offer != Price
Product != Inventory
Pricing != Tax
no Product.Price
no Product.Stock
no cross-module SQL join
Validation

Run FULL current validation:

dotnet restore src/backend/Tooba.slnx
dotnet build src/backend/Tooba.slnx
dotnet test src/backend/Tooba.slnx

Require:

warnings = 0
errors = 0
failed = 0
skipped = 0

Frontend:

cd src/frontend
npm ci
npm run typecheck
npm run lint
npm run test:grid
npm run test:workspace
npm run test:product-workspace
npm run build

Then:

git diff --check
git status --short --branch
Evidence Index

Create:

docs/evidence/TB-P04-T005/visual-repair-3-index.md

For every screenshot:

route
CSS viewport
PNG dimensions
theme
direction
live API
state
overflow check

Create 2 readable contact sheets if one would make text too small.

SoT

Keep:

TB-P04-T005 = VISUAL REPAIR ROUND 3 / AWAITING_ARCHITECT_ACCEPT
Functional Acceptance = ACCEPTED
Visual Acceptance = PENDING
TB-P04-T006 = NOT ISSUED
Save Envelope VERBATIM

Save:

docs/ai/tasks/TB-P04-T005-VISUAL-REPAIR-3.task.md
Result Contract

Return:

BEGIN_TOOBA_CURSOR_RESULT_V1

Protocol-Version: 1
Task-ID: TB-P04-T005
Visual-Repair: YES
Visual-Repair-Round: 3
Phase: P04 — Experience Foundation
Status: PASS | BLOCKED | RECOVERY_CONFLICT

...measured fields as specified in overlay...

END_TOOBA_CURSOR_RESULT_V1

After RESULT, surface the new contact sheets / raw images in this SAME session.

CRITICAL — RESULT DOES NOT END THIS AGENT SESSION

After RESULT:

PIPELINE_STATE: WAITING_FOR_ARCHITECT_IN_SAME_SESSION

Then:

DO NOT CLOSE OR LEAVE THIS SESSION.
WAIT HERE FOR THE NEXT AUTHORIZED ENVELOPE.

Do not finish.
Do not exit.
Do not self-advance.
Do not start TB-P04-T006.

RESULT = CONTROL RETURNED TO ARCHITECT.
RESULT != WORKFLOW FINISHED.

Cursor PASS is not Architect ACCEPT.
Functional ACCEPT is not Visual ACCEPT.

Leaving or ending the pipeline after RESULT is a protocol violation.

END_TOOBA_CURSOR_TASK_V1
