# ProductHistory read capability — W15

## Structure

```
Tooba.Catalog.Application/ProductHistory/
  Queries/GetProductHistoryQuery.cs
  Queries/GetProductHistoryHandler.cs
  Models/ProductHistoryModels.cs
  Ports/IProductHistoryReader.cs
  Validators/   (empty — NO_VALIDATOR_REQUIRED)

Tooba.Catalog.Infrastructure/ProductHistoryReader.cs
Tooba.Catalog.Endpoints/Admin/ProductHistory/CatalogProductHistoryAdminEndpoints.cs
```

## Ownership

- Catalog owns Admin history page read HTTP + paging/filter use-case + focused read persistence seam.
- Host retains aggregate Activity/Audit shell (`BuildHistoryShellListsAsync`) and `ProductHistoryItem`.
- History writes (`AppendProductHistoryAsync` / `QueueProductHistory`) not centralized in W15.

## Focused reader responsibilities

- Product existence → `workspace.product.missing`
- Section trim/exact filter
- skip/take normalization
- OccurredAt DESC, HistoryId DESC
- ActorSystemFa fallback + SectionLabelFa
- Map to `ProductHistoryPage` / entry DTOs

## Legacy wrapper

`ICatalogDirectory.ListProductHistoryAsync` delegates one-way to `IProductHistoryReader` and unwraps missing-product to prior IOE for:
- Host `BuildHistoryShellListsAsync`
- `ProductHistoryTests`
- `PrimaryCategoryMigrationTests`
