# TB-P07-T005 — Category Admin UI (AppCategoryTree + Workspace Shell)

## Status
Worker implementation complete for Architect review. `USER_VISUAL_ACCEPTED` remains **NO**.

## Delivered
- `AppCategoryTree` design-system wrapper over Ant Design Tree (`antd@5.27.6`)
- Admin routes: `/fa/admin/catalog/categories` (+ `/{id}` + `/{id}/{tab}`)
- API client wired to T004 endpoints (tree, workspace, create, move, reorder, optional publish)
- General summary + Translation readiness tabs; other tabs shell-only («این بخش در تسک بعدی تکمیل می‌شود» / به‌زودی)
- Desktop: tree (RTL right) + workspace; Mobile: tree → full-page workspace with Back
- Nav: Categories under Admin catalog

## Visual contract
Reference: `docs/evidence/TB-P07-T004/visual-contract-admin-categories.png`
Composition matched (tree + workspace). No generic Ant skin dominance; Tooba CSS under design-system. No fake future-tab CRUD.

## Preview
http://localhost:3000/fa/admin/catalog/categories

## Notes
- Local search filters loaded tree; drag handle separated via Ant `draggable.icon`
- Create: name + slug only (Draft, root/child parent)
