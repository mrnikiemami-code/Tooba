# TB-P07-T004 evidence notes

## Scope delivered
- Domain: CatalogCategory core fields + Move/Archive/SetCoreFields; CatalogCategoryTranslation; CatalogCategorySlugHistory; CatalogCategoryTreeRules; CatalogCategorySlugNormalizer; extension-point markers
- Persistence: migration `20260828172727_AddCategoryFoundation` (+ LocalizedText→translation backfill SQL)
- Application: Category DTOs + ICatalogDirectory methods (create/update/upsert/move/reorder/tree/workspace/resolve/archive)
- Host: `CatalogCategoryEndpoints` admin + storefront resolve
- Docs: `docs/catalog/CATEGORY-ARCHITECTURE.md`; PROJECT-STATE + RECOVERY-CONTEXT updated
- Tests: `CatalogCategoryFoundationTests`
- UI: NOT built (T005); visual contract images preserved; AppDataGrid untouched

## APIs
- GET /v1/admin/catalog/categories/tree?locale=
- GET /v1/admin/catalog/categories/{id}
- POST /v1/admin/catalog/categories
- PATCH /v1/admin/catalog/categories/{id}
- PUT /v1/admin/catalog/categories/{id}/translations/{locale}
- POST /v1/admin/catalog/categories/{id}/move
- POST /v1/admin/catalog/categories/reorder
- POST /v1/admin/catalog/categories/{id}/publish
- POST /v1/admin/catalog/categories/{id}/archive
- GET /v1/storefront/category-routes/resolve?locale=&slug=&forStorefront=

## Validation
- `dotnet build` Catalog.Infrastructure + Host.Tests + Host: 0 errors / 0 warnings
- Focused tests: CatalogCategoryFoundationTests + CatalogFoundationTests + CatalogAttributeSchemaTests → **10 passed**, 0 failed, 0 skipped (Docker available)
- `git diff --check` on touched Catalog/Host/docs paths: clean
- Host `:5088` restarted with new build; FE `:3000` and Shopeiva `:3001` kept alive
- UI not built (T005); AppDataGrid untouched
