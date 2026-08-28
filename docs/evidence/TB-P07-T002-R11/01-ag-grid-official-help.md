# TB-P07-T002-R11 — AG Grid v36.1.0 Official Documentation Audit

**Audited before implementation:** 2026-08-28  
**Installed version:** `ag-grid-community@36.1.0`, `ag-grid-react@36.1.0`  
**Theme mode:** `theme="legacy"` + `ag-theme-quartz` + `.ag-theme-tooba` parameter overrides

## Official docs/pages inspected

| Page | URL | Purpose |
|------|-----|---------|
| Legacy theming overview | https://www.ag-grid.com/react-data-grid/theming-v32/ | Confirm legacy CSS variable approach for v36 |
| Theme customization | https://www.ag-grid.com/react-data-grid/theming-v32-customisation/ | Parameter override on theme class |
| Theme variables list | https://www.ag-grid.com/react-data-grid/theming-v32-customisation-variables/ | Supported `--ag-*` tokens |
| Header customization | https://www.ag-grid.com/react-data-grid/theming-v32-customisation-headers/ | Header bg, separators, resize handles |
| Border customization | https://www.ag-grid.com/javascript-data-grid/theming-v32-customisation-borders/ | Row/cell/grid borders |
| Column state API | https://www.ag-grid.com/react-data-grid/column-state/ | Column visibility/order persistence |
| Selection (row) | https://www.ag-grid.com/react-data-grid/row-selection/ | `selectionColumnDef`, localized header |
| RTL | https://www.ag-grid.com/react-data-grid/rtl/ | `enableRtl` — already used |
| Popup parent | https://www.ag-grid.com/react-data-grid/context-menu/ | `popupParent` for menus outside grid clip |

## Official variables/APIs used in Tooba (R11)

### Header (on `.ag-theme-tooba`)
- `--ag-header-background-color`
- `--ag-header-foreground-color`
- `--ag-header-height`
- `--ag-header-cell-hover-background-color`
- `--ag-header-cell-moving-background-color`
- `--ag-header-column-separator-display`
- `--ag-header-column-separator-height`
- `--ag-header-column-separator-width`
- `--ag-header-column-separator-color`
- `--ag-header-column-resize-handle-display`
- `--ag-header-column-resize-handle-height`
- `--ag-header-column-resize-handle-width`
- `--ag-header-column-resize-handle-color`

### Borders / grid lines
- `--ag-borders`
- `--ag-border-color`
- `--ag-borders-critical`
- `--ag-critical-border-color`
- `--ag-borders-secondary`
- `--ag-secondary-border-color`
- `--ag-row-border-style`
- `--ag-row-border-width`
- `--ag-row-border-color`
- `--ag-cell-horizontal-border`

### Filters / popups (opaque surfaces)
- `--ag-menu-background-color`
- `--ag-popup-shadow`
- Narrow fallback: `.ag-theme-tooba .ag-filter { background-color: … }` (color only)

### Row selection
- `selectionColumnDef.headerName` — fa: انتخاب / en: Selection
- `selectionColumnDef.lockVisible: true`

### Column state
- `api.getColumnState()` / `api.applyColumnState()`
- `onColumnVisible` / `onColumnMoved` events for drawer sync

## Forbidden structural hacks (removed / not added)

- `position` / `z-index` / `transform` on `.ag-header*`, `.ag-body*`, `.ag-row`, `.ag-cell`
- `display: flex` on `.ag-cell` or `.ag-cell-wrapper`
- `isolation: isolate` on grid viewport
- Custom sticky header outside AG Grid
- Manual header/body horizontal scroll sync
- Translucent `rgba(..., <1)` on `.ag-filter` / `.ag-menu` content surfaces

## Tooba application-owned layers (outside AG internals)

- App header filters: `AppColumnHeader` + `ColumnFilterPopover` (Portal)
- Advanced filter: `AdvancedFilterDrawer` (Portal)
- Cell alignment: `.app-grid-cell-content` wrapper inside renderers
