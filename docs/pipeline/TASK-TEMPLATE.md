PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

TASK-ID: TB-PXX-TXXX
PHASE: PXX — <Phase Name>
CHANNEL: tooba-main
STATUS: ISSUED
TASK-TYPE: IMPLEMENTATION
WORKER-POLICY: ONE WORKER = ONE ACTIVE TASK

## Objective

<One clear objective.>

## Scope

### In scope

- ...

### Out of scope

- ...

## Repository recovery

Run:

```bash
git rev-parse --show-toplevel
git fetch origin
git branch --show-current
git rev-parse HEAD
git rev-parse origin/main
git status --short --branch
```

Require:

```text
branch = main
HEAD == origin/main
known/safe working tree
```

## Implementation

...

## Validation

...

## Evidence

Create:

```text
docs/evidence/<TASK-ID>/
```

## Source of Truth

Update only authorized current governance files.

## Git

```bash
git diff --check
git status --short --branch
git add ...
git commit -m "<type> <summary> [<TASK-ID>]"
git push origin main
git fetch origin
git rev-parse HEAD
git rev-parse origin/main
git status --short --branch
```

Require:

```text
HEAD == origin/main
working tree clean
```

## Result contract

Return through Bridge:

```text
BEGIN_TOOBA_WORKER_RESULT
Task-ID: <TASK-ID>
Channel: tooba-main
Status: PASS | FAIL | BLOCKED | RECOVERY_CONFLICT
...
END_TOOBA_WORKER_RESULT
```

`Worker PASS != Architect ACCEPT`.
`SYSTEM-BRIDGE-ALERT` is not a Result.

After successful Result delivery, call the appropriate Bridge task complete/fail
endpoint. Return to **IDLE** and stop. Do **not** resume continuous polling.

END_TASK
