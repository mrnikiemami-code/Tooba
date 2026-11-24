# RECEIPTS — TB-296 (gap analysis; no product code changed)
- Read orders grid: src/frontend/app/admin/admin-screens.tsx → local `ServerGridPage` (composes design-system DataGrid + toolbar/saved views/export/actions).
- Read admin offers: src/frontend/app/admin/offers/page.tsx → `DataGrid columns={columns} queryAdapter={adapter}`.
- Read seller offers: src/frontend/app/vendor-panel/products/page.tsx → `DataGrid columns queryAdapter` (server paging).
- Existing routes confirmed (list/new/[id]) for admin and vendor-panel.
- No frontend/backend/DB source modified; no build run; no tests run (not fabricated).
- Next stages: extract shared ServerGridPage (no copy) → offer columns/adapter → shared 7-tab workspace → domain wiring + gap handling → 30 real tests + regression + perf → build/run → Impeccable.
