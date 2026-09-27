# Foldering — W3

```
Tooba.Catalog.Application/Units/
  Commands/   Create|Update|Deactivate Command+Handler
  Queries/    List|Get Query+Handler
  Models/     UnitOfMeasureModels.cs (list/detail/write/result records)
  Ports/      IUnitOfMeasureDirectory.cs
  Validators/ Create|Update validators
```

```
Tooba.Catalog.Endpoints/Admin/Units/UnitOfMeasureEndpoints.cs
```

Eliminated:

- `UnitOfMeasureWriteContracts.cs`
- `UnitOfMeasureWriteHandlers.cs`
- Host `Admin/UnitOfMeasureEndpoints.cs`

Path↔namespace exact under `Units/`. No one-leaf-folder-per-request. No `*Contracts.cs` bundle.
