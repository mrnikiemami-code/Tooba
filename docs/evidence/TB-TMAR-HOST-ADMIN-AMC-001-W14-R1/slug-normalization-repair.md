# Slug normalization repair — W14-R1

## Design

`CatalogCategorySlugNormalizer`:

- Private core: `TryBuildNormalizedSlug` (single algorithm; empty → false).
- Public throwing: `NormalizeSlug` / `SlugifyFromName` — `ArgumentException` on null/blank; `InvalidOperationException` when core fails (punctuation-only etc.). Compatible with pre-R1 callers.
- Public safe: `TryNormalizeSlug(string? slug, out string normalized)` / `TrySlugifyFromName(...)` — false + empty out for null/blank/empty-after-normalize; true + same value as throwing API for valid inputs.

## ProductSeoDirectory

```csharp
if (string.IsNullOrWhiteSpace(input.Slug))
{
    var name = await ResolveProductNameForSeoAsync(...);
    if (!CatalogCategorySlugNormalizer.TrySlugifyFromName(name, out slug))
        return Failure(WorkspaceProductSlugInvalid);
}
else if (!CatalogCategorySlugNormalizer.TryNormalizeSlug(input.Slug, out slug))
{
    return Failure(WorkspaceProductSlugInvalid);
}
```

- Zero `catch (InvalidOperationException`.
- Zero message classification.
- Same error code `workspace.product.slug.invalid`.

## Parity proofs (focused tests)

| Case | Try* | Throwing |
|---|---|---|
| ASCII / spaces / `_` `/` `\` / hyphen collapse | true + equal | NormalizeSlug equal |
| Persian mixed | true + equal | equal |
| null / blank / punctuation-only | false | ArgumentException or InvalidOperationException |
| SlugifyFromName path | TrySlugify parity | SlugifyFromName equal |

## Unchanged

Routes, DTOs, uniqueness, ExpectedUpdatedAt, localization, history, EventSeoChanged, single SaveChanges, ProductSeoRules authority, Host Admin count 52.
