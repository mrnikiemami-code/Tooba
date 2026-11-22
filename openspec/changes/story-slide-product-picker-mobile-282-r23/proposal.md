# Proposal

## Why

The existing slide editor lets an operator choose a product, but the selected product is only a muted sentence below the results. The selected option is not marked, long names can crowd the row, and the selection can feel lost while searching. On mobile the low-contrast, low-emphasis treatment makes it hard to confirm what will be saved.

## What Changes

- Add one prominent selected-product card above the search with the current name and an accessible selected state.
- Mark the selected option with a check icon, text badge and `aria-pressed`, not colour alone.
- Keep the current selection and its label visible through search, paging, loading and lookup errors.
- Make the current selection honest when it is outside the visible result page.
- Separate loading, empty and error states, with a 44px retry control.
- Wrap long Persian names and keep the list, Save and Cancel controls usable at 360px.
- Reuse the existing copy catalogs, Dialog, tokens, lookup API, update-item command and recovery path.
- No backend, permission, schema, storefront or seller-shell change.

## Capabilities

### New Capabilities

- `story-slide-product-picker`: the shared slide editor presents and preserves a clear, accessible product selection.

### Modified Capabilities

<!-- None. -->

## Impact

`src/frontend/app/stories/management/StoryManagementScreen.tsx`,
`src/frontend/app/stories/management/story-management-copy.ts`, the existing picker mount test and a task-scoped Playwright browser proof. The existing admin and seller routes, `/stories/product-options` lookup and `update_item` PUT remain authoritative.
