# Tooba — Pipeline Controller V1

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

Read Architect Chat.

Check Architect ACCEPT / next authorized task file.

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

WAITING

If no new authorized envelope:
WAITING
Keep pipeline active/checking.
Do not invent task.

STOP only for explicit pause or real hard conflict.

RECOVERY_CONFLICT on unsafe/irreconcilable repository state.

TOOBA_AUTOMATION_PAUSE
```

## Important

Cursor PASS != Architect ACCEPT.

A temporary empty task queue is not a reason to exit PIPELINE.

Do not execute recommendations from ROADMAP without an Architect envelope.
