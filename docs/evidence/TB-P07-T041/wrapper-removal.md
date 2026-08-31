# Wrapper removal — TB-P07-T041

- **Deleted** `LegacyAppDataGrid.tsx` (render-level wrapper).
- **Kept** pure helpers: `buildLegacyGridBridge`, `createClientGridQueryAdapter`, `useLegacyAdminGridDirectProps`, `adminGridQueryAdapter`, `postAdminGridQuery`.
- All migrated screens now render **`AppDataGrid` directly** with bridge-mapped `ColDef`s.
- Design-system exports no longer expose `LegacyAppDataGrid`.
