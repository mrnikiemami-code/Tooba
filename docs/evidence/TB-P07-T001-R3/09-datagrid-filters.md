# TB-P07-T001-R3 — DataGrid filters

## Scope
Product + Order Admin grids via shared `DataGrid` foundation.

## Operator labels
`FilterControl` uses `faFilterOperatorLabels` (شامل / برابر با / شروع می‌شود با / بیشتر از / کمتر از / بین / …). Raw English operator tokens are not shown in the Admin UI.

## Enum / status multi-select
| Grid | Column | Values (codes → FA via `formatAdminStatus` / explicit labels) |
| --- | --- | --- |
| Products | `status` | Draft→پیش‌نویس, Published→منتشرشده, Archived→بایگانی |
| Orders | `payment` | Paid, PendingPayment, Cancelled |
| Orders | `status` | Submitted, PendingPayment, ReservationRequested, Paid, Cancelled, Mixed, Processing |

## Applied filters UX
`DataGrid` shows:
- filter count on the filters button
- chip row (`data-testid="grid-filter-chips"`) with per-filter clear
- **پاک‌کردن همهٔ فیلترها** (`clearAllFilters`) in chip row and filter drawer

## Files
- `src/frontend/design-system/data-grid/FilterControl.tsx`
- `src/frontend/design-system/data-grid/messages.ts`
- `src/frontend/design-system/data-grid/DataGrid.tsx`
- `src/frontend/app/admin/product-list.tsx`
- `src/frontend/app/admin/admin-screens.tsx`
