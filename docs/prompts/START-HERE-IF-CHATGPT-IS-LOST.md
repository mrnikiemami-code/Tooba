# Tooba — Start Here If ChatGPT Architect Context Is Lost

Recorded repository state for recovery (do not invent the next envelope):

```text
Current Phase: P04 — Experience Foundation
Last Architect Accepted Task: TB-P04-T007
Last Architect Accepted Gate: TB-P03-GATE
Current Issued Task: TB-P04-T009
Task State: AWAITING_ARCHITECT_ACCEPT
Current Gate: NONE
P01 = COMPLETE
P02 = COMPLETE
P03 = COMPLETE
P04 is IN_PROGRESS; T001–T008 Architect-accepted; T009 code is on main; current envelope is TB-P04-T009-REPAIR live screenshots plus full validation; purchased Shopeiva is Next 16.2.6 / React 19.2.4 / Tailwind 4; Persian RTL first; Tooba Data Grid remains; core API integration by end of P06
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
