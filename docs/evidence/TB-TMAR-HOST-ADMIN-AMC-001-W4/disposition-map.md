# Disposition map — W4 Catalog Tags

Member-level map for `Admin/CatalogTagEndpoints.cs` and CatalogDirectory tag members required by the 9 routes.

| Member / concern | Classification | Destination |
|---|---|---|
| `MapCatalogTagEndpoints` + 9 routes | MOVE | `Catalog.Endpoints/Admin/Tags/CatalogTagEndpoints.cs` |
| `ListTagsAsync` HTTP + auth | MOVE | Endpoints → `ListTagsQuery` + `ICatalogAdminAuthorizer` |
| `CreateTagAsync` HTTP + auth + NameFa/NameEn overlay | MOVE | Endpoints → `CreateTagCommand` (overlay in endpoint→model) |
| `CreateTagBody` transport | MOVE | Catalog.Endpoints transport record |
| `GetTagAsync` HTTP + NotFound | MOVE | `GetTagQuery` → `catalog.tag.missing` / ApiResponseFactory |
| `ListProductTagsAsync` HTTP | MOVE | `ListProductTagsQuery` |
| `AssignProductTagAsync` HTTP + list-after | MOVE | `AssignProductTagCommand` returns tag list |
| `RemoveProductTagAsync` HTTP + list-after | MOVE | `RemoveProductTagCommand` |
| `ListCategoryTagsAsync` HTTP | MOVE | `ListCategoryTagsQuery` |
| `AssignCategoryTagAsync` HTTP + list-after | MOVE | `AssignCategoryTagCommand` |
| `RemoveCategoryTagAsync` HTTP + list-after | MOVE | `RemoveCategoryTagCommand` |
| `AdminPanelAccess` usage | REPLACE | `ICatalogAdminAuthorizer` |
| `CatalogActorHttpBinding` on tag groups | DROP (documented) | Not required for Tag mutations; no Host filter copy |
| `ICatalogDirectory` tag methods (9-route surface) | EXTRACT | `ITagDirectory` / `TagDirectory`; CatalogDirectory retains thin legacy wrappers for CatalogDemo |
| `MapTagViewAsync` / `PreferLocaleNameAsync` | MOVE | TagDirectory private helpers |
| `SlugifyTagCode` / `ResolveUniqueTagCodeAsync` | MOVE | TagDirectory private helpers |
| Mutation guard usage | PRESERVE | TagDirectory calls `ICatalogUseCaseGuard` |
| Duplicate explicit code → IOE Persian | NORMALIZE | `Result` + `catalog.tag.code.duplicate` (was same wire code as invalid) |
| Missing name → IOE Persian | NORMALIZE | `Result` + `catalog.tag.invalid` |
| Duplicate product/category assign | NORMALIZE | `Result` + `catalog.tag.assign.duplicate` |
| Missing product/category/tag on assign (EF SingleAsync IOE wrongly mapped to assign.duplicate) | NORMALIZE | `catalog.tag.product.missing` / `catalog.tag.category.missing` / `catalog.tag.missing` |
| Get missing | PRESERVE→Result | `catalog.tag.missing` 404 |
| Program `MapCatalogTagEndpoints()` | REMOVE | Covered by `MapCatalogModuleEndpoints` |
| Host file `CatalogTagEndpoints.cs` | DELETE | Full evacuation |
| StoreAppearance* | NOT_MOVED | Deferred |
| Other Host Catalog* Admin files | NOT_TOUCHED | W5+ |

## Host/Admin inventory

| | Count (recursive `*.cs`) |
|---|---|
| Before | 57 |
| After (delete `CatalogTagEndpoints.cs`) | 56 |

## Validator classification

| Request | Classification |
|---|---|
| `ListTagsQuery` | NO_VALIDATOR_REQUIRED |
| `CreateTagCommand` | VALIDATOR_REQUIRED |
| `GetTagQuery` | NO_VALIDATOR_REQUIRED |
| `ListProductTagsQuery` | NO_VALIDATOR_REQUIRED |
| `AssignProductTagCommand` | NO_VALIDATOR_REQUIRED |
| `RemoveProductTagCommand` | NO_VALIDATOR_REQUIRED |
| `ListCategoryTagsQuery` | NO_VALIDATOR_REQUIRED |
| `AssignCategoryTagCommand` | NO_VALIDATOR_REQUIRED |
| `RemoveCategoryTagCommand` | NO_VALIDATOR_REQUIRED |
