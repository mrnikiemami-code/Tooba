# Impeccable R8 — Offer Workspace

Task: TB-296-R8-OWNER-UX-I18N-DATA-INTEGRITY-REPAIR
Authority: approved 295 / R7 foundation (no theme redesign, no parallel form).

## Before (R7, owner-rejected)

- Owner score context: R7 visual/UX was rejected with specific findings (picker height/whitespace, product vs variant confusion, header imbalance, mixed Persian/English numbers, small/low-contrast text, hardcoded channel).
- Independent R7 Impeccable baseline: ~76/100, PASS_WITH_FINDINGS.
- Detector then reported no source findings, but R7 still failed owner review; detector was never treated as proof.

## After (R8)

- Independent review estimate: ~86/100, PASS_WITH_FINDINGS.
- Resolved:
  - Seller/product/variant pickers are modal, searched, paged, closable; no inline height explosion.
  - Product and variant are independent; human Catalog labels are primary; Code is secondary.
  - Header uses a container query (3-col + price row below 1180px content width; 4-col above) so the panel sidebar no longer squeezes it.
  - Field hints/eyebrows/footer are 12–13px; footer contrast uses a darker token; tab numbers/counters/prices use the active formatter.
  - Real channel, human variant label in both lists, 64px rows, product first, actions last, image `object-fit: cover`.
  - fa/en runtime, third-language fixture, direction/calendar/numbers/currency from active locale.
  - Amount input formats grouping and normalizes digits while preserving typing/paste/cursor.
- Residual findings:
  - One advisory `straggler` in the workspace card header (last child alignment); not a functional blocker.
  - Admin shell/sidebar and some unrelated screens still have 10px/11px text; outside the Offer workspace and shared-shell scope.
  - Owner visual acceptance of tabs beyond the reported defects remains external.
- Detector:
  - Source scan `src/frontend/lib/offer-workspace`: `[]` (exit 0).
  - Live URL scan `/fa/admin/offers/new`: 12 findings total; most are admin shell/sidebar selectors outside `offer-workspace`. Workspace-specific: one advisory straggler. Tiny-text findings from the first R8 scan were fixed by increasing hint/footer text.
  - Evidence: `evidence/r8-runtime/impeccable-detect-url.json`.

## Evidence

- 360/768/1440: `evidence/r8-runtime/admin-edit-360.png`, `admin-edit-768.png`, `admin-edit-1440.png`
- English edit: `admin-edit-en-1440.png`
- Create: `admin-create-1440.png`
- Lists: `admin-list-1440.png`
- Duplicate: `seller-duplicate-1440.png`
- Seller edit: `seller-edit-1440.png`
