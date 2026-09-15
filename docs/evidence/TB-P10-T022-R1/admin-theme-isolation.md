# Admin / Seller theme isolation

Root `<html>` no longer receives `storefrontAppearanceStyle`. Tokens apply on storefront/customer canvases via `StorefrontAppearanceProvider` + `StorefrontThemedCanvas` / customer panel style. Admin/Seller roots use `data-panel-theme` fixed Tooba-blue tokens (LOCK-SF-261).

Runtime: adminPrimary remains `37 99 235` after store palette change; seller + storefront screenshots captured.
