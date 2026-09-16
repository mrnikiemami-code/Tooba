# Admin DataGrid gap matrix — TB-P11-T001

Canonical: `/admin/orders` → `AdminOrdersScreen` (`admin-screens.tsx`) via `ServerGridPage` + `AppDataGrid` (typed filters, sort, pagination, resize/reorder, column manager, saved views `grid.admin.orders`, CSV/Excel export, pinned RTL actions, icon-only View+kebab, truncation/tooltips). Landing pages label this `orders-canonical`.

| Module | Grid | Filt | Sort | Page | Resize | ColMgr | Saved | Export | Bulk | PinOps | IconOps | Gap severity |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Orders | ADG server | Y | Y | Y | Y | Y | Y | Y | N (OK) | Y | Y | canonical |
| Fulfillments | ADG server | Y | Y | Y | Y | Y | Y | Y | Y | Y | Y | LOW |
| Returns | ADG server | Y | Y | Y | Y | Y | Y | Y | N | Y | Y | LOW |
| Products | ADG server | Y | Y | Y | Y | Y | Y | Y | N | Y | Y | LOW |
| Content | ADG server | Y | Y | Y | Y | Y | Y | Y | N | Y | Y | LOW |
| Store Pages | ADG client | Y | Y | Y | Y | Y | Y | N | N | Y | Y | MEDIUM export off |
| Tickets | custom cards | N | N | local 8 | N | N | N | N | N | N | N | HIGH |
| Menus | custom table | N | N | N | N | N | N | N | N | N | N | HIGH |
| Page composition | custom ol | N | N | N | N | N | N | N | N | N | N | HIGH |
| Access control | custom matrix | N/A | N/A | N/A | N | N | N | N | N | N | N | MEDIUM (matrix UX) |
| Sellers | ADG | partial | partial | Y | Y | Y | Y | Y | N | N | N | HIGH |
| Customers | ADG | partial | partial | Y | Y | Y | Y | Y | N | N | N | HIGH |
| Reviews | ADG | weak | Y | Y | Y | Y | Y | Y | N | N | text | HIGH |
| Promotions | ADG | N | Y | Y | Y | Y | Y | Y | N | N | text | HIGH |
| Payouts | ADG | partial | Y | Y | Y | Y | Y | Y | N | N | text | HIGH |
| Shipping | ADG | N | Y | Y | Y | Y | Y | Y | N | weak | text | HIGH |
| Units | ADG | N | Y | Y | Y | Y | Y | Y | N | miss `ops` | ad-hoc | HIGH |
| Gift cards | ADG | partial | Y | Y | Y | Y | Y | Y | N | N | N | HIGH |
| Stories | ADG | partial | Y | Y | Y | Y | Y | Y | N | N | text | MEDIUM |
| Settlement | ADG client | sparse | Y | client | Y | Y | Y | Y | N | N | N | HIGH N+1 |
| Languages | ADG | N | Y | Y | Y | Y | Y | Y | N | N | icon | MEDIUM |
| Attributes | ADG | partial | Y | Y | Y | Y | Y | Y | N | N | text | MEDIUM |
| Receipts | ADG | Y | Y | Y | Y | Y | Y | Y | N | N | link cell | MEDIUM |
| Category trees | tree | search | n/a | n/a | n/a | n/a | n/a | n/a | n/a | n/a | n/a | LOW by design |

Applicability: bulk/export not required on dashboard, settings, ACC matrix, trees, wallet inspect.
