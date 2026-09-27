# Analyze — TB-TMAR-HOST-ADMIN-AMC-001-W9

## Structured state (pre-migrate)

| Field | Value |
|---|---|
| Foundation-State | FOUNDATION_READY (Catalog certified; extend Attributes/Schema beside Definitions) |
| Ownership-State | MUST_SPLIT (mixed Attribute Host file) |
| File-Cohesion-State | MULTI_RESPONSIBILITY_COHESION_VIOLATION |
| Oversized/God-File-State | CatalogAttributeEndpoints.cs — schema + product attributes/variants + category-change |
| Localization-State | EXCEPTION_MESSAGE_BASED on schema surface (IOE Persian title + catalog.schema.invalid) |
| API-Result-Pattern-State | RAW_RESULTS / AD_HOC (`Results.Json`, PlatformHttpException ToError) |
| Stable-Error-Code-State | STRING_HEURISTIC — all schema failures collapse to catalog.schema.invalid 400 |
| Logging-State | CANONICAL |
| Sensitive-Logging-State | NONE |
| OpenTelemetry-State | CANONICAL |
| Correlation-Trace-State | CANONICAL |
| CQRS-State | MISSING on five schema routes |
| Validator-Coverage-State | GAPS (none on schema HTTP) |
| Contracts-Boundary-State | CLEAN for W9 schema (no Offer) |
| Cross-Module-Coupling-State | NONE on five schema routes |
| Cross-Module-Join-State | NONE |
| Persistence-Ownership-State | HOST_OWNED HTTP → Catalog Directory (correct once focused) |
| Endpoint-Ownership-State | HOST_OWNED (W9 target: MODULE_OWNED) |
| Host-Residue-State | Partial file retain for product/variant/category-change waves |
| Schema-Migration-State | UNCHANGED |
| Behavior-Preservation-Risk | LOW–MEDIUM (typed error codes replace generic catalog.schema.invalid) |
| Canonical-Reference-Used | W8 Attributes/Definitions; W6 Facets; ApiResponseFactory; CatalogErrorCodes |
| Final-Disposition | READY_TO_MIGRATE |

## Target

Five Admin routes under `/v1/admin/catalog/categories/{categoryId}/attribute-schema` in Host `CatalogAttributeEndpoints.cs`.

## Responsibility map

- DOMAIN_RULE: `CatalogCategoryAttributeAssignmentRules.ValidateVariantAxis`; `CatalogCategorySchemaResolver` inheritance/override
- APPLICATION_USE_CASE: effective read; bind/update/unbind/reorder bindings
- PERSISTENCE: Categories + AttributeDefinitions + CategoryAttributeBindings
- HTTP_ENDPOINT: five Admin routes → Catalog.Endpoints
- AUTHORIZATION_ADAPTER: ICatalogAdminAuthorizer
- INTEGRATION_ADAPTER: none

## Ownership

Catalog owns category attribute-schema administration. Host retains product/variant/category-change Attribute HTTP until later waves.

## Illegal dependencies (current Host schema surface)

- Endpoints → ICatalogDirectory
- PlatformHttpException catch + Results.Json
- InvalidOperationException message → catalog.schema.invalid

## ResolveEffectiveBindings

Private copies exist in CatalogDirectory and FacetDirectory; both call `CatalogCategorySchemaResolver.ResolveEffectiveSchema`. W9 places schema load inside `CategoryAttributeSchemaDirectory`. CatalogDirectory keeps its private helper for retained product/variant callers; FacetDirectory unchanged (lawful narrow design — domain resolver already shared).

## Target paths

```
Application/Attributes/Schema/{Commands,Queries,Models,Ports,Validators}
Infrastructure/CategoryAttributeSchemaDirectory.cs
Endpoints/Admin/Attributes/Schema/CatalogCategoryAttributeSchemaAdminEndpoints.cs
```

## Migration order

1. Disposition map (this folder)
2. Error codes + catalog + resx
3. Port + CategoryAttributeSchemaDirectory (Result)
4. CQRS + validators
5. Catalog.Endpoints map + module registration
6. CatalogDirectory thin wrappers
7. Strip five schema routes/records from Host
8. Guards + SoT + focused tests
9. Builds/tests → commit → push
