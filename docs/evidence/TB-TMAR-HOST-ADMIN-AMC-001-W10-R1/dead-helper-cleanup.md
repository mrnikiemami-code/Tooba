# Dead helper cleanup — TB-TMAR-HOST-ADMIN-AMC-001-W10-R1

## Helper

`MapAttributeInvalid(InvalidOperationException ex)` in Host `Admin/CatalogAttributeEndpoints.cs`.

## Runtime reference audit

| Site | Kind |
|---|---|
| Definition in CatalogAttributeEndpoints.cs (pre-repair) | DEFINITION |
| Call sites in Host production Attribute file | **NONE** |
| Call sites elsewhere under `src/` | **NONE** |

Conclusion: dead helper. Retained only because W8/W9 guards asserted its string presence after W10 vacated product-attribute routes that previously called it.

## Action

Removed the entire private method body and definition from `CatalogAttributeEndpoints.cs`.

Retained helpers that still serve variant/category-change:

- `MapCategoryChangeInvalid`
- `ToError(PlatformHttpException)`
- Variant transport records (`SetProductVariantAxesRequest`, preview/apply DTOs, etc.)

## Post-state

| Check | Result |
|---|---|
| Definition in Host Attribute file | ABSENT |
| Runtime references | ZERO |
| Migrated Catalog Attribute surfaces (Definitions / Schema / ProductValues) | Still no `MapAttributeInvalid`, no `ex.Message` heuristics |

## Guard alignment

W8/W9 Host-file assertions flipped from `Contains("MapAttributeInvalid")` → `DoesNotContain(...)`.
Catalog migrated-surface tests strengthened to also forbid `MapAttributeInvalid` (message-parsing already forbidden).
