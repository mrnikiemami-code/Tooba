# ISO-RULES — Frontend ordinary feature (task-type)
Read when the issued task changes `src/frontend/**`.

MUST read:
1. `.cursor/skills/tooba-frontend-feature/SKILL.md`
2. `docs/architecture/FRONTEND-ARCHITECTURE-STANDARD.md`
3. Existing route/page, feature owner, design-system usage, and typed API client for the surface

MUST:
- Preserve module ownership, FE-BOUNDARY / ARCH-FE-FREEZE locks, and Design System reuse (`design-system/`, AppDataGrid, forms, tokens).
- Registry languages (not hard-coded locale authority); correct RTL/LTR; accessible controls.
- Typed API adapters; no fake prices/stock; loading/empty/error handled.
- Impeccable critique/post-check when UI actually changes; else `NOT_APPLICABLE: NO_UI_CHANGE`.
- Focused tests / typecheck when feasible; visual storefront gates when those surfaces change.
- Do **not** invent a parallel shell, menu, or architecture track.

AMSC is not a frontend default. Stop for Architect if the fix requires unauthorized cross-boundary redesign.
