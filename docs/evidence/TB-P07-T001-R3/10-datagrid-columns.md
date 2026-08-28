# TB-P07-T001-R3 — DataGrid columns

## Capabilities (shared foundation)
Already present in `DataGrid` column manager drawer:
- show/hide (`hideable`)
- drag reorder + Alt+Arrow keyboard reorder
- resize handles
- restore defaults (`restoreColumns`)
- sticky start/end columns (`stickyLogicalSide` RTL-aware)
- compact selection checkbox column

## Product grid sticky
- Title/thumbnail column: `sticky: "start"`
- Actions menu: `sticky: "end"`

## Order grid
- Reference column sticky start
- Actions (مشاهده) column added

## Files
- `src/frontend/design-system/data-grid/DataGrid.tsx`
- `src/frontend/design-system/data-grid/serialize.ts`
- `src/frontend/app/admin/product-list.tsx`
- `src/frontend/app/admin/admin-screens.tsx`
