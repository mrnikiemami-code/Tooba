# ISO-RULES — Security & permissions (always-on summary)
Sources: task TASK.md / ACCEPTANCE-MATRIX when present; product auth contracts under backend modules.

MUST:
- Forbidden by default unless task explicitly allows: migrations, seeds, live canonical business writes, grant/reset.
- Negative permission cases matter: missing permission / cross-shop / wrong actor must fail closed from API, not only UI hide.
- Idempotency: same key+same payload vs same key+changed intent are different invariants.
- Stale/late responses must not overwrite newer UI/server state.
- Secrets/tokens/keys: never print; never commit.
- Never rewrite the main-tree `AGENTS.md` or change Host/module boundaries as a side effect of OpenCode config.

When analyzing: call out authz boundary and any write-path risk even if not executing writes.
