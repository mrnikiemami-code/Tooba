Tooba — TB-P03-T005 — RECOVERY REVIEW

BEGIN_TOOBA_CURSOR_TASK_V1

Protocol-Version: 1
Task-ID: TB-P03-T005
Recovery-Review: YES
Phase: P03 — Commerce Core
Type: Evidence Recovery / Architect Review Support
Repository: https://github.com/mrnikiemami-code/Tooba
Branch: main
Execution-Mode: PIPELINE

Why This Envelope Exists

The USER reports that the original TB-P03-T005 RESULT was already sent in this same Architect-controlled thread.

The Architect currently cannot safely recover the full RESULT evidence from conversation context.

Do NOT re-implement T005.
Do NOT start T006.

Recover the current repository truth for T005 from the repository itself and re-emit a concise evidence-grade RESULT so the Architect can ACCEPT or issue a bounded repair.

USER-reported current origin/main HEAD:

21cf8b09a4febd3fa8730b6cc9693a6cdf5837d2
Repository Recovery

Run:

git rev-parse --show-toplevel
git fetch origin
git branch --show-current
git rev-parse HEAD
git rev-parse origin/main
git status --short --branch

Expected current synchronized HEAD:

21cf8b09a4febd3fa8730b6cc9693a6cdf5837d2

Require:

branch = main
HEAD == origin/main

If not synchronized or working tree contains unknown changes:

RECOVERY_CONFLICT

Do not force push.
Do not reset unknown work.
Do not silently stash.
Do not rewrite history.

Recover T005 Evidence

Read:

docs/ai/tasks/TB-P03-T005.task.md
docs/PROJECT-STATE.md
docs/ROADMAP.md
docs/ai/TOOBA-RECOVERY-CONTEXT.md

Inspect the T005 commit(s), implementation, tests, and documentation.

Recover and verify at minimum:

Cart module ownership
Cart aggregate/lifecycle
authenticated ownership
anonymous cart security
Offer-based cart lines
Market/Currency/SalesChannel context
Pricing contract usage
pricing snapshot non-authoritative semantics
Inventory reservation ownership
reservation expiry
release/reconciliation
no distributed transaction
multi-seller cart support
concurrency safety
tenant isolation
Cart != Order
both Request-to-Reserve and Online Purchase remain future conversion modes
Persian documentation
SoT state

Hard invariants:

Cart != Order
Cart != Payment
Cart != Inventory
Cart != Pricing source of truth
Cart line targets OfferId
Product/Offer do not gain cart-owned price/quantity state
Current Validation — REQUIRED

Run NOW:

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

Then from repo root:

git diff --check
git status --short --branch

Do not inherit prior validation.

No Unauthorized Changes

This is an evidence recovery review.

Do NOT modify production code unless current validation reveals that the already-implemented T005 is broken.

If a real defect is found:

Status = REPAIR_REQUIRED

and report it.

Do not silently fix architectural defects under this recovery envelope.

Minor evidence/SoT correction is allowed only if it does not alter product behavior.

Source of Truth

Report exactly:

Last Architect Accepted Task
Current Issued Task
Current Phase
Task State
Recovery Ready

Do NOT mark T005 Architect ACCEPTED.
Do NOT issue or execute T006.

Result Contract

Return:

BEGIN_TOOBA_CURSOR_RESULT_V1

Protocol-Version: 1
Task-ID: TB-P03-T005
Recovery-Review: YES
Phase: P03 — Commerce Core
Status: PASS | REPAIR_REQUIRED | BLOCKED | RECOVERY_CONFLICT

Summary:
...

Repository-Recovery:
- Repo-Root:
- Branch:
- Starting-HEAD:
- Starting-Origin-Main:
- Final-HEAD:
- Final-Origin-Main:
- Head-Matches-Origin:
- Working-Tree:

Recovered-T005-Commit:
- Commit:
- Commit-Message:

Cart-Module:
- ...

Cart-Aggregate:
- ...

Ownership:
- Authenticated:
- Anonymous:

Cart-Lines:
- ...

Commercial-Context:
- ...

Pricing-Boundary:
- ...

Inventory-Reservations:
- ...

Reservation-Expiry-Reconciliation:
- ...

Cross-Module-Consistency:
- ...

Multi-Seller:
- ...

Concurrency:
- ...

Tenant-Isolation:
- ...

Order-Model-Readiness:
- Request-to-Reserve:
- Online Purchase:

Persian-Documentation:
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

Source-of-Truth:
- Last-Architect-Accepted-Task:
- Current-Issued-Task:
- Current-Phase:
- Task-State:
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
execute TB-P03-T006

After RESULT, stay active in this SAME chat/session and wait here until the USER / Architect sends the next valid Envelope.

Only when a new valid Envelope is provided in this SAME chat/session may you execute the next task.

Cursor PASS is not Architect ACCEPT.

Leaving or ending the pipeline after RESULT is a protocol violation.

END_TOOBA_CURSOR_TASK_V1
