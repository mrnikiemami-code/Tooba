# Quantity settings — W2

Moved to Catalog.Endpoints Admin/Settings/QuantitySettingsEndpoints.cs.
- GET → GetStoreQuantitySettingsQuery (ISender)
- PUT → SaveStoreQuantitySettingsCommand → Result<StoreQuantitySettingsView>
- Auth → ICatalogAdminAuthorizer
- Errors → CatalogErrorCodes.QuantityRoundingInvalid + catalog/resources
- Directory Get/Save via IStoreQuantitySettingsDirectory (no endpoint gateway/DbContext)
- Host MapQuantitySettingsEndpoints removed; Catalog MapCatalogModuleEndpoints registers route
- Success JSON raw via ApiResponseFactory.From (GlobalRoundingMode/LabelFa/LabelEn preserved)
