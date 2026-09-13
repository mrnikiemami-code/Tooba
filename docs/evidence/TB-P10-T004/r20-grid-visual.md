# R20 grid visual

Orders `/fa/admin/orders`:

- Canonical AppDataGrid (search, columns, CSV/Excel, saved view, pagination).
- Rows for R20 H/G/B and visual-review order; Action «مشاهده» / «عملیات» intact.
- Compact «رزرو موجودی» column is in `admin-screens.tsx` header set; no overflow observed in loaded rows.

Payments `/fa/admin/receipts`:

- Title «دریافت‌ها»; statuses موفق / در انتظار / ناموفق.
- Order number links; no duplicated full reservation audit.
- Same grid chrome (filter/columns/export/saved view).
