# Tooba — Bridge-Wake-V1 Recovery Start

```text
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
CHANNEL: tooba-main
Current Phase: P05 — Operational Surface Integration
Last Architect Accepted Product Task: TB-P05-T025
Last Architect Accepted Governance Task: TB-P05-GOV-MIGRATION-BRIDGE-WAKE-V1
Current Product Task: TB-P05-T026-R2 = AWAITING_ARCHITECT_ACCEPT
TB-P05-T026 = REPAIR_REQUIRED
TB-P05-T026-R1 = ACCEPTED
HOME_VISUAL_ACCEPTANCE = AWAITING_USER_REVIEW
P05: AWAITING_ARCHITECT_GATE
Legacy TB-P05-T010 transport artifact: RETIRED / NOT EXECUTED
```

Worker PASS is not Architect ACCEPT. Do not execute roadmap prose or historical task archives.

## Recovery procedure

Run:

```bash
git rev-parse --show-toplevel
git fetch origin
git branch --show-current
git rev-parse HEAD
git rev-parse origin/main
git status --short --branch
```

Read:

```text
AGENTS.md
docs/PROJECT-STATE.md
docs/ROADMAP.md
docs/ai/TOOBA-PIPELINE-PROTOCOL.md
docs/ai/TOOBA-PIPELINE-CONTROLLER.md
docs/ai/TOOBA-RECOVERY-CONTEXT.md
```

Then recover current phase, accepted history, Bridge channel, active Task, blockers, and Git safety.

Evidence for the current product task lives under `docs/evidence/TB-P05-T022/`.
