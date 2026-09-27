# CQRS — W5 MegaMenu

| Request | Handler | Port method | Endpoint |
|---|---|---|---|
| `GetCategoryMegaMenuQuery` | GetCategoryMegaMenuHandler | GetCategoryConfigurationAsync | Admin GET mega-menu |
| `ListMegaMenuPlacementOptionsQuery` | ListMegaMenuPlacementOptionsHandler | ListPlacementOptionsAsync | Admin GET placement-options |
| `UpsertCategoryMegaMenuCommand` | UpsertCategoryMegaMenuHandler | UpsertBindingAsync | Admin PUT mega-menu |
| `RemoveCategoryMegaMenuCommand` | RemoveCategoryMegaMenuHandler | RemoveBindingAsync | Admin DELETE mega-menu |
| `GetStorefrontMegaMenuQuery` | GetStorefrontMegaMenuHandler | GetStorefrontMenuAsync | Storefront GET mega-menu |

All five HTTP operations are MediatR/`ISender`-backed. Endpoints inject no directory/DbContext.
