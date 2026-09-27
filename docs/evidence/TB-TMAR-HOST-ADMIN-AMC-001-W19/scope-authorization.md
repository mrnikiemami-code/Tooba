# W19 — Scope / authorization

## Authorization

GET uses `IProductWorkspaceAdminAuthorizer` (module Endpoints).
Host `AdminPanelAccess` no longer authorizes aggregate GET.

## Workspace scope

Header `X-Tooba-Workspace-Scope` parsed in ProductWorkspace.Endpoints:

- `view` (case-insensitive) → Permissions(true, false, false, false, false)
- otherwise → Permissions(true, true, true, true, true)

GET does not reject edit/publish based on scope; scope only shapes Permissions JSON.

Host write routes retain a local `ReadPermissions` copy until those routes evacuate.
