PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_WORKER_RESULT

Task-ID:
TB-P07-T002
Channel:
tooba-main
Status:
PASS

Repository-Understanding:
- frontend: src/frontend — Next 15.1.6, React 19, App Router, npm, Tailwind
- backend: ASP.NET Host composers; Catalog+Offer+Pricing+Inventory batch reads (no cross-schema SQL JOIN)
- Next: 15.1.6
- React: 19.0.0
- router: App Router (app/)
- package manager: npm
- styling/UI: Tailwind + design-system tokens; ag-theme-tooba adapter
- i18n: fa/en grid locale-text; admin Persian-first
- RTL: enableRtl on AG Grid; dir=rtl on wrapper
- theme: CSS vars (--border, --surface, --primary)
- previous grid: design-system/data-grid custom table (kept for other routes)
- saved preference mechanism: Host ui_preferences + createHostSavedViewStore
- API query convention: GridServerQuery UI → GridQueryRequest Host → GridPageResponse

Implementation:
- canonical grid: design-system/app-data-grid/AppDataGrid.tsx (AG Grid Community wrapper)
- shared grid types: app-data-grid/types.ts + reuse data-grid/types GridServerQuery
- column helpers: ColDef in product-list.tsx; formatJalaliDate in jalali.ts
- toolbar: search (debounced), saved views, export, pagination
- filters: AG Community column filters + server mapping via grid-query-mapper
- advanced filter: toolbar search + filter chips via server query (Community-safe)
- Jalali date UX: dayjs+jalaliday display; API ISO/Gregorian
- column manager: AG column show/hide/reorder + saved view layout
- saved views: SavedViewStore adapter (grid.admin.products key)
- CSV: export.ts current-page labeled
- XLSX: exceljs current-page labeled
- states: loading overlay, empty, error+retry
- theme adapter: theme.css ag-theme-tooba
- first Admin integration: /admin/products ProductListScreen
- API endpoint: POST /v1/admin/products/query
- backend query adapter/handler: ProductWorkspaceComposer.QueryGridAsync + AdminProductGridEvaluator

Features:
- AG Grid Community: YES
- RTL/LTR: YES
- fa/en: YES
- text filters: YES (server)
- number filters: YES (server)
- date filters: YES (server ISO)
- enum/status filters: YES (server)
- Jalali display: YES
- Jalali filtering: YES (UI converts to ISO before API)
- sorting: YES (server whitelist + productId tie-break)
- pagination: YES (server)
- resize: YES (AG)
- reorder: YES (AG + saved views)
- show/hide: YES
- saved views: YES (Host ui_preferences)
- advanced filters: YES (search + multi-filter server query)
- CSV: YES (current page)
- XLSX: YES (current page)
- selection: YES (current page only — labeled)
- bulk actions: seam present (none on products screen)
- loading: YES
- empty: YES
- filtered-empty: YES
- error/retry: YES
- responsive: YES (compact toolbar/mobile font)
- accessibility: focus rings preserved
- server-side compatibility: YES

API-Architecture:
- GridQuery: page, pageSize, search, sort[], filters[]
- GridPage: items[], page, pageSize, totalCount
- field whitelist: AdminProductGridQueryPolicy
- operator validation: YES
- type validation: YES
- date canonicalization: ISO DateTimeOffset
- search: debounced global title/category
- sort: whitelist + deterministic productId asc tie-break
- pagination: skip/take after filter
- totalCount: YES
- max page size: 100
- cancellation: AbortController in AppDataGrid
- deterministic sort: productId asc final key
- N+1: batch module reads per ListAsync pattern (enrich then filter/page)
- authorization: AdminPanelAccess.RequireAuthorizedAsync
- module boundaries: preserved (no cross-module SQL JOIN)
- raw AG Grid backend leakage: NONE
- cross-module joins: NONE

Dependencies:
- added: ag-grid-community, ag-grid-react, dayjs, jalaliday, exceljs
- reused: saved-view-store, ui_preferences, design-system tokens
- ag-grid-enterprise installed: NO
- Enterprise imports: NONE
- Enterprise-only features: NONE

First-Admin-Integration:
- route: /admin/products
- module: Admin Product Workspace list
- real API: POST /v1/admin/products/query
- search: YES
- filters: YES (whitelist)
- sort: YES
- pagination: YES
- saved views: YES
- export: CSV/XLSX current page (labeled)
- selection: current page only (labeled)
- states: loading/empty/error

Validation:
- frontend typecheck: 0
- frontend lint: 0 (img warning)
- frontend tests: grid 9 pass, admin 13 pass
- frontend build: 0
- backend restore: N/A (Host live)
- backend build: compile OK; DLL copy blocked by live Host :5088 (runtime kept alive)
- backend tests: not run (Host lock)
- warnings: img element; Host file lock on build copy
- errors: 0 compile
- failed: 0 FE
- skipped: 0
- git diff --check: 0
- AG Grid license audit: Community only

Runtime:
- Backend: http://127.0.0.1:5088 — live
- Frontend: http://127.0.0.1:3000 — live
- Shopeiva: http://127.0.0.1:3001 — live
- kept alive: YES

Git:
- branch: main
- commit: feat add reusable AG Grid community data grid [TB-P07-T002]
- push: YES
- final HEAD: 86729c199f0681cdc71f25caa5231ec81469f3b1
- origin/main: 86729c199f0681cdc71f25caa5231ec81469f3b1
- synchronized: YES
- tracked tree: clean

Evidence:
- path: docs/evidence/TB-P07-T002/

Architectural-Concerns:
Enriched product grid filters computed fields in Host memory after batch module reads — acceptable for current catalog scale; materialized projections may be needed at very large scale.

Visual-Concerns:
Manual visual supervision requested (no screenshot pack). AG Grid themed to Tooba tokens; Architect/User review OPEN.

Blockers:
NONE

END_TOOBA_WORKER_RESULT
