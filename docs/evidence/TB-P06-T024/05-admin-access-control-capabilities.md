# 05 — Admin Access Control capabilities

Task: TB-P06-T024  
Status: LIVE

## Routes (FE)

| Surface | Path |
|---------|------|
| Center | `/admin/access-control` |
| Thin page | `src/frontend/app/admin/access-control/page.tsx` → `AccessControlCenter mode="admin"` |
| Seller-specific | `/admin/sellers/[sellerId]/access-control` |

Nav: `admin-shell.tsx` item `access-control` → `/admin/access-control` (`live: true`).

## API (Host)

`src/backend/Host/Tooba.Host/AccessControl/AccessControlEndpoints.cs`

Prefix: `/v1/admin/access-control`

- permissions, roles CRUD/clone/archive, role permissions, assignments, users search, effective access, bootstrap

Seller-scoped Admin override prefix:

`/v1/admin/sellers/{sellerId}/access-control` — ceiling get/set, roles, assignments, effective

## Capabilities

- Manage platform Admin roles + permissions + user assignments
- Inspect/manage per-Seller Access Control (shared UI `mode="admin-seller"`)
- Set `PlatformSellerCeiling` for a Seller
- Gate mutations with SpiceDB `permission#check` for `accesscontrol.manage` (view endpoints require `accesscontrol.view`)

## UI API client

`createAdminAccessApi` / `createAdminSellerAccessApi` in `src/frontend/app/access-control/access-control-api.ts`
