PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

TASK-ID: TB-PXX-GATE
PHASE: PXX — <Phase Name>
CHANNEL: tooba-main
STATUS: ISSUED
TASK-TYPE: GATE
WORKER-POLICY: ONE WORKER = ONE ACTIVE TASK

## Objective

Perform phase gate review only.

## Gate checks

- architecture;
- product;
- implementation;
- tests;
- evidence;
- Source of Truth;
- known limitations;
- next-phase recommendation.

No feature implementation is permitted unless explicitly authorized.

## Validation

- `git diff --check`;
- relevant tests/build only when needed;
- `HEAD == origin/main` and working-tree status after push.

## Deliverable

Create a human-readable review under:

```text
docs/evidence/<TASK-ID>/GATE-REVIEW.md
```

Return the complete Result through Bridge. `Worker PASS != Architect ACCEPT`.
`SYSTEM-BRIDGE-ALERT` is not a Result.

After successful Result delivery, call the appropriate Bridge task
complete/fail endpoint. Return to **IDLE** and stop. Do **not** resume
continuous polling.

END_TASK
