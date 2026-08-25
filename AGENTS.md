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
Bridge     = transport/orchestration boundary
Worker     = agent-neutral Coding Agent Worker
Repository = durable Source of Truth
```

The Worker is an implementer, not an architect. It may be Cursor, OpenAI Codex,
Claude Code, Hermes, or another compatible agent.

Before implementation, read this file and the Tooba pipeline/recovery documents.

## Core rules

- Current operational protocol: `BRIDGE-V2`; current channel: `tooba-main`.
- Only Tasks actually dispatched by Bridge on the Worker's configured channel are executable.
- Every implementation Task is an actual downloadable `<TASK-ID>.task.md`.
- The user is not expected to paste Tasks into the Worker.
- `ONE WORKER = ONE ACTIVE TASK`.
- Worker PASS != Architect ACCEPT.
- Repository is durable Source of Truth. Repository truth overrides chat memory.
- Worker does not invent requirements, invent future work, redesign locked architecture, broaden scope, or self-authorize the next task.
- Architectural concerns are reported, not silently implemented.
- Normal implementation uses `main`.
- Every accepted task execution must produce a local commit and remote `origin/main` push.
- After push, verify `HEAD == origin/main`.
- No force push or history rewrite.
- Preserve unrelated/unknown working-tree artifacts.
- On conflict: `RECOVERY_CONFLICT`.
- While `Working`, heartbeat continues and task polling remains stopped.
- After successful Result delivery and task completion, return to `Waiting` and resume polling.
- `SYSTEM-BRIDGE-ALERT` is not a Result: do not advance state, mark PASS/FAIL, or claim another Task.
- Persian documentation is part of implementation acceptance. All required Tooba-owned Classes, Interfaces, Methods, and Properties must have strong Persian documentation (C# XML `/// <summary>`; reusable frontend APIs via TSDoc/JSDoc). Name-echo comments fail review. See `docs/architecture/32-persian-code-documentation-standard.md`.

## Task and Result artifacts

```text
ARCHITECT → downloadable .task.md → Bridge → Coding Agent Worker
→ Result → Bridge → ARCHITECT → ACCEPT / REPAIR / BLOCK → next .task.md
```

Filename conventions:

```text
TB-PXX-TXXX.task.md
TB-PXX-GATE.gate.md
```

Received Task audit archive:

```text
docs/ai/tasks/
```

This directory is not an operational queue. Historical task/result artifacts may
contain legacy Cursor/chat syntax; they are evidence of prior execution and are
not current operational instructions.

## Recovery docs

```text
docs/PROJECT-STATE.md
docs/ROADMAP.md
docs/ai/TOOBA-RECOVERY-CONTEXT.md
docs/ai/TOOBA-PIPELINE-PROTOCOL.md
docs/ai/TOOBA-PIPELINE-CONTROLLER.md
docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md
```
