# TB-P10-T008-R1 — Focused validation

| Check | Result |
| --- | --- |
| recovery-staleness.guard.test.mjs | 4/4 (`CURRENT_TASK_ID=TB-P10-T008-R1`) |
| theme-mode.test.ts | 3/3 |
| storefront-theme.test.ts | 4/4 |
| storefront-appearance-api.test.ts | 3/3 |
| palette-registry.test.ts | 7/7 |
| admin-appearance-settings.test.ts | 5/5 |
| product-card-dark.guard.test.ts | 2/2 |
| appearance+theme+card focused set | 24/24 |
| test:storefront | 67/67 |
| test:critical-storefront | 18/18 (home 6 + pdp 5 + listing+card 6 + category 1) |
| Host StoreAppearance (Host stopped, then rebuilt) | 12/12 |
| git diff --check | clean (CRLF warnings only) |
| full tsc | not PASS — pre-existing Admin/grid; not repaired |
| USER_VISUAL_ACCEPTED | NO |
| TB-P10-T009 | not created |
