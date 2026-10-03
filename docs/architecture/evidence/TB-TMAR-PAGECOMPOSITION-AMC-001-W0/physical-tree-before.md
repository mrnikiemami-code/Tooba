# PageComposition physical tree (before AMC-001)

```
Modules/PageComposition/
  Tooba.PageComposition.Domain/
    PageCompositionEntities.cs          # PageKeys, TenantIds, SectionCatalog, PageDefinition, PageSection
  Tooba.PageComposition.Application/
    PageCompositionContracts.cs         # models + IPageCompositionDirectory
    PageCompositionFailureMapper.cs     # message.Contains → SemanticException
    Commands/AdminHomeCompositionCommands.cs
    Queries/HomeCompositionQueries.cs
    Validators/PageCompositionValidators.cs
    Presentation/PageCompositionPresentationComposer.cs
  Tooba.PageComposition.Contracts/
    Errors/PageCompositionErrorCodes.cs
  Tooba.PageComposition.Infrastructure/
    PageCompositionModule.cs
    PageCompositionDirectory.cs
    PageCompositionDevelopmentSeed.cs
    Persistence/PageCompositionDbContext.cs
    Migrations/…                        # root Migrations (non-canonical)
  Tooba.PageComposition.Endpoints/
    PageCompositionEndpointModule.cs
    PageCompositionHttpErrors.cs
    Admin/… Storefront/… Models/
    Errors/ + Resources/
```

slnx: Domain/Application/Infrastructure under flat `/Modules/`; Contracts + Endpoints absent.
