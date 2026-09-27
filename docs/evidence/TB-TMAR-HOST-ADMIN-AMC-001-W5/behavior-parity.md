# Behavior parity — W5 MegaMenu

| Behavior | State |
|---|---|
| Routes/methods (5) | PRESERVED |
| Locale default `fa-IR` | PRESERVED (handlers) |
| Locale normalize | PRESERVED (`CatalogCategorySlugNormalizer`) |
| Unbound category preview config | PRESERVED |
| Display title fallback | PRESERVED |
| Parent path / presentation level | PRESERVED |
| Placement options exclude self + MaxPresentationDepth filter + SortOrder | PRESERVED |
| Upsert create/update + translation overrides | PRESERVED |
| Remove absent = idempotent | PRESERVED |
| Remove with children = rejected | PRESERVED→canonical `catalog.megamenu.remove.has_children` |
| Category missing | PRESERVED→canonical `catalog.megamenu.category.missing` (was IOE) |
| Invalid placement | PRESERVED→canonical `catalog.megamenu.placement.invalid` (was Persian IOE→400) |
| Storefront composition/publication/visibility | PRESERVED (Domain composer) |
| Admin auth | PRESERVED via `ICatalogAdminAuthorizer` |
| Storefront unauthenticated | PRESERVED |
| Success JSON / 204 | PRESERVED via ApiResponseFactory |
| Schema / frontend | UNCHANGED |
