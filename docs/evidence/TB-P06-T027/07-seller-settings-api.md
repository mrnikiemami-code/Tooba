# Seller settings API

- Routes: `GET/PUT /v1/seller/settings`
- Auth: `SellerPanelAccess.RequireAuthorizedAsync` + effective capability check.
- GET requires `seller.settings.view`; returns `canManage` from `seller.settings.manage`.
- PUT requires `seller.settings.manage`, then Party `UpdateOrganizationProfileAsync`.
- Headers: `X-Tooba-Dev-Actor-User-Id`, `X-Tooba-Seller-Party-Id`.
- Catalog: `seller.settings.view` / `seller.settings.manage` (delegable).
- Mobile-order employee role seed does **not** grant manage.
