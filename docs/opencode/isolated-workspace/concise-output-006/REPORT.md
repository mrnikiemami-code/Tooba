# TOOBA-OPENCODE-CONCISE-OUTPUT-006

## Applied
- Portable project config: `.opencode/opencode.json` → `instructions: [".opencode/instructions/concise-chat-output.md"]`
- Same files mirrored on iso worktree `SarvNewVer-opencode-iso-002`
- Official OpenCode **1.19.6** (CLI + Desktop Product 1.19.6): schema has **`instructions[]`**, **no** `verbosity` / quiet / silent chat key — none invented
- Model unchanged; `agent.steps` not reduced; main `AGENTS.md` untouched

## Smoke
- exit 0; final chat **306 chars**; template keys present; narration hits **0**; tools 3; Desktop PIDs **7/7**
- Evidence summary: `docs/opencode/isolated-workspace/concise-output-006/smoke-result.json`

## Git
- Commit `.opencode/` + this evidence folder on `main` (recoverable on another machine)
- Iso local branch updated separately; not merged as AGENTS replacement
