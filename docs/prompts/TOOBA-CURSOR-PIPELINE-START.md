# Tooba — Legacy Cursor Pipeline Bootstrap

> **RETIRED / HISTORICAL ONLY**
>
> This prompt documented the pre-Bridge Cursor/chat transport. Do not use it to
> start or continue current Tooba work.

Current operation is agent-neutral:

```text
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
CHANNEL: tooba-main
ARCHITECT → downloadable .task.md → Tampermonkey → Bridge → External Watchdog
→ BRIDGE-WAKE → Coding Agent Worker → claim → implement → Result → Bridge
→ Tampermonkey → ARCHITECT → ACCEPT / REPAIR / BLOCK → next .task.md
```

Use `docs/ai/TOOBA-PIPELINE-PROTOCOL.md` and
`docs/ai/TOOBA-PIPELINE-CONTROLLER.md`.

The Worker:

- is normally **IDLE / OFFLINE** between Tasks;
- wakes only on `BRIDGE-WAKE` after a Pending Task appears;
- receives Tasks only from Bridge;
- follows `ONE WORKER = ONE ACTIVE TASK`;
- sends `Working` heartbeat only for the active lifecycle;
- does **not** poll Bridge continuously while idle;
- returns the complete Result through Bridge;
- returns to **IDLE** after Result delivery;
- never treats Worker PASS as Architect ACCEPT;
- never treats `SYSTEM-BRIDGE-ALERT` as a Result;
- does not replay historical files from `docs/ai/tasks/`.

Historical Cursor task/result markers remain in old artifacts as execution
evidence only.
