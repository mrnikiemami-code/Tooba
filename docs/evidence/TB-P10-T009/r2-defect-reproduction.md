# TB-P10-T009-R2 — Defect reproduction

A. Empty chrome boxes: R1 tiles used only `article` + empty `media`. No title/price/CTA. See `screenshots/admin-skin-default.png`.

B. English keys: `{skin.key}` rendered as secondary caption (`classic`/`clean`/`elevated`/`glass`). Palette and ThemeMode also showed raw keys.

C. Console: R1 Playwright recorded Maximum update depth. Isolated probe after R2 start:

- Admin Appearance select/preview: 0
- Home: 2409, from `StorefrontThemeToggle` syncing `setColorScheme` whose identity changed every ThemeProvider render (`useMemo(..., [theme])` plus `setTheme({ ...current })` always allocating a new object).

Hydration: one generic Next mismatch on multi-page sessions. Isolated Admin and Home: 0. Not `data-storefront-product-card-skin`.
