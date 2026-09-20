# Slice selection — TB-TMAR-FE-ADMIN-W4

## Selected: admin-customers

| Criterion | Evidence |
| --- | --- |
| Bounded ownership | `/admin/customers` known-buyer list |
| admin-api debt | `AdminCustomerRow`, `mapAdminCustomers`, `loadAdminCustomers`, `queryAdminCustomersGrid` |
| Flat touch | `AdminCustomersScreen` + `customerColumns` |
| Risk | Low — read-only; not CRM; no checkout coupling |

## Rejected this wave

- orders — checkout/payment coupling
- receipts — deferred (shared reservation mapper with payment ops)
