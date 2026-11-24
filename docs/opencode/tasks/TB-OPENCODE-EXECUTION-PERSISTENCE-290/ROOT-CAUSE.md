# ROOT-CAUSE — TB-OPENCODE-EXECUTION-PERSISTENCE-290

## Symptom
The OpenCode worker voluntarily ended turns before completing long tasks, emitting a "≤N-line STOP_FOR_ARCHITECT"
report while acceptance gates were still NOT_RUN (observed across TB-287/288/289 turns).

## What is NOT the cause (measured)
- `.opencode/opencode.json`: no `maxSteps`/step limit; only `tool_output {max_lines:200,max_bytes:20000}` (truncation),
  `compaction {auto:true,prune:true}` (context housekeeping), `permission {edit:allow,webfetch:deny}`, model
  `metisdeepseek/deepseek-v4-flash`.
- Global `~/.config/opencode/opencode.jsonc`: only provider definitions + default model; **no step/turn limit**.
- => Early stopping is NOT enforced by a system limit, context cap, or explicit token/step ceiling.

## Root cause (instruction-induced)
1. `.opencode/instructions/concise-chat-output.md` mandated a final block shaped `STATUS … STOP_FOR_ARCHITECT` and
   framed "when the issued work is finished (or blocked)" — but the worker collapsed "emit STOP_FOR_ARCHITECT" with
   "stop the turn now", ending after partial progress.
2. The loader/root `AGENTS.md` and every task prompt end with `STOP_FOR_ARCHITECT` and "no automatic next task",
   reinforcing turn-finality.
3. Strong token-thrift framing ("Concise…", base "minimize output tokens", task "≤8 lines / no repeated analysis")
   biased the agent toward cost over completion — the "low-work" failure: low-consumption instead of low-work.
4. No execution-persistence rule existed to distinguish "STOP_FOR_ARCHITECT = end-of-task reporting field" from
   "permission to stop", and "blocked" was never required to cite a concrete evidenced cause.
5. `.opencode/AGENTS.md` was **0 bytes** — the canonical portable loader was absent, so behavior leaned on the
   root loader + stop-heavy task text.

## Fix (OpenCode-specific infra only; standards unchanged)
- `.opencode/instructions/concise-chat-output.md`: added "Execution persistence (mandatory)": concise = chat TEXT only;
  never reduce tools/tests/stages; STOP_FOR_ARCHITECT is an end-of-task field; continue until the matrix is executed
  or a concrete evidenced blocker; unmeasured "budget" is not a blocker.
- `.opencode/AGENTS.md`: filled (was empty) with the canonical loader + "Execution persistence (do not self-stop)".
- Root `AGENTS.md`, model config, permissions, architecture/security/design/validation standards: unchanged.
