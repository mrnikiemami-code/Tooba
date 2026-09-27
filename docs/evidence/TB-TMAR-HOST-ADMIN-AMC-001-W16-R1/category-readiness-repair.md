# Category readiness repair — W16-R1

## Change

`ProductPublishReadinessReader.IsProductPrimaryCategoryAssignableAsync`:

Before:

```csharp
try
{
    CatalogCategoryTreeRules.EnsureAssignableProductCategory(primaryCategoryId, parentById);
    return true;
}
catch (InvalidOperationException)
{
    return false;
}
```

After:

```csharp
return CatalogCategoryTreeRules.IsAssignableProductCategory(primaryCategoryId, parentById);
```

Empty primary (`Guid.Empty`) still returns `false` before Domain call.

## Semantics preserved

| Case | Before | After |
|---|---|---|
| no primary category | false | false |
| level 1 / level 2 category | Ensure throws → catch → false | IsAssignable → false |
| level 3 category | Ensure ok → true | IsAssignable → true |
| missing-list code/message | `category` + MessageCategoryIncompleteFa | unchanged |
| missing order | category → identity → attributes → variants → media → seo | unchanged |

## Non-goals

- No Domain algorithm copy in Infrastructure
- No try/catch around another throwing helper
- No assignability / route / dependency-reuse changes
- W17 not started
