# TB-P07-T001-R3 — Admin Product CRUD

## Scope
Admin Product list + workspace expose real Host lifecycle actions.

## Actions wired

| UI surface | Action | Host |
| --- | --- | --- |
| Product list menu | مشاهده / ویرایش | `/admin/products/{id}` |
| Product list menu | انتشار | `POST /v1/admin/products/{id}/publish` |
| Product list menu | لغو انتشار | `POST /v1/admin/products/{id}/unpublish` |
| Product list menu | بایگانی | `POST /v1/admin/products/{id}/archive` |
| Product list menu | حذف امن | `DELETE /v1/admin/products/{id}` |
| Workspace shell | انتشار | same publish POST |
| Publication section | لغو انتشار / بایگانی / حذف امن | same paths |
| List create | محصول جدید | `POST /v1/admin/products` |

## Client
- `mutateAdminProductLifecycle(productId, "publish" \| "unpublish" \| "archive" \| "delete")` in `src/frontend/app/admin/host-client.ts`
- Delete: 204 NoContent; referenced products return 409 and soft-archive on Host

## Files
- `src/frontend/app/admin/host-client.ts`
- `src/frontend/app/admin/product-list.tsx`
- `src/frontend/app/admin/product-workspace-screen.tsx`

## Notes
- No “باز کردن” as the only action.
- Price/stock remain Offer-scoped; create is Catalog + default variant only.
