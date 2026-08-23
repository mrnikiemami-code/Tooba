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
Cursor stores authorized file in docs/ai/tasks/ (fast local path)
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

Fast local execution path after an envelope is obtained from Architect chat:

```text
docs/ai/tasks/TB-PXX-TXXX.task.md
docs/ai/tasks/TB-PXX-GATE.gate.md
```

## Governance

```text
USER       = product/business authority
ChatGPT    = Chief/Senior Architect / Task Issuer / Reviewer / Pipeline Controller
Cursor     = implementation agent
Repository = durable Source of Truth
```

Cursor is not the architect.

- Cursor PASS != Architect ACCEPT.
- No Envelope = No Execution.
- Cursor must not invent requirements, redesign locked architecture, broaden scope, or self-authorize the next task.
- Architectural concerns are reported, not silently implemented.
- Repository truth overrides chat memory.

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

## Lifecycle

```text
Architect issues one complete authorized Markdown task
→ Cursor validates repository + envelope
→ Cursor executes only that task
→ tests / validation / visual review if required
→ SoT sync
→ local commit
→ push origin main
→ git fetch origin
→ verify HEAD == origin/main
→ Cursor sends RESULT
→ Architect reviews
→ ACCEPT / REPAIR / BLOCKED
→ if ACCEPT and no real blocker, Architect automatically issues the next task/gate
```

## Task marker

```text
BEGIN_TOOBA_CURSOR_TASK_V1
...
END_TOOBA_CURSOR_TASK_V1
```

Filename:

```text
TB-PXX-TXXX.task.md
```

## Gate marker

```text
BEGIN_TOOBA_CURSOR_GATE_V1
...
END_TOOBA_CURSOR_GATE_V1
```

Filename:

```text
TB-PXX-GATE.gate.md
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

Never force-push or rewrite history.

## Automation

- one task at a time;
- auto-continue after Architect ACCEPT;
- auto-start next planned phase after accepted gate when no real blocker;
- stop for true architectural/business/recovery blockers only;
- Cursor never invents tasks;
- Persian documentation quality is part of implementation acceptance (see `docs/architecture/32-persian-code-documentation-standard.md`);
- do not execute `TB-P00-T001` unless Architect issues that exact envelope.
