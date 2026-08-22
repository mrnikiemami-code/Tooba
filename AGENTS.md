# Tooba — AGENTS.md

Canonical repository:

```text
https://github.com/mrnikiemami-code/Tooba
```

Primary branch:

```text
main
```

## Roles

```text
USER       = product/business authority
ChatGPT    = Chief/Senior Architect / Task Issuer / Reviewer / Pipeline Controller
Cursor     = implementation agent
Repository = durable Source of Truth
```

Cursor is an implementer, not an architect.

Before implementation, read this file and the Tooba pipeline/recovery documents.

## Core rules

- Only Architect-issued valid `.task.md` / `.gate.md` files are executable.
- No Envelope = No Execution.
- Cursor PASS != Architect ACCEPT.
- Repository is durable Source of Truth. Repository truth overrides chat memory.
- One task at a time.
- Cursor does not invent requirements, invent future work, redesign locked architecture, broaden scope, or self-authorize the next task.
- Architectural concerns are reported, not silently implemented.
- Normal implementation uses `main`.
- Every accepted task execution must produce a local commit and remote `origin/main` push.
- After push, verify `HEAD == origin/main`.
- No force push or history rewrite.
- Preserve unrelated/unknown working-tree artifacts.
- On conflict: `RECOVERY_CONFLICT`.
- After RESULT: remain PIPELINE / WAITING for the next authorized envelope.

## Envelope markers

```text
BEGIN_TOOBA_CURSOR_TASK_V1
END_TOOBA_CURSOR_TASK_V1

BEGIN_TOOBA_CURSOR_GATE_V1
END_TOOBA_CURSOR_GATE_V1

BEGIN_TOOBA_CURSOR_RESULT_V1
END_TOOBA_CURSOR_RESULT_V1
```

Filename conventions:

```text
TB-PXX-TXXX.task.md
TB-PXX-GATE.gate.md
```

Authorized envelope inbox (fast local execution path):

```text
docs/ai/tasks/
```

## Recovery docs

```text
docs/PROJECT-STATE.md
docs/ROADMAP.md
docs/ai/TOOBA-RECOVERY-CONTEXT.md
docs/ai/TOOBA-PIPELINE-PROTOCOL.md
docs/ai/TOOBA-PIPELINE-CONTROLLER.md
docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md
```
