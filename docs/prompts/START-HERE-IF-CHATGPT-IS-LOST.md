# Tooba — Start Here If ChatGPT Architect Context Is Lost

Recorded repository state for recovery (do not invent the next envelope):

```text
Current Phase: P02 — Identity / Authorization
Last Architect Accepted Task: TB-P02-T005
Current Gate: TB-P02-GATE
Gate State: AWAITING_ARCHITECT_ACCEPT
P01 = COMPLETE
P02 is IN_PROGRESS; do not start P03 without a new envelope
```

Cursor must NOT continue implementation automatically from ROADMAP.

A recovered Architect must first recover from the repository.

Run:

```bash
git rev-parse --show-toplevel
git fetch origin
git branch --show-current
git rev-parse HEAD
git rev-parse origin/main
git status --short --branch
```

Then read:

```text
AGENTS.md
docs/PROJECT-STATE.md
docs/ROADMAP.md
docs/ai/TOOBA-PIPELINE-PROTOCOL.md
docs/ai/TOOBA-PIPELINE-CONTROLLER.md
docs/ai/TOOBA-RECOVERY-CONTEXT.md
```

Then determine:

- current phase;
- last Architect accepted task;
- issued-but-unaccepted task;
- blockers;
- locked / confirmed requirements;
- unresolved decisions;
- exact resume rule.

Never invent the next task from memory.

Produce a recovery packet containing:

- project status;
- current phase;
- last Architect accepted task;
- issued but not accepted task;
- HEAD;
- origin/main;
- HEAD == origin/main;
- working tree;
- known blockers;
- locked architecture / confirmed requirements;
- unresolved P00 decisions;
- resume rule.

Prefer the existing Tooba Architect conversation if it is still available. If Architect context is truly lost, paste the recovery packet into a new ChatGPT Architect chat.

Do not implement until the Architect reconciles state and sends a new valid Tooba task/gate file.

No Envelope = No Execution.
Cursor PASS != Architect ACCEPT.
