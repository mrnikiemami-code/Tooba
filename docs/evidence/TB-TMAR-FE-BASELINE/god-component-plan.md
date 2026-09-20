# God-component / oversized plan — TB-TMAR-FE-BASELINE

Thresholds: WATCH>500, OVERSIZED>800, CRITICAL>1200 physical LOC.

## Counts

- CRITICAL: 10
- OVERSIZED: 16
- WATCH: 39

## Top critical / oversized (decomposition later — NOT this task)

| LOC | Path | Suggested seams (later) |
| --- | --- | --- |
| 2215 | `src/frontend/app/admin/category-admin-screen.tsx` | extract panels/hooks/API; keep route thin |
| 1570 | `src/frontend/app/admin/product-workspace-screen.tsx` | extract panels/hooks/API; keep route thin |
| 1528 | `src/frontend/app/admin/content-article-admin-screen.tsx` | extract panels/hooks/API; keep route thin |
| 1440 | `src/frontend/app/admin/landing-pages/admin-landing-page-composer.tsx` | extract panels/hooks/API; keep route thin |
| 1326 | `src/frontend/app/admin/admin-api.ts` | extract panels/hooks/API; keep route thin |
| 1259 | `src/frontend/app/admin/catalog-attribute-api.ts` | extract panels/hooks/API; keep route thin |
| 1254 | `src/frontend/app/admin/host-client.ts` | extract panels/hooks/API; keep route thin |
| 1248 | `src/frontend/app/access-control/access-control-center.tsx` | extract panels/hooks/API; keep route thin |
| 1230 | `src/frontend/app/admin/admin-screens.tsx` | extract panels/hooks/API; keep route thin |
| 1211 | `src/frontend/app/vendor-panel/seller-api.ts` | extract panels/hooks/API; keep route thin |
| 1190 | `src/frontend/app/admin/category-attributes-panel.tsx` | extract panels/hooks/API; keep route thin |
| 1096 | `src/frontend/app/admin/admin-order-detail-screen.tsx` | extract panels/hooks/API; keep route thin |
| 1086 | `src/frontend/design-system/app-data-grid/AppDataGrid.tsx` | extract panels/hooks/API; keep route thin |
| 1062 | `src/frontend/app/admin/catalog-attribute-ui.tsx` | extract panels/hooks/API; keep route thin |
| 1053 | `src/frontend/app/wallet/wallet-ui.tsx` | extract panels/hooks/API; keep route thin |

## Guard

- Backend repo-wide: `tmar-source-size-baseline.json` + `TmarSourceSizeAndInfraAppTests` (already lists these FE files; shrink-only).
- Frontend: `lib/architecture/frontend-source-size.guard.test.ts` reads `docs/evidence/TB-TMAR-FE-BASELINE/frontend-source-size-baseline.json`.

No splits in this task.
