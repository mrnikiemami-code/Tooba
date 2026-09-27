# Attribute Definition capability — W8

## Owned surface

Admin Attribute Definition administration:

- list / get / create (+ optional metadata) / update metadata
- variant-axis capability disable-preview / set
- add enumeration option

## Application structure

```
Tooba.Catalog.Application/Attributes/Definitions/
  Commands/
  Queries/
  Models/
  Ports/
  Validators/
```

Exact path ↔ namespace. No `*Contracts.cs` bundle. No root Application Attribute dump.

## Persistence

`IAttributeDefinitionDirectory` + `AttributeDefinitionDirectory`

Focused methods only for the seven Definition operations. Legacy `ICatalogDirectory` Definition methods are thin Unwrap wrappers (residual debt for CatalogDemo / existing tests).

## HTTP

`Catalog.Endpoints/Admin/Attributes/Definitions/CatalogAttributeDefinitionAdminEndpoints.cs`

Pipeline: `ICatalogAdminAuthorizer` → `ISender` → Command/Query → focused port → `Result` → `ApiResponseFactory`.

Endpoints inject no directory / DbContext / Offer.

## Domain authority

`CatalogAttributeDefinition`, `CatalogAttributeOption`, `CatalogCategoryAttributeAssignmentRules.ValueKindSupportsVariantAxis` remain authoritative. Directory pre-checks emit typed `CatalogErrorCodes` (no IOE message parse on the moved surface).
