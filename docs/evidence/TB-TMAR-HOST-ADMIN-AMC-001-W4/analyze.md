# Analyze — TB-TMAR-HOST-ADMIN-AMC-001-W4

## Target

`src/backend/Host/Tooba.Host/Admin/CatalogTagEndpoints.cs` — 9 Admin Catalog tag routes.

## Ownership verdict

True owner = **Catalog**. Host currently owns HTTP + AdminPanelAccess + CatalogActorHttpBinding + IOE→Problem mapping. Persistence/use-case authority already lives in `CatalogDirectory` tag members (god-directory debt).

## Routes (preserve exactly)

| Method | Path |
|---|---|
| GET | `/v1/admin/catalog/tags/` |
| POST | `/v1/admin/catalog/tags/` |
| GET | `/v1/admin/catalog/tags/{tagId:guid}` |
| GET | `/v1/admin/catalog/products/{productId:guid}/tags/` |
| POST | `/v1/admin/catalog/products/{productId:guid}/tags/{tagId:guid}` |
| DELETE | `/v1/admin/catalog/products/{productId:guid}/tags/{tagId:guid}` |
| GET | `/v1/admin/catalog/categories/{categoryId:guid}/tags/` |
| POST | `/v1/admin/catalog/categories/{categoryId:guid}/tags/{tagId:guid}` |
| DELETE | `/v1/admin/catalog/categories/{categoryId:guid}/tags/{tagId:guid}` |

## Current violations

- Host HTTP ownership of Catalog Admin tags
- Direct `ICatalogDirectory` from Host endpoints
- `AdminPanelAccess` + Host `CatalogActorHttpBinding` coupling
- Expected failures via `InvalidOperationException` + `Results.Problem` with Persian message as title
- Hard-coded `errorCode` extensions; duplicate-code and missing-name both mapped to `catalog.tag.invalid`
- Assign endpoints catch all IOE (including EF `SingleAsync` misses) as `catalog.tag.assign.duplicate`
- Tag capability not structured as Commands/Queries/Models/Ports/Validators

## Behavior to preserve

- Create: NameFa/NameEn overlay into LocalizedNames; Locale default `fa-IR`; generated unique code when Code omitted; explicit duplicate code rejection
- List: order by Code, Take(200), search Name or Code (case-insensitive)
- Get: null → NotFound
- Product/category assign: duplicate rejection; list after mutate with `fa-IR`
- Remove: idempotent when assignment absent
- Locale fallback: exact locale → fa* → first row → Code
- Mutation guard seam (`EnsureCanMutateAsync`)
- Success JSON = `TagView` shape
- Auth: Admin panel authorization (module `ICatalogAdminAuthorizer`)

## Actor binding

Tag mutations call `_guard.EnsureCanMutateAsync` only; they do **not** write product-history actor fields. Units/Quantity already ship without Host `CatalogActorHttpBinding`. Preserve commerce/tenant isolation via Catalog DbContext. Do **not** copy Host filter into Catalog.Endpoints. Actor binding = not required for Tag slice (documented).

## Canonical target

- Endpoints: `Catalog.Endpoints/Admin/Tags/CatalogTagEndpoints.cs`
- Application: `Tags/{Commands,Queries,Models,Ports,Validators}`
- Persistence: focused `ITagDirectory` + `TagDirectory`
- Result + `ApiResponseFactory` + `CatalogErrorCodes` + resx
- Auth: `ICatalogAdminAuthorizer`
- Legacy `ICatalogDirectory` tag methods remain for CatalogDemo/non-HTTP callers; delegate to `ITagDirectory` where safe

## Out of scope

StoreAppearance*, other Catalog Admin Host files, W5, schema/migrations/frontend.
