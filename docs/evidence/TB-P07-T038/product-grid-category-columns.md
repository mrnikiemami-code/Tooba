# Product grid category columns (TB-P07-T038)

## Contract
- Separate AppDataGrid columns: `دسته اصلی` (`primaryCategoryName`) and `نمایش در دسته‌های دیگر` (`additionalCategoryNames`).
- Primary cell = localized leaf name only (no `L1 > L2 > L3`).
- Additional = individual chips; max 3 inline; `+N` for remainder.
- Zero additional => quiet `—`.
- `categorySummary` remains leaf-joined compatibility for host filters; grid UI does not use path blobs.

## Files
- `ProductWorkspaceModels.cs` — DTO fields
- `ProductWorkspaceComposer.BuildListItemsForProductIdsAsync` — batched leaf names
- `product-list.tsx`, `category-products-panel.tsx`, `additional-category-chips-cell.tsx`, `host-client.ts`
