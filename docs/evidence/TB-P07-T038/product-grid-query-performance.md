# Product grid query performance (TB-P07-T038)

## Before
`BuildListItemsForProductIdsAsync` called `BuildCategoryPathMapAsync` which loaded the entire category parent map and walked ancestors for every linked CategoryId on the page.

## After
- Collect distinct CategoryIds from ProductCategories for the page product IDs.
- One batched `LoadNamesAsync(Category, ids)` for leaf names only.
- Group Primary vs Additional in memory.
- Workspace/Get path composition (`BuildCategoryPathAsync`) preserved for Product Workspace only — not used for Admin grid cells/tooltips.

## Non-goals avoided
No per-row Category query, no per-chip query, no full-tree load solely for grid cells.
