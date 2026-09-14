# TB-P10-T007 — Focused validation

| Check | Result |
| --- | --- |
| recovery-staleness.guard.test.mjs | 4/4 |
| palette-registry + token-completion + appearance-api | 11/11 |
| admin-appearance-settings.test.ts | 5/5 |
| test:storefront | 67/67 |
| test:critical-storefront (home/pdp/listing/category) | 16/16 |
| Host StoreAppearance (Host recycled off :5088) | 10/10 |
| git diff --check | clean (CRLF warnings only) |
| full tsc | not PASS — pre-existing Admin/grid; not repaired |
| Cart Active until COMMIT / 409 on Shipping | covered by existing storefront-shipping + checkout tests (67/67) |
| account identity / merge | storefront-identity-api tests green |
| USER_VISUAL_ACCEPTED | NO |
| TB-P10-T008 | not created |
