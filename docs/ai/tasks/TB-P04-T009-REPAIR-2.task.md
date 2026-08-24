Library
/
Tooba
/
TB-P04-T009-REPAIR-2.task.md
Tooba — TB-P04-T009 — REPAIR 2 — Mobile Evidence Integrity Only

BEGIN_TOOBA_CURSOR_TASK_V1

Protocol-Version: 1
Task-ID: TB-P04-T009
Repair: YES
Repair-Round: 2
Phase: P04 — Experience Foundation
Type: Visual Evidence Integrity
Repository: https://github.com/mrnikiemami-code/Tooba
Branch: main
Execution-Mode: PIPELINE

Architect Decision

TB-P04-T009 is FUNCTIONALLY ACCEPTED.

Do NOT change Checkout/Order business logic.

Visual acceptance is still PENDING only because the submitted mobile evidence is not credible.

The Architect directly opened:

02-checkout-mobile-390x844.png
07-order-confirmation-mobile.png

Observed:

UI rendered in a narrow strip at the left
large unexplained white canvas to the right
image does not visually correspond to a true 390x844 viewport capture

This may be a screenshot-capture bug rather than an application bug.

Resolve evidence integrity only.

TB-P04-T010 is NOT issued.

Repository Recovery

Run:

git rev-parse --show-toplevel
git fetch origin
git branch --show-current
git rev-parse HEAD
git rev-parse origin/main
git status --short --branch

Expected predecessor:

642ad8f0d765527b84c2f4bfee50c0e6dc6ac1ed

Require synchronized safe main.

Scope

Allowed:

verify real mobile viewport
fix screenshot capture process
fix bounded responsive bug only if the page itself is actually wrong
capture correct mobile evidence
document dimensions/overflow

Forbidden:

redesign Checkout
redesign Order confirmation
change commerce architecture
start Payment
start T010
Real Mobile Viewport

Use actual browser emulation/device metrics:

CSS viewport: 390x844
deviceScaleFactor: 1
mobile: true

Capture the browser viewport itself.

Do NOT:

capture a desktop canvas containing a 390px page
use forceNarrow
resize only an inner application wrapper
paste/mobile-compose into a larger image
Programmatic Dimension Proof

For each new mobile PNG record actual PNG dimensions.

Required:

checkout-mobile.png ≈ 390x844 pixels
confirmation-mobile.png ≈ 390x844 pixels

At deviceScaleFactor=1, exact:

390 x 844

is preferred.

Create:

docs/evidence/TB-P04-T009/repair-2/mobile-capture-proof.md

Record:

CSS viewport width
CSS viewport height
deviceScaleFactor
window.innerWidth
window.innerHeight
documentElement.clientWidth
documentElement.scrollWidth
PNG pixel width
PNG pixel height
horizontal overflow

For both Checkout and Confirmation.

Require:

documentElement.scrollWidth <= documentElement.clientWidth
New Evidence

Store:

docs/evidence/TB-P04-T009/repair-2/

Capture:

01-checkout-mobile-390x844.png
02-order-confirmation-mobile-390x844.png
03-checkout-mobile-validation-error-390x844.png

All must be:

live Tooba
real backend flow
Persian RTL
no debug overlay
no fixture
no secret
actual 390x844 capture
Visual Check

Before PASS, inspect the PNG files themselves.

PASS requires:

no giant white canvas around a narrow app
checkout fills the mobile viewport naturally
confirmation fills the mobile viewport naturally
no horizontal overflow
text readable
CTA/forms usable
Shopeiva family preserved

If the page itself is broken at 390px, make only the bounded responsive fix needed.

Do not polish unrelated desktop UI.

Functional Recheck

Do not rerun the entire commercial flow unless needed for capture.

Still verify live:

Checkout loads
Confirmation loads
PendingPayment remains
no fake paid state

No business logic changes expected.

Validation

If NO source code changes:

git diff --check
git status --short --branch

No full suite is required merely for screenshot recapture.

If ANY frontend source changes are needed to fix a real mobile bug, run:

cd src/frontend
npm ci
npm run typecheck
npm run lint
npm run test:grid
npm run test:workspace
npm run test:product-workspace
npm run test:storefront
npm run build

Then:

git diff --check

If backend code changes unexpectedly:

STOP and return RECOVERY_CONFLICT
SoT

Keep:

TB-P04-T008 = ACCEPTED
TB-P04-T009 = FUNCTIONAL_ACCEPTED / VISUAL_AWAITING_ARCHITECT_ACCEPT
P04 = IN_PROGRESS
TB-P04-T010 = NOT ISSUED
Save Envelope VERBATIM

Save:

docs/ai/tasks/TB-P04-T009-REPAIR-2.task.md
Commit / Push

Commit evidence/any bounded responsive fix:

git diff --check
git status --short --branch
git add ...
git commit -m "test correct checkout mobile evidence [TB-P04-T009]"
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
Task-ID: TB-P04-T009
Repair: YES
Repair-Round: 2
Phase: P04 — Experience Foundation
Status: PASS | BLOCKED | RECOVERY_CONFLICT

Summary:
...

Repository-Recovery:
- ...

Mobile-Capture:
- CSS viewport:
- deviceScaleFactor:
- innerWidth/innerHeight:
- clientWidth:
- scrollWidth:
- horizontal overflow:

PNG-Dimensions:
- Checkout:
- Confirmation:
- Validation error:

Application-Bug:
- actual responsive bug found:
- source code changed:
- bounded fix:

Visual-Evidence:
- Checkout:
- Confirmation:
- Validation error:
- Proof document:

Functional-Recheck:
- Checkout loads:
- Confirmation loads:
- PendingPayment:
- fake paid state:

Validation:
- source changed:
- npm ci:
- typecheck:
- lint:
- storefront tests:
- build:
- git diff --check:

Git:
- commit:
- push:
- final HEAD:
- origin/main:
- synchronized:

Source-of-Truth:
- TB-P04-T009 Functional Acceptance: ACCEPTED
- TB-P04-T009 Visual Acceptance: PENDING
- TB-P04-T010 Issued: NO
- P04: IN_PROGRESS

Blockers:
- ...

END_TOOBA_CURSOR_RESULT_V1

After RESULT, surface the NEW mobile PNGs directly in this SAME Architect-controlled session.

CRITICAL — RESULT DOES NOT END THIS AGENT SESSION

After RESULT:

PIPELINE_STATE: WAITING_FOR_ARCHITECT_IN_SAME_SESSION

Then:

DO NOT CLOSE OR LEAVE THIS SESSION.
WAIT HERE FOR THE NEXT AUTHORIZED ENVELOPE.

Do not finish.
Do not exit.
Do not self-advance.
Do not start Payment or T010.

RESULT = CONTROL RETURNED TO ARCHITECT.
RESULT != WORKFLOW FINISHED.

Cursor PASS is not Architect ACCEPT.

Leaving or ending the pipeline after RESULT is a protocol violation.

END_TOOBA_CURSOR_TASK_V1
