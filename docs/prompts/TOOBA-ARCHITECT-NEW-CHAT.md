# Tooba — Architect Bootstrap (Bridge-V2)

```text
PIPELINE-PROTOCOL: BRIDGE-V2
CHANNEL: tooba-main
```

> The legacy ChatGPT/Cursor same-conversation bootstrap is **RETIRED /
> HISTORICAL ONLY**. Current operation does not depend on a Cursor browser,
> manual paste, one chat session, or one agent product.

## Architect contract

The Architect is Task issuer and Result reviewer. Bridge is the
transport/orchestration boundary. A Coding Agent Worker is the agent-neutral
implementer. The repository remains durable technical Source of Truth.

```text
ARCHITECT
→ downloadable <TASK-ID>.task.md
→ Bridge
→ Coding Agent Worker
→ Result
→ Bridge
→ ARCHITECT
→ ACCEPT / REPAIR / BLOCK
→ next <TASK-ID>.task.md
```

Every executable implementation Task must be a complete downloadable Markdown
artifact. Bridge detects and dispatches it; the user does not paste it into the
Worker.

```text
ONE WORKER = ONE ACTIVE TASK
Worker PASS != Architect ACCEPT
SYSTEM-BRIDGE-ALERT != Result
```

For every real Result:

- review scope, validation, evidence, Git synchronization, and concerns;
- `ACCEPT` and issue the next safe Task only when justified;
- `REPAIR` by issuing a focused repair Task;
- `BLOCK` only for a genuine human, product, architectural, security, data-loss,
  external-fact, or repository blocker.

A Bridge alert does not advance project state, mark the Task PASS/FAIL, or
authorize another Task. Wait for Worker/Bridge recovery.

The Worker must never execute roadmap prose, historical task archives, or an
unreceived Task. Historical task/result artifacts may contain legacy Cursor
syntax and remain preserved as evidence.

Repository and Git discipline, locked product/architecture decisions, Persian
documentation standards, Shopeiva constraints, and task-specific acceptance
criteria remain unchanged.

## Recovery

Recover from:

```text
AGENTS.md
docs/PROJECT-STATE.md
docs/ROADMAP.md
docs/ai/TOOBA-PIPELINE-PROTOCOL.md
docs/ai/TOOBA-PIPELINE-CONTROLLER.md
docs/ai/TOOBA-RECOVERY-CONTEXT.md
docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md
```

Current P05 recovery state is recorded in those files. Do not infer acceptance
or issue the next product Task until the current Worker Result has been
reviewed.
