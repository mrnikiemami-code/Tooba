PIPELINE-PROTOCOL: BRIDGE-V2

TASK-ID: TB-PXX-TXXX
PHASE: PXX — <Phase Name>
CHANNEL: tooba-main
STATUS: ISSUED
TASK-TYPE: IMPLEMENTATION
WORKER-POLICY: ONE WORKER = ONE ACTIVE TASK

## Objective

<one focused objective>

## Accepted baseline

- ...

## Required reading

- `AGENTS.md`
- `docs/PROJECT-STATE.md`
- `docs/ROADMAP.md`
- `docs/ai/TOOBA-RECOVERY-CONTEXT.md`

## Scope

1. ...

## Out of scope

- ...

## Architecture and product guardrails

- ...

## Repository safety

- recover `main`;
- require `HEAD == origin/main`;
- inspect the working tree;
- no force push, destructive reset, silent stash, or history rewrite.

## Validation and evidence

- run relevant build/tests;
- run `git diff --check`;
- perform visual review when UI is in scope;
- create `docs/evidence/<TASK-ID>/...`;
- require `HEAD == origin/main` and a clean working tree after push.

## Source-of-Truth sync

Update only as authorized:

- `docs/PROJECT-STATE.md`
- `docs/ROADMAP.md`
- `docs/ai/TOOBA-RECOVERY-CONTEXT.md`

## Result contract

Return the complete Task-specific Result through Bridge.

```text
Worker PASS != Architect ACCEPT
SYSTEM-BRIDGE-ALERT != Result
```

After successful Result delivery, call the appropriate Bridge task
complete/fail endpoint. Only after the active lifecycle completes may the Worker
return to `Waiting` and resume polling `tooba-main`.

END_TASK
