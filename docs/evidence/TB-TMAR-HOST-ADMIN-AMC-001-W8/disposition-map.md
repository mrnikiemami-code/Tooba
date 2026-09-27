# Disposition map — W8 Attribute Definition Admin (member-level)

Source: `Admin/CatalogAttributeEndpoints.cs` (partial evacuation).  
Host file is **RETAINED_PARTIAL_FOR_LATER_WAVES** (53 → 53).

## Map registration

| Member | Classification | Destination |
|---|---|---|
| `MapGroup("/v1/admin/catalog/attribute-definitions")` + 7 route maps | MOVE_W8 / REPLACE_WITH_MODULE_ENDPOINT | `Catalog.Endpoints/Admin/Attributes/Definitions/CatalogAttributeDefinitionAdminEndpoints.cs` |
| `MapGroup(".../attribute-schema")` + 5 routes | RETAIN_FOR_W9_PLUS | Host |
| `MapGroup(".../products/{productId}")` + 11 routes | RETAIN_FOR_W9_PLUS | Host |
| Program `MapCatalogAttributeEndpoints()` | RETAIN | Host still maps retained groups |

## Definition HTTP methods (MOVE_W8)

| Member | Classification | Destination |
|---|---|---|
| `ListDefinitionsAsync` | MOVE_W8 | `ListAttributeDefinitionsQuery` + handler |
| `GetDefinitionAsync` | MOVE_W8 | `GetAttributeDefinitionQuery` + handler |
| `CreateDefinitionAsync` | MOVE_W8 | `CreateAttributeDefinitionCommand` (+ optional metadata atomic) |
| `UpdateDefinitionAsync` | MOVE_W8 | `UpdateAttributeDefinitionCommand` |
| `PreviewVariantAxisCapabilityDisableAsync` | MOVE_W8 | `PreviewVariantAxisCapabilityDisableQuery` |
| `SetVariantAxisCapabilityAsync` | MOVE_W8 | `SetVariantAxisCapabilityCommand` |
| `AddOptionAsync` | MOVE_W8 | `AddAttributeOptionCommand` |

## Transport records

| Record | Classification | Notes |
|---|---|---|
| `CreateAttributeDefinitionRequest` | MOVE_W8 | → Application Models / Command |
| `UpdateAttributeDefinitionRequest` | MOVE_W8 | Definition-only (create metadata + update body) |
| `SetVariantAxisCapabilityRequest` | MOVE_W8 | → Command |
| `AddAttributeOptionRequest` | MOVE_W8 | → Command |
| `BindCategoryAttributeRequest` … `CategoryChangePreviewRequest` | RETAIN_FOR_W9_PLUS | Host |

## Helpers

| Member | Classification | Notes |
|---|---|---|
| `MapAttributeInvalid` | RETAIN_FOR_W9_PLUS | Still required by retained Host Attribute routes |
| `MapCategoryChangeInvalid` | RETAIN_FOR_W9_PLUS | Host |
| `ToError` | RETAIN_FOR_W9_PLUS | Host retained routes still catch PlatformHttpException |
| `EnrichVariantEditorWithOfferCountsAsync` | RETAIN_FOR_W9_PLUS | Offer.Contracts used **only** by retained variant editor/preview/apply |

## ICatalogDirectory Definition members

| Method | Classification | Destination |
|---|---|---|
| `ListAttributeDefinitionsAsync` | EXTRACT → focused port; thin wrapper residual | `IAttributeDefinitionDirectory` / `AttributeDefinitionDirectory` → `Result`; CatalogDirectory Unwrap wrapper |
| `GetAttributeDefinitionAsync` | same | same |
| `CreateAttributeDefinitionAsync` | same | same (+ create+metadata atomic on focused Create) |
| `UpdateAttributeDefinitionAsync` | same | same |
| `PreviewVariantAxisCapabilityDisableImpactAsync` | same | same (Catalog-only impact; **no Offer**) |
| `SetAttributeDefinitionVariantAxisCapabilityAsync` | same | same |
| `AddAttributeOptionAsync` | same | same |
| Category-schema / product attribute / variant methods | RETAIN | CatalogDirectory (later waves) |

## Domain / Offer / Auth

| Concern | Classification |
|---|---|
| `CatalogAttributeDefinition` / `CatalogAttributeOption` / `CatalogCategoryAttributeAssignmentRules` | DOMAIN_AUTHORITY preserved; directory pre-checks → typed Result (no message parse) |
| Offer.Contracts on Definition 7 routes | NONE — Offer unused by W8 routes |
| `AdminPanelAccess` on 7 routes | REPLACE_WITH_MODULE_ENDPOINT → `ICatalogAdminAuthorizer` |
| PlatformHttpException / IOE catch on 7 routes | REMOVE_DEAD on moved surface → Result + ApiResponseFactory |

## Host metrics

| Metric | Before | After |
|---|---|---|
| Host/Admin recursive `*.cs` | 53 | 53 |
| Definition route mappings in Host | 7 | 0 |
| `CatalogAttributeEndpoints.cs` | present | RETAINED_PARTIAL_FOR_LATER_WAVES |

## Validator matrix (W8)

| Request | Classification |
|---|---|
| `ListAttributeDefinitionsQuery` | NO_VALIDATOR_REQUIRED |
| `GetAttributeDefinitionQuery` | NO_VALIDATOR_REQUIRED |
| `CreateAttributeDefinitionCommand` | VALIDATOR_REQUIRED (Code non-blank) |
| `UpdateAttributeDefinitionCommand` | NO_VALIDATOR_REQUIRED |
| `PreviewVariantAxisCapabilityDisableQuery` | NO_VALIDATOR_REQUIRED |
| `SetVariantAxisCapabilityCommand` | NO_VALIDATOR_REQUIRED |
| `AddAttributeOptionCommand` | VALIDATOR_REQUIRED (Code non-blank) |

## Final disposition

`READY_TO_MIGRATE` — seven-route Attribute Definition Admin slice only.
