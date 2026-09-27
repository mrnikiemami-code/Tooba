# Analyze — W16-R1 ProductPublishReadinessReader category InvalidOperationException control flow

## Parent

- Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W16
- Parent-Commit: bf9dda6ae2b336a4159d37dbc790026aa578b3f2

## Blocker

`ProductPublishReadinessReader.IsProductPrimaryCategoryAssignableAsync` used:

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

This is expected-control-flow via `InvalidOperationException` for ordinary "not assignable"
boolean readiness, contradicting W16 claim
`invalidOperationExpectedFlowState=ABSENT_ON_PUBLISH_READINESS_HTTP_AND_READER_LEGACY_UNWRAP_OUTSIDE`.

No message parsing was present; the catch still converted Ensure's throw into a boolean.

## Root authority

`CatalogCategoryTreeRules` (Domain):

| API | Behavior |
|---|---|
| `IsAssignableProductCategory` | `GetCategoryLevel(...) == ProductAssignableLevel` (3); returns bool; does not throw for level 1/2 |
| `EnsureAssignableProductCategory` | throws `InvalidOperationException` when `!IsAssignableProductCategory` |

`Ensure` is a thin throw wrapper over `IsAssignable`. Semantically equivalent for the
reader's boolean need; reader needs no error-code distinction (only `categoryReady` bool +
`ProductPublishRules.MessageCategoryIncompleteFa` on missing list).

## Other expected IOE inside ProductPublishReadinessReader (pre-repair)

| Location | Kind | Disposition |
|---|---|---|
| category try/catch around Ensure | expected IOE → bool | **in scope** → `IsAssignableProductCategory` |
| SEO / attributes / variants / media Result paths | Result failures | preserve (not IOE) |
| no other `catch (InvalidOperationException)` | — | none |

## Disposition

Replace Ensure + catch with `CatalogCategoryTreeRules.IsAssignableProductCategory`.
No hierarchy duplication in Infrastructure. No message classification. W16 routes/reuse/order preserved.
W17 not started.
