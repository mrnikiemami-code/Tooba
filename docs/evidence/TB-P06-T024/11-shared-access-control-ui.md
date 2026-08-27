# 11 — Shared Access Control UI

Task: TB-P06-T024  
Status: LIVE (single component system)

## Shared core

| Piece | Path |
|-------|------|
| Center | `src/frontend/app/access-control/access-control-center.tsx` (`AccessControlCenter`) |
| API adapters | `src/frontend/app/access-control/access-control-api.ts` |

Covered in one screen (tabs + panels): RoleList, RoleEditor, PermissionMatrix (search/group/expand/select/clear), Scope fields on grants, UserAssignments, EffectiveAccessPreview, RoleClone, UnsavedChangesGuard (confirm + sticky banner), Ceiling editor (admin-seller / ceiling tab).

## Thin wrappers only

| Mode | Page |
|------|------|
| admin | `app/admin/access-control/page.tsx` |
| seller | `app/vendor-panel/access-control/page.tsx` |
| admin-seller | `app/admin/sellers/[sellerId]/access-control/page.tsx` |

Difference: DATA SCOPE + AVAILABLE CAPABILITIES + DELEGATION CEILING via `AccApi` + `mode` prop — not duplicated CSS/JS implementations.

## Build proof

`frontend-build.log` routes:

- `/admin/access-control`
- `/admin/sellers/[sellerId]/access-control`
- `/vendor-panel/access-control`
