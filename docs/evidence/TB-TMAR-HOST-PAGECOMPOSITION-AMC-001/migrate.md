# Migrate — Host/PageComposition AMC-001

## Waves

1. **Foundation**: `Tooba.PageComposition.Contracts` (Errors), `Tooba.PageComposition.Endpoints` (Storefront/Admin)
2. **CQRS**: Queries/Commands over `PageCompositionPresentationComposer` (moved from Host)
3. **Auth seam**: `IPageCompositionAdminAuthorizer` → `IAdminPanelAccess`
4. **Errors**: stable codes + catalog/resources; Application `PageCompositionFailureMapper` (Endpoints use `ApiResponseFactory`, no message classification)
5. **Host ZERO**: deleted `Host/PageComposition`; Program maps `MapPageCompositionModuleEndpoints`

## Behavior preserved

- Routes/verbs unchanged
- Error codes unchanged
- Schema/migrations unchanged
- Frontend unchanged
