Tooba — TB-P04-T008-REPAIR — Cart ↔ Inventory reservation lifecycle

Captured from Architect chat overlay text (not Download).

BEGIN_TOOBA_CURSOR_TASK_V1

Protocol-Version: 1
Task-ID: TB-P04-T008
Repair: YES
Phase: P04 — Experience Foundation
Type: Commerce Correctness Repair
Repository: https://github.com/mrnikiemami-code/Tooba
Branch: main
Execution-Mode: PIPELINE

Architect Decision

Previous TB-P04-T008 RESULT is NOT YET ACCEPTED.

The live Cart integration and Shopeiva UI are directionally accepted.

Do NOT redesign Cart.

The blocking issue is commercial correctness:

abandoned guest carts can leave Held inventory
quantity decrease/remove can hit reservation mismatch
customer-facing error exposes technical Held/reservation language

This can make sellable inventory unavailable without a completed purchase.

It MUST be repaired before Checkout integration.

TB-P04-T009 is NOT issued.

Repository Recovery

Run:

git rev-parse --show-toplevel
git fetch origin
git branch --show-current
git rev-parse HEAD
git rev-parse origin/main
git status --short --branch

Expected predecessor:

6184f199f1712658bb7f5d6c8041a14afdf0aae9

Require:

branch = main
HEAD == origin/main
safe/known working tree

No force push.
No destructive reset.
No silent stash.
No history rewrite.

Scope

Repair ONLY:

Cart ↔ Inventory reservation lifecycle
abandoned/expired guest Cart release
quantity decrease/release correctness
remove-line release correctness
idempotency/retry correctness
customer-safe Cart error mapping

Do NOT:

start Checkout
redesign Cart
add new wallet/promotion features
expand multilingual/LTR
change Shopeiva visual language
Required Investigation

Trace the actual current flow:

Cart AddLine
→ inventory hold/reserve
→ Cart persistence
→ quantity change
→ release/adjust hold
→ remove
→ cart expiry/abandonment

Identify the exact reason the live Digistyle line could reach:

reservation mismatch

while another Seller Offer removed successfully.

Do not patch by swallowing the error.

Create evidence:

docs/evidence/TB-P04-T008/repair/reservation-root-cause.md

Include:

root cause
affected code paths
affected invariants
why previous tests did not catch it
chosen repair
Cart Reservation Invariants

After repair, enforce:

1 Cart line / Offer reservation relationship is deterministic
quantity increase reserves only the delta
quantity decrease releases only the delta
remove releases the remaining held quantity
retry does not double-reserve
retry does not double-release
same command replay is idempotent where contract requires it
failed Cart mutation does not leave partial Cart/Inventory divergence
expired/abandoned Cart eventually releases inventory

Do NOT couple via cross-module SQL JOIN.

Use existing contracts/application boundaries.

Abandoned / Expired Guest Cart — REQUIRED

A sellable system cannot hold inventory forever because a browser was closed.

Use the existing Cart expiry architecture if already present.

Recover prior Cart expiry decisions/evidence from repository before inventing a new mechanism.

Implement or complete the narrowest durable path that ensures:

expired Cart
→ held inventory released
→ Cart marked/reconciled appropriately

Important:

do not rely on browser unload
do not rely on sessionStorage cleanup
do not require the customer to return

The release must be server-side/durable.

If an existing background worker pattern is already intended, implement the minimum production-safe worker/reconciliation needed now.

Do not create a parallel scheduler architecture if existing platform/background infrastructure exists.

Concurrency / Idempotency

Test at least:

Add qty 1
Increase to 2
Increase to available maximum
Attempt above maximum → real failure, no divergence
Decrease
Remove
Repeat remove/retry
Concurrent/repeated adjustment where relevant
Expiry release

For multi-seller:

same Product
different Offer
separate reservation identity

must remain correct.

One Seller Offer's reservation must not release another's inventory.

Transaction / Failure Semantics

Document and ensure safe behavior for:

Cart write succeeds / Inventory update fails
Inventory update succeeds / Cart write fails
retry after transient failure
duplicate command

Use the existing architecture's consistency pattern.

Do not introduce distributed SQL transaction across modules.

If compensation/reconciliation is used, make it explicit and tested.

Public Error Contract

Customer UI must NOT expose internal implementation vocabulary as the primary error.

Examples of forbidden primary customer copy:

Held
reservation mismatch
inventory reservation
internal code

Map backend errors into customer-safe Persian messages.

Examples:

موجودی این کالا تغییر کرده است. لطفاً تعداد را دوباره بررسی کنید.
تعداد انتخاب‌شده بیشتر از موجودی قابل فروش است.
این کالا در حال حاضر قابل افزودن به سبد نیست.

Technical code may remain in:

logs
trace
secondary diagnostics/evidence

Preserve structured machine-readable error codes for frontend branching.

UI Scope

Minimal only.

Preserve current Shopeiva Cart UI.

Allowed UI changes:

customer-safe error message
retry/reload action if necessary
updated quantity after authoritative refresh

No visual redesign.

Live Evidence

Store under:

docs/evidence/TB-P04-T008/repair/

Required real scenarios:

01-cart-quantity-valid.png
02-cart-over-availability-customer-error.png
03-cart-after-decrease.png
04-cart-after-remove.png
05-cart-after-expiry-release.md
06-multi-seller-reservation-proof.md

The expiry proof may be textual/API/DB-safe evidence rather than waiting a long production TTL.

Use a deterministic test/dev clock or bounded expiry setup if architecture supports it.

Do NOT weaken production expiry merely to make screenshot capture easy.

Tests — REQUIRED

Add focused automated tests for the root cause.

At minimum cover:

reserve initial quantity
increase delta
decrease delta
remove releases
same Product / different Offers isolated
insufficient inventory leaves consistent state
duplicate/retry safety
expired Cart releases held inventory

If background reconciliation/worker is implemented, test it directly.

Full Validation

Backend:

dotnet restore src/backend/Tooba.slnx
dotnet build src/backend/Tooba.slnx
dotnet test src/backend/Tooba.slnx

NO filters.

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

Run existing Storefront/Cart tests plus new focused tests.

Then:

npm run build
git diff --check
git status --short --branch
Architecture Recheck

Must remain true:

Cart != Inventory
Product != Offer
Offer identity preserved
no Product.Price
no Product.Stock
no cross-module SQL JOIN
frontend does not own reservation truth
SoT

Keep:

TB-P04-T007 = ACCEPTED
TB-P04-T008 = REPAIR / AWAITING_ARCHITECT_ACCEPT
P04 = IN_PROGRESS
TB-P04-T009 = NOT ISSUED

Record reservation lifecycle resolution/reconciliation in recovery docs.

Save Envelope VERBATIM

Save exactly:

docs/ai/tasks/TB-P04-T008-REPAIR.task.md
Commit / Push

After work:

git diff --check
git status --short --branch
git add ...
git commit -m "fix reconcile cart inventory reservations [TB-P04-T008]"
git push origin main
git fetch origin
git rev-parse HEAD
git rev-parse origin/main
git status --short --branch

Require:

HEAD == origin/main
Result Contract

Return:

BEGIN_TOOBA_CURSOR_RESULT_V1

Protocol-Version: 1
Task-ID: TB-P04-T008
Repair: YES
Phase: P04 — Experience Foundation
Status: PASS | BLOCKED | RECOVERY_CONFLICT

Summary:
...

Repository-Recovery:
- ...

Root-Cause:
- reservation mismatch cause:
- abandoned-cart hold cause:
- why existing tests missed it:

Reservation-Invariants:
- add:
- increase:
- decrease:
- remove:
- retry:
- multi-seller isolation:
- expiry:

Expiry-Reconciliation:
- mechanism:
- server-side:
- durable:
- browser-independent:
- release evidence:

Failure-Semantics:
- cart succeeds/inventory fails:
- inventory succeeds/cart fails:
- retries:
- duplicates:

Customer-Errors:
- backend codes:
- Persian messages:
- technical Held/reservation exposed to customer:

Live-Evidence:
- valid quantity:
- insufficient inventory:
- decrease:
- remove:
- expiry release:
- multi-seller:

Tests:
- focused reservation tests:
- expiry tests:
- retry/idempotency:
- multi-seller tests:

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
- storefront/cart:
- frontend build:
- git diff --check:

Architecture:
- cross-module SQL join:
- Product.Price:
- Product.Stock:
- frontend reservation truth:

Git:
- commit:
- push:
- final HEAD:
- origin/main:
- synchronized:
- final status:

Source-of-Truth:
- TB-P04-T007: ACCEPTED
- TB-P04-T008: AWAITING_ARCHITECT_ACCEPT
- P04: IN_PROGRESS
- T009 Issued: NO
- Recovery-ready:

Architectural-Concerns:
- ...

Blockers:
- ...

END_TOOBA_CURSOR_RESULT_V1

After RESULT remain in the SAME Architect-controlled session.

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
Do not invent TB-P04-T009.

RESULT = CONTROL RETURNED TO ARCHITECT.
RESULT != WORKFLOW FINISHED.

Cursor PASS is not Architect ACCEPT.

Leaving or ending the pipeline after RESULT is a protocol violation.

END_TOOBA_CURSOR_TASK_V1
