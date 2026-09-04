# Category tree polish (TB-P08-T016-R5)

- Content category admin keeps `AppCategoryTree`.
- Removed duplicate outer search input; tree-owned search remains.
- Passes `loading={loading}` into `AppCategoryTree` instead of replacing tree with Spinner-only UI.
- Tree pane widened to `minmax(320px,48%)`.
- Selected node styles slightly stronger (`--act-selected` + inset primary bar + bold name).
