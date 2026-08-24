# Tooba — TB-P05-T001 — REPAIR — Real Seller Authorization + Shopeiva Vendor Fidelity

BEGIN_TOOBA_CURSOR_TASK_V1

Protocol-Version: 1
Task-ID: TB-P05-T001
Repair: YES
Phase: P05 — Operational Surface Integration
Type: Security + Visual Fidelity Repair
Repository: https://github.com/mriquemami-code/Tooba
Branch: main
Execution-Mode: PIPELINE

Architect Decision

Previous TB-P05-T001 RESULT is REJECTED / REPAIR REQUIRED.

Blocking issues:

1. Seller authorization is not yet proven through authenticated/use-case authorization.
2. Seller UI does not sufficiently preserve the purchased Shopeiva Vendor Panel language.

Do NOT start TB-P05-T002.
Do NOT add unrelated Seller features.

Repository Recovery

Run:

git rev-parse --show-toplevel
git fetch origin
git branch --show-current
git rev-parse HEAD
git rev-parse origin/main
git status --short --branch

Expected predecessor:

15fa111941c38e78b18dc1e95139ab46e57604c7

Require main, HEAD == origin/main, safe/known working tree.

No force push, destructive reset, silent stash, or history rewrite.

BLOCKER 1 — Real Seller Authorization

Current interim selector/header:

X-Tooba-Seller-Party-Id

may remain as request context for development/testing, but MUST NOT itself be trusted as authorization.

Backend must derive/verify:

authenticated actor
→ Party/Membership/relationship
→ authorization policy
→ permitted Seller Party

using existing Tooba Identity/Party/SpiceDB foundations.

Required conceptual flow:

current actor
+
requested SellerPartyId
→ authorization service / SpiceDB
→ allow or deny
→ Seller endpoint

Do not invent a second authorization system.

If full interactive login is unavailable, use a bounded dev actor/session seam, but actor identity and requested SellerPartyId MUST be distinct and backend-verified.

Required proof:

Actor A + Seller A → allowed
Actor A + Seller B → denied
Actor B + Seller B → allowed
Actor B + Seller A → denied
missing actor → fail closed

Changing only SellerPartyId while preserving actor MUST NOT grant access.

Apply authorization to:

Seller dashboard
Products list
Offer detail
Offer mutation
Orders list
Order detail

Create:

docs/evidence/TB-P05-T001/repair/seller-authorization-proof.md
docs/evidence/TB-P05-T001/repair/seller-authorization-architecture.md
BLOCKER 2 — Shopeiva Vendor Fidelity

Canonical visual reference:

docs/evidence/TB-P04-T006/visual-atlas/02-vendor-contact-sheet.png

Current Seller output is too sparse/custom.

Do NOT deliver:

generic white dashboard
custom T005-like admin language
Data Grid page merely wrapped by a sidebar

Required composition:

Shopeiva Vendor shell
+
Shopeiva panel/card/form/toolbar language
+
Tooba Professional Data Grid where operationally needed
+
live Tooba backend

Preserve/closely port:

topbar/header density
sidebar structure
title/breadcrumb treatment
dashboard stat cards
panel/card containers
toolbar spacing
form controls
status chips
action buttons
table/card treatment
mobile navigation
loading/empty/error states

Approved adaptation:

Shopeiva primary accent → Tooba blue

Do not broadly redesign.

Dashboard

Use real summary data where available:

active offers
open orders
paid orders

Do not invent fake analytics/charts.

Maintain Shopeiva panel/card density even when data is minimal.

Products

Use Shopeiva Vendor Products visual pattern.

Use Tooba Data Grid inside Shopeiva containers/toolbars.

Minimum live columns:

Product
Seller SKU
Offer status
Price
Availability
Updated
Actions

No Product.Price.
No Product.Stock.

Product Detail / Edit

Use Shopeiva Vendor product form/detail patterns.

Preserve card sections, headings, inputs, buttons, spacing, status treatment.

Semantics:

Catalog Product = read-only context
Seller Offer = seller-controlled commercial seam

Only supported mutations are editable.

Orders

Use Shopeiva Vendor Orders/order-detail layout.

Seller only sees seller-owned lines.

Keep Data Grid if useful, but preserve Shopeiva panel/toolbar/status language.

Data Grid Presentation

Do not expose component-demo style controls prominently.

Operational UX should favor:

search
filter panel/button
header sorting
column chooser
pagination
bulk toolbar only when selection exists
UX Cleanup

Primary Seller UI must not show raw UUIDs or awkward English states.

Prefer Persian operator language such as:

فعال
در انتظار پرداخت
پرداخت‌شده
ناموجود
۷ عدد
۱٬۸۵۰٬۰۰۰ ریال

Technical identifiers may appear only as secondary detail where useful.

Mobile

At:

390x844

Products and Orders must be intentionally responsive.

Require no page horizontal overflow and no desktop table merely squeezed into mobile.

Visual Evidence

Store:

docs/evidence/TB-P05-T001/repair/

Capture:

01-seller-dashboard-desktop.png
02-seller-products-desktop.png
03-seller-products-mobile-390x844.png
04-seller-product-detail.png
05-seller-orders-desktop.png
06-seller-orders-mobile-390x844.png
07-seller-order-detail.png
08-seller-auth-denied.png
09-seller-auth-allowed.png
10-shopeiva-vendor-fidelity.png

Create:

docs/evidence/TB-P05-T001/repair/shopeiva-vendor-fidelity.md

For Dashboard, Products, Product Detail, Orders, Order Detail, Mobile record:

Shopeiva source route/pattern
Tooba route
preserved structure
minimal adaptation
reason for deviation
Tests

Backend:

Actor A → Seller A allowed
Actor A → Seller B denied
Actor B → Seller B allowed
Actor B → Seller A denied
missing actor denied
Offer mutation cross-seller denied
Order detail cross-seller denied

Frontend:

authorized seller route
denied state
products mapping
orders mapping
Full Validation

Run:

dotnet restore src/backend/Tooba.slnx
dotnet build src/backend/Tooba.slnx
dotnet test src/backend/Tooba.slnx

NO filters. Require warnings=0, errors=0, failed=0, skipped=0.

Frontend:

cd src/frontend
npm ci
npm run typecheck
npm run lint
npm run test:grid
npm run test:workspace
npm run test:product-workspace

Run all supported suites for:

storefront
cart
checkout/order
payment
seller

Then:

npm run build
git diff --check
git status --short --branch

Report every suite explicitly.

Architecture Invariants

Must remain true:

Catalog Product != Seller Offer
Seller Party != authenticated User
requested SellerPartyId != authorization authority
frontend != seller authorization authority

Forbidden:

Product.Price
Product.Stock
cross-module SQL JOIN
frontend-only seller scoping
trusting SellerPartyId header without authorization
Deferred Concerns

Carry unchanged:

Payment missing IdempotencyKey → 500/NRE
Cart replace-hold release→reserve competition window
Acceptance Conditions

PASS requires:

real Seller authorization proven
requested Seller ID alone cannot change authority
existing SpiceDB/authorization seam used
cross-seller access denied
Shopeiva Vendor identity clearly recognizable
dashboard not generic/custom
Products/Orders preserve Shopeiva language
Tooba Data Grid sits inside Shopeiva visual shell
desktop works
390x844 mobile works
no raw UUIDs as primary UX
Persian statuses
no Product.Price
no Product.Stock
no cross-module SQL JOIN
full validation green
SoT

Keep:

P04 = COMPLETE
TB-P04-GATE = ACCEPTED
P05 = IN_PROGRESS
TB-P05-T001 = REPAIR / AWAITING_ARCHITECT_ACCEPT
TB-P05-T002 = NOT ISSUED

Record:

Seller authorization must bind authenticated actor to Seller Party;
requested SellerPartyId is context, never authority.
Save Envelope VERBATIM

Save:

docs/ai/tasks/TB-P05-T001-REPAIR.task.md
Commit / Push
git diff --check
git status --short --branch
git add ...
git commit --trailer "Co-authored-by: Cursor <cursoragent@cursor.com>" -m "fix seller authorization and Shopeiva fidelity [TB-P05-T001]"
git push origin main
git fetch origin
git rev-parse HEAD
git rev-parse origin/main
git status --short --branch

Require HEAD == origin/main.

Result Contract

Return:

BEGIN_TOOBA_CURSOR_RESULT_V1

Protocol-Version: 1
Task-ID: TB-P05-T001
Repair: YES
Phase: P05 — Operational Surface Integration
Status: PASS | BLOCKED | RECOVERY_CONFLICT

Summary:
...

Repository-Recovery:
- ...

Seller-Authorization:
- actor source:
- requested SellerPartyId source:
- authorization service:
- SpiceDB:
- Actor A → Seller A:
- Actor A → Seller B:
- Actor B → Seller B:
- Actor B → Seller A:
- missing actor:
- request SellerPartyId trusted as authority:

Shopeiva-Fidelity:
- dashboard:
- shell:
- products:
- product detail:
- orders:
- order detail:
- mobile:
- deviations:

Data-Grid:
- products:
- orders:
- Shopeiva wrapper:
- mobile behavior:

UX-Cleanup:
- raw IDs:
- English statuses:
- money formatting:
- operator errors:

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
- fidelity doc:
- authorization proof:
- authorization architecture:

Tests:
- actor/seller allow:
- actor/seller deny:
- cross-seller mutation:
- cross-seller order:
- frontend seller:

Validation:
- backend restore:
- backend build:
- warnings:
- errors:
- backend passed:
- failed:
- skipped:
- npm ci:
- typecheck:
- lint:
- grid:
- workspace:
- product-workspace:
- storefront:
- cart:
- checkout/order:
- payment:
- seller:
- frontend build:
- git diff --check:

Architecture:
- Product.Price:
- Product.Stock:
- cross-module SQL join:
- frontend seller authority:
- SellerPartyId trusted without auth:

Git:
- commit:
- push:
- final HEAD:
- origin/main:
- synchronized:
- final status:

Source-of-Truth:
- P05:
- TB-P05-T001:
- TB-P05-T002 Issued:
- Recovery-ready:

Architectural-Concerns:
- ...

Visual-Concerns:
- ...

Blockers:
- ...

END_TOOBA_CURSOR_RESULT_V1

After RESULT surface repaired Seller screenshots directly in this SAME Architect-controlled session.

CRITICAL — RESULT DOES NOT END THIS AGENT SESSION

After RESULT:

PIPELINE_STATE: WAITING_FOR_ARCHITECT_IN_SAME_SESSION

Then:

DO NOT CLOSE OR LEAVE THIS SESSION.
WAIT HERE FOR THE NEXT AUTHORIZED ENVELOPE.

Do not finish.
Do not exit.
Do not self-advance.
Do not invent TB-P05-T002.

RESULT = CONTROL RETURNED TO ARCHITECT.
RESULT != WORKFLOW FINISHED.

Cursor PASS is not Architect ACCEPT.

Leaving or ending the pipeline after RESULT is a protocol violation.

END_TOOBA_CURSOR_TASK_V1
