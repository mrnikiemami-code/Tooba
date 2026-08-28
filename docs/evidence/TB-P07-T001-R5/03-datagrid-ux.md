# TB-P07-T001-R5 — DataGrid UX polish

## Scope
UI/UX only under `src/frontend/design-system/data-grid/` (+ shared `Drawer` in `primitives/overlays.tsx`).

## Changes

### 1. Column manager — drag reorder
- Drawer rows are draggable with a **⋮⋮** handle (`aria-label` / title از `dragColumn`).
- ↑↓ remain as optional secondary controls with FA aria-labels (`moveColumnUp` / `moveColumnDown`).
- Dense row layout (`gap-1.5`, compact padding) instead of sparse stacks.

### 2. Applied filter chips — FA operators
- Chip text uses `faFilterOperatorLabels` (شامل / برابر با / بیشتر از / …).
- Examples: `عنوان: شامل foo`, `مبلغ: بین 1–2`, `وضعیت: برابر با منتشرشده`.
- Clear chip aria uses FA header + `clearFilter`, not raw column id.

### 3. Saved views
- Default name: **نمای ذخیره‌شده** (`defaultViewName` / placeholder).
- Chip-style list with **active** (`aria-pressed`, primary fill) vs idle border.
- Per-view **×** delete (`SavedViewStore.remove`) with FA aria-label.
- Editing filters/columns/sort clears active selection so state stays honest.

### 4. Selection column
- Fixed width **44px** (`SELECTION_COLUMN_WIDTH`).
- Sticky data columns use the same offset (`insetInlineStart: 44`) for consistent RTL/LTR alignment.

### 5. Filter / column drawers
- Light open transition: opacity + short slide on mount (`Drawer`).
- Narrower drawer (`22rem`), tighter padding (`p-3`), denser inner gaps (`gap-2` / `gap-1.5`).

### 6. Aria / messages FA
New `GridMessages` keys (FA + EN catalogs): `deleteView`, `defaultViewName`, `moveColumnUp/Down`, `dragColumn`, `resizeColumn`, `selectRow`, `clearFilter`.
FilterControl operator aria-labels no longer use English `operator` / `to`.

## Files
- `DataGrid.tsx`, `messages.ts`, `types.ts`, `FilterControl.tsx`, `index.ts`
- `src/frontend/design-system/primitives/overlays.tsx` (Drawer transition + density)
