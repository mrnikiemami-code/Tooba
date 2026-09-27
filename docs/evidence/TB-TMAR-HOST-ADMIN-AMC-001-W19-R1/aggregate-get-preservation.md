# Aggregate GET preservation — W19-R1

## Gateway repair only

`CatalogAdminProductWorkspaceReadGateway` assignability:

```csharp
isPrimaryCategoryAssignable =
    CatalogCategoryTreeRules.TryIsAssignableProductCategory(
        assignableProbe, parentById, out var assignable)
    && assignable;

if (!isPrimaryCategoryAssignable)
{
    assignabilityWarning = CatalogCategoryTreeRules.ProductAssignableLevelRequiredMessageFa;
}
```

## Preserved W19 state

| Item | State |
|---|---|
| Host aggregate GET route | ABSENT |
| ProductWorkspace aggregate GET routes | 1 |
| Host ProductWorkspace routes | 18 |
| Host/Admin .cs count | 52 |
| Catalog read boundary | `ICatalogAdminProductWorkspaceReadGateway` |
| ProductWorkspace Application foreign | Contracts-only (+ IPartyLookup) |
| Response models | ProductWorkspace.Application authoritative |
| IsPrimaryCategoryAssignable / warning Fa | PRESERVED |
| Host composer GetAsync residue | UNTOUCHED (writes) |
| list/grid/brand-options/writes | UNTOUCHED |
| StoreAppearance | DEFERRED |
| schema / frontend | UNCHANGED |
| W20 | NOT STARTED |
