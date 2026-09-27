# W18 — Project structure

## Module family (exactly once)

```text
src/backend/Modules/ProductWorkspace/
  Tooba.ProductWorkspace.Contracts/
    ProductWorkspaceContractsMarker.cs
  Tooba.ProductWorkspace.Domain/          (empty-but-valid; no .cs)
  Tooba.ProductWorkspace.Application/
    Composition/
      Models/.gitkeep
      Ports/.gitkeep
      Queries/.gitkeep
      Validators/.gitkeep
  Tooba.ProductWorkspace.Infrastructure/
    ProductWorkspaceModule.cs
  Tooba.ProductWorkspace.Endpoints/
    ProductWorkspaceEndpointModule.cs
    Admin/IProductWorkspaceAdminAuthorizer.cs
```

## Path ↔ namespace (exact)

| Path | Namespace |
|---|---|
| `...Contracts/ProductWorkspaceContractsMarker.cs` | `Tooba.ProductWorkspace.Contracts` |
| `...Infrastructure/ProductWorkspaceModule.cs` | `Tooba.ProductWorkspace.Infrastructure` |
| `...Endpoints/ProductWorkspaceEndpointModule.cs` | `Tooba.ProductWorkspace.Endpoints` |
| `...Endpoints/Admin/IProductWorkspaceAdminAuthorizer.cs` | `Tooba.ProductWorkspace.Endpoints.Admin` |

## Root allowlists (preCert)

- Contracts: `ProductWorkspaceContractsMarker.cs`
- Domain: (none — empty)
- Application: (none — Composition folders only)
- Infrastructure: `ProductWorkspaceModule.cs`
- Endpoints: `ProductWorkspaceEndpointModule.cs`

## Solution grouping

`src/backend/Tooba.slnx` → `/Modules/ProductWorkspace/` contains the five projects exactly once.

## Manifest / certification

- `preCertModules` entry for ProductWorkspace with `structureCertified: false`
- **NOT** added to `structureLock.certifiedModules`
- W18 does **not** claim ARCH-COMPLETE-002

## Host integration (behavior-neutral)

- `ProductWorkspaceModule` registered in `ToobaModuleComposition` with empty `AddServices`
- Host references `Tooba.ProductWorkspace.Infrastructure` only
- Host does **not** call `MapProductWorkspaceModuleEndpoints` or `AddProductWorkspaceEndpointPresentation`
- Host still calls `MapProductWorkspaceEndpoints()` (19 routes)

## Composition folders for W19

Empty `.gitkeep` under `Application/Composition/{Models,Ports,Queries,Validators}` reserved for the immediately-next W19 aggregate-read structure. No speculative commands or duplicate Host DTOs.
