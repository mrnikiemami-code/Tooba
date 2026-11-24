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

## Execution persistence (mandatory — TB-OPENCODE-EXECUTION-PERSISTENCE-290)
- "Concise" governs chat TEXT only. It MUST NOT reduce tool calls, tests, build steps, internal stages,
  OpenSpec/Impeccable/acceptance work, or evidence collection.
- Do NOT end the turn after partial progress. Continue through as many internal stages as needed
  (inspect → implement → build/test → connected verify → restore → record) until the issued acceptance
  matrix is ACTUALLY executed, or all safe independent authorized work has been exhausted and a real technical blocker is proven. A missing contract in one tab never alone justifies a task-wide stop.
- `STOP_FOR_ARCHITECT` is a FIELD of the final block, not permission to stop. Emit the final block ONLY when
  the issued work is genuinely finished (every required gate run to PASS/FAIL/NOT_RUN with evidence) or
  blocked by a concrete, evidenced technical/environment/tooling blocker **and no safe independent authorized work remains**, per delivery method §0A.
- Phrases like "budget", "token thrift", "≤8 lines", "no repeated analysis", "don't stop", or
  "STOP_FOR_ARCHITECT" in the task text constrain COST/VERBOSITY only — never completion.
- A blocked stop MUST cite the exact command/error/schema/tooling that is missing. Unmeasured "budget/credit"
  is NOT an acceptable blocker. A long task is allowed to span many internal stages within one turn.
