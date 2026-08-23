# Tooba — TB-P04-T002 — REPAIR — Checkout Concurrency Regression

BEGIN_TOOBA_CURSOR_TASK_V1

Protocol-Version: 1
Task-ID: TB-P04-T002
Repair: YES
Phase: P04 — Experience Foundation
Type: REPAIR / Regression Recovery
Repository: https://github.com/mrnikiemami-code/Tooba
Branch: main
Execution-Mode: PIPELINE
Architect-Decision-On-Previous-Result: REPAIR_REQUIRED

Why This Repair Exists

The Design System implementation itself is acceptable, but full repository validation failed:

Failed: 1
Passed: 121
Skipped: 0

Failing test:

Checkout_revalidates_price_splits_sellers_and_isolates_tenants_on_postgres

Reported symptom:

concurrent SubmitCheckout CheckoutId mismatch

The failure was reproduced in isolation, so this is not acceptable as transient noise.

The previously accepted P03 invariant must remain true:

one CartId → at most one CheckoutGroup

and concurrent checkout attempts must converge on the same durable checkout.

Do NOT redesign the Design System.
Do NOT start Professional Data Grid.
Do NOT start TB-P04-T003.

Repository Recovery

Run:

git rev-parse --show-toplevel
git fetch origin
git branch --show-current
git rev-parse HEAD
git rev-parse origin/main
git status --short --branch

Expected predecessor:

146f450909171e75c4467e520497d28985fa486d

Require:

branch = main
HEAD == origin/main
safe/known working tree

Unsafe/ambiguous state => RECOVERY_CONFLICT.

No force push.
No destructive reset.
No silent stash.
No history rewrite.

Repair Objective

Find the actual root cause of the concurrent checkout mismatch.

Do not simply weaken/delete/change the assertion to make the suite green.

Determine whether the regression is caused by:

CheckoutDirectory concurrency path
DbUpdateException winner reload
tracking/detach behavior
transaction visibility
unique CartId constraint handling
IdempotencyKey handling
test fixture ordering/state leakage
tenant context leakage
parallel test interference

Fix the smallest correct root cause.

Required Invariant

Under two concurrent checkout submissions for the same Cart:

exactly one CheckoutGroup is persisted
both callers resolve to that same CheckoutId
no duplicate SellerOrders
no duplicate inventory effect
no duplicate commercial Order

This must hold for:

same IdempotencyKey
different IdempotencyKey

where the accepted T006 policy says a previously converted/claimed Cart reuses the existing Checkout.

Persistence Constraint

Re-verify the durable uniqueness around Cart conversion, such as:

unique order.checkouts.cart_id

or accepted equivalent.

Do not rely only on application pre-check.

Concurrency Path

If losing concurrent insert hits a unique constraint:

detach/reset conflicting tracked entity safely
reload durable winner
return winner CheckoutId
reconcile Cart conversion idempotently

Do not leave a stale tracked loser that can cause a mismatched returned identifier.

Test Quality

The failing test must remain meaningful.

Add or refine focused tests covering:

same Cart + two concurrent submits
same key concurrent
different key concurrent
exactly one CheckoutGroup in DB
same CheckoutId returned by both callers
single seller-order set
Cart conversion reconciliation
tenant isolation still holds

If test infrastructure is the actual bug, prove it with evidence and fix fixture isolation rather than product logic.

No Scope Creep

Do NOT change:

Design System tokens
Design System primitives
Theme model
RTL/LTR foundation
P04 sequence
Payment
Promotion
Tax
Pricing
Inventory

unless compilation requires a tiny mechanical adjustment.

Design System Recheck

Confirm the T002 implementation remains intact:

semantic tokens
light/dark
RTL/LTR
core primitives
form foundation
feedback
overlay foundation
commerce presentation primitives
no Data Grid
no Workspace implementation

No need to create additional UI features.

Validation — MANDATORY

Run focused failing test repeatedly enough to prove determinism.

At minimum:

dotnet test src/backend/Tooba.slnx --filter "Checkout_revalidates_price_splits_sellers_and_isolates_tenants_on_postgres"

Run it multiple times or use an equivalent repeat strategy.

Then full backend:

dotnet restore src/backend/Tooba.slnx
dotnet build src/backend/Tooba.slnx
dotnet test src/backend/Tooba.slnx

Require:

Build warnings = 0
Build errors = 0
Failed = 0
Skipped = 0

Frontend:

cd src/frontend
npm ci
npm run typecheck
npm run lint
npm run build

Then:

git diff --check
git status --short --branch
Persian Documentation — MANDATORY

Any changed Tooba-owned public API must keep strong meaningful Persian XML documentation.

If only internal implementation/test changes occur, do not add noisy comments.

CS1591 must remain green.

Documentation / SoT

Update only if needed:

docs/PROJECT-STATE.md
docs/ROADMAP.md
docs/ai/TOOBA-RECOVERY-CONTEXT.md
docs/architecture/47-checkout-order-foundation.md
docs/architecture/52-design-system-foundation.md

If the root cause reveals a real checkout consistency nuance, document it in the checkout architecture doc.

Keep:

TB-P04-T002 = REPAIR IN PROGRESS / AWAITING_ARCHITECT_ACCEPT
P04 = IN_PROGRESS

Do NOT mark T002 accepted.
Do NOT issue T003.

Save Envelope VERBATIM

Save exactly:

docs/ai/tasks/TB-P04-T002-REPAIR.task.md

Do not summarize or condense.

Git

Suggested commit:

fix checkout concurrency regression [TB-P04-T002]

Push origin/main, fetch, require:

HEAD == origin/main

No force push.

Result Contract

Return:

BEGIN_TOOBA_CURSOR_RESULT_V1

Protocol-Version: 1
Task-ID: TB-P04-T002
Repair: YES
Phase: P04 — Experience Foundation
Status: PASS | BLOCKED | RECOVERY_CONFLICT

Summary:
...

Repository-Recovery:
- ...

Root-Cause:
- ...

Checkout-Concurrency-Repair:
- ...

Persistence-Invariant:
- ...

Concurrent-Behavior:
- Same key:
- Different key:
- Returned CheckoutId:
- DB row count:
- SellerOrder duplication:
- Inventory duplication:

Tenant-Isolation:
- ...

Design-System-Recheck:
- ...

Tests:
- focused failing test:
- repeat evidence:
- concurrency tests:
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
- T003 Issued:
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
start Professional Data Grid
start Workspace implementation

After RESULT, stay active in this SAME chat/session and wait here until the USER / Architect sends the next valid Envelope.

Only when a new valid Envelope is provided in this SAME chat/session may you execute the next task.

Cursor PASS is not Architect ACCEPT.

Leaving or ending the pipeline after RESULT is a protocol violation.

END_TOOBA_CURSOR_TASK_V1
