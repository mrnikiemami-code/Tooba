# TOOBA OpenCode isolated AGENTS (canonical, versioned in `.opencode/`)

Short always-on loader for OpenCode workers, restored from `.opencode/` on `main`.
Full historical `AGENTS.md` remains at the repository root on `main` only.
This worktree is an **execution environment**, not a separate product architecture.

## Always-on (read with the read tool before other work)
1. `.opencode/iso-rules/01-architecture.md`
2. `.opencode/iso-rules/02-security-permissions.md`
3. `.opencode/iso-rules/03-openspec.md`
4. `.opencode/iso-rules/04-opencode-acceptance.md`
5. `.opencode/iso-rules/05-task-layer.md`

## Task-type packs (read only when the issued task matches)
- Backend feature/fix → `.opencode/iso-rules/06-backend.md` then `.cursor/skills/tooba-backend-feature/SKILL.md`
- Frontend feature/fix → `.opencode/iso-rules/07-frontend.md` then `.cursor/skills/tooba-frontend-feature/SKILL.md`
- Explicit AMSC / module completion → `.opencode/iso-rules/08-amsc.md` (never auto-start AMSC)

## Non-negotiables
- Exact issued REQs cannot be dropped; Worker PASS ≠ Architect ACCEPT ≠ owner acceptance.
- Persistence claims need owning command + independent reread.
- No migrations/live canonical writes unless the issued task authorizes them.
- Do not rewrite main-tree `AGENTS.md`, Host, or invent parallel architecture.
- New OpenCode tasks use `TOOBA-OPENCODE-DELIVERY-V1@1.3.0` and end with `STOP_FOR_ARCHITECT` when required.

## Out of always-on scope
Do not auto-load historical story dumps or unrelated past tasks.
