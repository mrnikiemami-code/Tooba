# 05 — Permissions + FE projection

Task: TB-P06-T025

## PermissionCatalog (Host)

| PermissionId | Delegable | Scope |
|--------------|-----------|-------|
| support.view | yes | GlobalWithinOwner |
| support.create | yes | GlobalWithinOwner |
| support.reply | yes | GlobalWithinOwner |
| support.manage | yes (admin ops; confirm with catalog) | GlobalWithinOwner |

## FE nav

| Panel | Href | live | viewPermission |
|-------|------|------|----------------|
| Customer | `/customer-panel/tickets` | true | (session; no SpiceDB view key) |
| Vendor | `/vendor-panel/tickets` | true | `support.view` |
| Admin | `/admin/tickets` | true | `support.view` |

Removed from `CUSTOMER_DEFERRED_NAV_HREFS` / `VENDOR_DEFERRED_NAV_HREFS`.
