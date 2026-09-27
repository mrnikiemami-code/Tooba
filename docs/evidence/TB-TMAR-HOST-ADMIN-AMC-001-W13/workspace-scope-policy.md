# Workspace-scope policy — W13

## Live behavior (Host before)

`X-Tooba-Workspace-Scope=view` → `ReadPermissions` → `CanEditCatalog=false` → write methods call `EnsureCatalogEdit` → `workspace.permission.denied` 403.

Reads (list/readiness) do **not** call EnsureCatalogEdit.

## W13 implementation

Module-owned `CatalogWorkspaceMediaScope.AllowsCatalogEdit(HttpRequest)` in Catalog.Endpoints:

- `view` (case-insensitive) → false
- otherwise → true

Applied on six write routes before MediatR. Failure → `ApiResponseFactory.FromFailure(workspace.permission.denied)`.

Admin panel auth remains `ICatalogAdminAuthorizer` (canonical). No Host `ProductWorkspacePermissions` type copied into Catalog.

## Classification

PRESERVED_LIVE_TRANSPORT_POLICY via smallest module-owned endpoint helper. Not removed.
