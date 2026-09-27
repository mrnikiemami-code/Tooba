# Category Attribute-Schema capability — W9

## Structure

```
Application/Attributes/Schema/
  Commands/   Bind, Update, Unbind, Reorder (+ handlers)
  Queries/    GetEffective (+ handler)
  Models/     Bind/Update write models, MutationResult
  Ports/      ICategoryAttributeSchemaDirectory
  Validators/ Bind + Reorder only

Infrastructure/CategoryAttributeSchemaDirectory.cs
Endpoints/Admin/Attributes/Schema/CatalogCategoryAttributeSchemaAdminEndpoints.cs
```

## Persistence seam

`ICategoryAttributeSchemaDirectory` / `CategoryAttributeSchemaDirectory` owns effective read + bind/update/unbind/reorder with `Result` + `CatalogErrorCodes`.

`CatalogDirectory` retains thin Unwrap wrappers for CatalogDemo/legacy tests and product/variant callers that still need `GetEffectiveCategorySchemaAsync` / bind helpers via `ICatalogDirectory`.

## Domain

`CatalogCategoryAttributeAssignmentRules` remains authority; directory pre-checks capability/value-kind and returns typed Result (no IOE message-as-code on W9 surface).
`CatalogCategorySchemaResolver` remains inheritance/override authority.
