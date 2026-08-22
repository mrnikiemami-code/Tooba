BEGIN_TOOBA_CURSOR_GATE_V1

Protocol-Version:
1

Task-ID:
TB-PXX-GATE

Phase:
PXX — <Phase Name>

Objective:
Perform phase gate review only.

Required Reading:
- ...

Gate Checks:
- architecture
- product
- implementation
- tests
- evidence
- SoT
- known limitations
- next phase recommendation

No feature implementation unless explicitly authorized.

Validation:
- git diff --check
- relevant tests/build if needed
- HEAD == origin/main after push
- Working Tree status

Deliverable:
Create a human-readable gate review Markdown under:
docs/evidence/<Task-ID>/GATE-REVIEW.md

Result Contract:

BEGIN_TOOBA_CURSOR_RESULT_V1

Protocol-Version:
1

Task-ID:
TB-PXX-GATE

Status:
PASS / PASS WITH KNOWN LIMITATIONS / FAIL / BLOCKED

Include:
- gate verdict
- assessments
- known limitations
- next recommendation
- evidence path
- commit
- HEAD == origin/main
- Working Tree

Next-State:
AWAITING_ARCHITECT_REVIEW

END_TOOBA_CURSOR_RESULT_V1

Pipeline Continuity:
- remain PIPELINE
- WAITING after RESULT
- no invented next work

END_TOOBA_CURSOR_GATE_V1
