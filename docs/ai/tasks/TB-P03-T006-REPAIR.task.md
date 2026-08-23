Tooba — TB-P03-T006 — REPAIR — Cart Conversion Reconciliation

BEGIN_TOOBA_CURSOR_TASK_V1

Protocol-Version: 1
Task-ID: TB-P03-T006
Repair: YES
Phase: P03 — Commerce Core
Type: REPAIR / Checkout Consistency
Repository: https://github.com/mrnikiemami-code/Tooba
Branch: main
Execution-Mode: PIPELINE
Architect-Decision-On-Previous-Result: REPAIR_REQUIRED

Why This Repair Exists

The T006 implementation is structurally correct, but Architect ACCEPT is withheld because the RESULT reported this failure window:

Checkout/Order persists successfully
→ Cart ConvertAsync fails
→ retry with same IdempotencyKey returns existing checkout
→ Cart may remain Active

An Active Cart after successful Order creation may later be submitted with another idempotency key and create a duplicate commercial order.

This repair must close that consistency gap.

Do NOT redesign Checkout/Order.
Do NOT start T007.

Repository Recovery

Run:

git rev-parse --show-toplevel
git fetch origin
git branch --show-current
git rev-parse HEAD
git rev-parse origin/main
git status --short --branch

Expected predecessor:

dcd04e25deec5e36b82a942dda7dd23e684e6c2e

Require:

branch = main
HEAD == origin/main

Unsafe/ambiguous state => RECOVERY_CONFLICT.

No force push, history rewrite, destructive reset, silent stash, or unrelated work.

Required Invariant

After an Order/CheckoutGroup has been durably created from a Cart:

that Cart must never be able to create a second independent checkout

even if the original Cart conversion acknowledgement/update fails temporarily.

The invariant must survive:

process crash
network failure
retry
different IdempotencyKey
same IdempotencyKey
Preferred Architecture

Use a durable cross-module reconciliation design.

Acceptable patterns include:

Order persists CartId + Checkout identity
→ durable integration/outbox event
→ Cart conversion handler marks Cart Converted
→ retry-safe reconciliation

and/or a Cart-side durable claim/checkout-lock seam established before final Order creation.

Choose the smallest robust design consistent with current module boundaries.

Hard rules:

no distributed transaction
no direct CartDbContext access from Order
no direct OrderDbContext access from Cart
no cross-module FK
Duplicate Checkout Prevention

A Cart must have a durable logical conversion identity.

The system must reject:

same Cart + different new IdempotencyKey

after a CheckoutGroup already exists for that Cart.

Do not rely only on request IdempotencyKey uniqueness.

Enforce with durable persistence uniqueness where practical, such as:

unique CartId in checkout/order conversion table

or equivalent invariant.

Application-only pre-check is not enough.

Retry Behavior
Same IdempotencyKey
return same existing checkout
ensure Cart conversion reconciliation is retried
Different IdempotencyKey for already-converted/claimed Cart
do not create a new checkout
return existing checkout or explicit ALREADY_CONVERTED outcome

Choose one documented behavior.

Cart State Reconciliation

If Order exists but Cart is still Active:

reconciliation must eventually mark Cart Converted

Provide an idempotent durable path.

Possible mechanisms:

integration event handler
outbox-driven worker
explicit retry during checkout replay
periodic reconciliation

At least one durable path must exist now.

Do not rely only on an operator/manual fix.

Inventory Safety

The repair must not:

double-reserve
double-consume
double-release

inventory.

Existing ReservationId handoff must remain stable.

Repeated reconciliation must be idempotent.

Pricing Safety

Do not reprice an already-created checkout merely because Cart conversion reconciliation is retried.

Historical Order snapshots remain immutable.

Tests — MANDATORY

Add tests covering at minimum:

Order persists, Cart conversion fails, retry reconciles Cart
same IdempotencyKey returns same checkout
different IdempotencyKey cannot create second checkout for same Cart
database uniqueness prevents duplicate Cart conversion under concurrency
two concurrent checkout attempts for same Cart create exactly one CheckoutGroup
reconciliation is idempotent
inventory is not double-reserved/consumed/released
existing Order price snapshots do not change during reconciliation
Cart ends Converted after recovery path succeeds

Use PostgreSQL Testcontainers.

Container-backed tests remain deterministic.

Require:

Failed = 0
Skipped = 0
Existing T006 Invariants — Recheck

Confirm unchanged:

Cart != Order
RequestToReserve != unpaid OnlinePurchase
BuyerPartyId != PlacedByUserId
price revalidated at checkout
PRICE_CHANGED remains explicit
historical price snapshots immutable
Order != Payment
Order != Fulfillment
multi-seller SellerOrders remain independent
Tenant isolation remains intact
Persian Documentation — MANDATORY

Every new/changed Tooba-owned Class/Interface/Method/Property/etc. must have strong meaningful Persian documentation.

Comments must explain:

Cart conversion invariant
reconciliation
idempotency
duplicate prevention
cross-module consistency
inventory safety

Weak/name-echo comments = acceptance failure.

CS1591 must remain green.

Documentation

Update:

docs/architecture/47-checkout-order-foundation.md

Document:

Cart conversion durable invariant
failure window
duplicate-checkout prevention
same/different idempotency-key behavior
reconciliation path
database uniqueness
inventory safety during retries

Remove any wording that leaves Cart conversion as a manual/future-only seam.

SoT

Keep:

TB-P03-T006 = REPAIR IN PROGRESS / AWAITING_ARCHITECT_ACCEPT
P03 = IN_PROGRESS

Do NOT mark T006 accepted.
Do NOT issue T007.

Save Envelope VERBATIM

Save exactly:

docs/ai/tasks/TB-P03-T006-REPAIR.task.md

Do not summarize or condense.

Validation

Run current:

dotnet restore
dotnet build
dotnet test

Require:

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
Git

Suggested commit:

fix reconcile cart checkout conversion [TB-P03-T006]

Push origin/main, fetch, require:

HEAD == origin/main

No force push.

Result Contract

Return:

BEGIN_TOOBA_CURSOR_RESULT_V1

Protocol-Version: 1
Task-ID: TB-P03-T006
Repair: YES
Phase: P03 — Commerce Core
Status: PASS | BLOCKED | RECOVERY_CONFLICT

Summary:
...

Repository-Recovery:
- Repo-Root:
- Branch:
- Starting-HEAD:
- Starting-Origin-Main:
- Starting-Status:

Consistency-Repair:
- Root Cause:
- Durable Invariant:
- Persistence Constraint:
- Reconciliation Path:

Idempotency:
- Same Key:
- Different Key:
- Concurrent Requests:

Cart-State:
- ...

Inventory-Safety:
- ...

Pricing-Snapshot-Safety:
- ...

T006-Invariant-Recheck:
- ...

Persian-Documentation:
- ...

Tests:
- ...

Validation:
- backend restore:
- backend build:
- backend tests:
- backend passed:
- backend failed:
- backend skipped:
- postgres/integration tests:
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
- T007 Issued:
- Recovery-Ready:

Architectural-Concerns:
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
treat RESULT as the end of the work
move to another chat
wait outside this pipeline
invent the next task
infer the next task
prepare the next task
execute TB-P03-T007

After RESULT, stay active in this SAME chat/session and wait here until the USER / Architect sends the next valid Envelope.

Only when a new valid Envelope is provided in this SAME chat/session may you execute the next task.

Cursor PASS is not Architect ACCEPT.

Leaving or ending the pipeline after RESULT is a protocol violation.

END_TOOBA_CURSOR_TASK_V1
