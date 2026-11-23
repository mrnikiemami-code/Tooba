# Concise OpenCode chat output (TOOBA-OPENCODE-CONCISE-OUTPUT-006)

These rules govern **chat messages only**. They do not reduce tool use, tests, architecture depth, or evidence quality.

## Chat behavior
- Do **not** narrate progress (“I’ll start by…”, “Next I’ll…”, “Now reading…”) in the chat.
- Do **not** paste, paraphrase, or rewrite RESULT/RECEIPTS/test logs/evidence bodies into the chat.
- Do **not** send mid-task status essays. Work silently with tools; write durable proof into the task’s own files under `docs/opencode/tasks/<Task-ID>/` (or the paths named by the issued TASK).
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
