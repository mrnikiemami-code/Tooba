# ISO-RULES — OpenCode acceptance method (always-on summary)
Source of truth: `docs/opencode/ARCHITECT-DELIVERY-METHOD.md` (**TOOBA-OPENCODE-DELIVERY-V1@1.4.0** for newly issued tasks)

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
