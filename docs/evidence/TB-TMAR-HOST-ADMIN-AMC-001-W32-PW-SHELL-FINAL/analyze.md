# W32-PW-SHELL-FINAL analyze

## Source (Host Admin residue)
- ProductWorkspaceEndpoints.cs (0 Map routes; no-op shell)
- ProductWorkspaceComposer.cs (residual composition after W26–W31 evacuation)
- ProductWorkspaceModels.cs (marker only)
- ProductWorkspaceDevelopmentBootstrap.cs (Host development seed/migrate)
- CatalogActorHttpBinding.cs (unused after PW Host endpoints deleted)

## Destination / disposition
| Responsibility | Action |
|---|---|
| Endpoints / Composer / Models | DELETE from Host Admin |
| CatalogActorHttpBinding | DELETE (only used by deleted PW Host endpoints) |
| Development bootstrap | Relocate → `Host/Development/ProductWorkspaceDevelopmentBootstrap.cs` (`Tooba.Host.Development`) |
| Module HTTP | KEEP `MapProductWorkspaceModuleEndpoints` + `AddProductWorkspaceEndpointPresentation` |

## Out of scope
StoreAppearance*, HoldPolicy*, template seeds, Merchandising (W33 done)
