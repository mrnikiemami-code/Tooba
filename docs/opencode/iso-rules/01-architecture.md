# ISO-RULES — Architecture (always-on summary)
Source of truth (read when planning/editing): `docs/architecture/TOOBA-DELIVERY-ASSURANCE.md`

MUST:
- One authorized task scope; do not drop issued REQ IDs.
- Before production edits: source-grounded ARCHITECTURE-PLAN + DELIVERY.json with real path UI→transport→auth→CQRS/domain→persistence→independent read.
- Small connected waves inside the same task; UI button without owning command is incomplete.
- Worker PASS ≠ Architect ACCEPT ≠ owner acceptance.
- No automatic next task; STOP_FOR_ARCHITECT when required.
- Do not invent new domain abstractions that replace existing Contracts/CQRS/Design System.
- This isolated worktree is an OpenCode **agent cwd** only — product authority stays the main Tooba repository and its Git rules.

When analyzing only (no edits): still name the module boundary and owning contract/path you observed.

## Mandatory senior architect quality gate (owner directive 2026-11-25)
Read and apply `docs/opencode/ARCHITECT-QUALITY-GATE.md` at pre-issue and independent review. All eight AQ checks are mandatory where applicable, with source/guard evidence and explicit outcomes. Functional PASS never waives architecture violations; no new debt or baseline manipulation, no feature-local locale workaround, no oversized-file growth. Existing immutable tasks and §0A scope/continuity/authorization remain intact. No automatic next task.

## Owner-approved DEV authority and stop gate (v1.4.0)
Read `docs/opencode/DEV-HOST-TEST-POLICY.md` and delivery method §0B. Confirmed existing DEV migrations, test deletion/reset and same Host restart are already authorized within task scope; no fresh Host/DB/Testcontainers or repeat permission. A missing authorized contract or failed initial test is implementation work. Isolate actual undecided policy, continue independent work, and document exhausted safe slices before terminal BLOCKED. No architecture/security/acceptance waiver.
