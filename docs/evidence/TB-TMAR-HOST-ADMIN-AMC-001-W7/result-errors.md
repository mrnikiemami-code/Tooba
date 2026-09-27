# Result / errors — W7 Categories

## Canonical path

Handler → `Result` / `Result<T>` → Endpoint `ApiResponseFactory.From` / `Created`.

## Stable codes (CatalogErrorCodes)

| Code | HTTP | Use |
|---|---|---|
| catalog.category.missing | 404 | Workspace / mutation target absent |
| catalog.category.invalid | 400 | Empty create translations / invalid locale |
| catalog.category.slug.invalid | 400 | Empty/invalid slug after normalize |
| catalog.category.slug.duplicate | 409 | Typed uniqueness (no message parse) |
| catalog.category.parent.missing | 400 | Parent not found |
| catalog.category.parent.self | 400 | Self-parent |
| catalog.category.parent.descendant | 400 | Descendant-as-parent |
| catalog.category.depth.max | 400 | Max depth 3 |
| catalog.category.reorder.invalid | 400 | Sibling set mismatch |
| catalog.category.concurrency.conflict | 409 | ExpectedUpdatedAt mismatch |
| catalog.category.route.invalid | 400 | Resolve locale/slug invalid |
| catalog.category.route.missing | 404 | Resolve not found / storefront ineligible |

Registered in `CatalogErrorCatalogContributor` + `CatalogErrors.resx` / `.fa.resx`.

## Removed

- PlatformHttpException catch/ToError on Category surface
- InvalidOperationException message-as-code
- `ex.Message` contains "slug"/"تکراری" classification
- Hard-coded Persian/English transport titles in Host endpoints
