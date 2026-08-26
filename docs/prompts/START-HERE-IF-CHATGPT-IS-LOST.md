# Tooba — Bridge-Wake-V1 Recovery Start

```text
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
CHANNEL: tooba-main
Current Phase: P05 — Operational Surface Integration
Last Architect Accepted Product Task: TB-P05-T025
Last Architect Accepted Governance Task: TB-P05-GOV-MIGRATION-BRIDGE-WAKE-V1
Last Implementation Task: TB-P05-GATE (Worker PASS submitted)
Current Gate: TB-P05-GATE = AWAITING_ARCHITECT_ACCEPT
TB-P05-T026 = ACCEPTED
TB-P05-T026-R1 = ACCEPTED
TB-P05-T026-R2 = ACCEPTED
HOME_VISUAL_REVIEW = OPEN_FOR_USER_FEEDBACK
PDP_VISUAL_REVIEW = OPEN_FOR_USER_FEEDBACK
P05: AWAITING_ARCHITECT_ACCEPT
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

Evidence for the current gate lives under `docs/evidence/TB-P05-GATE/`.
