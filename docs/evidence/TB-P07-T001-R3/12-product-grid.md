# TB-P07-T001-R3 — Product grid

## Visual hierarchy
- Thumbnail from `storefrontMediaUrl(primaryMediaAssetId)` when Host provides `PrimaryMediaAssetId`; placeholder otherwise
- Title + category subtitle
- Status badge via `formatAdminStatus` (Draft / Published / Archived)
- Variant / offer / price projection / inventory projection / updated date
- Compact action menu: مشاهده، ویرایش، انتشار، بایگانی

## Lifecycle actions
`mutateAdminProductLifecycle` POSTs `/v1/admin/products/{id}/publish|archive`.
If Host returns 404, UI shows «این عملیات هنوز روی Host فعال نیست» (graceful).

## Saved views
`savedViewStore={createHostSavedViewStore("grid.admin.products")}`

## Files
- `src/frontend/app/admin/product-list.tsx`
- `src/frontend/app/admin/host-client.ts` (`primaryMediaAssetId`, lifecycle helper)
