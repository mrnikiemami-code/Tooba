# 14 — Access Control localization

- Primary UI labels come from `permission-labels.ts` (FA + EN) keyed by PermissionCatalog ids.
- Raw ids (`product.view`) and i18n keys (`perm.*.desc`) are never shown as primary text.
- Fallback: `humanizePermissionId` for unknown ids.
- Modules use `getModuleLabel` (e.g. Order → سفارش).

Proof: permission matrix / ceiling / effective access render `getPermissionLabel(...).title`.
