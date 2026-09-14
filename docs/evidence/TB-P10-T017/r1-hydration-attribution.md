# TB-P10-T017-R1 — Hydration attribution

## Task-owned (fixed)

`useTheme` throw during SSR of `StorefrontThemeToggle` is gone. That was a first-paint 500, not an attribute mismatch.

## Unrelated leftover (attributed, not redesigned)

Playwright still records Next.js:

```text
A tree hydrated but some attributes of the server rendered HTML didn't match the client properties.
```

Dev overlay “1 Issue” appears on some 1440×1200 clips. `html` already has `suppressHydrationWarning`. Remaining mismatch is not the ThemeToggle `useTheme` path (source no longer calls it). Likely pre-existing client/SSR attribute drift on child chrome (theme bootstrap vs React, client-only toggle `visible` after effect, product-card client state). Unrelated local login/account working-tree files were not committed and are not treated as this repair.

No product-code sleep/retry was added to hide hydration.
