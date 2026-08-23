# Tooba — Pipeline Controller V1

## States

```text
RECOVERING
READY
TASK_ISSUED
EXECUTING
RESULT_SUBMITTED
WAITING_ARCHITECT_REVIEW
WAITING
BLOCKED
RECOVERY_CONFLICT
```

Only Architect authorization may advance to a new implementation objective.

## Resume

```text
TOOBA_AUTOMATION_RESUME

PIPELINE

git rev-parse --show-toplevel
git fetch origin
git rev-parse HEAD
git rev-parse origin/main
git status --short --branch
git branch --show-current

Read:
AGENTS.md
docs/PROJECT-STATE.md
docs/ROADMAP.md
docs/ai/TOOBA-RECOVERY-CONTEXT.md
docs/ai/TOOBA-PAUSE-RECOVERY-CHECKPOINT.md (if it exists)
docs/ai/TOOBA-PIPELINE-PROTOCOL.md
docs/ai/TOOBA-PIPELINE-CONTROLLER.md
docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md

Read Architect Chat (same conversation; do not start a new Architect chat).

Check Architect ACCEPT / next authorized task file.

Local inbox (fast path once saved):
docs/ai/tasks/*.task.md
docs/ai/tasks/*.gate.md

If valid:
BEGIN_TOOBA_CURSOR_TASK_V1
or
BEGIN_TOOBA_CURSOR_GATE_V1

Execute only that task.
Test.
Evidence.
SoT sync as authorized.
git diff --check.
Commit.
push origin main.

Then:
git fetch origin
git rev-parse HEAD
git rev-parse origin/main

HEAD == origin/main must be YES.

Return:
BEGIN_TOOBA_CURSOR_RESULT_V1
...
END_TOOBA_CURSOR_RESULT_V1

Architect Chat → Composer → Send

button[data-testid="send-button"]

WAITING means only: no unauthorized self-advance while waiting for Architect review/envelope.
WAITING does not mean stop the project, close the chat, or require a manual restart.

If no new authorized envelope:
WAITING
Keep pipeline active/checking.
Do not invent task.

Persian documentation quality is part of implementation acceptance.

STOP only for explicit pause or real hard conflict.

RECOVERY_CONFLICT on unsafe/irreconcilable repository state.

TOOBA_AUTOMATION_PAUSE
```

## Cycle (must not stop without cause)

```text
find authorized envelope in Architect chat
→ save to docs/ai/tasks/
→ execute in Cursor
→ paste RESULT into the same chat
→ Send
→ wait for Architect reply
→ repeat
```

A temporary empty task queue is not a reason to exit PIPELINE.

## Important

Cursor PASS != Architect ACCEPT.

Do not execute recommendations from ROADMAP without an Architect envelope.
