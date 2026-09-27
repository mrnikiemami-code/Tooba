# Analyze — W19-R1 CatalogAdminProductWorkspaceReadGateway category assignability IOE

## Parent

- Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W19
- Parent-Commit: 186ed01ecf7e789db5998eb2ca6747f6003b62af

## Blocker

`CatalogAdminProductWorkspaceReadGateway.GetAggregateSnapshotAsync` used:

```csharp
try
{
    isPrimaryCategoryAssignable = CatalogCategoryTreeRules.IsAssignableProductCategory(
        assignableProbe, parentById);
}
catch (InvalidOperationException)
{
    isPrimaryCategoryAssignable = false;
}
```

This is expected control flow via `InvalidOperationException` for ordinary
"not assignable" / invalid-tree-state handling on the migrated aggregate GET path.

## Why `IsAssignableProductCategory` still throws

Both go through `GetCategoryLevel`:

| Condition | Throw |
|---|---|
| categoryId missing from `parentById` | `InvalidOperationException("رده در Catalog این Tenant وجود ندارد.")` |
| cyclic parent graph (guard exceeded) | `InvalidOperationException("حلقهٔ موجود در درخت رده تشخیص داده شد.")` |
| valid L1 / L2 | returns false (no throw) |
| valid L3 | returns true (no throw) |

## Callers of `IsAssignableProductCategory` (out of scope)

Host composer write residue, ProductPublishReadinessReader, CategoryChangeDirectory,
CatalogDemo*, Host bootstrap, tests — **not repaired** in W19-R1.

## Disposition

Canonical Domain non-throwing probes:

- `TryGetCategoryLevel` / `TryIsAssignableProductCategory`
- one shared `TryResolveCategoryLevel` core with throwing `GetCategoryLevel` / `IsAssignableProductCategory`

Gateway uses `TryIsAssignableProductCategory` only; zero `catch (InvalidOperationException)`.
Preserve warning via `ProductAssignableLevelRequiredMessageFa` when not assignable.
W20 not started.
