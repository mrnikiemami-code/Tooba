# TB-P10-T006 — Focused validation

| Suite | Result |
| --- | --- |
| `node --test docs/ai/recovery-staleness.guard.test.mjs` | 4/4 |
| Host `FullyQualifiedName~StoreAppearance` isolated `-o .tmp-t006-test-out` | 10/10 |
| FE appearance + settings tests | 18/18 |
| `npm run test:storefront` | 67/67 |
| `npm run test:critical-storefront` | home 6 + pdp 5 + listing 4 + category 1 |
| `git diff --check` (task files) | clean |

Host coverage: default GET, valid update, invalid key 400, ThemeMode preserved, unknown persisted key fallback, isolation + cache, unauthorized source (`AdminPanelAccess`).

FE coverage: tab on existing shell, dirty/cancel/save, preview registry tokens, status colors, PUT `paletteKey` only, save failure keeps draft + error, no `type="color"`.

Storefront: SSR marker/tokens, default blue, no per-card appearance fetch.

Regression: identity/cart/checkout/shipping/pending stay in `test:storefront`.

Full `tsc` not treated as PASS — pre-existing Admin/grid errors not repaired.
