# W18 — Authorization + workspace-scope boundary

## Authorization

### Created (zero consumers)

| Item | Value |
|---|---|
| Interface | `IProductWorkspaceAdminAuthorizer` |
| Implementation | `ProductWorkspaceAdminAuthorizer` |
| Path | `...Endpoints/Admin/IProductWorkspaceAdminAuthorizer.cs` |
| Seam | `Tooba.BuildingBlocks.Security.IAdminPanelAccess` (Host remains implementer) |
| Registration | `AddProductWorkspaceEndpointPresentation` (not called from Host in W18) |

Mirrors Catalog’s `ICatalogAdminAuthorizer` pattern. No security business policy in Application. No route consumes it yet.

### Host residual (unchanged)

Host ProductWorkspace routes still use `AdminPanelAccess.RequireAuthorizedAsync` directly.

## Workspace scope

### Current Host behavior

Header `X-Tooba-Workspace-Scope=view` → view-only `ProductWorkspacePermissions` via Host `ProductWorkspaceEndpoints.ReadPermissions`.

### Canonical future owner (decision)

**ProductWorkspace.Endpoints transport policy** (module-owned parser/policy when routes migrate).

### W18 action

**Document only.** Do not move/remove Host `ReadPermissions`. Do not create a duplicate runtime parser that Host would also execute.

## Error boundary

No ProductWorkspace error-code catalog created in W18 (avoid placeholders). Catalog-owned codes stay in `CatalogErrorCodes`. Composition/transport errors will be ProductWorkspace-owned only when real W19 codes are known.
