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

Architect gate: no ACCEPT or consolidated repair without the independent source-grounded ARCHITECT-REVIEW.md receipt defined in ARCHITECT-QUALITY-GATE.md. Worker AQ report is not independent evidence.

## Mandatory architect-owned correction and acceptance gate (owner directive v1.5.0)
Read the current `docs/opencode/ARCHITECT-DELIVERY-METHOD.md` and `docs/opencode/ARCHITECT-QUALITY-GATE.md` on every handoff/recovery. After STOP_FOR_ARCHITECT, only the architect edits product or existing tests/guards; OpenCode validates exact commits with test-only harness/evidence. Before acceptance or next dependent goal require architect-authored `ARCHITECT-REVIEW.md`, `ARCHITECT-REVIEW.json` and successful `docs/opencode/test-infra/architect-review-gate.cjs` receipt, bound to actual Git diff, all eight AQ checks, immutable mandatory tests and raw evidence. No worker self-approval, no unchecked applicable gate, no repeated repair tickets. See `docs/opencode/test-infra/ARCHITECT-REVIEW-GATE.md`. Preserve main/Cursor and immutable task inputs.

## Scoped test scheduling clarification (delivery v1.5.1)
For TB-296 P1-A/B/C only, follow the owner-authorized phase development release and deferred test ledger in `docs/opencode/tasks/TB-296-R10-OFFER-PRODUCTION-READINESS/ARCHITECT-PHASE-TEST-SCHEDULING.md`. This narrow exception permits isolated B development before final predecessor ACCEPT, and isolated C evidence assembly. It does not waive independent source review, focused checks, architecture requirements, the full original matrices, or the final machine-checked architect acceptance gate. All other tasks retain the existing hard gate.
