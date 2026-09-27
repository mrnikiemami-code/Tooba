# CQRS — W6 Facets

| HTTP | Request | Handler | Port |
|---|---|---|---|
| GET .../facets/effective | `GetEffectiveCategoryFacetsQuery` | Handler | `IFacetDirectory.GetEffectiveFacetsAsync` |
| GET .../facets/local | `ListLocalCategoryFacetsQuery` | Handler | `ListLocalConfigurationsAsync` |
| PUT .../facets/{definitionId} | `UpsertCategoryFacetCommand` | Handler | `UpsertConfigurationAsync` |
| DELETE .../facets/{definitionId} | `RemoveCategoryFacetOverrideCommand` | Handler | `RemoveOverrideAsync` |
| PUT .../facets/order | `ReorderCategoryFacetsCommand` | Handler | `ReorderConfigurationsAsync` |
| GET /v1/storefront/categories/{id}/facets | `GetStorefrontCategoryFacetsQuery` | Handler | `GetEffectiveFacetsAsync` |

All six return `Result` / `Result<T>`. Endpoints dispatch only via `ISender`; no Directory/DbContext injection in Endpoints.
Locale default `fa-IR` applied in query handlers when blank.
