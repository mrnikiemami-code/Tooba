# Behavior parity — W6 Facets

| Concern | Preserved |
|---|---|
| Routes / methods | 5 Admin + 1 Storefront paths unchanged |
| Default locale | `fa-IR` when blank |
| Effective / inheritance | `CatalogCategoryFacetResolver` + SourceCategoryId / IsInherited |
| Localized names | Same LocalizedTexts fallback → Code |
| Empty effective list | Success empty array when no filterable configs |
| Local list order | SortOrder ascending |
| Upsert create/update | Same fields; IsSearchable coerced via `IsSearchableAllowed` |
| Remove override | Deletes local row; inheritance fallback |
| Reorder | Sequential SortOrder 0..n-1 for exact local set |
| Storefront | Same effective semantics as Admin effective |
| Success JSON | Effective/local list DTO shapes unchanged |
| Mutations success | NoContent via ApiResponseFactory Result success |
| Admin auth | All 5 Admin routes `ICatalogAdminAuthorizer` |
| Storefront auth | No Admin authorizer |
| Mutation guard | `ICatalogUseCaseGuard.EnsureCanMutateAsync` on writes |
| Schema / frontend | Unchanged |
| StoreAppearance | Deferred / untouched |
