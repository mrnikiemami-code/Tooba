# Behavior parity — W9 Category Attribute-Schema

| Concern | State |
|---|---|
| Five route methods/paths | PRESERVED |
| Admin authorization | ICatalogAdminAuthorizer (parity with AdminPanelAccess) |
| Effective schema JSON shape | EffectiveSchemaEntry fields preserved |
| Inherited/overridden flags + source ids | CatalogCategorySchemaResolver unchanged |
| Bind → 201 + `{ ok: true }` | api.Created + MutationResult |
| Update/Unbind/Reorder → `{ ok: true }` | api.From(Result MutationResult) |
| Duplicate / missing / reorder / variant-axis rejection | Typed CatalogErrorCodes (documented split from generic catalog.schema.invalid) |
| Mutation guard / tenant isolation | ICatalogUseCaseGuard + CatalogDbContext tenant filters |
| Product/variant Host routes | UNTOUCHED |
