# Anti-pattern scan — TB-P10-T017-R1

CLEAN.

- No page-specific theme tokens or page-local color overrides
- No screenshot-only CSS hacks
- No client retry/sleep/`setTimeout` on PDP first paint
- ThemeToggle no longer calls `useTheme`; showcase still uses `useTheme` under `ThemeProvider`
- No custom builder / appearance controls
- Four global surface roles unchanged
- No TB-P10-T018
- Unrelated login/account leftovers not committed
