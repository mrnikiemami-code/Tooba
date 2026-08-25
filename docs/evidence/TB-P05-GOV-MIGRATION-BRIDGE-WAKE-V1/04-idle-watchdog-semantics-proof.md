# 04 — Idle / Watchdog Semantics Proof

Task: `TB-P05-GOV-MIGRATION-BRIDGE-WAKE-V1`

## Idle between Tasks

Documented in protocol, controller, AGENTS, runtime policy, and recovery docs:

```text
Worker offline + no active Task = NORMAL
```

No continuous polling, no idle heartbeat, and no waiting loop are required
between Tasks.

## Watchdog authority (narrow)

The External Watchdog **MAY**:

- inspect Bridge for a new Pending Task;
- send `BRIDGE-WAKE` once per newly observed Pending Task.

The External Watchdog **MUST NOT**:

- create Tasks;
- modify Task scope;
- ACCEPT / REPAIR / BLOCK;
- judge implementation success;
- invent recovery work;
- advance roadmap;
- spam repeated wakes for the same Pending Task.

## BRIDGE-WAKE classification

`BRIDGE-WAKE` is infrastructure control traffic. It is **not**:

- a Task;
- a Result;
- an architectural instruction;
- implementation evidence.

## SYSTEM-BRIDGE-ALERT update

Current docs state that an alert must **not** be emitted or interpreted merely
because the Worker is offline between Tasks. Valid alerts remain real transport
or execution failures (Bridge unavailable, dispatch failure, Result transport
failure, Watchdog failure preventing wake, active-task failure).

## Runtime policy fields

`docs/ai/pipeline-runtime-policy.json` records:

- `wakeControlMessage: BRIDGE-WAKE`
- `idleSemantics.offlineBetweenTasksIsNormal: true`
- `idleSemantics.continuousPollingRequired: false`
- `retiredBridgeV2Semantics.*: RETIRED`
