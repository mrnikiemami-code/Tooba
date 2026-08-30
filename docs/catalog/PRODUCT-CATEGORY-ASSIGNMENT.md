# Product Category Assignment

> **Task:** TB-P07-T016 · **Phase:** P07 Advanced Catalog
> **Depends on:** [CATEGORY-ARCHITECTURE.md](./CATEGORY-ARCHITECTURE.md), [PRODUCT-CATALOG-ADMIN.md](./PRODUCT-CATALOG-ADMIN.md)

## Rule — Level-3 only

Product may be assigned **only** to a category at taxonomy **level 3**.

| Level | How computed | Product role |
|-------|----------------|--------------|
| 1 | Root (`ParentId` null) — ancestors = 0 → level = 1 | Navigation / browse only |
| 2 | Child of L1 — ancestors = 1 → level = 2 | Navigation / browse only |
| 3 | Child of L2 — ancestors = 2 → level = 3 | **Assignable** |

```text
Level = 1 + ancestor count via ParentId
Assignable iff Level === 3
```

Example:

```text
کالای دیجیتال          (L1 — expand only)
  → موبایل و تبلت      (L2 — expand only)
    → گوشی موبایل      (L3 — selectable)
```

Full human path shown in UI:

```text
کالای دیجیتال > موبایل و تبلت > گوشی موبایل
```

No raw CategoryId in picker labels.

## Backend enforcement

Shared helper: `CatalogCategoryTreeRules` (`GetCategoryLevel`, `IsAssignableProductCategory`, `EnsureAssignableProductCategory`).

Enforced in:

- `CatalogDirectory.AssignCategoryAsync`
- `CatalogDirectory.ReplaceProductPrimaryCategoryAsync` (via preview)
- `CatalogDirectory.PreviewCategoryChangeAsync` (target must be L3)
- `CatalogDirectory.PublishProductAsync` (existing primary must be L3)
- `ProductWorkspaceComposer.CreateSimpleProductAsync`
- `ProductWorkspaceComposer.AssignProductCategoryAsync`

Persian error (stable code on workspace HTTP):

| Message | Error code |
|---------|------------|
| محصول باید به یک دسته‌بندی سطح سوم اختصاص داده شود. | `workspace.product.category.level.invalid` |

Frontend-only checks are **not** sufficient.

## Category picker UX

- Searchable hierarchical 3-level tree
- L1 / L2: expand/open only — not broken/disabled styling; click does **not** close as successful selection
- L3: selectable with selected state
- Search shows full path; L1/L2 hits non-selectable (navigation/focus); L3 selectable
- Create modal and Workspace EDIT use the same picker

## Legacy / invalid primary category

Do **not** silently migrate.

If an existing product already points at L1/L2 (or otherwise non-assignable):

- VIEW remains allowed
- Explicit warning in Workspace (`isPrimaryCategoryAssignable: false` + readiness warning)
- Save / Publish require an explicit valid L3 selection
- No automatic reassignment

## Product attributes source

Product attribute **values** come only from the effective Category attribute schema.

- No Product-local `AttributeDefinition` creation in Product Workspace
- Empty schema: «برای این دسته‌بندی هنوز ویژگی‌ای تعریف نشده است.» with optional link «مدیریت ویژگی‌های دسته‌بندی» → Category Admin
- Category-change attribute impact (T012) and variant impact (T013) remain; target category must still be L3

## Future extensibility

If assignment depth becomes configurable, keep:

- one shared level helper (backend + FE)
- backend as source of truth
- picker policy driven by the same assignable-level constant

## Related

- [PRIMARY-CATEGORY-MIGRATION.md](./PRIMARY-CATEGORY-MIGRATION.md)
- [PRODUCT-CATALOG-ADMIN.md](./PRODUCT-CATALOG-ADMIN.md)
- [PRODUCT-ATTRIBUTES.md](./PRODUCT-ATTRIBUTES.md)
- [PRODUCT-VARIANTS.md](./PRODUCT-VARIANTS.md)
