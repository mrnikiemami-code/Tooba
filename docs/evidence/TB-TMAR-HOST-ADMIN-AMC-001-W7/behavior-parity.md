# Behavior parity — W7 Categories

| Concern | State |
|---|---|
| Route methods/paths (10) | PRESERVED |
| 9 Admin auth via ICatalogAdminAuthorizer | PRESERVED |
| Storefront resolve unauthenticated | PRESERVED |
| Tree locale required + search/ancestors | PRESERVED |
| Workspace missing → 404 category.missing | PRESERVED |
| Create structured Translations | PRESERVED |
| Create legacy LocalizedNames fallback | PRESERVED |
| Create 201 + CategoryReference | PRESERVED (`ApiResponseFactory.Created`) |
| Update core + media clear/set + ExpectedUpdatedAt | PRESERVED |
| Update/move/publish/archive return workspace | PRESERVED |
| Translation upsert + slug history | PRESERVED |
| Reorder → `{ ok: true }` | PRESERVED (`CategoryOkResult`) |
| Resolve current / historical redirect / storefront eligibility | PRESERVED |
| Duplicate slug → 409 catalog.category.slug.duplicate | PRESERVED (typed) |
| Tenant isolation + mutation guard | PRESERVED |
| Schema / frontend | UNCHANGED |
| StoreAppearance | NOT MOVED |
