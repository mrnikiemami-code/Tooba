# 03 — BRIDGE-WAKE-V1 Proof

Task: `TB-P05-GOV-MIGRATION-BRIDGE-WAKE-V1`

## Canonical marker

Current operational docs state:

```text
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
CHANNEL: tooba-main
```

Primary sources:

- `docs/ai/TOOBA-PIPELINE-PROTOCOL.md`
- `docs/ai/TOOBA-PIPELINE-CONTROLLER.md`
- `AGENTS.md`
- `docs/ai/pipeline-runtime-policy.json`

## Artifact flow

```text
ARCHITECT
→ downloadable <TASK-ID>.task.md
→ Tampermonkey
→ Bridge Task = Pending
→ External Watchdog
→ BRIDGE-WAKE
→ Coding Agent wakes
→ GET /api/tasks/next?channelId=tooba-main
→ claim exactly one Task
→ persist docs/ai/tasks/<TASK-ID>.task.md
→ implement
→ POST /api/results
→ Bridge
→ Tampermonkey
→ ARCHITECT
→ ACCEPT / REPAIR / BLOCK
```

This migration Task itself was received from Bridge after `BRIDGE-WAKE` and
persisted as `docs/ai/tasks/TB-P05-GOV-MIGRATION-BRIDGE-WAKE-V1.task.md`.

## Preserved invariants

- `ONE WORKER = ONE ACTIVE TASK`
- `Worker PASS != Architect ACCEPT`
- Repository remains durable technical Source of Truth
- Historical task/result artifacts preserved unchanged

## Retired operational behavior

BRIDGE-V2 continuous online Worker polling, idle heartbeat, and post-Result
polling loop are explicitly marked **RETIRED / HISTORICAL ONLY** in current
governance docs.
