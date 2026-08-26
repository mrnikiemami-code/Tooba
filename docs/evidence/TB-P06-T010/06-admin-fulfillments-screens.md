# 06 — Admin fulfillments grid + detail

Routes:

- `/admin/fulfillments` → `AdminFulfillmentsScreen`
- `/admin/fulfillments/{fulfillmentId}` → `AdminFulfillmentDetailScreen`

Files:

- `src/frontend/app/admin/admin-screens.tsx` (grid + read-only detail)
- `src/frontend/app/admin/fulfillments/page.tsx`
- `src/frontend/app/admin/fulfillments/[fulfillmentId]/page.tsx`
- Nav under عملیات in `admin-shell.tsx`

Admin detail is inspect-only (no mutations).
