# Disposition map — W5 Catalog MegaMenu

Member-level map for `Admin/CatalogMegaMenuEndpoints.cs` and CatalogDirectory MegaMenu members/helpers required by the 5 routes.

| Member / concern | Classification | Destination |
|---|---|---|
| `MapCatalogMegaMenuEndpoints` + 5 routes | MOVE | Catalog.Endpoints Admin + Storefront MegaMenu mappers |
| `GetCategoryMegaMenuAsync` HTTP + auth | MOVE | Endpoints → `GetCategoryMegaMenuQuery` + `ICatalogAdminAuthorizer` |
| `ListPlacementOptionsAsync` HTTP + auth | MOVE | `ListMegaMenuPlacementOptionsQuery` |
| `UpsertCategoryMegaMenuAsync` HTTP + auth | MOVE | `UpsertCategoryMegaMenuCommand` |
| `RemoveCategoryMegaMenuAsync` HTTP + auth | MOVE | `RemoveCategoryMegaMenuCommand` |
| `GetStorefrontMegaMenuAsync` HTTP (no auth) | MOVE | Storefront endpoint → `GetStorefrontMegaMenuQuery` |
| `ReadLocale` | MOVE | Locale defaulting in handlers (`fa-IR`) |
| `ToError` / PlatformHttpException catch | DROP | Canonical `ApiResponseFactory` |
| `InvalidOperationException` → `Results.Problem` | DROP / NORMALIZE | `Result` + `CatalogErrorCodes` |
| `AdminPanelAccess` usage | REPLACE | `ICatalogAdminAuthorizer` |
| `CategoryMegaMenuBindingInput` ownership | KEEP | Application root `CatalogContracts.cs` (shared with ICatalogDirectory / CatalogDemo); command uses same type — no duplicate CQRS shape in Models |
| Models views (`CategoryMegaMenuConfigurationView`, `MegaMenuPlacementOption`, `StorefrontMegaMenuItem`) | KEEP | Application root (ICatalogDirectory surface); no `*Contracts.cs` bundle under MegaMenu |
| `GetCategoryMegaMenuConfigurationAsync` | EXTRACT | `IMegaMenuDirectory` / `MegaMenuDirectory`; CatalogDirectory thin legacy wrapper |
| `ListMegaMenuPlacementOptionsAsync` | EXTRACT | same |
| `UpsertCategoryMegaMenuBindingAsync` | EXTRACT | same → `Result` |
| `RemoveCategoryMegaMenuBindingAsync` | EXTRACT | same → `Result` |
| `GetStorefrontMegaMenuAsync` (directory) | EXTRACT | same |
| `UpsertMegaMenuTranslationAsync` | MOVE | MegaMenuDirectory private helper |
| `BuildMenuPathAsync` | MOVE | MegaMenuDirectory private helper |
| `ResolveCategoryDisplayNameAsync` | MOVE (MegaMenu copy) | MegaMenuDirectory private helper (CatalogDirectory retains its own for other capabilities) |
| `ComputePresentationLevel` | MOVE | MegaMenuDirectory private static |
| `BuildUiCategoryRoute` / `MapUiLocaleSegment` | MOVE (MegaMenu copy) | MegaMenuDirectory private static |
| Category existence behavior (get/upsert) | NORMALIZE | `Result` + `catalog.megamenu.category.missing` (404) |
| Tree placement validation | NORMALIZE | Domain returns `CatalogMegaMenuPlacementViolation`; Directory → `catalog.megamenu.placement.invalid` (400) |
| Remove-with-children rejection | NORMALIZE | `catalog.megamenu.remove.has_children` (400) |
| Locale normalization/default | PRESERVE | `NormalizeLocale` + default `fa-IR` |
| Storefront publication/visibility/composition | PRESERVE | Domain `CatalogMegaMenuComposer` unchanged semantics |
| Program `MapCatalogMegaMenuEndpoints()` | REMOVE | Covered by `MapCatalogModuleEndpoints` |
| Host file `CatalogMegaMenuEndpoints.cs` | DELETE | Full evacuation |
| StoreAppearance* | NOT_MOVED | Deferred |
| Facets/Categories/Attributes/etc. | NOT_TOUCHED | W6+ |

## Host/Admin inventory

| | Count (recursive `*.cs`) |
|---|---|
| Before | 56 |
| After (delete `CatalogMegaMenuEndpoints.cs`) | 55 |

## Validator classification

| Request | Classification |
|---|---|
| `GetCategoryMegaMenuQuery` | NO_VALIDATOR_REQUIRED |
| `ListMegaMenuPlacementOptionsQuery` | NO_VALIDATOR_REQUIRED |
| `UpsertCategoryMegaMenuCommand` | VALIDATOR_REQUIRED (transport shape: Input not null; optional text max lengths) |
| `RemoveCategoryMegaMenuCommand` | NO_VALIDATOR_REQUIRED |
| `GetStorefrontMegaMenuQuery` | NO_VALIDATOR_REQUIRED |
