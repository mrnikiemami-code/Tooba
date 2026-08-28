# TB-P07-T001-R3 — Product variants

## UX
Variants tab shows human terminology:

| Field | Display |
| --- | --- |
| Fingerprint | Humanized (`color=sand\|size=m` → `رنگ: sand · سایز: m`) |
| Status | Persian via `formatAdminStatus` (پیش‌نویس / منتشرشده / بایگانی) |
| Catalog code | `catalogCodeSeam` |
| Offers | count only (no price/stock on variant) |

## Actions
- List + patch status: `PATCH /v1/admin/products/{id}/variants/{variantId}`
- Create from axis-allowed attribute definitions: `POST /v1/admin/products/{id}/variants`
- Link to attributes tab for axis ownership (`setAdminProductVariantAxes`)

## Boundaries
- No Product/Variant price or stock shortcuts.
- Full combinatorial matrix remains DEFERRED.
- Duplicate combination denied by Host (`workspace.variant.create.rejected`).

## Files
- `src/frontend/app/admin/product-workspace-screen.tsx` (`humanizeFingerprint`, variants section)
- `src/frontend/app/admin/host-client.ts` (`createAdminProductVariant`, `patchAdminProductVariant`)
- Attribute axes foundation: `catalog-attribute-ui.tsx` / `catalog-attribute-api.ts` (kept)
