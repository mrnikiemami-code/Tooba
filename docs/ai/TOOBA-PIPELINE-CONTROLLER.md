# Tooba — Pipeline Controller

```text
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
CHANNEL: tooba-main
ONE WORKER = ONE ACTIVE TASK
```

Bridge is the transport/orchestration boundary. The External Watchdog observes
Pending Tasks and sends `BRIDGE-WAKE`. Worker execution is agent-neutral. The
Architect issues Tasks and reviews Results. The repository is durable technical
Source of Truth.

## States

```text
IDLE
RECOVERING
WORKING
RESULT_DELIVERING
AWAITING_ARCHITECT_REVIEW
BLOCKED
RECOVERY_CONFLICT
```

`IDLE` is the normal between-Task state. The Worker is offline or inactive and
does **not** poll Bridge continuously.

Only a Task dispatched by Bridge and claimed after `BRIDGE-WAKE` may start
implementation. Only Architect `ACCEPT`, `REPAIR`, or `BLOCK` may determine the
next lifecycle action.

## Startup and idle

Between Tasks the Worker remains **IDLE**. No continuous polling, no idle
heartbeat, and no waiting loop are required.

When `BRIDGE-WAKE` arrives:

1. recover `main`, compare `HEAD` with `origin/main`, and inspect the working
   tree;
2. read current governance and recovery documents;
3. check Bridge health;
4. claim exactly one Pending Task:

```text
GET /api/tasks/next?channelId=tooba-main
```

5. verify `receivedTask.channelId == "tooba-main"`;
6. send `Working` heartbeat for the active lifecycle only.

The Worker must not use another channel or infer work from ROADMAP, chat, or
historical task files.

## Task acquisition and execution

After claim:

1. acquire the Worker's busy/mutex protection if configured;
2. persist the received downloadable `.task.md` artifact for audit;
3. execute only that Task;
4. run its validation and create its evidence;
5. update only authorized Source-of-Truth documents;
6. commit, push, fetch, and require `HEAD == origin/main`;
7. post the complete Result to Bridge;
8. after Result delivery succeeds, call the appropriate complete/fail endpoint;
9. release the mutex if used;
10. return to **IDLE** and stop.

**Retired BRIDGE-V2 behavior:** resume `Waiting` heartbeats and continuous
polling after every Result.

While `Working`, `Working` heartbeats may continue for the active lifecycle
only. Bridge polling must not run in parallel with an active Task.

## Watchdog control

The External Watchdog:

- MAY inspect Bridge for Pending Tasks and send one `BRIDGE-WAKE` per newly
  observed Pending Task;
- MUST NOT create Tasks, modify scope, review Results, or advance roadmap;
- MUST NOT spam repeated wakes for the same Pending Task.

`BRIDGE-WAKE` is control traffic only. It is not a Task, Result, or architectural
instruction.

## Result control

```text
Worker PASS != Architect ACCEPT
```

After a real Result, control returns through Bridge to the Architect:

- `ACCEPT` → Architect issues the next safe downloadable Task;
- `REPAIR` → Architect issues a focused repair Task;
- `BLOCK` → stop for a genuine blocker.

The controller never self-issues the next Task.

## Bridge alerts

```text
SYSTEM-BRIDGE-ALERT != Result
```

Do not emit or interpret an alert merely because the Worker is idle/offline
between Tasks.

An alert does not advance project state, does not mark the Task `PASS` or
`FAIL`, and does not authorize another Task. Keep the active lifecycle reserved
and wait for Worker/Bridge recovery when a real transport or execution failure
occurs.

## Recovery and stop conditions

Use `RECOVERY_CONFLICT` for an unsafe or irreconcilable repository state. Stop
only for an explicit pause or genuine architectural, product, security,
data-loss, external-business-fact, or repository blocker.

Persian documentation quality remains part of implementation acceptance.

## Legacy notice

Pipeline Controller V1 and **BRIDGE-V2 continuous online Worker polling** are
**RETIRED / HISTORICAL ONLY**. Current operation must not:

- poll Bridge continuously while idle;
- require permanent online Worker presence;
- treat idle/offline between Tasks as failure;
- read an Architect chat, paste Tasks or Results, drive a browser composer,
  depend on one Cursor conversation, or treat `docs/ai/tasks/` as a live inbox.

Historical files may retain those instructions solely as prior-execution
evidence.
