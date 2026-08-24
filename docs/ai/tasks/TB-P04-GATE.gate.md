Tooba — TB-P04-GATE — Experience Foundation Acceptance Gate

BEGIN_TOOBA_CURSOR_TASK_V1

Protocol-Version: 1
Task-ID: TB-P04-GATE
Phase: P04 — Experience Foundation
Type: PHASE GATE
Repository: https://github.com/mriquemami-code/Tooba
Branch: main
Execution-Mode: PIPELINE

Architect Decision

TB-P04-T010 is ACCEPTED.

P04 implementation work is now frozen for Gate review.

Do NOT start P05 work.
Do NOT invent TB-P05-T001.

The Gate must prove that P04 produced a real presentation-first commerce foundation while preserving the sell-first rule:

PRESERVE SHOPEIVA
→ MINIMUM CHANGE
→ CONNECT REAL TOOBA BACKEND
→ SELL QUICKLY
→ POLISH/HARDEN LATER

Repository Recovery

Run:

git rev-parse --show-toplevel
git fetch origin
git branch --show-current
git rev-parse HEAD
git rev-parse origin/main
git status --short --branch

Expected predecessor:

a52e1b4808f46e2af4f707210f34457611effcdb

Require:

branch = main
HEAD == origin/main
safe/known working tree

No force push.
No destructive reset.
No silent stash.
No history rewrite.

P04 Accepted Task Chain

Verify repository evidence/SoT for:

TB-P04-T001 = ACCEPTED
TB-P04-T002 = ACCEPTED
TB-P04-T003 = ACCEPTED
TB-P04-T004 = ACCEPTED
TB-P04-T005 = ACCEPTED as functional/interaction foundation
TB-P04-T006 = ACCEPTED
TB-P04-T007 = ACCEPTED
TB-P04-T008 = ACCEPTED
TB-P04-T009 = ACCEPTED
TB-P04-T010 = ACCEPTED

T005 custom visual language is NOT the final visual target.

Shopeiva runtime/template visual language is the initial sellable visual source of truth.

Gate Objective

Prove P04 now provides:

Design System foundation
Professional Data Grid foundation
Workspace interaction foundation
Shopeiva runtime atlas/reuse map
live Storefront Home/Listing/PDP
live Cart
real Cart ↔ Inventory lifecycle
live Checkout
real Order creation
live Payment boundary
verified Payment → Order Paid transition
desktop RTL evidence
mobile RTL evidence
sell-first Shopeiva preservation

P04 must be recovery-ready and safe to hand off into P05.

Gate Rule — NO FEATURE EXPANSION

Gate may only:

fix bounded validation/evidence/SoT defects
fix a regression required to make the accepted P04 flows run

Gate must NOT:

start Seller implementation
start Customer dashboard implementation
start Admin redesign
add Fulfillment
add Returns
add real PSP
add refund
add new commerce feature
change architecture without Architect decision

If a serious new architecture issue is found, return BLOCKED.

Full Backend Validation — REQUIRED

Run exactly:

dotnet restore src/backend/Tooba.slnx
dotnet build src/backend/Tooba.slnx
dotnet test src/backend/Tooba.slnx

NO filters.

Require:

warnings = 0
errors = 0
failed = 0
skipped = 0

Report exact passed count.

Full Frontend Validation — REQUIRED

Run:

cd src/frontend
npm ci
npm run typecheck
npm run lint
npm run test:grid
npm run test:workspace
npm run test:product-workspace

Run all repository-supported focused suites for:

Storefront
Cart
Checkout/Order
Payment

Then:

npm run build

Require all green.

Do not substitute existing node_modules for npm ci.

Report every suite explicitly.

Architecture Invariants — MUST STILL HOLD

Verify:

Product != Variant
Product != Offer
Offer != Price
Product != Inventory
Pricing != Promotion
Pricing != Tax
Cart != Inventory
Cart != Checkout
Checkout != Order
Payment != Order
Payment Provider != Payment domain
Order snapshot != live Product truth
Frontend != commercial/payment authority
Backend/module boundary != UI boundary

Forbidden:

Product.Price
Product.Stock
cross-module SQL JOIN
foreign-module ORM navigation
frontend direct DB
frontend authoritative total
frontend authoritative payment success

Storefront Live Flow Gate

Verify the accepted live path still works:

Home
→ Listing
→ PDP
→ Add to Cart
→ Cart
→ Checkout
→ Order PendingPayment
→ Payment initiation
→ sandbox/dev provider verification
→ Payment Succeeded
→ Order Paid

The Gate does NOT require a real production PSP.

The sandbox/dev provider must remain clearly non-bank and replaceable.

Cart Correctness Gate

Verify accepted T008 Repair remains true:

quantity increase
quantity decrease
remove
insufficient inventory rejection
idempotent release
multi-seller Offer isolation
expired/abandoned Cart server-side release
customer-safe errors

Carry forward non-blocking concern:

replace-hold release→reserve competition window

Do not lose it.

Checkout / Order Correctness Gate

Verify:

Preview does not persist
Submit creates CheckoutGroup
one CartId → at most one CheckoutGroup
seller Orders are created
Order snapshots are durable
Tax is backend authoritative
Payment is not falsely successful

Payment Correctness Gate

Verify:

Payment initiation uses durable backend amount
provider adapter is replaceable
callback/result server verified
duplicate initiation safe
duplicate callback safe
amount/currency correlation
failed Payment leaves Order unpaid
successful Payment transitions Order once
frontend cannot mark paid
provider secrets server-side

Shopeiva Preservation Gate

P04 is NOT a custom redesign phase.

Verify current live surfaces remain recognizably Shopeiva-based:

Header / search / nav
Home composition
product cards
Listing
PDP
Cart
Checkout
Order confirmation
Payment/result experience
mobile shell

Approved visual adaptation:

primary accent red → Tooba professional blue

No broad redesign should have been introduced.

Create/update:

docs/evidence/TB-P04-GATE/shopeiva-preservation-summary.md

Summarize:

what was preserved
what was minimally adapted
what was intentionally not completed
why current result is sufficient for sell-first progression

Visual Evidence Integrity

Do not recapture every prior screenshot unless a regression exists.

Verify required evidence exists for:

T006 visual atlas
T007 Home/Listing/PDP
T008 Cart
T009 Checkout/Order desktop + real 390x844 mobile
T010 Payment desktop + real 390x844 mobile

Verify mobile evidence dimensions where previously required.

If a missing/broken image is discovered, recapture only that evidence.

P04 Evidence Summary

Create:

docs/evidence/TB-P04-GATE/p04-evidence-index.md

Link/record canonical evidence for:

Shopeiva runtime atlas
Design System
Data Grid
Workspace
Home
Listing
PDP
Cart
Checkout
Order
Payment
desktop
mobile
architecture maps
live-flow proofs

This is for recovery and future Architect review.

Deferred / P05+ List — LOCK

Record without implementing:

real production PSP adapter(s)
refund/capture/void
seller settlement/payout
Fulfillment/Shipment
Returns/RMA
Customer persistent Address/Party seam
Guest BuyerPartyId improvement
Cart replace-hold concurrency hardening
Grid virtualization
advanced multilingual/LTR UI
advanced multi-currency UX
theme configurator
advanced Search
Media binary/CDN pipeline

Also preserve previously accepted deferred concerns from recovery context.

P05 Entry Readiness

The Gate should conclude whether P05 can start with the next sell-first focus.

Expected P05 direction if Gate passes:

connect remaining core operational/customer surfaces using Shopeiva reuse

with strong candidates:

Seller products/orders
Admin operational product/order surfaces
Customer orders/account surfaces

Do NOT issue or implement those during this Gate.

Source of Truth Updates

Update:

docs/PROJECT-STATE.md
docs/ROADMAP.md
docs/ai/TOOBA-RECOVERY-CONTEXT.md
docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md

If Gate PASS:

P04 = COMPLETE
TB-P04-GATE = AWAITING_ARCHITECT_ACCEPT
Last Implementation Task = TB-P04-T010
Next Phase = P05
Next Task = NOT YET ISSUED

Do not mark Architect ACCEPTED yourself.

Save Envelope VERBATIM

Save exactly:

docs/ai/tasks/TB-P04-GATE.gate.md

Gate Evidence

Create:

docs/evidence/TB-P04-GATE/

Required:

p04-evidence-index.md
shopeiva-preservation-summary.md
validation-summary.md
architecture-invariant-summary.md
deferred-concerns.md

Commit / Push

After Gate work:

git diff --check
git status --short --branch
git add ...
git commit -m "docs close experience foundation gate [TB-P04-GATE]"
git push origin main
git fetch origin
git rev-parse HEAD
git rev-parse origin/main
git status --short --branch

Require:

HEAD == origin/main

Gate PASS Conditions

PASS only if:

all backend validation green
all frontend validation green
accepted live purchase path still works
no architecture invariant regression
Shopeiva preservation rule intact
required evidence exists
mobile evidence valid
SoT recovery-ready
no serious unresolved blocker to P05

A non-blocking deferred concern does NOT fail the Gate if explicitly recorded.

Result Contract

Return:

BEGIN_TOOBA_CURSOR_RESULT_V1

Protocol-Version: 1
Task-ID: TB-P04-GATE
Phase: P04 — Experience Foundation
Status: PASS | BLOCKED | RECOVERY_CONFLICT

Summary:
...

Repository-Recovery:
- root:
- branch:
- predecessor:
- final HEAD:
- origin/main:
- synchronized:

Accepted-Chain:
- T001:
- T002:
- T003:
- T004:
- T005:
- T006:
- T007:
- T008:
- T009:
- T010:

Backend-Validation:
- restore:
- build:
- warnings:
- errors:
- test passed:
- failed:
- skipped:

Frontend-Validation:
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
- build:

Live-Commerce:
- Home:
- Listing:
- PDP:
- Cart:
- Checkout:
- Order:
- Payment:
- Order paid transition:

Architecture:
- Product.Price:
- Product.Stock:
- cross-module SQL join:
- frontend authoritative totals:
- frontend payment authority:
- one CartId → at most one CheckoutGroup:

Shopeiva:
- preservation:
- blue theme:
- redesign introduced:
- desktop:
- mobile:

Evidence:
- index:
- preservation summary:
- validation:
- architecture:
- deferred:

Deferred:
- replace-hold window:
- PSP:
- fulfillment:
- returns:
- addresses:
- other:

P05-Readiness:
- ready:
- blockers:
- recommended next focus:

Git:
- commit:
- push:
- final HEAD:
- origin/main:
- synchronized:
- final status:

Source-of-Truth:
- P04:
- TB-P04-GATE:
- Last Implementation Task:
- Next Phase:
- Next Task Issued:
- Recovery-ready:

Blockers:
- ...

END_TOOBA_CURSOR_RESULT_V1

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
Do not invent P05 Task.

RESULT = CONTROL RETURNED TO ARCHITECT.
RESULT != WORKFLOW FINISHED.

Cursor PASS is not Architect ACCEPT.

Leaving or ending the pipeline after RESULT is a protocol violation.

END_TOOBA_CURSOR_TASK_V1
