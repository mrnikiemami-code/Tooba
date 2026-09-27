# Analyze — TB-TMAR-HOST-ADMIN-AMC-001-W6 (Catalog Facets)

## Target analyzed

- Host: `src/backend/Host/Tooba.Host/Admin/CatalogFacetEndpoints.cs` (6 routes: 5 Admin + 1 Storefront)
- Infrastructure: Facet members/helpers on `CatalogDirectory` + `ResolveEffectiveFacetsAsync` / `GetAttributeDefinitionNamesAsync` (shared helper)
- Domain: `CatalogCategoryFacetRules`, `CatalogCategoryFacetResolver`, `CatalogCategoryFacetConfiguration`, `CatalogFacetDisplayType`

## Structured state (pre-migration)

| Field | State |
|---|---|
| Foundation-State (Catalog) | FOUNDATION_READY (extend Facets capability like Tags/MegaMenu) |
| Ownership-State | MUST_SPLIT — Host HTTP + broad Directory authority |
| File-Cohesion-State | MULTI_RESPONSIBILITY_COHESION_VIOLATION (Admin+Storefront+transport records+error mapping) |
| Localization-State | EXCEPTION_MESSAGE_BASED (Persian IOE messages + hard-coded endpoint titles) |
| API-Result-Pattern-State | AD_HOC / RAW_RESULTS (`Results.Json`, PlatformHttpException catch) |
| Stable-Error-Code-State | STRING_HEURISTIC (`catalog.facet.invalid` / `catalog.facet.missing` hard-coded in Host) |
| CQRS-State | MISSING |
| Validator-Coverage-State | GAPS (no validators) |
| Contracts-Boundary-State | CLEAN for Facets (Application models) |
| Cross-Module-Coupling-State | NONE for Facet slice |
| Persistence-Ownership-State | CORRECT module schema; Host calls Directory |
| Endpoint-Ownership-State | HOST_OWNED |
| Schema-Migration-State | UNCHANGED |
| Behavior-Preservation-Risk | LOW if Result+codes preserve statuses/JSON shapes |
| Final-Disposition | READY_TO_MIGRATE |

## Responsibility map

| Concern | Class |
|---|---|
| 6 HTTP routes | HTTP_ENDPOINT → Catalog.Endpoints |
| Admin auth | AUTHORIZATION_ADAPTER → `ICatalogAdminAuthorizer` |
| Facet use cases | APPLICATION_USE_CASE → Facets Commands/Queries |
| Facet persistence / effective resolve | PERSISTENCE → `IFacetDirectory` / `FacetDirectory` |
| Display-type / searchable / inheritance rules | DOMAIN_RULE → keep in Catalog.Domain |
| Program registration | HOST_COMPOSITION_ROOT → drop Facet map; module mapper already present |
| StoreAppearance* | NOT_IN_SCOPE |

## Illegal / non-canonical findings

- Host owns Facet HTTP and injects `ICatalogDirectory`
- `PlatformHttpException` + `ToError` ad-hoc mapping
- `InvalidOperationException` + `ex.Message` as user-facing title / message-as-code
- Hard-coded `catalog.facet.*` in Host endpoints
- Facet authority buried in broad `CatalogDirectory`
- `CatalogCategoryFacetRules.ValidateDisplayType` throws Persian IOE (expected business)

## Target paths

```
Application/Facets/{Commands,Queries,Models,Ports,Validators}
Infrastructure/FacetDirectory.cs
Endpoints/Admin/Facets/CatalogFacetAdminEndpoints.cs
Endpoints/Storefront/Facets/CatalogFacetStorefrontEndpoints.cs
```

## Migration order

1. Persist disposition map (this wave)
2. Domain typed display-type violation (no message-as-code)
3. `IFacetDirectory` + `FacetDirectory` + Result/CatalogErrorCodes
4. CQRS handlers/validators
5. Catalog.Endpoints Admin+Storefront + module registration
6. CatalogDirectory thin wrappers; remove Host file + Program map
7. Guards/tests/SoT/evidence; focused validation; commit/push

## Canonical references

- W4 Tags / W5 MegaMenu evacuate pattern
- `ApiResponseFactory` + `CatalogErrorCodes` + resx contributor
- Offer shape for CQRS/ISender (principles only)
