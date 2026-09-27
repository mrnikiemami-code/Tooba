# Workspace scope policy — W14

## Preserved Host semantics

Header `X-Tooba-Workspace-Scope=view`:

| Route | Allowed |
|---|---|
| GET SEO | yes (CanView) |
| GET SEO readiness | yes (CanView) |
| PUT SEO | no → `workspace.permission.denied` 403 |

## Module ownership

- Shared helper: `Catalog.Endpoints.Admin.CatalogWorkspaceScope.AllowsCatalogEdit`
- W13 `CatalogWorkspaceMediaScope` retained as thin alias delegating to shared helper (W13 guards preserved)
- ProductSeo PUT uses `CatalogWorkspaceScope`
- Do not copy Host `ProductWorkspacePermissions` into Catalog Application
