# Category-tree safe probe — W19-R1

## Domain change

`CatalogCategoryTreeRules` now has one traversal core:

- private `TryResolveCategoryLevel` → `Ok` | `Missing` | `Cycle` + level
- `TryGetCategoryLevel` → non-throwing; missing/cycle → false, level=0
- `TryIsAssignableProductCategory` → non-throwing; missing/cycle → false + isAssignable=false; valid → level==3
- `GetCategoryLevel` → same messages as before via shared core
- `IsAssignableProductCategory` → still `GetCategoryLevel(...) == ProductAssignableLevel`

## Parity

| Case | GetCategoryLevel / IsAssignable | TryGet / TryIsAssignable |
|---|---|---|
| valid L1 | level 1 / false | true,1 / true,false |
| valid L2 | level 2 / false | true,2 / true,false |
| valid L3 | level 3 / true | true,3 / true,true |
| missing | throw Missing | false / false |
| cycle | throw Cycle | false / false |

No Persian message classification. No duplicate parent-walk in Infrastructure.
