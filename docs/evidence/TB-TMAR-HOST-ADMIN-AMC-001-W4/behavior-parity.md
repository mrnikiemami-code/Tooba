# Behavior parity — W4 Tags

Preserved:

- Routes/methods/paths for all 9 operations
- Success JSON = `TagView` shape
- Auth via Admin panel seam (`ICatalogAdminAuthorizer`)
- Default list locale `fa-IR` when blank
- Create NameFa → `fa-IR`, NameEn → `en` overlay into LocalizedNames
- Locale default `fa-IR` on create
- Generated unique code when Code omitted; Slugify + suffix uniqueness
- Explicit duplicate code rejection
- List order by Code, Take(200), search Name or Code (ordinal ignore-case)
- Locale fallback: exact → fa* → first → Code
- Assign returns list at `fa-IR`; remove idempotent; list after mutate
- Product/category tag list ordered by Code
- Mutation guard seam
- Schema/migrations/frontend unchanged

Documented normalizations:

- Duplicate code → distinct `catalog.tag.code.duplicate` (was `catalog.tag.invalid`)
- Get missing → `catalog.tag.missing` via ApiResponseFactory (was bare 404)
- Missing product/category/tag on assign → dedicated missing codes (was wrongly assign.duplicate)
- Host `CatalogActorHttpBinding` not copied: Tag mutations do not write actor history fields; commerce isolation via Catalog DbContext (same as Units/Quantity)
