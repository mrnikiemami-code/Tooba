# Tooba — Pipeline Protocol

```text
PIPELINE-PROTOCOL: BRIDGE-V2
CHANNEL: tooba-main
```

This document is the canonical operational protocol. Pipeline V1 and its
ChatGPT/Cursor same-conversation transport are **RETIRED / HISTORICAL ONLY**.
Historical task and result artifacts may retain legacy syntax as evidence of
prior execution; they are not current operational instructions.

## Roles and flow

```text
USER                = product/business authority
ARCHITECT           = task issuer and result reviewer
BRIDGE              = transport/orchestration boundary
CODING AGENT WORKER = agent-neutral implementation worker
REPOSITORY          = durable technical Source of Truth
```

The Worker may be Cursor, OpenAI Codex, Claude Code, Hermes, or another
compatible agent. Repository governance must not depend on one agent product.

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

The user is not expected to paste Tasks into a Coding Agent. Bridge detects and
dispatches the actual downloadable task artifact on its assigned channel.

## Worker contract

```text
ONE WORKER = ONE ACTIVE TASK
Worker PASS != Architect ACCEPT
```

- A Worker polls only its configured Bridge channel while `Waiting`.
- On delivery it verifies `receivedTask.channelId`, acquires its busy/mutex
  protection, changes to `Working`, continues `Working` heartbeats, and stops
  task polling.
- It executes only the received Task and does not claim parallel work.
- It validates, commits, pushes, and returns the complete Result through Bridge.
- It calls the Bridge completion/failure endpoint only after Result delivery.
- It resumes `Waiting` heartbeats and polling only after the active lifecycle is
  complete.
- It never invents requirements, broadens scope, redesigns locked architecture,
  or self-authorizes the next Task.
- Architectural concerns are reported, not silently implemented.

## Result review lifecycle

Every real Worker Result is reviewed by the Architect:

```text
Result → review evidence → ACCEPT / REPAIR / BLOCK
```

- `ACCEPT`: Architect issues the next safe downloadable Task artifact.
- `REPAIR`: Architect issues a focused repair Task artifact.
- `BLOCK`: processing stops only for a genuine human, product, architectural,
  security, data-loss, or repository-recovery blocker.

Automatic continuation occurs only after Architect review. A Worker Result,
including `PASS`, does not itself advance accepted project state.

## SYSTEM-BRIDGE-ALERT

```text
SYSTEM-BRIDGE-ALERT != Result
```

On an alert:

- do not advance project state;
- do not mark the active Task `PASS` or `FAIL`;
- do not issue or claim another Task;
- wait for Worker/Bridge recovery.

An alert must never be substituted for the Task's Result contract.

## Task and result artifacts

Every implementation Task is an actual downloadable:

```text
<TASK-ID>.task.md
```

Bridge dispatch is the sole operational Task transport. The Worker persists the
received artifact under `docs/ai/tasks/` for durable audit after receipt; that
directory is not a queue and old files must never be replayed.

Bridge-V2 Results use the contract defined by the received Task and are posted
to Bridge. Legacy `BEGIN_TOOBA_CURSOR_*` markers inside historical files are
preserved as evidence and compatibility records, not current transport.

## Source of Truth and Git

The repository is durable technical Source of Truth. Bridge carries Tasks,
Results, and lifecycle state; it does not replace repository truth.

Before normal execution:

```text
branch = main
HEAD == origin/main
known/safe working tree
```

After successful execution:

```text
local commit
push origin main
git fetch origin
HEAD == origin/main
clean working tree
```

Unsafe divergence is `RECOVERY_CONFLICT`. Never force-push, rewrite history,
silently stash, or destroy unrelated work.

## Retired behavior

The following are historical only and must not be used operationally:

- manual Architect envelopes pasted into a Coding Agent;
- same-session or same-chat continuation;
- `WAITING_FOR_ARCHITECT_IN_SAME_SESSION`;
- HUMAN/PIPELINE conversational handoff;
- Cursor browser/composer/send-button transport;
- requiring one specific agent product;
- chat-session `No Envelope = No Execution` mechanics.

Current authority is a valid Task actually received from Bridge on the Worker's
configured channel. Historical task/result records remain unchanged.
