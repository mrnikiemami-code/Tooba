# ISO-RULES — OpenCode acceptance method (always-on summary)
Source of truth: `docs/opencode/ARCHITECT-DELIVERY-METHOD.md` (**TOOBA-OPENCODE-DELIVERY-V1@1.3.0** for newly issued tasks)

MUST:
- OpenCode-only artifacts live under `docs/opencode/` — do not rewrite Cursor delivery gates or `docs/ai/delivery/**` to satisfy OpenCode.
- Follow issued TASK.md + ACCEPTANCE-MATRIX.json exactly; do not reinterpret or drop REQs.
- Evidence must be independent/re-readable; HTTP200 / modal-open / helper-only is insufficient for persistence claims.
- Impeccable: for real UI design/form/a11y/interaction changes, document critique → choices → implementation → post-check. No UI change ⇒ `NOT_APPLICABLE: NO_UI_CHANGE`.
- Preserve Bridge envelope keys and `STOP_FOR_ARCHITECT`; `automaticNextImplementationTask: NONE` unless the issued task says otherwise.
- One bounded repair round only when the issued task allows it.

When analyzing: do not claim ARCHITECT_ACCEPT.
