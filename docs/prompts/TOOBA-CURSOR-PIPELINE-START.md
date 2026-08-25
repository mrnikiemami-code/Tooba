# Tooba — Legacy Cursor Pipeline Bootstrap

> **RETIRED / HISTORICAL ONLY**
>
> This prompt documented the pre-Bridge Cursor/chat transport. Do not use it to
> start or continue current Tooba work.

Current operation is agent-neutral:

```text
PIPELINE-PROTOCOL: BRIDGE-V2
CHANNEL: tooba-main
ARCHITECT → downloadable .task.md → Bridge → Coding Agent Worker
→ Result → Bridge → ARCHITECT → ACCEPT / REPAIR / BLOCK → next .task.md
```

Use `docs/ai/TOOBA-PIPELINE-PROTOCOL.md` and
`docs/ai/TOOBA-PIPELINE-CONTROLLER.md`.

The Worker:

- receives Tasks only from Bridge;
- follows `ONE WORKER = ONE ACTIVE TASK`;
- sends heartbeats while `Waiting` and `Working`;
- stops polling while one Task is active;
- returns the complete Result through Bridge;
- never treats Worker PASS as Architect ACCEPT;
- never treats `SYSTEM-BRIDGE-ALERT` as a Result;
- does not replay historical files from `docs/ai/tasks/`.

Historical Cursor task/result markers remain in old artifacts as execution
evidence only.
