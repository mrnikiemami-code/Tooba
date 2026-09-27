# ProductSeo capability — W14

## Application structure

```
Tooba.Catalog.Application/ProductSeo/
  Commands/ UpdateProductSeoCommand.cs, UpdateProductSeoHandler.cs
  Queries/  GetProductSeoQuery.cs, GetProductSeoHandler.cs,
            GetProductSeoReadinessQuery.cs, GetProductSeoReadinessHandler.cs
  Models/   ProductSeoModels.cs (write + detail + readiness views)
  Ports/    IProductSeoDirectory.cs
  Validators/ UpdateProductSeoCommandValidator.cs
```

No `*Contracts.cs` bundle. Exact path↔namespace.

## Persistence

- `IProductSeoDirectory` / `ProductSeoDirectory`
- Extracted from `CatalogDirectory` SEO methods/helpers
- Legacy `ICatalogDirectory` SEO methods remain as one-way `UnwrapSeo` wrappers for ProductSeoTests / ProductPublishPrep / publish readiness
- Single `SaveChangesAsync` after localized upsert + slug/SeoTitleSeam + history

## Domain authority retained

- `ProductSeoRules.NormalizeLocale` / `Evaluate` / `BuildPublicPath`
- `CatalogCategorySlugNormalizer.NormalizeSlug` / `SlugifyFromName`
- fa-IR `SeoTitleSeam` compatibility
- `ProductHistoryRules.EventSeoChanged`
