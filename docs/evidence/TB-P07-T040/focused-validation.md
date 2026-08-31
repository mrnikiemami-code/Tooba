# Focused validation (TB-P07-T040)

```
node --test design-system/app-data-grid/legacy-grid-bridge.test.ts → 2/2
node --test app/admin/admin-grid-migration.test.ts → 5/5
node --test app/admin/product-catalog-admin.test.ts → pass (AppDataGrid preserved)
node docs/ai/recovery-staleness.guard.test.mjs → 3/3
git diff --check → OK
```

No repository-wide FE/BE suites run (per task scope).
