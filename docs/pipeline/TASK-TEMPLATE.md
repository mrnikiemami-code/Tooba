BEGIN_TOOBA_CURSOR_TASK_V1

Protocol-Version:
1

Task-ID:
TB-PXX-TXXX

Phase:
PXX — <Phase Name>

Objective:
<one focused objective>

Accepted Baseline:
- ...

Required Reading:
- AGENTS.md
- docs/PROJECT-STATE.md
- docs/ROADMAP.md
- docs/ai/TOOBA-RECOVERY-CONTEXT.md
- ...

Scope:
1. ...

Out of Scope:
- ...

Architecture / Product Guardrails:
- ...

Repository Safety:
Before work:
- git rev-parse --show-toplevel
- git fetch origin
- verify branch main
- verify HEAD == origin/main
- inspect working tree

Validation:
- relevant build/tests
- git diff --check
- visual self-review if UI
- HEAD == origin/main after push
- Working Tree status

Evidence:
- docs/evidence/<Task-ID>/...

SoT Sync:
Update as appropriate:
- docs/PROJECT-STATE.md
- docs/ROADMAP.md
- docs/ai/TOOBA-RECOVERY-CONTEXT.md

Result Contract:

BEGIN_TOOBA_CURSOR_RESULT_V1

Protocol-Version:
1

Task-ID:
TB-PXX-TXXX

Phase:
PXX — <Phase Name>

Status:
PASS / PARTIAL / BLOCKED / FAIL

Include:
- summary
- tests
- evidence
- changed files
- commit
- HEAD
- origin/main
- HEAD == origin/main
- Working Tree
- Architectural Concerns
- recommended next authorized task

Next-State:
AWAITING_ARCHITECT_REVIEW

END_TOOBA_CURSOR_RESULT_V1

Pipeline Continuity:
- remain in PIPELINE mode
- enter WAITING mode after RESULT
- do not invent/execute next task
- wait/check for next authorized Architect Markdown envelope

END_TOOBA_CURSOR_TASK_V1
