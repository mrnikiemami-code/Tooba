# Analyze — TB-TMAR-HOST-ADMIN-AMC-001-W7 (Catalog Categories)

## Target analyzed

- Host: `src/backend/Host/Tooba.Host/Admin/CatalogCategoryEndpoints.cs` (10 routes: 9 Admin + 1 Storefront)
- Infrastructure: Category members/helpers on `CatalogDirectory` (`CreateCategoryAsync`×2, `UpdateCategoryCoreAsync`, `UpsertCategoryTranslationAsync`, `MoveCategoryAsync`, `ReorderCategorySiblingsAsync`, `GetCategoryTreeAsync`, `GetCategoryWorkspaceAsync`, `ResolveCategoryRouteAsync`, `PublishCategoryAsync`, `ArchiveCategoryAsync`, `EnsureSlugAvailableAsync`, `EnsureExpectedUpdatedAt`, `IsStorefrontEligible`, `BuildCanonicalPath`, `ToTranslationDto`, `UpsertLocalizedNameAsync`, `AddLocalizedNames` for Category)
- Domain: `CatalogCategoryTreeRules`, `CatalogCategorySlugNormalizer`, `CatalogCategory`, `CatalogCategoryTranslation`, `CatalogCategorySlugHistory`
- Contracts/presentation: Host hard-coded `catalog.category.*` codes + Persian/English titles; `PlatformHttpException` / `InvalidOperationException` + `ex.Message` slug duplicate parsing

## Host/Admin inventory (pre-migration)

| | Count (recursive `*.cs`) |
|---|---|
| Before W7 | 54 |
| After (delete `CatalogCategoryEndpoints.cs`) | 53 |

## Structured state (pre-migration)

| Field | State |
|---|---|
| Foundation-State (Catalog) | FOUNDATION_READY (extend Categories capability like Tags/MegaMenu/Facets) |
| Ownership-State | MUST_SPLIT — Host HTTP + broad Directory authority |
| File-Cohesion-State | MULTI_RESPONSIBILITY_COHESION_VIOLATION (Admin+Storefront+transport records+error mapping) |
| Localization-State | EXCEPTION_MESSAGE_BASED + HARDCODED_TEXT |
| API-Result-Pattern-State | AD_HOC / RAW_RESULTS |
| Stable-Error-Code-State | STRING_HEURISTIC + message parsing for slug duplicate |
| CQRS-State | MISSING |
| Validator-Coverage-State | GAPS |
| Endpoint-Ownership-State | HOST_OWNED |
| Schema-Migration-State | UNCHANGED |
| Behavior-Preservation-Risk | LOW if Result+codes preserve statuses/JSON/auth/route-history |
| Final-Disposition | READY_TO_MIGRATE |

## Responsibility map

| Concern | Class |
|---|---|
| 10 HTTP routes | HTTP_ENDPOINT → Catalog.Endpoints Admin/Categories + Storefront/Categories |
| Admin auth | AUTHORIZATION_ADAPTER → `ICatalogAdminAuthorizer` |
| Category use cases | APPLICATION_USE_CASE → Categories Commands/Queries |
| Category persistence/orchestration | PERSISTENCE → `ICategoryDirectory` / `CategoryDirectory` |
| Tree depth/self-parent/descendant | DOMAIN_RULE → `CatalogCategoryTreeRules` (authority preserved) |
| Slug/locale normalize + history | DOMAIN_RULE + PERSISTENCE |
| Program `MapCatalogCategoryEndpoints()` | HOST_COMPOSITION_ROOT → remove; module mapper |
| StoreAppearance* | NOT_IN_SCOPE |

## Illegal / non-canonical findings

- Host owns Category HTTP and injects `ICatalogDirectory`
- `PlatformHttpException` + `ToError`
- `InvalidOperationException` message-as-code + slug duplicate `ex.Message` contains "slug"/"تکراری"
- Hard-coded Persian/English transport titles
- Category authority buried in broad `CatalogDirectory`
- Storefront resolve mixed into Host Admin file

## Target paths

```
Application/Categories/{Commands,Queries,Models,Ports,Validators}
Infrastructure/CategoryDirectory.cs
Endpoints/Admin/Categories/CatalogCategoryAdminEndpoints.cs
Endpoints/Storefront/Categories/CatalogCategoryStorefrontEndpoints.cs
```

## Migration order

1. Persist disposition map (this wave)
2. `CatalogErrorCodes` + catalog/resources for Category surface
3. `ICategoryDirectory` + `CategoryDirectory` + Result (no message parsing)
4. CQRS handlers/validators
5. Catalog.Endpoints Admin+Storefront + module registration
6. CatalogDirectory thin wrappers; remove Host file + Program map
7. Guards/tests/SoT/evidence; focused validation; commit/push

## Canonical references

- W4 Tags / W5 MegaMenu / W6 Facets evacuate pattern
- `ApiResponseFactory` + `CatalogErrorCodes` + resx contributor
- Offer shape for CQRS/ISender (principles only)
