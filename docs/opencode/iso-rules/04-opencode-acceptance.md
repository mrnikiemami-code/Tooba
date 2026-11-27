# ISO-RULES — OpenCode acceptance method (always-on summary)
Source of truth: `docs/opencode/ARCHITECT-DELIVERY-METHOD.md` (**TOOBA-OPENCODE-DELIVERY-V1@1.5.3** for newly issued tasks)

MUST:
- OpenCode-only artifacts live under `docs/opencode/` — do not rewrite Cursor delivery gates or `docs/ai/delivery/**` to satisfy OpenCode.
- Follow issued TASK.md + ACCEPTANCE-MATRIX.json exactly; do not reinterpret or drop REQs.
- Evidence must be independent/re-readable; HTTP200 / modal-open / helper-only is insufficient for persistence claims.
- Impeccable: for real UI design/form/a11y/interaction changes, document critique → choices → implementation → post-check. No UI change ⇒ `NOT_APPLICABLE: NO_UI_CHANGE`.
- Preserve Bridge envelope keys and `STOP_FOR_ARCHITECT`; `automaticNextImplementationTask: NONE` unless the issued task says otherwise.
- One bounded repair round only when the issued task allows it.

## Mandatory execution continuity (owner-approved; delivery method §0A)
- Scope every blocker to its affected requirement. Continue all safe independent authorized slices and tests before stopping the task.
- Missing API/DTO/endpoint is an implementation gap: for authorized full-stack work with decided behavior, implement the real owning-module contract; never fabricate UI/API results or invent policy. Preserve security, schema/live-write authorization and architecture boundaries.
- Fix in-scope build/test failures during initial development; the one correction limit begins only after the first complete acceptance matrix. Volume, token thrift and intermediate milestones are not blockers.
- STOP_FOR_ARCHITECT is terminal only after completion or exhaustion of safe authorized work, or a proven global safety blocker. Document exact evidence, remaining affected requirements and why no independent work remains.
- Applies to ongoing and future OpenCode tasks; do not bypass genuine safety gates or auto-start a new task.

When analyzing: do not claim ARCHITECT_ACCEPT.

## Mandatory senior architect quality gate (owner directive 2026-11-25)
Apply `docs/opencode/ARCHITECT-QUALITY-GATE.md` at pre-issue and independent review. All eight applicable AQ checks require source and guard evidence. Functional PASS never waives architectural violations. Preserve immutable task inputs, authorization and §0A; no automatic next task.

## Owner-approved DEV authority and stop gate (v1.4.0)
Read `docs/opencode/DEV-HOST-TEST-POLICY.md` and delivery method §0B. Confirmed existing DEV migrations, test deletion/reset and same Host restart are already authorized within task scope; no fresh Host/DB/Testcontainers or repeat permission. A missing authorized contract or failed initial test is implementation work. Isolate actual undecided policy, continue independent work, and document exhausted safe slices before terminal BLOCKED. No architecture/security/acceptance waiver.

## Mandatory execution controls (R10 root-cause prevention)
- Before product edits read actual mandatory files, task/owner addenda, relevant architecture locks and OpenSpec proposal/design/spec/tasks. Record a task-local read receipt (paths, content hashes, observed constraints, conflicts); hashes prove version, not comprehension. Record per-finding plan with owning boundary, explicit policy authority, test oracle and completion status; no schema/eligibility defaults without policy evidence. Unknown owner must not silently grant access.
- Before implementation prepare exact matrix-ID runner and architect-reviewed per-ID assertion plan. Use `node docs/opencode/test-infra/acceptance-gate.cjs --self-test` and at final gate `node docs/opencode/test-infra/acceptance-gate.cjs MATRIX PLAN RUN EVIDENCE_ROOT`. See adjacent ACCEPTANCE-GATE.md. Related/descriptive evidence cannot substitute for executing each issued scenario. Structural gate never replaces source/semantic review. Required test project build must succeed on exact candidate before tests; no stale --no-build acceptance. Link actual build log and hashes, independently verify served build.
- Prioritize highest-risk owning contracts and test-oracle implementation first, then UI and full connected matrix. Context pressure: persist concise task-local checkpoint with completed evidence, unresolved findings, next safe action and exact source SHA; resume same task from checkpoint on context reset/continuation. Do not manufacture BLOCKED or launch another task for context pressure. Terminal BLOCKED remains valid for evidenced safety/policy/repair-budget limits, with independent safe work exhausted and exact authority recorded. No infinite retries or automatic task creation.
- For concurrent tests bind response to original request ID and assert durable winner equals successful request; losing writes and side effects must be absent. For cross-owner workflows inject failure between owner commit and projection; verify reliable owner-local record and idempotent recovery, never cross-context ACID. For shared localization verify central provider, actual DB metadata, shell/grid/API locale, invalid and disabled prefix.

## Mandatory architect-owned correction and acceptance gate (owner directive v1.5.0)
Read the current `docs/opencode/ARCHITECT-DELIVERY-METHOD.md` and `docs/opencode/ARCHITECT-QUALITY-GATE.md` on every handoff/recovery. After STOP_FOR_ARCHITECT, the architect owns substantive product and test/guard corrections; OpenCode validates exact commits and makes only the narrow mechanical compile fixes permitted by v1.5.2, with separate commit and evidence. Before acceptance or next dependent goal require architect-authored `ARCHITECT-REVIEW.md`, `ARCHITECT-REVIEW.json` and successful `docs/opencode/test-infra/architect-review-gate.cjs` receipt, bound to actual Git diff, all eight AQ checks, immutable mandatory tests and raw evidence. No worker self-approval, no unchecked applicable gate, no repeated repair tickets. See `docs/opencode/test-infra/ARCHITECT-REVIEW-GATE.md`. Preserve main/Cursor and immutable task inputs.

## Scoped test scheduling clarification (delivery v1.5.1)
For TB-296 P1-A/B/C only, follow the owner-authorized phase development release and deferred test ledger in `docs/opencode/tasks/TB-296-R10-OFFER-PRODUCTION-READINESS/ARCHITECT-PHASE-TEST-SCHEDULING.md`. This narrow exception permits isolated B development before final predecessor ACCEPT, and isolated C evidence assembly. It does not waive independent source review, focused checks, architecture requirements, the full original matrices, or the final machine-checked architect acceptance gate. All other tasks retain the existing hard gate.

## Mechanical compile fixes (delivery v1.5.2)
Follow the owner-authorized mechanical-correction rule in `docs/opencode/ARCHITECT-DELIVERY-METHOD.md`. OpenCode fixes obvious local syntax and unambiguous mechanical compile errors during the same validation task without stopping for architect approval. No semantic, architecture, authorization, localization-authority, test/guard/baseline or contract edits. Record exact diff and rerun affected checks; independent architect acceptance remains mandatory. This overrides earlier blanket validation-only edit wording only within that narrow scope.

## Low-risk local corrections (v1.5.3)
Follow the current delivery method: worker fixes mechanical and unambiguous local presentation/test-harness defects in the same goal. Architect retains substantive result semantics, data/security, domain, central locale and architecture repairs. No weakening assertions, bypassing dialogs, unsafe fixture cleanup or stopping independent safe cases. Exact separate correction/evidence commits required.
