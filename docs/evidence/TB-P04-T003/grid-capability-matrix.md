# TB-P04-T003 — Grid capability matrix

| Capability | Foundation | Notes |
| --- | --- | --- |
| Typed filters | Done | text, number, money, date, enum, boolean, entity, status |
| Sorting | Done | Serializable single-column cycle; array contract for multi-sort |
| Column reorder | Done | Drag-and-drop plus Alt+Arrow and drawer buttons |
| Column resize | Done | Range control; min/max clamp; RTL logical widths |
| Show/hide | Done | Column drawer; restore defaults |
| Saved views | Done | Storage-agnostic `SavedViewStore`; memory adapter for showcase |
| Pagination | Done | page, pageSize, total, next/previous |
| Selection | Done | Row + select page; no cross-page select-all |
| Bulk actions | Done | Generic callback + confirmation |
| Export | Done | Visible CSV, selected CSV, server-export seam |
| Sticky header | Done | `thead` sticky |
| Sticky columns | Done | start/end via inline logical insets |
| Keyboard baseline | Done | Tab, sort keys, drawers, selection, reorder/resize fallbacks |
| RTL/LTR | Done | Logical CSS; theme `dir` |
| Responsive | Done | Overflow + mobile cards + filter drawer |
| Server-side query | Done | `GridServerQuery` / adapter; demo engine is not production SQL |
