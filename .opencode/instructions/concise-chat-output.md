# Concise OpenCode chat output (TOOBA-OPENCODE-CONCISE-OUTPUT-006)

These rules govern **chat messages only**. They do not reduce tool use, tests, architecture depth, or evidence quality.

## Chat behavior
- Do **not** narrate progress (“I’ll start by…”, “Next I’ll…”, “Now reading…”) in the chat.
- Do **not** paste, paraphrase, or rewrite RESULT/RECEIPTS/test logs/evidence bodies into the chat.
- Do **not** send mid-task status essays or routine progress narration. Work with tools; write durable proof into the task’s own files under `docs/opencode/tasks/<Task-ID>/` (or paths in the issued TASK).
- **Exception — immediate actionable blocker:** when a real blocker prevents further meaningful progress (for example all isolated test requests return 403), emit ONE concise notification as soon as it is detected: `BLOCKER <Task-ID>: <specific cause>; ACTION: <automatic bounded fix or architect decision needed>`. Never stay silently stuck until asked. This notification is not the final result.
- If the blocker is within the task's approved scope (including isolated test fixture setup), repair it autonomously and continue; do NOT request permission merely to fix an authorized fixture. Use the existing bounded-repair budget and real tests, not retry loops.
- If the only safe remedy requires an unapproved schema/public contract/architecture change, production data mutation, real authorization bypass, or exhausted repair budget, write exact evidence and STOP with BLOCKED/STOP_FOR_ARCHITECT. Do not wait for a user question, invent progress, keep polling, or generate a new task.
- A tool or long-running test that is genuinely making progress is not itself a blocker. Avoid heartbeat spam; notify only on a real decision point.
- Keep full reports, test outcomes, errors, and evidence **only** in those task files.

## Final chat message (mandatory shape)
When the issued work is finished (or blocked), the **entire** final chat message MUST be exactly:

```text
TASK: <ID>
STATUS: PASS / BLOCKED
RESULT: <one sentence>
BLOCKER: <none or one sentence>
CODE: <SHA>
EVIDENCE: <path and SHA>
DURATION: <real elapsed>
STOP_FOR_ARCHITECT
```

No other sections, tables, or prose after that block unless the user explicitly asks for detail.

## Non-goals
- Do not skip required tools, OpenSpec, Impeccable, acceptance matrix, or persistence proofs.
- Do not change model, reduce agent steps, or weaken architecture/security rules.
- Do not edit main-tree `AGENTS.md` as a side effect of this instruction.
