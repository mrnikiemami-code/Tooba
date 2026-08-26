PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

TASK-ID: TB-P05-GATE
PROJECT: Tooba
PHASE: P05
CHANNEL: tooba-main
STATUS: ISSUED
TASK-TYPE: PHASE GATE / SOURCE-OF-TRUTH FINALIZATION
WORKER-POLICY: ONE WORKER = ONE ACTIVE TASK

Title

P05 Architect Gate Finalization — Close P05 Without Blocking on Manual User Visual Review

Context

Architect ACCEPTED TB-P05-T026-R2 for implemented repair and evidence.

Current policy is locked:

Pipeline never waits for manual user visual review.

Home/PDP feedback can arrive later and will create focused Repair tasks.

Pending user review is NOT a pipeline blocker.

Functional PASS != Visual ACCEPT.

Visual fidelity includes structure + CSS + spacing + typography + shadow + hover/focus/active + transitions + animations + carousel/autoplay + overlays + badges + icons + micro-interactions + responsive/mobile + density + rhythm.

Repository Recovery

Run:
git rev-parse --show-toplevel
git fetch origin
git branch --show-current
git rev-parse HEAD
git rev-parse origin/main
git status --short --branch

Expected predecessor:
1a39ffc9bf3d4d8958729b9315f4bcd6c7cfd58a

Require main, HEAD == origin/main, safe tracked tree.
If conflict: RECOVERY_CONFLICT.

A — Accepted P05 Chain

Create:
docs/evidence/TB-P05-GATE/01-p05-accepted-chain.md

Document accepted P05 tasks, commits, purpose, outcome.

B — Normalize T026

Update SoT:
TB-P05-T026 = ACCEPTED
TB-P05-T026-R1 = ACCEPTED
TB-P05-T026-R2 = ACCEPTED

Do NOT write FINAL_USER_ACCEPTED for Home/PDP.

Write:
HOME_VISUAL_REVIEW = OPEN_FOR_USER_FEEDBACK
PDP_VISUAL_REVIEW = OPEN_FOR_USER_FEEDBACK

C — Permanent Visual Governance

Confirm governance covers:
Storefront
Home
PDP
Listing/Search
Cart/Checkout
Customer
Seller
Admin
Future UI

Required workflow:
actual source
→ component
→ CSS
→ interaction
→ animation
→ responsive behavior
→ reuse/port
→ replace demo bindings with live Tooba data

Forbidden:
screenshot-only approximation
reinterpretation
simplification by taste
modernization without approval
generic replacement

Unauthorized visual deviation = VISUAL REGRESSION.

Create:
docs/evidence/TB-P05-GATE/02-visual-governance-lock-proof.md

D — Non-Blocking User Feedback Policy

Lock:
User UI review may occur asynchronously.
Pipeline continues automatically.
Later user complaint creates focused Repair.
No immediate user confirmation is required to issue next Task.
Pending manual visual review != Pipeline BLOCK.

Create:
docs/evidence/TB-P05-GATE/03-nonblocking-user-feedback-policy.md

E — P05 Completion Summary

Create:
docs/evidence/TB-P05-GATE/04-p05-completion-summary.md

Summarize live Storefront, Customer, Seller, Admin capabilities and honest unavailable/deferred features.

F — Deferred Items

Create:
docs/evidence/TB-P05-GATE/05-p05-deferred-items-final.md

Classify:
P06
Later Product Phase
Hardening
Post-sale/B2B
User-feedback-driven visual repair

Verify from current repo/SoT.

G — Runtime Policy Forward

Lock future UI Task behavior:

start Backend first

start Frontend first

start original Shopeiva runtime when visual comparison is relevant

verify runtime before code changes

after build restart runtimes if needed

return exact preview URLs

keep runtimes available when technically possible

Create:
docs/evidence/TB-P05-GATE/06-runtime-preview-policy.md

H — Final Validation

Frontend:
cd src/frontend
npm run typecheck
npm run lint
npm run test
npm run test
npm run test
npm run test
npm run build

Run Admin/listing/cart/checkout suites if available.

Backend using accepted zero-warning validation path:
dotnet restore src/backend/Tooba.slnx
dotnet build src/backend/Tooba.slnx
dotnet test src/backend/Tooba.slnx

Require:
warnings=0
errors=0
failed=0
skipped=0

Always:
git diff --check

I — Final SoT

Update:
docs/PROJECT-STATE.md
docs/ROADMAP.md
docs/ai/TOOBA-RECOVERY-CONTEXT.md
docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md

Expected:
PIPELINE = BRIDGE-WAKE-V1
TB-P05-T026 = ACCEPTED
TB-P05-T026-R1 = ACCEPTED
TB-P05-T026-R2 = ACCEPTED
HOME_VISUAL_REVIEW = OPEN_FOR_USER_FEEDBACK
PDP_VISUAL_REVIEW = OPEN_FOR_USER_FEEDBACK
P05 = AWAITING_ARCHITECT_ACCEPT

Worker must NOT mark P05 accepted.

J — Next Phase Handoff

Create:
docs/evidence/TB-P05-GATE/07-next-phase-handoff.md

Read ROADMAP and identify:
Next Phase ID
Next Phase Name
First safe task candidate
Prerequisites
Dependencies
Known deferred issues that do NOT block start

Do not start next phase implementation here.

Git

git diff --check
git status --short --branch
git add ...
git commit -m "docs finalize P05 gate and visual governance [TB-P05-GATE]"
git push origin main
git fetch origin
git rev-parse HEAD
git rev-parse origin/main
git status --short --branch

Require HEAD == origin/main and tracked tree clean.

Result Contract

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-P05-GATE
Channel: tooba-main
Status: PASS | FAIL | BLOCKED | RECOVERY_CONFLICT

Summary:
...

Accepted-Chain:
...

Visual-Governance:

all UI surfaces covered:

CSS preserved:

hover preserved:

animation preserved:

carousel/motion preserved:

micro-interactions preserved:

unauthorized deviation rule:

User-Feedback-Policy:

manual review blocking:

future feedback action:

Home review:

PDP review:

P05-Summary:
...

Deferred:
...

Validation:

typecheck:

lint:

critical-storefront:

storefront:

customer:

seller:

admin/listing/cart/checkout:

frontend build:

backend restore:

backend build:

backend tests:

warnings:

errors:

failed:

skipped:

git diff --check:

Next-Phase:

phase id:

phase name:

first task candidate:

prerequisites:

Source-of-Truth:

T026:

T026-R1:

T026-R2:

Home visual review:

PDP visual review:

P05:

Git:

commit:

push:

final HEAD:

origin/main:

synchronized:

tracked tree:

Architectural-Concerns:
...

Visual-Concerns:
...

Blockers:
...

END_TOOBA_WORKER_RESULT

After Result return to IDLE.
Do not self-issue another Task.

END_TASK