# TB-P10-T017-R1 — PDP 500 root cause

Owner: Theme (T008 UserChoice toggle), not product-data or appearance tokens.

T017 capture / FE `next dev` log:

```text
Error: useTheme must be used within ThemeProvider
  at useTheme (design-system/theme/ThemeProvider.tsx:55)
  at StorefrontThemeToggle (app/storefront/storefront-theme-toggle.tsx)
GET /products/demo-prod-fashion-men-men-pants-1 500
GET / 500
GET /products 500
```

`StorefrontThemeToggle` ran in the storefront header (outside `ThemeProvider`). Showcase still uses `useTheme` under `ThemeProvider`. Storefront dark class already comes from `html` + `THEME_BOOTSTRAP_SCRIPT` + local `applyScheme`.

Fix: toggle persists cookie/localStorage and toggles `html.dark` only. No `useTheme`. No client retry/sleep on PDP.

Post-fix cold GET `/fa/products/demo-prod-fashion-men-men-pants-1` = 200 on the first request. Capture `no-theme-throw` pass. Historical 500 lines in the same FE log predate this repair.
