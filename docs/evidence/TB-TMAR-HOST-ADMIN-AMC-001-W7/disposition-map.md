# Disposition map — W7 Catalog Categories

Member-level map for `Admin/CatalogCategoryEndpoints.cs` and every category-specific CatalogDirectory member/helper required by the ten routes.

| Member / concern | Classification | Destination |
|---|---|---|
| `MapCatalogCategoryEndpoints` + 10 routes | MOVE | Catalog.Endpoints Admin/Categories + Storefront/Categories |
| `GetTreeAsync` HTTP + auth + locale blank check | MOVE | `GetCategoryTreeQuery` + validator + `ICatalogAdminAuthorizer` |
| `GetWorkspaceAsync` HTTP + auth + null→404 | MOVE | `GetCategoryWorkspaceQuery` |
| `CreateAsync` HTTP + auth + structured/legacy | MOVE | `CreateCategoryCommand` (both shapes preserved) |
| `UpdateCoreAsync` HTTP + auth + return workspace | MOVE | `UpdateCategoryCoreCommand` → update + workspace |
| `UpsertTranslationAsync` HTTP + auth | MOVE | `UpsertCategoryTranslationCommand` |
| `MoveAsync` HTTP + auth + return workspace | MOVE | `MoveCategoryCommand` |
| `ReorderAsync` HTTP + auth + `{ ok: true }` | MOVE | `ReorderCategoriesCommand` → `CategoryOkResult` |
| `PublishAsync` / `ArchiveAsync` HTTP + auth + workspace | MOVE | `PublishCategoryCommand` / `ArchiveCategoryCommand` |
| `ResolveRouteAsync` Storefront (no Admin auth) | MOVE | Storefront → `ResolveCategoryRouteQuery` |
| `MapCategoryInvalid` + slug message parsing | DROP | Typed `Result` + `CatalogErrorCodes.CategorySlugDuplicate` |
| `ToError` / PlatformHttpException catch | DROP | Canonical `ApiResponseFactory` |
| `AdminPanelAccess` usage | REPLACE | `ICatalogAdminAuthorizer` |
| Six Host transport records | DROP | Commands / Application Models / existing Application request DTOs |
| `GetCategoryTreeAsync` | EXTRACT | `ICategoryDirectory` / `CategoryDirectory` → `Result` |
| `GetCategoryWorkspaceAsync` | EXTRACT | same → `Result` (null → missing) |
| `CreateCategoryAsync` (legacy LocalizedNames) | EXTRACT | same → `Result`; thin wrapper retained |
| `CreateCategoryAsync` (structured) | EXTRACT | same → `Result` |
| `UpdateCategoryCoreAsync` | EXTRACT | same → `Result` |
| `UpsertCategoryTranslationAsync` | EXTRACT | same → `Result` |
| `MoveCategoryAsync` | EXTRACT | same → `Result` |
| `ReorderCategorySiblingsAsync` | EXTRACT | same → `Result` |
| `PublishCategoryAsync` / `ArchiveCategoryAsync` | EXTRACT | same → `Result` |
| `ResolveCategoryRouteAsync` | EXTRACT | same → `Result` (null → route.missing) |
| `EnsureSlugAvailableAsync` | EXTRACT | CategoryDirectory private; typed duplicate → `slug.duplicate` |
| `EnsureExpectedUpdatedAt` | EXTRACT | CategoryDirectory private → `concurrency.conflict` |
| `IsStorefrontEligible` / `BuildCanonicalPath` / `ToTranslationDto` | EXTRACT | CategoryDirectory private |
| `UpsertLocalizedNameAsync` / Category `AddLocalizedNames` usage | EXTRACT | CategoryDirectory private |
| Mutation guard (`EnsureCanMutateAsync`) | PRESERVE | CategoryDirectory mutations |
| Tree rules (self/descendant/max-depth=3) | PRESERVE | Domain `CatalogCategoryTreeRules` authority; Application maps outcomes to codes without message parse |
| Slug history / current-wins / redirect | PRESERVE | CategoryDirectory resolve semantics |
| Locale/slug normalization | PRESERVE | Domain normalizer |
| Media clear/set + ExpectedUpdatedAt | PRESERVE | Update core |
| Program `MapCatalogCategoryEndpoints()` | REMOVE | Covered by `MapCatalogModuleEndpoints` |
| Host file `CatalogCategoryEndpoints.cs` | DELETE | Full evacuation |
| StoreAppearance* | NOT_MOVED | Deferred |
| Attributes/ProductWorkspace/Facets/MegaMenu/etc. | NOT_TOUCHED | Except minimal Program/CatalogEndpointModule registration |

## Host/Admin inventory

| | Count (recursive `*.cs`) |
|---|---|
| Before | 54 |
| After (delete `CatalogCategoryEndpoints.cs`) | 53 |

## Validator classification

| Request | Classification |
|---|---|
| `GetCategoryTreeQuery` | VALIDATOR_REQUIRED (locale non-blank) |
| `GetCategoryWorkspaceQuery` | NO_VALIDATOR_REQUIRED |
| `CreateCategoryCommand` | VALIDATOR_REQUIRED (at least one create shape present) |
| `UpdateCategoryCoreCommand` | NO_VALIDATOR_REQUIRED |
| `UpsertCategoryTranslationCommand` | VALIDATOR_REQUIRED (Name/Slug non-blank) |
| `MoveCategoryCommand` | NO_VALIDATOR_REQUIRED |
| `ReorderCategoriesCommand` | VALIDATOR_REQUIRED (OrderedCategoryIds not null) |
| `PublishCategoryCommand` | NO_VALIDATOR_REQUIRED |
| `ArchiveCategoryCommand` | NO_VALIDATOR_REQUIRED |
| `ResolveCategoryRouteQuery` | VALIDATOR_REQUIRED (locale + slug non-blank) |

## Error code normalization (prior → stable)

| Prior Host surface | Stable CatalogErrorCodes |
|---|---|
| `catalog.category.missing` (workspace null) | `catalog.category.missing` (404) |
| `catalog.category.invalid` (locale blank / generic IOE bag) | Split: `invalid`, `parent.missing`, `parent.self`, `parent.descendant`, `depth.max`, `reorder.invalid`, `concurrency.conflict`, `slug.invalid` |
| `catalog.category.slug.duplicate` (message parse) | `catalog.category.slug.duplicate` (409) typed — no message parse |
| `catalog.category.route.invalid` | `catalog.category.route.invalid` (400) |
| `catalog.category.route.missing` | `catalog.category.route.missing` (404) |
| Auth PlatformHttpException | Unchanged via authorizer / global presentation |

## Residual debt (explicit)

- `ICatalogDirectory` thin Category wrappers remain for CatalogDemo / foundation tests
- Broad CatalogDirectory still owns non-Category capabilities
- StoreAppearance deferred; W8 not started
