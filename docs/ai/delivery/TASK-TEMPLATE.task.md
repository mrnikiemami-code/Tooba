PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: <existing-family-next-valid-Task-ID>
Parent-Task: <actual-parent-or-NONE>
Channel: tooba-main
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Title: <concrete outcome>
Mode: <actual authorized mode>
Expected-Base-SHA: <actual verified 40-hex SHA>
Issued-Date: <YYYY-MM-DD> (Asia/Tehran)
Stop-State: STOP_FOR_ARCHITECT_AND_USER_REVIEW
automaticNextImplementationTask: NONE

## Goal / authority

<Concrete outcome and bounded scope; current source-grounded defect and original requirements.>
Read docs/architecture/TOOBA-DELIVERY-ASSURANCE.md, AGENTS/applicable feature instructions and OPERATOR-UX-ACCEPTANCE.
Before production code changes, persist ARCHITECTURE-PLAN.md and DELIVERY.json; pass plan gate using exact issued TASK hash. Execute all internal waves in this SAME task. No analysis-only handoff or automatic next task. One worker; no delegation/screenshots/polling loops unless explicitly authorized.

## A — fixed requirements and acceptance

| REQ | Behaviour | Required scenarios / proof | Done criterion |
|---|---|---|---|
| REQ-01 | <user-visible effect> | <real component/owner/persistence/browser as applicable> | <measurable final behaviour> |

<Do not issue this template with placeholder rows. Name actual source ownership, APIs, state/permission/data contracts.>

## B — architecture and ordered execution

<Source map, invariants, dependencies, internal waves; each a small connected vertical slice. Plan first, then reproduce -> implement -> verify -> self-review -> continue. Scope cannot shrink to match tests. A real lock/scope/data blocker must be reported honestly.>

## C — scenarios and connected verification

<New/Edit/legacy route, save+reread, cancel/Escape/Back, permission/cross-scope, malformed/loss/exact retry, concurrency, current grid query, locale/device/media/public states as applicable.>
<Real owning methods/serializer/provider and real mounted UI are distinct evidence. Add browser proof for browser-dependent claims. Match effect to command, not merely HTTP200/id. No source scan as behaviour proof.>

## D — checks, evidence and served handoff

<Focused commands appropriate to changed source; no habitual whole-solution sweep.>
Pass review gate, include receipts and exact requirement ledger. Record code/evidence SHAs and matching final artifacts where required. Missing mandatory proof remains PARTIAL/BLOCKED; not PASS with a known-limit footnote.

## E — boundaries / repository recovery

<Canonical environment and data protection from actual task authority. No inferred seed/grant/migration/live fixture/reset/restore permission.>
Verify ancestry and incoming delta. Save exact TASK.md in docs/ai/tasks/<Task-ID>/TASK.md and docs/architecture/evidence/<Task-ID>/TASK.md with exact-byte hash/receipt. Preserve history and user work; no force/rebase/amend/reset/stash; no secrets/scratch/binaries staged. Normal commit/push/fetch parity. Update authorized CURRENT pointers truthfully.

## F — RESULT CONTRACT

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: <actual task ID>
Parent-Task: <actual parent task ID>
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: TOOBA
Mode: <actual mode>
Bridge-Task-Row: <actual row>
Status: <PASS or PARTIAL or BLOCKED or RECOVERY_CONFLICT>
Stop-State: STOP_FOR_ARCHITECT_AND_USER_REVIEW
Expected-Base-SHA: <issued SHA>
Base-SHA: <actual SHA>
Code-SHA: <actual or NONE>
Evidence-SHA: <actual or NONE>
automaticNextImplementationTask: NONE
Readiness: <READY_FOR_ARCHITECT_REVIEW or NOT_READY>
<REQ-by-REQ outcome; architecture/plan/waves; reproduction before and after; actual commands/provider/results; real remaining defects/evidence gaps/risks; data effects; served identity; final parity; short Persian owner handoff. No placeholder or template text transmitted. Worker PASS is not Architect ACCEPT or owner acceptance.>
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK
