# MegaMenu capability — W5

## Structure

```
Tooba.Catalog.Application/MegaMenu/
  Commands/   UpsertCategoryMegaMenu*, RemoveCategoryMegaMenu*
  Queries/    GetCategoryMegaMenu*, ListMegaMenuPlacementOptions*, GetStorefrontMegaMenu*
  Models/     MegaMenuSharedModelNote (views/inputs stay Application-root with ICatalogDirectory)
  Ports/      IMegaMenuDirectory
  Validators/ UpsertCategoryMegaMenuCommandValidator

Tooba.Catalog.Endpoints/
  Admin/MegaMenu/CatalogMegaMenuAdminEndpoints.cs
  Storefront/MegaMenu/CatalogMegaMenuStorefrontEndpoints.cs

Tooba.Catalog.Infrastructure/MegaMenuDirectory.cs
```

## Persistence

- Port: `IMegaMenuDirectory`
- Impl: `MegaMenuDirectory` (CatalogDbContext + Domain tree rules/composer)
- Legacy: `CatalogDirectory` MegaMenu members are thin one-way wrappers (`MegaMenuPort()`)

## Auth

- 4 Admin routes: `ICatalogAdminAuthorizer`
- Storefront GET: no Admin authorizer (public read)
