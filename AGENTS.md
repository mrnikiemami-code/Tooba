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
- New OpenCode tasks use `TOOBA-OPENCODE-DELIVERY-V1@1.5.1` and end with `STOP_FOR_ARCHITECT` when required.

## Out of always-on scope
Do not auto-load historical story dumps or unrelated past tasks.

## Mandatory architect-owned correction and acceptance gate (owner directive v1.5.0)
Read the current `docs/opencode/ARCHITECT-DELIVERY-METHOD.md` and `docs/opencode/ARCHITECT-QUALITY-GATE.md` on every handoff/recovery. After STOP_FOR_ARCHITECT, only the architect edits product or existing tests/guards; OpenCode validates exact commits with test-only harness/evidence. Before acceptance or next dependent goal require architect-authored `ARCHITECT-REVIEW.md`, `ARCHITECT-REVIEW.json` and successful `docs/opencode/test-infra/architect-review-gate.cjs` receipt, bound to actual Git diff, all eight AQ checks, immutable mandatory tests and raw evidence. No worker self-approval, no unchecked applicable gate, no repeated repair tickets. See `docs/opencode/test-infra/ARCHITECT-REVIEW-GATE.md`. Preserve main/Cursor and immutable task inputs.

## Scoped test scheduling clarification (delivery v1.5.1)
For TB-296 P1-A/B/C only, follow the owner-authorized phase development release and deferred test ledger in `docs/opencode/tasks/TB-296-R10-OFFER-PRODUCTION-READINESS/ARCHITECT-PHASE-TEST-SCHEDULING.md`. This narrow exception permits isolated B development before final predecessor ACCEPT, and isolated C evidence assembly. It does not waive independent source review, focused checks, architecture requirements, the full original matrices, or the final machine-checked architect acceptance gate. All other tasks retain the existing hard gate.
