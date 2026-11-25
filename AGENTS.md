# TOOBA OpenCode isolated AGENTS (branch `opencode/iso-agents-002`)

Short always-on loader for OpenCode workers. Full historical `AGENTS.md` remains on `main` only.
This worktree is an **execution environment**, not a separate product architecture.

## Always-on (read with the read tool before other work)
0. `docs/opencode/DEV-HOST-TEST-POLICY.md` — standing owner authority, read FIRST; overrides older task/OpenSpec isolation and migration permission clauses for confirmed DEV.
1. `docs/opencode/iso-rules/01-architecture.md`
2. `docs/opencode/iso-rules/02-security-permissions.md`
3. `docs/opencode/iso-rules/03-openspec.md`
4. `docs/opencode/iso-rules/04-opencode-acceptance.md`
5. `docs/opencode/iso-rules/05-task-layer.md`

## Task-type packs (read only when the issued task matches)
- Backend feature/fix → `docs/opencode/iso-rules/06-backend-layer.md` then `.cursor/skills/tooba-backend-feature/SKILL.md`
- Frontend feature/fix → `docs/opencode/iso-rules/07-frontend-layer.md` then `.cursor/skills/tooba-frontend-feature/SKILL.md`
- Explicit AMSC / module completion → `docs/opencode/iso-rules/08-amsc-layer.md` (never auto-start AMSC)

## Non-negotiables
- Exact issued REQs cannot be dropped; Worker PASS ≠ Architect ACCEPT ≠ owner acceptance.
- Persistence claims need owning command + independent reread.
- Existing DEV Host 5088, Next 3000, tooba_alpha only: in-scope migrations, test deletion/reset, fixture writes and controlled restart already authorized. Verify target/build and restorable backup for destructive operations; no new Host/DB/Testcontainers or repeated approval. Production remains forbidden.
- Missing authorized contracts/endpoints, in-scope build/test failures and ordinary implementation gaps are work, not terminal blockers. Follow delivery method §0A; isolate genuine undecided policies and continue every independent safe slice.
- STOP_FOR_ARCHITECT is terminal, not an intermediate checkpoint. A task-wide BLOCKED requires exact evidence of a global blocker or exhausted independent work.
- Do not rewrite main-tree `AGENTS.md`, Host, or invent parallel architecture.
- New OpenCode tasks use `TOOBA-OPENCODE-DELIVERY-V1@1.4.0` and end with `STOP_FOR_ARCHITECT` when required.

## Out of always-on scope
Do not auto-load historical story dumps or unrelated past tasks.
