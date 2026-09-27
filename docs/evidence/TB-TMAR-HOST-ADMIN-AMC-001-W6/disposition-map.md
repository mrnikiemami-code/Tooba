# Disposition map — W6 Catalog Facets

Member-level map for `Admin/CatalogFacetEndpoints.cs` and every Facet-specific CatalogDirectory member/helper required by the six routes.

| Member / concern | Classification | Destination |
|---|---|---|
| `MapCatalogFacetEndpoints` + 6 routes | MOVE | Catalog.Endpoints Admin/Facets + Storefront/Facets |
| `GetEffectiveFacetsAsync` HTTP + auth | MOVE | `GetEffectiveCategoryFacetsQuery` + `ICatalogAdminAuthorizer` |
| `ListLocalFacetsAsync` HTTP + auth | MOVE | `ListLocalCategoryFacetsQuery` |
| `UpsertFacetAsync` HTTP + auth | MOVE | `UpsertCategoryFacetCommand` |
| `RemoveFacetOverrideAsync` HTTP + auth | MOVE | `RemoveCategoryFacetOverrideCommand` |
| `ReorderFacetsAsync` HTTP + auth | MOVE | `ReorderCategoryFacetsCommand` |
| `GetStorefrontFacetsAsync` HTTP (no Admin auth) | MOVE | Storefront → `GetStorefrontCategoryFacetsQuery` |
| `ToError` / PlatformHttpException catch | DROP | Canonical `ApiResponseFactory` |
| `InvalidOperationException` → Results.Json message-as-code | DROP / NORMALIZE | `Result` + `CatalogErrorCodes` |
| `AdminPanelAccess` usage | REPLACE | `ICatalogAdminAuthorizer` |
| `UpsertCategoryFacetRequest` Host transport record | DROP | Command uses Application `CategoryFacetConfigurationInput` |
| `ReorderCategoryFacetsRequest` Host transport record | DROP | `ReorderCategoryFacetsCommand.OrderedDefinitionIds` |
| Models (`CategoryFacetConfigurationInput/View`, `EffectiveCategoryFacet`) | KEEP | Application root `CatalogContracts.cs` (shared with ICatalogDirectory); no Facets `*Contracts.cs` bundle |
| `GetEffectiveCategoryFacetsAsync` | EXTRACT | `IFacetDirectory` / `FacetDirectory` → `Result` |
| `ListLocalFacetConfigurationsAsync` | EXTRACT | same → `Result` |
| `UpsertCategoryFacetConfigurationAsync` | EXTRACT | same → `Result` |
| `RemoveCategoryFacetOverrideAsync` | EXTRACT | same → `Result` |
| `ReorderCategoryFacetConfigurationsAsync` | EXTRACT | same → `Result` |
| `ResolveEffectiveFacetsAsync` | MOVE (Facet copy) | FacetDirectory private; CatalogDirectory retains schema resolve for non-Facet |
| `ResolveEffectiveBindingsAsync` usage by Facets | MOVE (Facet copy) | FacetDirectory private helper for schema eligibility |
| `GetAttributeDefinitionNamesAsync` if used by Facets | MOVE (Facet copy) | FacetDirectory private name resolution; CatalogDirectory retains own for other capabilities |
| Mutation guard (`EnsureCanMutateAsync`) | PRESERVE | FacetDirectory mutations |
| Category existence semantics | NORMALIZE | `catalog.facet.category.missing` (404) |
| Definition existence semantics | NORMALIZE | `catalog.facet.definition.missing` (404) |
| Effective-schema membership | NORMALIZE | `catalog.facet.schema.missing` (400) |
| Filterable-only rule | NORMALIZE | `catalog.facet.not_filterable` (400) |
| `CatalogCategoryFacetRules.ValidateDisplayType` | NORMALIZE | Typed `CatalogFacetDisplayTypeViolation` → `catalog.facet.display_type.invalid` (400); rules remain Domain authority |
| Searchable coercion (`IsSearchableAllowed`) | PRESERVE | FacetDirectory applies same coercion |
| Local override missing | NORMALIZE | `catalog.facet.override.missing` (404); prior Host code `catalog.facet.missing` |
| Exact reorder-set validation | NORMALIZE | `catalog.facet.reorder.invalid` (400) |
| Inherited/effective composition | PRESERVE | `CatalogCategoryFacetResolver` unchanged semantics |
| Locale default `fa-IR` | PRESERVE | Handlers default blank locale |
| Program `MapCatalogFacetEndpoints()` | REMOVE | Covered by `MapCatalogModuleEndpoints` |
| Host file `CatalogFacetEndpoints.cs` | DELETE | Full evacuation |
| StoreAppearance* | NOT_MOVED | Deferred |
| Categories/Attributes/ProductWorkspace/etc. | NOT_TOUCHED | Except minimal Program/CatalogEndpointModule registration |

## Host/Admin inventory

| | Count (recursive `*.cs`) |
|---|---|
| Before | 55 |
| After (delete `CatalogFacetEndpoints.cs`) | 54 |

## Validator classification

| Request | Classification |
|---|---|
| `GetEffectiveCategoryFacetsQuery` | NO_VALIDATOR_REQUIRED |
| `ListLocalCategoryFacetsQuery` | NO_VALIDATOR_REQUIRED |
| `UpsertCategoryFacetCommand` | VALIDATOR_REQUIRED (transport: Input not null) |
| `RemoveCategoryFacetOverrideCommand` | NO_VALIDATOR_REQUIRED |
| `ReorderCategoryFacetsCommand` | VALIDATOR_REQUIRED (transport: OrderedDefinitionIds not null) |
| `GetStorefrontCategoryFacetsQuery` | NO_VALIDATOR_REQUIRED |

## Error code normalization (prior → stable)

| Prior Host surface | Stable CatalogErrorCodes |
|---|---|
| `catalog.facet.invalid` (upsert/reorder/storefront IOE bag) | Split by outcome: `display_type.invalid`, `not_filterable`, `schema.missing`, `reorder.invalid`, `category.missing`, `definition.missing` |
| `catalog.facet.missing` (remove override) | `catalog.facet.override.missing` |
| Auth PlatformHttpException | Unchanged via authorizer / global presentation |

## Residual debt (explicit)

- `ICatalogDirectory` thin Facet wrappers remain for CatalogDemo / existing directory tests
- Broad CatalogDirectory still owns non-Facet schema/product authority
- StoreAppearance deferred; W7 not started
