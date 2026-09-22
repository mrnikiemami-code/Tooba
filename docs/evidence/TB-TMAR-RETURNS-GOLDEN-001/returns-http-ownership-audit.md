# Returns HTTP Ownership Audit

## Before
- Host/Tooba.Host/Returns/ReturnEndpoints.cs owned all `/v1/{customer,seller,admin}/returns*` routes
- Host/Tooba.Host/Returns/ReturnPanelComposer.cs orchestrated IReturnDirectory + IAdminReturnGridQuery + AdminListGridPolicies.Returns

## After
- Tooba.Returns.Endpoints owns routes via ReturnEndpointModule.MapReturnEndpoints()
- Host Program.cs only calls `app.MapReturnEndpoints()` and registers auth adapters
- Host/Returns/ deleted

## Verdict
Returns-HTTP-Ownership: MODULE_ENDPOINTS
