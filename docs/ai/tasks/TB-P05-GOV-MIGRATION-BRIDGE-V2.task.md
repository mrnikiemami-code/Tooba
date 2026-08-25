TASK-ID: TB-P05-GOV-MIGRATION-BRIDGE-V2

PROJECT: Tooba

PHASE: P05 — Operational Surface Integration

CHANNEL: tooba-main

STATUS: ISSUED

TASK-TYPE: PIPELINE GOVERNANCE MIGRATION

WORKER-POLICY: ONE WORKER = ONE ACTIVE TASK

## Objective

Migrate the Tooba repository's current operational AI-development governance
from the retired legacy ChatGPT/Cursor pipeline to the Bridge-V2 Worker
protocol.

This Task is governance-only. Do not execute product work or TB-P05-T010,
modify product/domain behavior, change accepted architecture, redesign
Shopeiva, or advance P05 product scope.

## Authoritative pipeline

```text
ARCHITECT
→ downloadable .task.md
→ Bridge
→ Coding Agent Worker
→ Result
→ Bridge
→ ARCHITECT
→ ACCEPT / REPAIR / BLOCK
→ next .task.md
```

Repository governance is agent-neutral. Retire current operational rules for
manual Cursor envelopes, same-session continuation, Architect chat waiting,
HUMAN/PIPELINE conversational handoff, manual Task paste, Cursor-specific
transport, and legacy chat-session execution mechanics. Preserve historical
Task and Result artifacts as evidence.

## Preserved state

- P05 — Operational Surface Integration remains current.
- TB-P05-T009 and TB-P05-T009-REPAIR-01 remain accepted.
- Legacy-form TB-P05-T010 is NOT EXECUTED and HELD during migration.
- Reissue TB-P05-T010 only after this migration is Architect-accepted,
  preserving product scope and acceptance intent.

## Required governance

- canonical marker `PIPELINE-PROTOCOL: BRIDGE-V2`;
- actual downloadable `<TASK-ID>.task.md` artifacts dispatched by Bridge;
- agent-neutral `Coding Agent Worker`, `Worker`, and `Bridge Worker` terms;
- `ONE WORKER = ONE ACTIVE TASK`;
- `Worker PASS != Architect ACCEPT`;
- Architect review lifecycle `ACCEPT / REPAIR / BLOCK`;
- `SYSTEM-BRIDGE-ALERT` is not a Result and does not advance state;
- Bridge as transport/orchestration boundary;
- repository as durable technical Source of Truth;
- automatic continuation only after Architect review;
- no current same-conversation dependency.

## Files to inspect

- `AGENTS.md`
- `README.md`
- `SETUP.md`
- `docs/PROJECT-STATE.md`
- `docs/ROADMAP.md`
- `docs/ai/TOOBA-PIPELINE-PROTOCOL.md`
- `docs/ai/TOOBA-PIPELINE-CONTROLLER.md`
- `docs/ai/TOOBA-RECOVERY-CONTEXT.md`
- current-operation references under `docs/ai/`, `docs/prompts/`, and root
  governance files

Do not mass-edit historical evidence.

## Required evidence

Create `docs/evidence/TB-P05-GOV-MIGRATION-BRIDGE-V2/`:

- `01-governance-files-reviewed.md`
- `02-retired-rule-audit.md`
- `03-bridge-v2-protocol-proof.md`
- `04-recovery-state-proof.md`

The audit must classify every relevant legacy match as
`CURRENT_OPERATIONAL` or `HISTORICAL_EVIDENCE`, with no unresolved current
conflict at PASS.

## Validation

- run repository governance/doc validation scripts if present;
- run `git diff --check`;
- inspect `git status --short --branch`;
- do not run unrelated expensive product builds.

## Expected pre-review state

```text
PIPELINE = BRIDGE-V2
TB-P05-GOV-MIGRATION-BRIDGE-V2 = AWAITING_ARCHITECT_ACCEPT
TB-P05-T010 = HELD / NOT EXECUTED
P05 = IN_PROGRESS
```

The Worker must not mark Architect ACCEPT.

## Git

Commit:

```text
docs migrate Tooba pipeline governance to Bridge-V2 [TB-P05-GOV-MIGRATION-BRIDGE-V2]
```

Push `main`, fetch, require `HEAD == origin/main`, and require a clean working
tree.

## Result delivery

Return the complete `BEGIN_TOOBA_WORKER_RESULT` contract through Bridge. A
`SYSTEM-BRIDGE-ALERT` must never substitute for this Result. Do not self-issue
the next product Task. Control returns through Bridge to Architect for
`ACCEPT / REPAIR / BLOCK`.

END_TASK
