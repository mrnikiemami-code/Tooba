# Analyze — W14-R1 ProductSeo slug InvalidOperationException control flow

## Parent

- Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W14
- Parent-Commit: 1a97aa24cefd5848bca38c0b64214d10c58cbac8

## Blocker

`ProductSeoDirectory.UpdateAsync` used:

```csharp
try { ... SlugifyFromName / NormalizeSlug ... }
catch (InvalidOperationException)
{
    return Result.Failure(..., WorkspaceProductSlugInvalid);
}
```

This is expected-invalid-input control flow via exception, contradicting W14 claim
`invalidOperationExpectedFlowState=ABSENT_ON_PRODUCT_SEO_HTTP_AND_PRODUCTSEODIRECTORY`.

## Root authority

`CatalogCategorySlugNormalizer` (Domain) — `NormalizeSlug` / `SlugifyFromName` throw when empty after normalize.
Do not duplicate algorithm in Infrastructure.

## Callers of NormalizeSlug / SlugifyFromName (pre-repair audit)

| Caller | Usage | Repair impact |
|---|---|---|
| ProductSeoDirectory | throw + catch IOE | **in scope** → Try* |
| ProductSeoRules.Evaluate | try/catch for readiness bool | out of scope (not HTTP/Directory IOE claim) |
| CatalogCategoryTranslation | throwing | preserve |
| CatalogCategorySlugHistory | throwing | preserve |
| CategoryDirectory private TryNormalizeSlug | catch Argument/IOE → Result | preserve (Category surface) |
| ProductWorkspaceComposer | throwing (non-SEO retained) | preserve |
| CatalogDemo* | throwing seed | preserve |
| Host tests | assert throw / values | preserve + add Try* tests |

## Disposition

Provide Domain `TryNormalizeSlug` / `TrySlugifyFromName` sharing private `TryBuildNormalizedSlug` core with throwing APIs; ProductSeoDirectory uses Try* and returns `workspace.product.slug.invalid` with zero catch.
