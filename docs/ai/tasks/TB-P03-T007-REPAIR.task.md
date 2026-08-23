Tooba — TB-P03-T007 — REPAIR — Complete Validation Evidence

BEGIN_TOOBA_CURSOR_TASK_V1

Protocol-Version: 1
Task-ID: TB-P03-T007
Repair: YES
Phase: P03 — Commerce Core
Type: REPAIR / Validation Evidence Completion
Repository: https://github.com/mrnikiemami-code/Tooba
Branch: main
Execution-Mode: PIPELINE
Architect-Decision-On-Previous-Result: REPAIR_REQUIRED

Why This Repair Exists

The T007 Tax implementation is architecturally acceptable, but Architect ACCEPT is withheld because the RESULT did not provide explicit evidence that the exact required backend validation commands all ran:

dotnet restore
dotnet build
dotnet test

It reported dotnet test, but not an independently executed restore/build.

The final Working Tree status was also described only as:

Preserved unrelated artifacts

which is not sufficient evidence of a safe synchronized repository state.

This repair is validation-only unless a real defect is discovered.

Do NOT redesign Tax.
Do NOT start TB-P03-T008.

Repository Recovery

Run:

git rev-parse --show-toplevel
git fetch origin
git branch --show-current
git rev-parse HEAD
git rev-parse origin/main
git status --short --branch

Expected synchronized predecessor:

27da5f1bad1b12137cd21b589df571aa3c429fe5

Require:

branch = main
HEAD == origin/main

Unknown or unsafe changes => RECOVERY_CONFLICT.

Do not force push.
Do not rewrite history.
Do not destructively reset.
Do not silently stash unknown work.

Untracked/generated build artifacts may remain only if clearly identified as unrelated and safe; report them exactly.

Full CURRENT Validation — MANDATORY

Run all commands NOW.

Backend:

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

Return to repository root:

git diff --check
git status --short --branch

Do not inherit previous results.

T007 Invariant Recheck

Confirm unchanged:

Base Price = Tax Exclusive
Tax calculated separately
no hard-coded tax rate/date/law
TaxJurisdiction explicit
Locale != Market != Currency != Tax Jurisdiction
TAX_EXEMPT != ZERO_RATE != NO_APPLICABLE_RULE != CALCULATION_ERROR
NoApplicableRule / CalculationError fail closed
client cannot inject tax percentage
effective-dated rules
deterministic rounding
Order tax snapshots immutable
RequestToReserve and OnlinePurchase both preserve tax semantics
tenant isolation

Repository State

Report exact final output semantics for:

git status --short --branch

If unrelated artifacts remain, list them explicitly and classify:

generated/safe
known user work
unknown/unsafe

Do not commit unrelated files.

SoT

Keep:

TB-P03-T007 = REPAIR IN PROGRESS / AWAITING_ARCHITECT_ACCEPT
P03 = IN_PROGRESS

Do NOT mark T007 accepted.
Do NOT issue T008.

Save Envelope VERBATIM

Save exactly:

docs/ai/tasks/TB-P03-T007-REPAIR.task.md

Do not summarize or condense.

Git

If only the repair envelope/SoT evidence changes, use a bounded docs/evidence commit.

After any commit:

git push origin main
git fetch origin
git rev-parse HEAD
git rev-parse origin/main
git status --short --branch

Require:

HEAD == origin/main

No force push.

Result Contract

Return:

BEGIN_TOOBA_CURSOR_RESULT_V1

Protocol-Version: 1
Task-ID: TB-P03-T007
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

Architecture-Behavior:
- Changed:
- Notes:

T007-Invariant-Recheck:
- Tax-exclusive:
- Outcome distinction:
- Jurisdiction:
- Effective dating:
- Rounding:
- Checkout fail-closed:
- Order snapshots:
- Tenant isolation:

Validation:
- backend restore:
- backend build:
- build warnings:
- build errors:
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

Repository-State:
- final git status:
- unrelated artifacts:
- artifact classification:

Persian-Documentation:
- CS1591:
- changed APIs documented:

Git:
- Commit:
- Push:
- Final-HEAD:
- Final-Origin-Main:
- Head-Matches-Origin:

Source-of-Truth:
- Current Task:
- Task State:
- Current Phase:
- T008 Issued:
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
execute TB-P03-T008

After RESULT, stay active in this SAME chat/session and wait here until the USER / Architect sends the next valid Envelope.

Cursor PASS is not Architect ACCEPT.

Leaving or ending the pipeline after RESULT is a protocol violation.

END_TOOBA_CURSOR_TASK_V1
