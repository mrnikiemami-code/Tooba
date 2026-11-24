# RESULT — TB-OPENCODE-EXECUTION-PERSISTENCE-290

Status: **PASS**

Root cause: instruction-induced early stop — the "concise + STOP_FOR_ARCHITECT final block" framing plus
token-thrift wording made the worker treat "emit STOP_FOR_ARCHITECT/≤N lines" as "stop the turn", while no
step/token limit actually exists in `.opencode/opencode.json` or the global config. `.opencode/AGENTS.md` was also
0 bytes (canonical loader missing).

Fix (OpenCode-specific only):
- `.opencode/instructions/concise-chat-output.md` → new mandatory "Execution persistence" section.
- `.opencode/AGENTS.md` → filled with canonical loader + "Execution persistence (do not self-stop)".
- No change to model, permissions, root `AGENTS.md`, or architecture/security/design/validation standards.

Multi-step proof (this task, one turn, stages executed without stopping): read repo+global config → captured
evidence (no step limit; 0-byte AGENTS.md; instruction quotes) → edited concise-chat-output → wrote `.opencode/AGENTS.md`
→ wrote task evidence → validated JSON + greps + ran an OpenSpec strict validate → committed/pushed.

Expected behavior after fix: a long task proceeds through internal stages; STOP_FOR_ARCHITECT is emitted only when
gates are run to PASS/FAIL/NOT_RUN with evidence or a concrete blocker is proven. Goal: low-consumption, NOT low-work.
