# Tooba — Start Here If ChatGPT Architect Context Is Lost

Cursor must NOT continue implementation automatically from ROADMAP.

Read:

```text
docs/PROJECT-STATE.md
docs/ROADMAP.md
docs/ai/TOOBA-RECOVERY-CONTEXT.md
docs/ai/TOOBA-PIPELINE-PROTOCOL.md
docs/ai/TOOBA-PIPELINE-CONTROLLER.md
```

Run:

```bash
git rev-parse --show-toplevel
git fetch origin
git rev-parse HEAD
git rev-parse origin/main
git status --short --branch
```

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
- locked architecture;
- resume rule.

Paste the recovery packet into a new ChatGPT Architect chat.

Do not implement until the Architect reconciles state and sends a new valid Tooba task/gate file.
