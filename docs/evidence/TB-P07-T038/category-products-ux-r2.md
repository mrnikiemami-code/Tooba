# TB-P07-T038 — Category Products Grid + Tree UX (R2 polish)

## Delivered

- Category→Products grid: compact resizable actions column (`actionSlots` 1–2, `compact` icons).
- «نمایش در دسته‌های دیگر»: comma-separated list + `AppGridTruncatedCell` tooltip on overflow.
- Column header tooltips + width tuning (incl. updatedAt 128px).
- Grid filter matrix: `additionalCategoryNames` text filter; advanced filter entry added.
- Backend: `additionalCategoryNames` filterable field (Additional role only).
- Category tree: expand-all / collapse-all toolbar; collapsible tree pane with expand rail.
- Product list filter matrix: `additionalCategoryNames` text filter enabled.

## Tests (focused)

- `category-products-panel.test.ts` 9/0
- `t038-category-grid-ux.test.ts` 4/0
- `app-category-tree` contract + tree-model 19/0

## USER_VISUAL_ACCEPTED

NO — human should confirm filters, comma overflow tooltip, actions resize, tree collapse in browser.
