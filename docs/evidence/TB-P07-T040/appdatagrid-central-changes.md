# AppDataGrid central changes (TB-P07-T040)

- **NEW** `legacy-grid-bridge.ts` — maps legacy `GridColumnDef` → AG `ColDef` + filter matrix
- **NEW** `LegacyAppDataGrid.tsx` — wraps AppDataGrid for client-side admin lists
- Exported from `design-system/app-data-grid/index.ts` and root `design-system/index.ts`
- No new grid wrapper fork; no raw `AgGridReact` in migrated pages
