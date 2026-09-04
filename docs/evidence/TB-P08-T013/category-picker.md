# TB-P08-T013 — Category picker

- Replaced flat `<select>` with `ContentArticleCategoryPicker` (searchable hierarchical list).
- Language-scoped via existing tree fetch for Article locale.
- L1/L2 labels: دسته اصلی / زیردسته; L2 shows `Parent › Child`.
- Inactive not newly assignable; empty state when no categories.
- Admin tree: `AppCategoryTree` with `maxDepth={2}`; Level 2 hides add-child.
