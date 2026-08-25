# Tooba — Pipeline Controller

```text
PIPELINE-PROTOCOL: BRIDGE-V2
CHANNEL: tooba-main
ONE WORKER = ONE ACTIVE TASK
```

Bridge is the transport/orchestration boundary. Worker execution is
agent-neutral. The Architect issues Tasks and reviews Results. The repository is
durable technical Source of Truth.

## States

```text
RECOVERING
WAITING
WORKING
RESULT_DELIVERING
AWAITING_ARCHITECT_REVIEW
BLOCKED
RECOVERY_CONFLICT
```

Only a Task dispatched by Bridge may start implementation. Only Architect
`ACCEPT`, `REPAIR`, or `BLOCK` may determine the next lifecycle action.

## Startup and waiting

1. Recover `main`, compare `HEAD` with `origin/main`, and inspect the working
   tree.
2. Read the current governance and recovery documents.
3. Check Bridge health.
4. Send `Waiting` heartbeat for the configured Worker and `tooba-main`.
5. Poll only:

```text
GET /api/tasks/next?channelId=tooba-main
```

`204` means remain `Waiting`, continue heartbeats, and keep polling. The Worker
must not use another channel or infer work from ROADMAP, chat, or historical
task files.

## Task acquisition and execution

When a Task arrives:

1. verify `receivedTask.channelId == "tooba-main"`;
2. acquire the Worker's busy/mutex protection;
3. set state `Working` and send `Working` heartbeat;
4. stop task polling;
5. persist the received downloadable `.task.md` artifact for audit;
6. execute only that Task;
7. run its validation and create its evidence;
8. update only authorized Source-of-Truth documents;
9. commit, push, fetch, and require `HEAD == origin/main`;
10. post the complete Result to Bridge;
11. after Result delivery succeeds, call the appropriate complete/fail endpoint;
12. only then release the mutex, set `Waiting`, send a `Waiting` heartbeat, and
    resume polling.

While `Working`, heartbeats continue and task polling remains stopped.

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

An alert does not advance project state, does not mark the Task `PASS` or
`FAIL`, and does not authorize another Task. Keep the active lifecycle reserved
and wait for Worker/Bridge recovery.

## Recovery and stop conditions

Use `RECOVERY_CONFLICT` for an unsafe or irreconcilable repository state. Stop
only for an explicit pause or genuine architectural, product, security,
data-loss, external-business-fact, or repository blocker.

Persian documentation quality remains part of implementation acceptance.

## Legacy notice

Pipeline Controller V1 is **RETIRED / HISTORICAL ONLY**. Current operation must
not read an Architect chat, paste Tasks or Results, drive a browser composer,
depend on one Cursor conversation, or treat `docs/ai/tasks/` as a live inbox.
Historical files may retain those instructions solely as prior-execution
evidence.
