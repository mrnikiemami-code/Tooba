# Comments UI (TB-P08-T015)

## Location

Article workspace tab **نظرات** (`ContentArticleCommentsPanel`).

## Features

- Pending badge/count on tab + panel
- Status filter (all / Pending / Approved / Rejected / Hidden)
- Search by display name / body
- Newest first, skip/take paging (max 50 server-side)
- Inline approve / reject / hide / return to pending
- Confirm dialog for reject/hide
- Empty / loading / error polished states
- Admin “seed” create for smoke only (not a public form)

## Grid choice

Compact moderation list preferred over AppDataGrid for inline Article-tab workflow; no raw AgGridReact.
