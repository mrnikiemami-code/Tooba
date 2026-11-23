# ISO-RULES — OpenCode acceptance method (always-on summary)
Source of truth: `docs/opencode/ARCHITECT-DELIVERY-METHOD.md` (**TOOBA-OPENCODE-DELIVERY-V1@1.3.0** for newly issued tasks)

MUST:
- OpenCode-only artifacts live under `docs/opencode/` — do not rewrite Cursor delivery gates or `docs/ai/delivery/**` to satisfy OpenCode.
- Follow issued TASK.md + ACCEPTANCE-MATRIX.json exactly; do not reinterpret or drop REQs.
- Evidence must be independent/re-readable; HTTP200 / modal-open / helper-only is insufficient for persistence claims.
- Impeccable: for real UI design/form/a11y/interaction changes, document critique → choices → implementation → post-check. No UI change ⇒ `NOT_APPLICABLE: NO_UI_CHANGE`.
- Preserve Bridge envelope keys and `STOP_FOR_ARCHITECT`; `automaticNextImplementationTask: NONE` unless the issued task says otherwise.
- One bounded post-matrix repair round only when the issued task allows it; focused implementation/fixture debugging before the first full matrix is not an infinite repair loop.
- Blocker handling: detect material progress-blocking failures early, notify user once in a short actionable line, autonomously fix authorized in-scope problems (including disposable auth fixtures), then resume the original tests. Never ask for approval already granted by TASK.
- If resolution requires new authority, unsafe live changes, or repair budget is exhausted: record exact evidence and immediately return BLOCKED/STOP_FOR_ARCHITECT. Never silently idle, misrepresent activity, repeatedly retry identical failures, or auto-issue follow-up tasks.
- Keep chat concise; the one-time blocker notice is an exception to the no-progress-message rule in `.opencode/instructions/concise-chat-output.md`.

When analyzing: do not claim ARCHITECT_ACCEPT.
