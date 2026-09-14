# TB-P10-T009 — Persistence API

- Column `catalog.store_appearance_settings.product_card_skin` varchar(16) default `classic`
- Store-scoped singleton row; no per-card lookup
- PUT `/v1/admin/settings/appearance` `{ paletteKey, themeMode, productCardSkin }`
- GET `/v1/storefront/appearance` emits `productCardSkin`
- Invalid key → 400 `appearance.skin.invalid`
- Missing/null preserves existing (default classic)
- Unknown persisted key projects as `classic`
- Cache invalidate current Store only
- Auth unchanged (`AdminPanelAccess.RequireAuthorizedAsync`)
