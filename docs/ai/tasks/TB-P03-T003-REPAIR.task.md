Tooba — TB-P03-T003 — REPAIR — Deterministic Full Validation

BEGIN_TOOBA_CURSOR_TASK_V1

Protocol-Version: 1
Task-ID: TB-P03-T003
Repair: YES
Phase: P03 — Commerce Core
Type: REPAIR / Test Determinism & Validation Evidence
Repository: https://github.com/mrnikiemami-code/Tooba
Branch: main
Execution-Mode: PIPELINE
Architect-Decision-On-Previous-Result: REPAIR_REQUIRED

Why This Repair Exists

The Pricing implementation is architecturally acceptable, but T003 is not yet Architect ACCEPTED because the RESULT reported:

backend tests: Passed 105, Skipped 1, Total 106

and:

backend build: PASS (via test run)

Acceptance requires deterministic current evidence:

0 skipped tests caused by infrastructure contention
explicit dotnet build execution

This repair is bounded to test determinism / validation evidence.

Do NOT redesign Pricing or begin T004.

Repository Recovery

Run:

git rev-parse --show-toplevel
git fetch origin
git branch --show-current
git rev-parse HEAD
git rev-parse origin/main
git status --short --branch

Expected predecessor:

68c480c3f4efeb3ac6315f4552e3524fae409876

Require:

branch = main
HEAD == origin/main

Unrelated local build artifacts may exist, but must not be committed.

Unsafe/ambiguous state => RECOVERY_CONFLICT.

No force push, history rewrite, destructive reset, or silent stash.

Repair Scope

Investigate and remove the Testcontainers contention that caused an Offer PostgreSQL test to skip during the complete backend suite.

Preferred solutions include:

xUnit collection serialization for Docker/PostgreSQL integration fixtures
shared deterministic fixture strategy
bounded concurrency control for container-heavy integration tests

Choose the smallest robust solution consistent with the current test architecture.

Do NOT:

mark integration tests skipped
weaken assertions
catch container failures and report success
remove Offer/Pricing integration coverage
disable parallelization for every unit test unless truly necessary

The solution should ensure infrastructure-heavy tests are deterministic without unnecessarily slowing all ordinary tests.

Validation — ALL MUST ACTUALLY RUN

From the current tree run:

dotnet restore
dotnet build
dotnet test

The final full backend test result must have:

Failed = 0
Skipped = 0

PostgreSQL Pricing and Offer integration coverage must both actually execute.

If SpiceDB/MassTransit integration tests are part of the same suite and environment support is available, they must continue to execute normally.

Frontend must also run now:

cd src/frontend
npm ci
npm run typecheck
npm run lint
npm run build

Return to repo root:

git diff --check
git status --short --branch

Do not inherit any result from a previous Task.

Pricing Invariant Recheck

Confirm no repair regression:

Product.Price absent
Offer.Price absent
Pricing owns Money
Market != Locale
Market != Currency
explicit SalesChannel
tax-exclusive authored base price
effective dating
overlap ambiguity prevented
tenant isolation preserved
Persian Documentation

If production/test-support Tooba-owned APIs change, retain strong Persian documentation where the project standard applies.

CS1591 must remain green.

SoT

Keep:

TB-P03-T003 = REPAIR IN PROGRESS / AWAITING_ARCHITECT_ACCEPT
P03 = IN_PROGRESS

Do NOT mark T003 accepted.
Do NOT issue TB-P03-T004.

Save Envelope VERBATIM

Save this full repair envelope exactly to:

docs/ai/tasks/TB-P03-T003-REPAIR.task.md

Do not summarize or condense.

Git

Use a bounded commit such as:

test stabilize commerce postgres integration suite [TB-P03-T003]

Then:

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
Task-ID: TB-P03-T003
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

Test-Determinism-Repair:
- Root Cause:
- Change:
- Why deterministic:
- Scope of serialization/concurrency control:

Pricing-Invariant-Recheck:
- Product.Price:
- Offer.Price:
- Market/Currency:
- SalesChannel:
- Tax-exclusive:
- Overlap:
- Tenant isolation:

Validation:
- backend restore:
- backend build:
- backend tests:
- backend passed:
- backend failed:
- backend skipped:
- Offer postgres integration executed:
- Pricing postgres integration executed:
- other integration tests:
- frontend install:
- frontend typecheck:
- frontend lint:
- frontend build:
- git diff --check:
- final working tree:

Persian-Documentation:
- CS1591:
- changed APIs documented:

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
- T004 Issued:
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
execute TB-P03-T004

After RESULT, stay active in this SAME chat/session and wait here until the USER / Architect sends the next valid Envelope.

Only when a new valid Envelope is provided in this SAME chat/session may you execute the next task.

Cursor PASS is not Architect ACCEPT.

Leaving or ending the pipeline after RESULT is a protocol violation.

END_TOOBA_CURSOR_TASK_V1
