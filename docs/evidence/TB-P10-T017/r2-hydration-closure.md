# TB-P10-T017-R2 — Hydration closure

Reproduced on fresh Home / PLP / PDP / Landing (`r2-hydration-probe.json`).

Exact warning: React hydration mismatch (`https://react.dev/link/hydration-mismatch`).

| | |
| --- | --- |
| Element | `<a data-testid="header-login-link">` inside `StorefrontAccountMenu` |
| Attribute | `href` |
| Server | `/fa/login?returnTo=%2Ffa` |
| Client (PLP) | `/fa/login?returnTo=%2Ffa%2Fproducts` |
| Client (PDP) | `/fa/login?returnTo=%2Ffa%2Fproducts%2Fdemo-prod-fashion-men-men-pants-1` |
| Client (Landing) | `/fa/login?returnTo=%2Ffa%2Flanding-campaign` |
| Home | no mismatch (both `/fa`) |

Owner: **unrelated leftover login/account working-tree files** (`storefront-account-menu.tsx` + `login-return-to.ts` using `usePathname`/`useSearchParams`). Not Appearance, ThemeToggle, surface roles, ProductCard, or media.

Not suppressed. Does not hide product media after the well-height fix. Dev “1 Issue” badge is this `href` drift. Those leftovers stay uncommitted.
