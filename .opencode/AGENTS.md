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

## Execution persistence (do not self-stop)
- Work through every required internal stage (inspect → implement → build/test → connected verify → restore →
  record) until the acceptance matrix is actually executed or a real, evidenced technical blocker exists.
- `STOP_FOR_ARCHITECT` is an END-OF-TASK reporting gate, NOT permission to end a turn mid-task. Emit it only
  after the issued gates are run to PASS/FAIL/NOT_RUN with evidence, or after a concrete blocker is proven.
- Concise/token-thrift/"≤N lines" rules constrain chat text and cost only — never tool execution or completion.
- Unmeasured "budget/credit" is not an acceptable blocker; cite the exact command/error/tooling that is missing.
- Goal: a LOW-CONSUMPTION agent, not a LOW-WORK agent.

## Out of always-on scope
Do not auto-load historical story dumps or unrelated past tasks.
## Delivery proof standard (permanent — TB-291)
Accepting any task requires MEASURED proof, never "home page 200" alone:
- source commit SHA + runtime FRONTEND build identity (.next/BUILD_ID) and reachability;
- BACKEND build identity (dll SHA) + /health + provenance (/__platform-commerce);
- DATABASE reachability + required migration column presence (front/back/db alignment);
- REAL access to each CHANGED page (expected heading text actually present).
Single source of truth: `docs/opencode/test-infra/DEV-TEST-ENV.json`.
One command: `scripts/dev/verify-dev-alignment.ps1` (returns non-zero on mismatch = auto-enforced).
Harnesses MUST read config/env + runtime discovery; NEVER hardcode old ports/dbs/actor ids.
