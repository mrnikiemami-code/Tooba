# Tooba — Bridge-Wake-V1 Recovery Start

```text
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
CHANNEL: tooba-main
Current Phase: P05 — Operational Surface Integration
Last Architect Accepted Product Task: TB-P05-T019
Last Architect Accepted Governance Task: TB-P05-GOV-MIGRATION-BRIDGE-WAKE-V1
Current Product Task: TB-P05-T020 = AWAITING_ARCHITECT_ACCEPT
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

```text
ONE WORKER = ONE ACTIVE TASK
Worker PASS != Architect ACCEPT
Worker offline between Tasks = NORMAL
```

P00–P04 remain complete. P05 remains in progress. Through TB-P05-T019 accepted. TB-P05-T020 restores Shopeiva Category/Search/Listing fidelity with live Tooba data.
