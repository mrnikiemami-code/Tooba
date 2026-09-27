# Closure — W31 ProductWorkspace list/grid

- Host list + grid routes removed (MapProductWorkspaceEndpoints no-op shell)
- ProductWorkspace.Endpoints owns GET `/` + POST `/query` (module **15→17**); Host PW routes **2→0**
- CatalogAdminProductWorkspaceListGateway + engine; AdminProductListItem + grid policy in ProductWorkspace.Application
- Admin **31** unchanged; Merchandising untouched; Host ProductWorkspace* shells RETAIN until W32
