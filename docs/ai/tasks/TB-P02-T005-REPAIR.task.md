# Tooba — TB-P02-T005 — REPAIR — Validation Completion

BEGIN_TOOBA_CURSOR_TASK_V1

Protocol-Version: 1
Task-ID: TB-P02-T005
Repair: YES
Phase: P02 — Identity / Authorization
Type: REPAIR / Validation Completion
Repository: https://github.com/mrnikiemami-code/Tooba
Branch: main
Execution-Mode: PIPELINE
Architect-Decision-On-Previous-Result: REPAIR_REQUIRED

Why This Repair Exists

The T005 implementation is technically acceptable, but Architect ACCEPT is withheld because the original T005 envelope required CURRENT frontend validation in the same task.

The RESULT reported:

frontend install: not run
frontend typecheck: not run
frontend lint: not run
frontend build: not run

This repair is bounded to completing the missing validation and reconciling SoT.

Do NOT redesign or expand the authentication HTTP boundary unless validation exposes a real defect.

Repository Recovery

Run:

git rev-parse --show-toplevel
git fetch origin
git branch --show-current
git rev-parse HEAD
git rev-parse origin/main
git status --short --branch

Expected synchronized predecessor:

feaeccfaf00905a83c2e22b01162a07f3fd6daa9

Require main and HEAD == origin/main.

Unsafe/ambiguous state => RECOVERY_CONFLICT.

No force push, history rewrite, destructive reset, silent stash, or unrelated work.

Full CURRENT Validation — MANDATORY

Backend:

dotnet restore
dotnet build
dotnet test

Frontend:

cd src/frontend
npm ci
npm run typecheck
npm run lint
npm run build

Return to repo root:

git diff --check
git status --short --branch

Do NOT report inherited or previous green results. All commands must actually run in this repair.

T005 Invariant Recheck

Confirm:

opaque session handle
refresh rotation
revocation
enumeration-safe login/reset
trusted Host tenant routing only
no tenant authority from body/query/header
ProblemDetails without secrets
no custom JWT crypto
no SpiceDB call for authentication
no secret logging
Persian Documentation

If any code changes, all required Tooba-owned classes/interfaces/methods/properties/etc. must retain strong Persian documentation.

CS1591 must remain green.

SoT

Keep:

TB-P02-T005 = REPAIR IN PROGRESS / AWAITING_ARCHITECT_ACCEPT
P02 = IN_PROGRESS

Do NOT mark T005 accepted.
Do NOT issue T006.

Git

If changes are committed, use a bounded commit such as:

chore complete T005 validation [TB-P02-T005]

Then push origin/main, fetch, and require HEAD == origin/main.

Result Contract

Return:

BEGIN_TOOBA_CURSOR_RESULT_V1

Protocol-Version: 1
Task-ID: TB-P02-T005
Repair: YES
Phase: P02 — Identity / Authorization
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

Validation:
- backend restore:
- backend build:
- backend tests:
- frontend install:
- frontend typecheck:
- frontend lint:
- frontend build:
- git diff --check:
- final working tree:

Invariant-Recheck:
- opaque session:
- refresh rotation:
- tenant trust boundary:
- enumeration-safe failures:
- no custom JWT:
- no secret logging:

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
- T006 Issued:
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
execute TB-P02-T006

After RESULT, stay active in this SAME chat/session and wait here until the USER / Architect sends the next valid Envelope.

Only when a new valid Envelope is provided in this SAME chat/session may you execute the next task.

Cursor PASS is not Architect ACCEPT.

Leaving or ending the pipeline after RESULT is a protocol violation.

END_TOOBA_CURSOR_TASK_V1
