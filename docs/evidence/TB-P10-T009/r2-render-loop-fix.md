# TB-P10-T009-R2 — Render-loop fix

Root cause: `ThemeProvider` recreated `setColorScheme` whenever `theme` changed. `StorefrontThemeToggle` listed `setColorScheme` as an effect dependency and called it during sync, so Theme state churned forever on Home.

Fix: `useCallback` setters; skip `setTheme` when the scheme/direction is unchanged.

Admin Appearance form has no `useEffect`. Settings refresh is `[]`. Isolated Admin probe: 0 Maximum update depth. Home after fix: 0.
