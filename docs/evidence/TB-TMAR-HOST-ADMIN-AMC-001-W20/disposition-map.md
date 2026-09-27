# disposition-map

| Member | Disposition |
|---|---|
| MapGet brand-options | MOVE → CatalogBrandOptionsAdminEndpoints |
| ListBrandOptionsAsync (Host endpoint) | DELETE |
| ListBrandOptionsAsync / Internal (Composer) | DELETE |
| AdminBrandOption | DELETE (zero consumers) |
| Brand persistence / localization | Catalog BrandOptionReader via CatalogDbContext |

