# Tooba — Pipeline Protocol V1

## Model

Controlled single-agent pipeline:

```text
USER
  ↓
ChatGPT Architect
  ↓
downloadable Markdown task/gate
  ↓
Cursor
  ↓
implementation + tests + evidence + SoT
  ↓
local commit + push origin main
  ↓
Cursor RESULT to Architect chat
  ↓
Architect ACCEPT / REPAIR / BLOCK
  ↓
automatic next task when safe
```

## Source of Truth

Repository is durable Source of Truth.

Chat is the architect communication/transport channel.

Executable authority is a complete Architect-issued Markdown `.task.md` / `.gate.md` with valid envelope.

## Mode

```text
TOOBA_AUTOMATION_RESUME
PIPELINE
```

Pipeline continues until explicit pause or a real stop condition.

After RESULT:

```text
WAITING
```

means no invented work and keep checking for the next authorized Architect envelope.

## Task marker

```text
BEGIN_TOOBA_CURSOR_TASK_V1
...
END_TOOBA_CURSOR_TASK_V1
```

## Gate marker

```text
BEGIN_TOOBA_CURSOR_GATE_V1
...
END_TOOBA_CURSOR_GATE_V1
```

## Result marker

```text
BEGIN_TOOBA_CURSOR_RESULT_V1
...
END_TOOBA_CURSOR_RESULT_V1
```

## Git

Before task:

```text
main
HEAD == origin/main
```

After task:

```text
local commit
push origin main
fetch
HEAD == origin/main
```

Any unsafe divergence:

```text
RECOVERY_CONFLICT
```

## Automation

- one task at a time;
- auto-continue after Architect ACCEPT;
- auto-start next planned phase after accepted gate when no real blocker;
- stop for true architectural/business/recovery blockers only;
- Cursor never invents tasks.
