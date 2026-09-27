# W36-STORE-APPEARANCE analyze

## Source (Host)
- Admin/StoreAppearanceSettingsEndpoints.cs (GET/PUT `/v1/admin/settings/appearance`)
- Admin/StoreAppearanceSettingsComposer.cs
- Storefront/StoreAppearanceProjection.cs (`StoreAppearanceProjection` + `StoreAppearanceProjector`)

## Destination
| Responsibility | Destination |
|---|---|
| Admin HTTP same routes | Catalog.Endpoints Admin.Settings.StoreAppearanceSettingsEndpoints |
| MediatR GET | Catalog.Application Settings/StoreAppearance GetStoreAppearanceAdminSettingsQuery |
| MediatR PUT | existing SaveStoreAppearanceSettingsCommand (+ cache invalidate) |
| Projector + projection | Catalog.Infrastructure/StoreAppearance/StoreAppearanceProjector |
| Port | Catalog.Application IStoreAppearanceProjector |
| Auth / Result | ICatalogAdminAuthorizer + ApiResponseFactory |

## Out of scope
Host Admin platform KEEP (~15 files)
