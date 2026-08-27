# 06 — Seller Access Control capabilities

Task: TB-P06-T024  
Status: LIVE (bounded by platform ceiling)

## Routes (FE)

| Surface | Path |
|---------|------|
| Center | `/vendor-panel/access-control` |
| Page | `src/frontend/app/vendor-panel/access-control/page.tsx` → `mode="seller"` |

Nav: `vendor-shell.tsx` → `/vendor-panel/access-control` (`live: true`).

## API

Prefix: `/v1/seller/access-control`  
Client: `createSellerAccessApi` (`access-control-api.ts`)  
Context: seller party header + actor (existing vendor auth headers).

## Allowed

- List own roles; create/edit/clone custom roles; archive mutable roles
- Search permissions (catalog with `disabledByCeiling` / `platformOnly`)
- Assign/remove roles to own-scope users
- Effective-access preview for a user in own Seller scope
- Read platform ceiling; grant only within ceiling ∩ delegable

## Denied (backend)

- Grant platform/admin permissions (`access.escalation.platform_permission`)
- Exceed ceiling (`access.escalation.ceiling`)
- Inspect foreign Seller roles/assignments
- Mutate system `seller-owner`
- Privilege-escalate via crafted payload (validated in `ValidateGrantsForOwnerAsync`)
