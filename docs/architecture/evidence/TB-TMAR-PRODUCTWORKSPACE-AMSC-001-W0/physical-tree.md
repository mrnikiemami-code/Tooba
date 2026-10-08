# TB-TMAR-PRODUCTWORKSPACE-AMSC-001-W0 — Physical Tree (before)

Baseline: `branch = main`, `HEAD == origin/main == eb116af3`.

## Module: `src/backend/Modules/ProductWorkspace`

```text
Tooba.ProductWorkspace.Contracts/
  ProductWorkspaceContractsMarker.cs                       (root, 8 LOC)
  Tooba.ProductWorkspace.Contracts.csproj

Tooba.ProductWorkspace.Domain/
  Tooba.ProductWorkspace.Domain.csproj                     (no production .cs)

Tooba.ProductWorkspace.Application/
  Composition/
    Grid/AdminProductGridQueryPolicy.cs                    (118 LOC)
    Models/.gitkeep
    Models/ProductWorkspaceModels.cs                       (164 LOC)
    Ports/.gitkeep
    Queries/.gitkeep
    Queries/GetProductWorkspaceHandler.cs                  (191 LOC)
    Queries/GetProductWorkspaceQuery.cs                    (8 LOC)
    Queries/ListProductWorkspaceHandler.cs                 (29 LOC)
    Queries/ListProductWorkspaceQuery.cs                   (7 LOC)
    Queries/ProductWorkspaceListComposer.cs                (82 LOC)
    Queries/QueryProductWorkspaceGridHandler.cs            (47 LOC)
    Queries/QueryProductWorkspaceGridQuery.cs              (8 LOC)
    Validators/.gitkeep
  Tooba.ProductWorkspace.Application.csproj

Tooba.ProductWorkspace.Endpoints/
  Admin/IProductWorkspaceAdminAuthorizer.cs                (25 LOC)
  ProductWorkspaceEndpointModule.cs                        (432 LOC, 17 routes)
  Tooba.ProductWorkspace.Endpoints.csproj

Tooba.ProductWorkspace.Infrastructure/
  ProductWorkspaceModule.cs                                (22 LOC, intentional no-op)
  Tooba.ProductWorkspace.Infrastructure.csproj
```

Production `.cs` files: **13**. Largest: `ProductWorkspaceEndpointModule.cs` 432 LOC (below the 800 LOC `ARCH-SIZE-001` ceiling, no baseline entry).

### Empty-folder ceremony (structural debt)

`Application/Composition/{Models,Ports,Queries,Validators}` currently contain `.gitkeep` placeholders for folders that hold no production file except `Models/` and `Queries/`. `Application/Composition/Validators/.gitkeep` and `Application/Composition/Ports/.gitkeep` are empty decoration.

### Physical-Copy-State

`CLEAN` — one authoritative home per type, no stale/duplicate copy inside the module.

### Solution-Explorer-State

`CANONICAL` — `src/backend/Tooba.slnx` lines 126–131 declare `Folder Name="/Modules/ProductWorkspace/"` with exactly the 5 module projects. Assembly names and project paths are canonical.

## Touched destination surface: `src/backend/Modules/Catalog`

W1 must touch Catalog because it is the natural owner of the ProductWorkspace write capability. Exact files in scope:

```text
Tooba.Catalog.Contracts/
  Errors/CatalogErrorCatalogContributor.cs      (already owns workspace.* descriptors)
  Errors/CatalogErrorCodes.cs                   (already declares workspace.* codes)
  Errors/CatalogErrorResourceSet.cs
  Ports/CatalogAdminProductWorkspaceReadContracts.cs   (existing read boundary)
  Ports/CatalogAdminProductWorkspaceListContracts.cs   (existing list boundary)
  Resources/CatalogErrors.resx / CatalogErrors.fa.resx (13 workspace.* keys each)

Tooba.Catalog.Application/
  ProductIdentity/{Commands,Models,Ports}
  ProductPublishing/{Commands,Models,Ports}
  ProductTaxonomy/{Commands,Models,Ports}
  Variants/{Commands,Models,Ports,Validators}
  ProductIdentity/Commands/CreateWorkspaceProductCommand.cs + 12 sibling workspace commands
  Models/ProductIdentityWriteModels.cs / ProductTaxonomyWriteModels.cs / Variants/ProductVariantWriteModels.cs

Tooba.Catalog.Infrastructure/
  CatalogModule.cs                                       (gateway registration seam)
  Directories/CatalogAdminProductWorkspaceReadGateway.cs
  Directories/CatalogAdminProductWorkspaceListGateway.cs
  Directories/ProductIdentityDirectory.cs
  Directories/ProductTaxonomyDirectory.cs
  Directories/ProductLifecycleDirectory.cs
  Directories/ProductVariantDirectory.cs
```

## Host surface (must stay `ALLOWED_COMPOSITION_ROOT`)

```text
src/backend/Host/Tooba.Host/Program.cs
  :50   using Tooba.ProductWorkspace.Endpoints;
  :197  typeof(Tooba.ProductWorkspace.Application.Composition.Queries.GetProductWorkspaceQuery).Assembly
  :245  builder.Services.AddProductWorkspaceEndpointPresentation();
  :416  app.MapProductWorkspaceModuleEndpoints();
src/backend/Host/Tooba.Host/Composition/ToobaModuleComposition.cs
  :80   new ProductWorkspaceModule(),
src/backend/Host/Tooba.Host/Tooba.Host.csproj
  :76-78 Infrastructure + Endpoints + Application project references
```

Host has **zero** `Host/Admin/ProductWorkspace*.cs` files (W32 shell-final closure). `Tooba.MigrationRunner/ModuleMigrationRegistry.cs` has no ProductWorkspace entry (no schema).
