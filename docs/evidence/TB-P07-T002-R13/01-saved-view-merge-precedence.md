# TB-P07-T002-R13 — Saved View merge precedence

When switching Saved Views (`applyView`):

1. **Always applied from target view:** column layout (order/visibility/widths), pageSize, sorts.
2. **Transient layer preserved when active:** if `hasActiveTransientFilters(current)` (applied search, column filters, advanced filter), current filters/search/advancedFilter remain; AG filter model is NOT overwritten.
3. **When no transient filters active:** restore saved view filters/search/advancedFilter from view snapshot.

Changing filters/search/sort/layout while a view is selected does **not** clear `activeViewId`; UI shows `• تغییر یافته` when `isSelectedViewDirty`.

Explicit deselect only on: restore system default, delete active view, initial load.

Search: `searchInput` = draft; `query.search` = applied; commit on Enter or search icon button only.
