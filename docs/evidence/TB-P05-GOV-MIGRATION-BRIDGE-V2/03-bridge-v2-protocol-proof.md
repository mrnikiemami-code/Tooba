# Bridge-V2 protocol proof

Task: `TB-P05-GOV-MIGRATION-BRIDGE-V2`

## Canonical declarations

| Required invariant | Canonical proof |
| --- | --- |
| Protocol | `PIPELINE-PROTOCOL: BRIDGE-V2` in `docs/ai/TOOBA-PIPELINE-PROTOCOL.md` |
| Channel | `CHANNEL: tooba-main` |
| Task artifact | Actual downloadable `<TASK-ID>.task.md`, detected and dispatched by Bridge |
| Agent neutrality | `Coding Agent Worker`; Cursor, Codex, Claude Code, Hermes, or compatible agent |
| Mutex | `ONE WORKER = ONE ACTIVE TASK` |
| Acceptance boundary | `Worker PASS != Architect ACCEPT` |
| Decisions | Architect reviews every real Result as `ACCEPT / REPAIR / BLOCK` |
| Alert boundary | `SYSTEM-BRIDGE-ALERT != Result` |
| Source of Truth | Repository is durable technical Source of Truth |

## Flow

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

## Worker lifecycle

```text
Waiting heartbeat + poll tooba-main
→ receive and verify channel
→ acquire busy/mutex
→ Working heartbeat
→ stop polling
→ execute only active Task
→ validate / evidence / commit / push
→ POST complete Result to Bridge
→ call complete/fail endpoint
→ release mutex
→ Waiting heartbeat and resume polling
```

While active, heartbeat continues and polling remains stopped.

## Result and alert proof

- A Worker `PASS` is only a submitted Result status.
- Architect review is required before accepted project state advances.
- `ACCEPT` issues the next safe Task.
- `REPAIR` issues a focused repair Task.
- `BLOCK` stops only for a genuine blocker.
- `SYSTEM-BRIDGE-ALERT` advances nothing, marks no Task PASS/FAIL, and
  authorizes no next Task.
