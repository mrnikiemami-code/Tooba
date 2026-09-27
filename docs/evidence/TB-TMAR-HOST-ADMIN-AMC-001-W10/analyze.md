# Analyze — TB-TMAR-HOST-ADMIN-AMC-001-W10

## Structured state (pre-migrate)

| Field | Value |
|---|---|
| Foundation-State | FOUNDATION_READY (Catalog certified; extend Attributes/ProductValues beside Definitions+Schema) |
| Ownership-State | MUST_SPLIT (mixed Attribute Host file) |
| File-Cohesion-State | MULTI_RESPONSIBILITY_COHESION_VIOLATION |
| Oversized/God-File-State | CatalogAttributeEndpoints.cs — product attributes + variants + category-change |
| Localization-State | EXCEPTION_MESSAGE_BASED on product-attribute surface (IOE Persian title + catalog.attribute.invalid) |
| API-Result-Pattern-State | RAW_RESULTS / AD_HOC (`Results.Json`, PlatformHttpException ToError) |
| Stable-Error-Code-State | STRING_HEURISTIC — all product-attr failures collapse to catalog.attribute.invalid 400 + ex.Message |
| Logging-State | CANONICAL |
| Sensitive-Logging-State | NONE |
| OpenTelemetry-State | CANONICAL |
| Correlation-Trace-State | CANONICAL |
| CQRS-State | MISSING on four product-attribute routes |
| Validator-Coverage-State | GAPS (none on product-attribute HTTP) |
| Contracts-Boundary-State | CLEAN for W10 product-attr (no Offer) |
| Cross-Module-Coupling-State | NONE on four routes (Offer only on retained variants) |
| Cross-Module-Join-State | NONE |
| Persistence-Ownership-State | HOST_OWNED HTTP → Catalog Directory (correct once focused) |
| Endpoint-Ownership-State | HOST_OWNED (W10 target: MODULE_OWNED) |
| Host-Residue-State | Partial file retain for variant/category-change waves |
| Schema-Migration-State | UNCHANGED |
| Behavior-Preservation-Risk | LOW–MEDIUM (typed error codes replace generic catalog.attribute.invalid + ex.Message) |
| Canonical-Reference-Used | W8 Definitions; W9 Schema; ApiResponseFactory; CatalogErrorCodes |
| Final-Disposition | READY_TO_MIGRATE |

## Target

Four Admin routes under `/v1/admin/catalog/products/{productId}` product attributes/readiness in Host `CatalogAttributeEndpoints.cs`.

## Responsibility map

- DOMAIN_RULE: `CatalogAttributeCanonicalizer`; schema eligibility; variant-axis exclusion; enum ownership/active; readiness
- APPLICATION_USE_CASE: editor read; readiness read; single set; bulk set (+ history)
- PERSISTENCE: Products + ProductAttributeValues + AttributeDefinitions/Options + Primary category + LocalizedTexts + ProductHistory
- HTTP_ENDPOINT: four Admin routes → Catalog.Endpoints
- AUTHORIZATION_ADAPTER: ICatalogAdminAuthorizer
- ACTOR_BINDING: module-owned transport bind for ICatalogActorContext (history)
- INTEGRATION_ADAPTER: none on W10 surface

## Ownership

Catalog owns product attribute editor/readiness/writes. Host retains variant/category-change Attribute HTTP until later waves.

## Illegal dependencies (current Host product-attr surface)

- Endpoints → ICatalogDirectory
- PlatformHttpException catch + Results.Json
- InvalidOperationException message → catalog.attribute.invalid
- Host AdminPanelAccess instead of ICatalogAdminAuthorizer

## Shared helpers

`ResolveEffectiveBindingsAsync` / primary-category / localization name helpers remain in CatalogDirectory for retained variant/category-change. ProductAttributeDirectory owns focused copies; domain `CatalogCategorySchemaResolver` stays the shared authority.

## Target paths

```
Application/Attributes/ProductValues/{Commands,Queries,Models,Ports,Validators}
Infrastructure/ProductAttributeDirectory.cs
Endpoints/Admin/Attributes/ProductValues/CatalogProductAttributeAdminEndpoints.cs
Endpoints/Admin/CatalogActorRequestBinding.cs (minimal module-owned actor bind)
```

## Migration order

1. Disposition map (this folder)
2. Error codes + catalog + resx
3. Port + ProductAttributeDirectory (Result + transaction)
4. CQRS + validators
5. Catalog.Endpoints map + actor bind + module registration
6. CatalogDirectory thin wrappers
7. Strip four product-attribute routes/records from Host
8. Update W8/W9 retention assertions; add W10 guards
9. Focused tests + SoT
10. Builds/tests → commit → push
