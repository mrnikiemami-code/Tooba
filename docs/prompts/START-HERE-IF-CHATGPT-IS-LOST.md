# Tooba — Bridge-V2 Recovery Start

```text
PIPELINE-PROTOCOL: BRIDGE-V2
CHANNEL: tooba-main
Current Phase: P05 — Operational Surface Integration
Last Architect Accepted Product Task: TB-P05-T012
Governance: TB-P05-GOV-MIGRATION-BRIDGE-V2 = ACCEPTED
Current Product Task: TB-P05-T013 Bridge-V2 = AWAITING_ARCHITECT_ACCEPT
Legacy TB-P05-T010 transport artifact: RETIRED / NOT EXECUTED
```

Worker PASS is not Architect ACCEPT. Do not execute roadmap prose or historical
task archives.

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

Then recover:

- current phase and accepted product history;
- current Bridge channel and Worker lifecycle;
- active or reviewed Task from Bridge;
- blockers and locked architecture/product decisions;
- `HEAD`, `origin/main`, and working-tree safety.

The sole current operational Task source is Bridge. The user does not paste
Tasks into a Worker.

```text
ONE WORKER = ONE ACTIVE TASK
Worker PASS != Architect ACCEPT
SYSTEM-BRIDGE-ALERT != Result
```

An alert does not advance state, mark a Task PASS/FAIL, or authorize another
Task. Wait for Worker/Bridge recovery.

Historical Task and Result artifacts may contain legacy Cursor/chat pipeline
syntax. Preserve them as prior-execution evidence; they are not current
operational instructions.

P00–P04 remain complete. P05 remains in progress. TB-P05-T010 through TB-P05-T012
are ACCEPTED. TB-P05-T013 is AWAITING_ARCHITECT_ACCEPT with private Wishlist
ownership, actor isolation, idempotent add, safe remove, batched membership,
live Storefront composition, customer list/empty states, PDP/card toggles, and
evidence under `docs/evidence/TB-P05-T013/`. Shopeiva decisions, module boundaries, accepted
architecture, and deferred Payment/Cart concerns remain unchanged.
