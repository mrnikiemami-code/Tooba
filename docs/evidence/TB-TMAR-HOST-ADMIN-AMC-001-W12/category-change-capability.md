# Category-change capability — W12

## Ownership

Catalog owns:

- category-change impact preview
- primary-category replacement/migration
- write/read models (`CategoryChangeWriteModel`, `CategoryChangePreviewWriteModel`)
- focused port `ICategoryChangeDirectory` / `CategoryChangeDirectory`
- stable errors (`catalog.category.assignment.level.invalid`, `catalog.category_change.invalid`, reuse product/category missing)
- both HTTP routes under `Catalog.Endpoints/Admin/CategoryChanges`

Host owns ZERO Catalog Attribute/category-change HTTP after W12 (`CatalogAttributeEndpoints.cs` DELETED).

## Structure

```
Application/CategoryChanges/{Commands,Queries,Models,Ports,Validators}
Endpoints/Admin/CategoryChanges/CatalogProductCategoryChangeAdminEndpoints.cs
Infrastructure/CategoryChangeDirectory.cs
```

No `*Contracts.cs` bundle. Exact path↔namespace.
