# Admin grid inventory (TB-P07-T040)

| Route | Component | Before | Classification | Outcome |
|-------|-----------|--------|----------------|---------|
| `/admin/products` | product-list.tsx | AppDataGrid | ALREADY_CANONICAL | Kept |
| `/admin/catalog/categories/.../products` | category-products-panel.tsx | AppDataGrid | ALREADY_CANONICAL | Kept |
| `/admin/orders` | admin-screens GridPage | DataGrid | ELIGIBLE_MIGRATE | Migrated via LegacyAppDataGrid |
| `/admin/fulfillments` | admin-screens GridPage | DataGrid | ELIGIBLE_MIGRATE | Migrated |
| `/admin/returns` | admin-screens GridPage | DataGrid | ELIGIBLE_MIGRATE | Migrated |
| `/admin/sellers` | admin-screens GridPage | DataGrid | ELIGIBLE_MIGRATE | Migrated |
| `/admin/customers` | admin-screens GridPage | DataGrid | ELIGIBLE_MIGRATE | Migrated |
| `/admin/settlement` | admin-screens GridPage | DataGrid | ELIGIBLE_MIGRATE | Migrated |
| `/admin/reviews` | admin-screens | DataGrid | ELIGIBLE_MIGRATE | Migrated |
| `/admin/promotions` | admin-screens | DataGrid | ELIGIBLE_MIGRATE | Migrated |
| `/admin/payouts` | admin-screens | DataGrid | ELIGIBLE_MIGRATE | Migrated |
| `/admin/content` | admin-screens | DataGrid | ELIGIBLE_MIGRATE | Migrated |
| `/admin/stories` | StoryManagementScreen | DataGrid | ELIGIBLE_MIGRATE | Migrated |
| `/admin/catalog/attributes` | catalog-attribute-ui | `<table>` | ELIGIBLE_MIGRATE | Migrated |
| `/admin/catalog/category-schema` | catalog-attribute-ui | `<table>` | ELIGIBLE_MIGRATE | Migrated |
| `/admin/gift-cards` | wallet-ui | `<table>` | ELIGIBLE_MIGRATE | Migrated |
| Category tree/workspace/panels | various | specialized | SPECIALIZED_KEEP | Kept |
