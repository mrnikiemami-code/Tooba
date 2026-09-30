# Migrate — Host/Story AMC-001

## Waves

1. **Foundation**: `Tooba.Story.Contracts` (Errors), `Tooba.Story.Endpoints` (Storefront/Admin/Seller)
2. **CQRS**: Queries/Commands over `StoryPresentationComposer` (moved from Host)
3. **Auth seams**: `IStoryAdminAuthorizer` (module + IAdminPanelAccess); `IStorySellerAuthorizer` + Host adapter
4. **Host ZERO**: deleted `Host/Story`; Program maps `MapStoryModuleEndpoints`

## Behavior preserved

- Routes/verbs unchanged (public, seller, admin including grid query)
- Error codes: story.missing / cta.rejected / mutation.rejected / tenant.missing / reviewStatus.invalid
- Seed ownership already module-owned (unchanged)
