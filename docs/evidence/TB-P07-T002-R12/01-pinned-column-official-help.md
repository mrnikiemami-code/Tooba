# TB-P07-T002-R12 — AG Grid v36.1.0 Pinned Column Official Documentation Audit

**Audited before implementation:** 2026-08-28  
**Installed version:** `ag-grid-community@36.1.0`, `ag-grid-react@36.1.0`  
**Theme mode:** `theme="legacy"` + `ag-theme-quartz` + `.ag-theme-tooba` parameter overrides  
**Scope:** `/admin/products` — actions column pinned right (`colId: actions`)

## Official docs/pages inspected

| Page | URL | Purpose |
|------|-----|---------|
| Column pinning | https://www.ag-grid.com/javascript-data-grid/column-pinning/ | Pin API, pinned left/right behaviour, RTL |
| Legacy theming | https://www.ag-grid.com/react-data-grid/theming-v32-customisation/ | Parameter overrides on legacy theme class |
| Legacy colours | https://www.ag-grid.com/react-data-grid/theming-v32-customisation-colours/ | Background/surface tokens |
| Legacy headers | https://www.ag-grid.com/react-data-grid/theming-v32-customisation-headers/ | Header surface on pinned header rows |
| Legacy borders | https://www.ag-grid.com/javascript-data-grid/theming-v32-customisation-borders/ | `--ag-borders-critical` for pinned/center separation |

## Installed v36 runtime DOM/classes (from `ag-grid.css`)

AG Grid v36 row/header layout uses sticky cell containers (not legacy `.ag-pinned-right-cols-container`):

- `.ag-grid-pinned-right-cells` / `.ag-grid-pinned-left-cells` — sticky pinned regions (`z-index: 2`)
- `.ag-grid-scrolling-cells` — center scroll region (`z-index: 0`)
- Body pinned surfaces: `background-image: linear-gradient(var(--ag-data-background-color), …)` on pinned cell containers
- Header pinned surfaces: `background-image: linear-gradient(var(--ag-background-color), …)` on pinned header containers (must match opaque header)
- Pinned boundary (body cells): `.ag-cell-first-right-pinned` / `.ag-cell-last-left-pinned` use `border-left/right: var(--ag-borders-critical) var(--ag-border-color)`
- Pinned boundary (header): `.ag-header-row .ag-grid-pinned-right-cells .ag-grid-container-wrapper { border-left: var(--ag-borders-critical) … }`

## Official variables/APIs used in Tooba (R12)

### Theme root (`.ag-theme-tooba`)
- `--ag-data-background-color` — explicit opaque data surface for body pinned cells
- `--ag-background-color` / `--ag-header-background-color` — already set; header pinned must stay opaque
- `--ag-borders-critical` + `--ag-critical-border-color` — pinned vs scrollable column separator (official “critical borders”)
- `--ag-borders` + `--ag-border-color` — general grid borders (unchanged from R11)

### Column pinning API (unchanged)
- `pinned: "right"` on `actions` column in `product-list.tsx`
- `lockVisible: true` on actions column

### Narrow fallbacks allowed (color/background/border only)
- Reinforce opaque surfaces on `.ag-grid-pinned-*-cells` and pinned header/body cells
- No position/transform/z-index/scroll-sync changes

## Approach chosen

1. Set `--ag-data-background-color` explicitly to opaque `hsl(var(--surface-elevated))`.
2. Keep `--ag-borders-critical` enabled with subtle `--ag-critical-border-color`.
3. Add minimal fallbacks mirroring AG v36 selectors so pinned header uses `--ag-header-background-color` and body pinned cells use `--ag-data-background-color`.
4. Preserve AG native sticky pinning — no overlay duplicates.

## Legacy-theme limitation

New Theming API parameter `pinnedColumnBorder` is **not** used (grid stays on `theme="legacy"`). Legacy equivalent is `--ag-borders-critical` + pinned cell border rules in AG source.

## Forbidden

- Manual scroll sync, fake fixed overlays, duplicate action columns
- Geometry hacks: `position`, `transform`, `z-index` overrides on grid internals
- Translucent pinned backgrounds
