Tooba — TB-P04-T005 — REPAIR — Live Visual & End-to-End Evidence

BEGIN_TOOBA_CURSOR_TASK_V1

Protocol-Version: 1
Task-ID: TB-P04-T005
Repair: YES
Phase: P04 — Experience Foundation
Type: REPAIR / Live Visual Acceptance Evidence
Repository: https://github.com/mrnikiemami-code/Tooba
Branch: main
Execution-Mode: PIPELINE
Architect-Decision-On-Previous-Result: REPAIR_REQUIRED

Why This Repair Exists

The Product Workspace implementation is functionally promising, but Architect ACCEPT is withheld because the submitted visual evidence was not captured against a live Tooba backend.

Reported limitations:

Host was down during capture
UI used an explicit fixture banner
Conflict screenshot was a UI demonstration, not a live HTTP 409

For the first Serious UI implementation:

Functional PASS != Visual ACCEPT
Fixture-only evidence != live product evidence

This repair is focused on live end-to-end evidence and any bounded defects discovered while obtaining it.

Do NOT redesign the Workspace unless live evidence exposes a real UX defect.
Do NOT start TB-P04-T006.

Repository Recovery

Run:

git rev-parse --show-toplevel
git fetch origin
git branch --show-current
git rev-parse HEAD
git rev-parse origin/main
git status --short --branch

Expected predecessor:

37f07be9c8947e70b97357ef9f96d6b7cf40c4cd

Require synchronized safe main.

Unsafe/ambiguous state => RECOVERY_CONFLICT.

No force push.
No history rewrite.
No destructive reset.
No silent stash.

Live Backend Requirement

Start the real Tooba backend/Host using the repository-supported development configuration.

Use a safe local development tenant/store setup.

The Product Workspace evidence must come from real HTTP responses from:

/v1/admin/products

and relevant Product Workspace endpoints.

Hard rules:

no fixture fallback for acceptance screenshots
no hard-coded fake Product/Offer/Price/Inventory data
no Shopeiva mock JSON as business truth

Synthetic seed data is acceptable only if it is inserted through the real Tooba application/persistence path and then read back through real HTTP APIs.

Live Data Composition

Evidence must prove that the UI is consuming a composed view whose sources remain separated:

Catalog
Offer
Pricing
Tax
Inventory

No cross-module SQL join may be introduced to simplify the evidence.

For a representative Product, the live workspace should show where applicable:

Product identity
Variant(s)
multiple Seller Offers
current Pricing
Tax classification
multi-location Inventory
publication state
SEO/content seams
history/audit

If one backend area genuinely has no implemented mutation/read support yet, show it explicitly as unsupported/read-only rather than fabricating data.

Multi-Seller Evidence

Capture at least one live Marketplace-style Product/Variant with:

Seller A Offer
Seller B Offer

and independent commercial/inventory summaries.

Prove visually that:

one Variant != one Seller

and that Offer/Pricing/Inventory remain distinct concepts.

Live Error / Permission / Conflict Evidence

At least one non-happy-path state must be real, not a static showcase toggle.

Preferred examples:

actual HTTP 409 concurrency conflict
actual permission/read-only response
actual section API failure
actual 404/not-found

A live 409 is preferred if current API supports optimistic concurrency.

If a real permission model is not wired to this surface yet, use a real API-level read-only/forbidden response if available.

Do not weaken security or inject production-only hacks merely to create evidence.

Visual Evidence — MANDATORY

Capture real screenshots from the live app.

Minimum required:

1. Admin Products list — desktop RTL — live backend
2. Product Workspace Overview — desktop RTL — live backend
3. Variants section — live backend
4. Commercial section with at least two Seller Offers — live backend
5. Inventory section with location-level OnHand/Reserved/Available — live backend
6. SEO & Content section — live backend/read-only seam if necessary
7. Mobile Product Workspace — live backend
8. LTR Product Workspace — live backend
9. Dark theme representative state — live backend
10. One real error/conflict/read-only state

Store appropriate evidence under:

docs/evidence/TB-P04-T005/live/

Do not capture secrets/tokens.

If browser/screenshot tooling genuinely cannot capture files, then this task is BLOCKED, not PASS.

Fixture Banner

The fixture/demo fallback may remain only for internal showcase/development if useful.

It must be visually and architecturally impossible to confuse with production/live acceptance.

For production Admin routes:

fixture fallback must not silently activate

If backend is unavailable, show an actual error/retry state.

Route / API Truth

Verify production routes:

/admin/products
/admin/products/[productId]

use live API integration by default.

No acceptance screenshot may rely on development fixture query flags or mock stores.

Product Workspace UX Review

While capturing live evidence, inspect and repair only bounded UX defects in:

visual hierarchy
spacing
density
section discoverability
action hierarchy
mobile layout
RTL/LTR
empty/error states
multi-seller readability
Pricing vs Offer vs Inventory separation

Do not add unrelated features.

Visual Quality Bar

Reject and repair if the live workspace still resembles:

basic CRUD
form dump
developer diagnostics
unstyled admin
module-per-tab technical UI

The page should feel like a coherent product-management workspace.

Backend Validation — CURRENT AND EXPLICIT

Run all three NOW:

dotnet restore src/backend/Tooba.slnx
dotnet build src/backend/Tooba.slnx
dotnet test src/backend/Tooba.slnx

Require:

Build warnings = 0
Build errors = 0
Failed = 0
Skipped = 0

Do not infer build success from dotnet test.

Frontend Validation

Run:

cd src/frontend
npm ci
npm run typecheck
npm run lint
npm run test:grid
npm run test:workspace
npm run test:product-workspace
npm run build

Require all available tests green.

Then from repo root:

git diff --check
git status --short --branch
Architecture Recheck

Confirm:

Product != Offer
Offer != Price
Product != Inventory
Price != Tax
Backend/module boundary != UI boundary

And:

no Product.Price
no Product.Stock
no frontend direct DB
no cross-module SQL join
Evidence Index

Update:

docs/evidence/TB-P04-T005/visual-review-index.md

For each screenshot record:

file
route
viewport
theme
direction
tenant/context
live API = yes
business state
what architectural/UX invariant it proves
Architecture Document

Update if needed:

docs/architecture/55-admin-product-workspace.md

Document the actual live integration path and remove wording that could imply fixture-backed production behavior.

SoT

Keep:

TB-P04-T005 = REPAIR IN PROGRESS / AWAITING_ARCHITECT_ACCEPT
P04 = IN_PROGRESS
Visual Acceptance = PENDING

Do NOT mark T005 accepted.
Do NOT issue T006.

Save Envelope VERBATIM

Save exactly:

docs/ai/tasks/TB-P04-T005-REPAIR.task.md

Do not summarize or condense.

Git

Suggested commit:

fix validate admin product workspace live evidence [TB-P04-T005]

Push origin/main, fetch, require:

HEAD == origin/main

No force push.

Result Contract

Return:

BEGIN_TOOBA_CURSOR_RESULT_V1

Protocol-Version: 1
Task-ID: TB-P04-T005
Repair: YES
Phase: P04 — Experience Foundation
Status: PASS | BLOCKED | RECOVERY_CONFLICT

Summary:
...

Repository-Recovery:
- ...

Live-Backend:
- Host:
- Tenant/Context:
- API base:
- Fixture fallback used for acceptance:
- Seed/data path:

Live-Composition:
- Catalog:
- Variants:
- Offers:
- Pricing:
- Tax:
- Inventory:
- SEO/Content:
- Publication:
- History/Audit:

Multi-Seller-Evidence:
- ...

Live-Error-Conflict-Permission:
- Type:
- HTTP/API evidence:
- UI behavior:

Routes:
- ...

Visual-Evidence:
- Product list:
- Overview:
- Variants:
- Commercial:
- Inventory:
- SEO/Content:
- Mobile:
- LTR:
- Dark:
- Error/conflict/read-only:
- Evidence index:

UX-Repairs:
- ...

Architecture-Recheck:
- ...

Validation:
- backend restore:
- backend build:
- build warnings:
- build errors:
- backend tests:
- backend passed:
- backend failed:
- backend skipped:
- frontend install:
- frontend typecheck:
- frontend lint:
- grid tests:
- workspace tests:
- product-workspace tests:
- frontend build:
- git diff --check:

Git:
- Commit:
- Push:
- Final-HEAD:
- Final-Origin-Main:
- Final-Status:
- Head-Matches-Origin:

Source-of-Truth:
- Current Task:
- Task State:
- Current Phase:
- Visual Acceptance:
- T006 Issued:
- Recovery-Ready:

Architectural-Concerns:
- ...

Visual-Concerns:
- ...

Blockers:
- ...

END_TOOBA_CURSOR_RESULT_V1
CRITICAL — DO NOT LEAVE PIPELINE

After sending RESULT:

WAIT HERE for the USER / Architect to provide the next valid task
in this SAME chat/session.

You MUST remain inside the Tooba Architect-controlled pipeline.

Do NOT:

close this chat/session
end the agent workflow
leave PIPELINE mode
treat RESULT as end of work
move to another chat
wait outside this pipeline
invent the next task
infer the next task
prepare the next task
start Order Workspace
start Seller Workspace
start Customer Workspace
start Storefront overhaul

After RESULT, stay active in this SAME chat/session and wait here until the USER / Architect sends the next valid Envelope.

Only when a new valid Envelope is provided in this SAME chat/session may you execute the next task.

Cursor PASS is not Architect ACCEPT.
Functional PASS is not Visual ACCEPT.
Fixture evidence is not Live Visual ACCEPT.

Leaving or ending the pipeline after RESULT is a protocol violation.

END_TOOBA_CURSOR_TASK_V1
