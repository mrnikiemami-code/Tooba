# Tooba — Pipeline Protocol

```text
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
CHANNEL: tooba-main
```

This document is the canonical operational protocol. Pipeline V1, its
ChatGPT/Cursor same-conversation transport, and **BRIDGE-V2 continuous online
Worker polling** are **RETIRED / HISTORICAL ONLY**. Historical task and result
artifacts may retain legacy syntax as evidence of prior execution; they are not
current operational instructions.

## Roles and flow

```text
USER                = product/business authority
ARCHITECT           = task issuer and result reviewer
TAMPERMONKEY        = dispatches downloadable .task.md to Bridge
BRIDGE              = transport/orchestration boundary
EXTERNAL WATCHDOG   = observes Pending Tasks and sends BRIDGE-WAKE
CODING AGENT WORKER = agent-neutral implementation worker (normally IDLE)
REPOSITORY          = durable technical Source of Truth
```

The Worker may be Cursor, OpenAI Codex, Claude Code, Hermes, or another
compatible agent. Repository governance must not depend on one agent product.

```text
ARCHITECT
→ downloadable <TASK-ID>.task.md
→ Tampermonkey
→ Bridge Task = Pending
→ External Watchdog
→ BRIDGE-WAKE
→ Coding Agent wakes
→ claim exactly one Task
→ implement
→ Result
→ Bridge
→ Tampermonkey
→ ARCHITECT
→ ACCEPT / REPAIR / BLOCK
→ next <TASK-ID>.task.md
```

The user is not expected to paste Tasks into a Coding Agent. Tampermonkey
dispatches the downloadable artifact to Bridge. The External Watchdog wakes the
idle Coding Agent when a Pending Task appears.

## Critical idle rule

The Coding Agent is normally **IDLE / OFFLINE** between Tasks.

```text
Worker offline + no active Task = NORMAL
```

Do **not** require:

- continuous `GET /api/tasks/next` polling while idle;
- a permanently online Worker;
- idle heartbeat;
- a Worker waiting loop between Tasks.

An idle/offline Worker is not an infrastructure failure by itself.

## Worker contract

```text
ONE WORKER = ONE ACTIVE TASK
Worker PASS != Architect ACCEPT
```

On `BRIDGE-WAKE`, the Worker:

1. checks Bridge health;
2. claims **exactly one** Pending Task on its configured channel;
3. sends `Working` heartbeat only for the active lifecycle;
4. persists the received downloadable `.task.md` artifact for audit;
5. executes only that Task;
6. validates, commits, pushes, and returns the complete Result through Bridge;
7. completes the Bridge task lifecycle;
8. returns to **IDLE** and stops.

It never invents requirements, broadens scope, redesigns locked architecture,
or self-authorizes the next Task. Architectural concerns are reported, not
silently implemented.

## Critical storefront visual lock

Home and PDP are critical Shopeiva-locked surfaces.

- Contracts: `docs/visual-baselines/HOME-FIDELITY-CONTRACT.md`,
  `docs/visual-baselines/PDP-FIDELITY-CONTRACT.md`
- Review checklist: `docs/visual-baselines/CRITICAL-STOREFRONT-VISUAL-REVIEW.md`
- Any Task touching shared storefront components must run
  `npm run test:critical-storefront` in `src/frontend`.
- Functional PASS does not imply Visual ACCEPT.

**Retired BRIDGE-V2 behavior:** resume `Waiting` heartbeats and continuous
polling after every Result. Under BRIDGE-WAKE-V1 the Worker waits for the next
`BRIDGE-WAKE`.

## Watchdog authority

The External Watchdog **MAY**:

- inspect Bridge for a new Pending Task;
- send `BRIDGE-WAKE` to the configured Coding Agent once for that Pending Task.

The Watchdog **MUST NOT**:

- create Tasks;
- modify Task scope;
- make architectural decisions;
- ACCEPT / REPAIR / BLOCK;
- judge implementation success;
- invent recovery work;
- advance the roadmap.

`BRIDGE-WAKE` is infrastructure control traffic. It is **not** a Task, Result,
architectural instruction, or implementation evidence. The Watchdog must not
spam repeated wakes for the same Pending Task.

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

Under BRIDGE-WAKE-V1, do **not** emit or interpret an alert merely because the
Worker is offline between Tasks.

Valid alerts include real failures such as:

- Bridge API unavailable;
- Task dispatch failure;
- Result transport failure;
- Watchdog failure preventing a Pending Task from waking the Coding Agent;
- real active-task transport or execution failure.

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

Tampermonkey dispatches it to Bridge. The Worker persists the received artifact
under `docs/ai/tasks/` for durable audit after receipt; that directory is not a
queue and old files must never be replayed.

Results use the contract defined by the received Task and are posted to Bridge.
Legacy `BEGIN_TOOBA_CURSOR_*` markers inside historical files are preserved as
evidence and compatibility records, not current transport.

## Canonical Worker Result envelope

The Task artifact delivered through Tampermonkey/Bridge is a real downloadable
`<TASK-ID>.task.md` file whose envelope is `BEGIN_TOOBA_TASK … END_TOOBA_TASK`
and which carries its own `RESULT CONTRACT` section. The Worker only consumes
that artifact; it never authors or dispatches Tasks.

The Worker Result posted to `POST /api/results` is a single `content` string with
this canonical envelope. Start and end markers must **never** change:

```text
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: <actual task ID>
Parent-Task: <actual parent task ID>
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: <actual program>
Mode: <actual execution mode>
Bridge-Task-Row: <actual Bridge row id>
Status: PASS | PARTIAL | BLOCKED | RECOVERY_CONFLICT
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: <verified expected SHA>
Base-SHA: <actual SHA>
Code-SHA: <actual or NONE>
Evidence-SHA: <actual or NONE>
automaticNextImplementationTask: NONE
<root cause; implementation; target provenance; service-method mapping; policy/grant
 fixture ledger; explicit shadow gate proof; live parity/mismatch matrix; unchanged
 legacy projection; restore/cleanup; focused tests; actual elapsed; changed paths;
 commit/push; blockers>
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK
```

Fixed-tail rules:

- the tail from `Expected-Base-SHA` through `END_TOOBA_WORKER_RESULT` and
  `END_TOOBA_TASK` is fixed in order; only the values change per Task;
- the body sits strictly between `automaticNextImplementationTask: NONE` and
  `END_TOOBA_WORKER_RESULT`, and nothing follows `END_TOOBA_TASK`;
- no unresolved placeholder (`<actual>`, `<…>`), template marker, or editorial
  note may be transmitted; every field carries a real value or `NONE`;
- `Expected-Base-SHA` is taken from the received Task, never from memory;
- `Status` is exactly one of the four listed values.

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
- chat-session `No Envelope = No Execution` mechanics;
- **continuous Bridge polling while idle (`BRIDGE-V2` online Worker model)**;
- **permanent idle heartbeat as a prerequisite for normal operation**;
- **treating Worker offline between Tasks as an infrastructure alert by itself**.

Current authority is a valid Task actually received from Bridge on the Worker's
configured channel after a `BRIDGE-WAKE`. Historical task/result records remain
unchanged.
