# ISO-RULES — OpenCode acceptance method (always-on summary)
Source of truth: `docs/opencode/ARCHITECT-DELIVERY-METHOD.md` (TOOBA-OPENCODE-DELIVERY-V1)

MUST:
- OpenCode-only artifacts live under `docs/opencode/` — do not rewrite Cursor delivery gates or `docs/ai/delivery/**` to satisfy OpenCode.
- Follow issued TASK.md + ACCEPTANCE-MATRIX.json exactly when implementing/verifying OpenCode tasks.
- Evidence must be independent/re-readable; HTTP200 / modal-open / helper-only is insufficient for persistence claims.
- One bounded repair round only when the issued task allows it.

When analyzing: do not claim ARCHITECT_ACCEPT.
