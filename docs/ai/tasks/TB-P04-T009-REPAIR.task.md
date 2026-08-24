Library
/
Tooba
/
TB-P04-T009-REPAIR.task.md
Tooba — TB-P04-T009 — REPAIR — Live Acceptance Evidence & Full Validation

BEGIN_TOOBA_CURSOR_TASK_V1

Protocol-Version: 1
Task-ID: TB-P04-T009
Repair: YES
Phase: P04 — Experience Foundation
Type: Acceptance Evidence / Validation Repair
Repository: https://github.com/mrnikiemami-code/Tooba
Branch: main
Execution-Mode: PIPELINE

Architect Decision

Previous TB-P04-T009 RESULT is NOT YET ACCEPTED.

The Checkout/Order implementation appears functionally credible, but acceptance is incomplete because:

mandatory live screenshots 01–07 were not captured
Host/Next were down at RESULT time
full frontend validation contract was not reported

Do NOT redesign Checkout.
Do NOT start Payment.
Do NOT start TB-P04-T010.

Repository Recovery

Run:

git rev-parse --show-toplevel
git fetch origin
git branch --show-current
git rev-parse HEAD
git rev-parse origin/main
git status --short --branch

Expected predecessor:

f45205ece8acd29947b15b3ca3be9c714b274531

Require main, HEAD == origin/main, safe/known working tree.

Objective

Prove the implemented live flow:

Cart
→ Checkout
→ Submit
→ CheckoutGroup
→ Seller Order(s)
→ Order confirmation
→ PendingPayment

No fake success. No fixture order truth.

Start Live Runtime

Start real Tooba Host and frontend using repository-supported development configuration.

Record:

Host URL
Frontend URL
tenant/store context
buyer/guest context
database context
Mandatory Live Flow

Execute one clean end-to-end run:

open live PDP
add real Offer to Cart
open Cart
continue to Checkout
enter valid shipping/contact snapshot
preview authoritative totals
submit Checkout
receive real CheckoutGroup/Order
open confirmation
verify PendingPayment
Mandatory Visual Evidence

Store under:

docs/evidence/TB-P04-T009/repair/

Capture all:

01-checkout-desktop-rtl.png
02-checkout-mobile-390x844.png
03-checkout-order-summary.png
04-checkout-tax-total.png
05-checkout-validation-error.png
06-order-confirmation-desktop.png
07-order-confirmation-mobile.png
08-duplicate-submit-proof.md

Requirements:

live Tooba runtime
real backend data
no fixture banner
no debug overlay
no secrets
no personal sensitive data
Persian RTL
Shopeiva visual language preserved

Mobile viewport approximately 390x844.

Validation Error Evidence

Trigger one REAL checkout validation error, for example missing required shipping data or stale/expired cart.

Capture the customer-safe Persian error.

Duplicate Submit Proof

Prove:

one CartId → at most one CheckoutGroup

Update:

docs/evidence/TB-P04-T009/repair/08-duplicate-submit-proof.md
Full Validation — REQUIRED NOW

Backend:

dotnet restore src/backend/Tooba.slnx
dotnet build src/backend/Tooba.slnx
dotnet test src/backend/Tooba.slnx

NO filters.

Require warnings=0, errors=0, failed=0, skipped=0.

Frontend:

cd src/frontend
npm ci
npm run typecheck
npm run lint
npm run test:grid
npm run test:workspace
npm run test:product-workspace

Run existing Storefront tests, existing Cart tests, and Checkout/Order focused tests.

Then:

npm run build
git diff --check
git status --short --branch

Do not omit any from RESULT.

Functional Recheck

Must remain true:

real Cart enters real Checkout
Preview does not persist
Submit creates real CheckoutGroup
seller Orders are PendingPayment
Tax comes from Tooba Tax
shipping is Checkout/Order snapshot
one CartId → at most one CheckoutGroup
no fake Payment success
no Product.Price
no Product.Stock
no cross-module SQL JOIN
no frontend authoritative final-total calculation
SoT

Keep until Architect acceptance:

TB-P04-T008 = ACCEPTED
TB-P04-T009 = REPAIR / AWAITING_ARCHITECT_ACCEPT
P04 = IN_PROGRESS
TB-P04-T010 = NOT ISSUED
Save Envelope VERBATIM

Save:

docs/ai/tasks/TB-P04-T009-REPAIR.task.md
Commit / Push

If files change:

git diff --check
git status --short --branch
git add ...
git commit -m "test capture live checkout order acceptance [TB-P04-T009]"
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
Task-ID: TB-P04-T009
Repair: YES
Phase: P04 — Experience Foundation
Status: PASS | BLOCKED | RECOVERY_CONFLICT

Summary:
...

Repository-Recovery:
- ...

Live-Runtime:
- Host:
- Frontend:
- Tenant/store:
- buyer/guest context:

Live-Flow:
- CartId:
- CheckoutGroupId:
- OrderId(s):
- status:
- PendingPayment:
- tax:
- final amount:
- duplicate submit behavior:

Visual-Evidence:
- 01:
- 02:
- 03:
- 04:
- 05:
- 06:
- 07:
- 08:
- mobile viewport:
- fixture used:
- debug overlay:

Customer-Error:
- scenario:
- backend code:
- Persian message:

Validation:
- dotnet restore:
- dotnet build:
- warnings:
- errors:
- dotnet test:
- passed:
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
- frontend build:
- git diff --check:

Functional-Recheck:
- Preview persists:
- Submit creates CheckoutGroup:
- seller orders:
- Payment falsely paid:
- one CartId -> one CheckoutGroup:
- Product.Price:
- Product.Stock:
- cross-module SQL join:
- frontend final-total truth:

Git:
- commit:
- push:
- final HEAD:
- origin/main:
- synchronized:
- final status:

Source-of-Truth:
- TB-P04-T008: ACCEPTED
- TB-P04-T009: AWAITING_ARCHITECT_ACCEPT
- P04: IN_PROGRESS
- T010 Issued: NO
- Recovery-ready:

Blockers:
- ...

END_TOOBA_CURSOR_RESULT_V1

After RESULT, surface the live Checkout and Order screenshots directly in this SAME Architect session.

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
Do not invent TB-P04-T010.

RESULT = CONTROL RETURNED TO ARCHITECT.
RESULT != WORKFLOW FINISHED.

Cursor PASS is not Architect ACCEPT.

Leaving or ending the pipeline after RESULT is a protocol violation.

END_TOOBA_CURSOR_TASK_V1
