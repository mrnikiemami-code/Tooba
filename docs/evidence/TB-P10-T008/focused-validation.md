# TB-P10-T008 — Focused validation

| Check | Result |
| --- | --- |
| recovery-staleness.guard.test.mjs | 4/4 |
| theme-mode.test.ts | 3/3 |
| storefront-theme.test.ts | 4/4 |
| storefront-appearance-api.test.ts | 3/3 |
| palette-registry.test.ts | 6/6 |
| admin-appearance-settings.test.ts | 5/5 |
| test:storefront | 67/67 |
| test:critical-storefront (home/pdp/listing/category) | 16/16 |
| Host StoreAppearance (Host stopped, then rebuilt) | 12/12 |
| git diff --check | clean (CRLF warnings only) |
| full tsc | not PASS — pre-existing Admin/grid; not repaired |
| Cart Active until COMMIT / 409 on Shipping | covered by storefront-shipping + checkout tests (67/67) |
| account identity / merge | storefront-identity-api tests green |
| USER_VISUAL_ACCEPTED | NO |
| TB-P10-T009 | not created |
