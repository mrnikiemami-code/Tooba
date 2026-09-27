# Analyze — TB-TMAR-HOST-ADMIN-AMC-001-W8

## Structured state (pre-migrate)

| Field | Value |
|---|---|
| Foundation-State | FOUNDATION_READY (Catalog certified structure; extend Attributes/Definitions) |
| Ownership-State | MUST_SPLIT (mixed Attribute Host file) |
| File-Cohesion-State | MULTI_RESPONSIBILITY_COHESION_VIOLATION |
| Oversized/God-File-State | CatalogAttributeEndpoints.cs ~1014 LOC — Definitions + category-schema + product attributes/variants + category-change |
| Localization-State | EXCEPTION_MESSAGE_BASED on Definition surface (Persian parse + code-as-message IOE) |
| API-Result-Pattern-State | RAW_RESULTS / AD_HOC (`Results.Json`, PlatformHttpException ToError) |
| Stable-Error-Code-State | PARTIAL — some stable codes exist as IOE messages; duplicate code/name via Persian heuristics |
| Logging-State | CANONICAL (no Definition-specific logging smell) |
| Sensitive-Logging-State | NONE |
| OpenTelemetry-State | CANONICAL (Host composition) |
| Correlation-Trace-State | CANONICAL |
| CQRS-State | MISSING on Definition routes |
| Validator-Coverage-State | GAPS (none on Definition HTTP) |
| Contracts-Boundary-State | CLEAN for W8 Definition (Offer unused) |
| Cross-Module-Coupling-State | NONE on W8 seven routes; Offer.Contracts retained on Host for later-wave variant enrichment only |
| Cross-Module-Join-State | NONE |
| Persistence-Ownership-State | HOST_OWNED HTTP → Catalog Directory (correct persistence owner once focused) |
| Endpoint-Ownership-State | HOST_OWNED (W8 target: MODULE_OWNED) |
| Host-Residue-State | Partial file retain for W9+ Attribute groups |
| Schema-Migration-State | UNCHANGED |
| Behavior-Preservation-Risk | LOW–MEDIUM (create+metadata atomicity improvement documented) |
| Canonical-Reference-Used | W4–W7 Catalog evacuate; Categories capability; ApiResponseFactory; CatalogErrorCodes |
| Final-Disposition | READY_TO_MIGRATE |

## Target

Seven Admin routes under `/v1/admin/catalog/attribute-definitions` currently in Host `CatalogAttributeEndpoints.cs`.

## Responsibility map

- DOMAIN_RULE: ValueKind / variant-axis eligibility / metadata bounds / option enum-only
- APPLICATION_USE_CASE: list/get/create(+metadata)/update/preview/set capability/add option
- PERSISTENCE: AttributeDefinitions + LocalizedTexts + CategoryAttributeBindings + ProductCategories + ProductVariantAxes
- HTTP_ENDPOINT: seven Admin routes → Catalog.Endpoints
- AUTHORIZATION_ADAPTER: ICatalogAdminAuthorizer
- INTEGRATION_ADAPTER: none for W8 (Offer not used)

## Ownership

Catalog owns Attribute Definition administration. Offer owns only later-wave variant offer-count enrichment (retained Host). Host retains non-Definition Attribute HTTP until later waves.

## Illegal dependencies (current Host Definition surface)

- Endpoints → ICatalogDirectory (Infrastructure-shaped directory from Host)
- PlatformHttpException catch + Results.Json
- InvalidOperationException message / Persian parse → status/errorCode

## Offer boundary (W8)

Preview/set impact is **Catalog-local** (bindings, primary products, ProductVariantAxes). No Offer.Contracts call on the seven routes. Host file retains Offer usings for variant editor enrichment (RETAIN_FOR_W9_PLUS).

## Create atomicity audit

Current Host Create: `CreateAttributeDefinitionAsync` SaveChanges, then optional `UpdateAttributeDefinitionAsync` SaveChanges. Partial persistence possible if metadata update fails. W8 Command makes create+metadata one Application operation with a single SaveChanges (same DbContext, no schema change) — improves without worsening parity of success payloads.

## Target paths

```
Application/Attributes/Definitions/{Commands,Queries,Models,Ports,Validators}
Infrastructure/AttributeDefinitionDirectory.cs
Endpoints/Admin/Attributes/Definitions/CatalogAttributeDefinitionAdminEndpoints.cs
```

## Migration order

1. Disposition map (this folder)
2. Error codes + catalog + resx
3. Port + AttributeDefinitionDirectory (Result)
4. CQRS + validators
5. Catalog.Endpoints map + module registration
6. CatalogDirectory thin wrappers
7. Strip seven routes/records from Host
8. Guards + SoT + focused tests
9. Builds/tests → commit → push
